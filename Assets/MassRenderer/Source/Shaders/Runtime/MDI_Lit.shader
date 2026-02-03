Shader "MassSimulation/MDI/Lit" 
{
    Properties
    {       
        [MainColor] _BaseColor("Base Color", Color) = (1,1,1,1)
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        // ---------------------------------------------------------------------
        // Pass 1: Universal Forward (Lighting)
        // ---------------------------------------------------------------------

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM

            #pragma target 4.5
            #pragma vertex   Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION

            #pragma shader_feature ENABLE_VAT
            #pragma shader_feature GLOBAL_TRANSFORM

            #define UNITY_INDIRECT_DRAW_ARGS IndirectDrawIndexedArgs

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "UnityIndirect.cginc"

            #ifdef ENABLE_VAT
                #include "Assets/MassRenderer/Source/Shaders/Common/VATSampling.hlsl"
            #endif

            CBUFFER_START(UnityPerMaterial)
                float4x4 _GlobalTransform;
                half4 _BaseColor;
                half _Smoothness;
                half _Metallic;
            CBUFFER_END

            TEXTURE2D_ARRAY(_TextureSkins);
            SAMPLER(sampler_TextureSkins);

            struct InstanceData
            {
                int      meshIndex;
                int      textureSkinIndex;
                int      animationIndex;
                float    animationSpeed;
                float4x4 objectToWorld;
            };

            StructuredBuffer<InstanceData> _InstanceDataBuffer;
            StructuredBuffer<int> _InstanceIdOffset;

            #ifdef ENABLE_VAT
                TEXTURE2D(_PositionVATAtlas);
                TEXTURE2D(_NormalVATAtlas);
                SAMPLER(sampler_PositionVATAtlas);
                
                StructuredBuffer<VATClipAtlasInfo> _VATClipsDataBuffer;
            #endif

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                float2 uv2        : TEXCOORD1;

                nointerpolation uint instanceID : SV_InstanceID;
                nointerpolation uint vertexID   : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float2 uv         : TEXCOORD2;
                float  fogFactor  : TEXCOORD3;
                
                uint   instanceID : TEXCOORD4;
                uint   cmdID      : TEXCOORD5;
            };

            Varyings Vert(Attributes input)
            {
                InitIndirectDrawArgs(0);

                uint cmdID = GetCommandID(0);
                uint instanceID = GetIndirectInstanceID(input.instanceID);
                int  index = _InstanceIdOffset[cmdID] + instanceID;

                InstanceData instanceData = _InstanceDataBuffer[index];

                float3 positionOS;
                float3 normalOS;

                #ifdef ENABLE_VAT
                    uint localVertexID = (uint)input.uv2.x;
                    VATClipAtlasInfo clipInfo = _VATClipsDataBuffer[instanceData.animationIndex];

                    float2 vatUV = CalculateVATUV(clipInfo, localVertexID, instanceData.animationSpeed);
                    
                    positionOS = SampleAnimPosition(_PositionVATAtlas, sampler_PositionVATAtlas, vatUV);
                    normalOS   = SampleAnimNormal(_NormalVATAtlas, sampler_PositionVATAtlas, vatUV);
                #else
                    positionOS = input.positionOS;
                    normalOS   = input.normalOS;
                #endif

                float4 positionPreInstance = mul(_GlobalTransform, float4(positionOS, 1.0));
                float3 normalPreInstance   = mul((float3x3)_GlobalTransform, normalOS);

                float4 positionWS = mul(instanceData.objectToWorld, positionPreInstance);            
                float3 normalWS   = normalize(mul((float3x3)instanceData.objectToWorld, normalPreInstance));

                Varyings output;
                output.positionWS = positionWS.xyz;
                output.positionCS = TransformWorldToHClip(positionWS.xyz);
                output.normalWS = normalWS;
                output.uv = input.uv;
                
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                
                output.instanceID = instanceID;
                output.cmdID = cmdID;

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                int index = _InstanceIdOffset[input.cmdID] + input.instanceID;
                InstanceData instanceData = _InstanceDataBuffer[index];

                half4 albedo = SAMPLE_TEXTURE2D_ARRAY(_TextureSkins, sampler_TextureSkins, input.uv, instanceData.textureSkinIndex) * _BaseColor;

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalize(input.normalWS); 
                inputData.viewDirectionWS = GetWorldSpaceViewDir(input.positionWS);
                
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                inputData.shadowCoord = shadowCoord;
                inputData.fogCoord = input.fogFactor; 
                inputData.bakedGI = SampleSH(input.normalWS); 
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = albedo.rgb;
                surfaceData.alpha = albedo.a;
                surfaceData.metallic = _Metallic;
                surfaceData.smoothness = _Smoothness;
                surfaceData.normalTS = float3(0, 0, 1);
                surfaceData.occlusion = 1.0;
                surfaceData.emission = 0.0;

                return UniversalFragmentPBR(inputData, surfaceData);
            }

            ENDHLSL
        }

        // ---------------------------------------------------------------------
        // Pass 2: Shadow Caster
        // ---------------------------------------------------------------------
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma shader_feature ENABLE_VAT
            #pragma shader_feature GLOBAL_TRANSFORM
            
            #pragma multi_compile_shadowcaster

            #define UNITY_INDIRECT_DRAW_ARGS IndirectDrawIndexedArgs

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl" 
            #include "UnityIndirect.cginc"

            #ifdef ENABLE_VAT
                #include "Assets/MassRenderer/Source/Shaders/Common/VATSampling.hlsl"
            #endif

            CBUFFER_START(UnityPerMaterial)
                float4x4 _GlobalTransform;
            CBUFFER_END

            struct InstanceData
            {
                int      meshIndex;
                int      textureSkinIndex;
                int      animationIndex;
                float    animationSpeed;
                float4x4 localToWorldMatrix;
            };

            StructuredBuffer<InstanceData> _InstanceDataBuffer;
            StructuredBuffer<int> _InstanceIdOffset;

            #ifdef ENABLE_VAT
                TEXTURE2D(_PositionVATAtlas);
                TEXTURE2D(_NormalVATAtlas);
                SAMPLER(sampler_PositionVATAtlas);
                
                StructuredBuffer<VATClipAtlasInfo> _VATClipsDataBuffer;
            #endif

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv2        : TEXCOORD1; 
                nointerpolation uint instanceID : SV_InstanceID;
                nointerpolation uint vertexID   : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings Vert(Attributes input)
            {
                InitIndirectDrawArgs(0);

                uint cmdID = GetCommandID(0);
                uint instanceID = GetIndirectInstanceID(input.instanceID);
                int  index = _InstanceIdOffset[cmdID] + instanceID;
                InstanceData instanceData = _InstanceDataBuffer[index];

                float3 positionOS;
                float3 normalOS;

                #ifdef ENABLE_VAT
                    uint localVertexID = (uint)input.uv2.x;
                    VATClipAtlasInfo clipInfo = _VATClipsDataBuffer[instanceData.animationIndex];
                    
                    float2 vatUV = CalculateVATUV(clipInfo, localVertexID, instanceData.animationSpeed);
                    
                    positionOS = SampleAnimPosition(_PositionVATAtlas, sampler_PositionVATAtlas, vatUV);
                    normalOS   = SampleAnimNormal(_NormalVATAtlas, sampler_PositionVATAtlas, vatUV);
                #else
                    positionOS = input.positionOS;
                    normalOS   = input.normalOS;
                #endif
                
                float4 positionPreInstance = mul(_GlobalTransform, float4(positionOS, 1.0));
                float3 normalPreInstance   = mul((float3x3)_GlobalTransform, normalOS);
                
                float4 positionWS = mul(instanceData.localToWorldMatrix, positionPreInstance);
                float3 normalWS   = normalize(mul((float3x3)instanceData.localToWorldMatrix, normalPreInstance));

                Varyings output;

                float3 lightDirection = _LightDirection;
                
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS.xyz, normalWS, lightDirection));

                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif

                output.positionCS = positionCS;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                return 0;
            }

            ENDHLSL
        }
    }
}