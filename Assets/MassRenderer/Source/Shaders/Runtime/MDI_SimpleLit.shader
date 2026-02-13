Shader "MassSimulation/MDI/Simplelit" 
{
    Properties
    {
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "LightMode"  = "SRPDefaultUnlit"
        }

        Pass
        {
            HLSLPROGRAM

            #pragma target 4.5
            #pragma vertex   Vert
            #pragma fragment Frag

            #pragma shader_feature ENABLE_VAT
            #pragma shader_feature GLOBAL_TRANSFORM

            #define UNITY_INDIRECT_DRAW_ARGS IndirectDrawIndexedArgs

            // ---------------------------------------------------------------------
            // Includes
            // ---------------------------------------------------------------------

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "UnityIndirect.cginc"
            #include "Assets/MassRenderer/Source/Shaders/Common/InstanceData.hlsl"

            #ifdef ENABLE_VAT
                #include "Assets/MassRenderer/Source/Shaders/Common/VATSampling.hlsl"
            #endif

            // ---------------------------------------------------------------------
            // Per Material
            // ---------------------------------------------------------------------

            CBUFFER_START(UnityPerMaterial)
                float4x4 _GlobalTransform;
            CBUFFER_END

            // ---------------------------------------------------------------------
            // Textures
            // ---------------------------------------------------------------------

            TEXTURE2D_ARRAY(_TextureSkins);
            SAMPLER(sampler_TextureSkins);

            // ---------------------------------------------------------------------
            // Buffers
            // ---------------------------------------------------------------------

            StructuredBuffer<InstanceData> _InstanceDataBuffer;
            StructuredBuffer<int> _InstanceIdOffset;

            #ifdef ENABLE_VAT
                TEXTURE2D(_PositionVATAtlas);
                TEXTURE2D(_NormalVATAtlas);
                SAMPLER(sampler_PositionVATAtlas);
                float4 _VATAtlas_TexelSize;

                StructuredBuffer<VATClipAtlasInfo> _VATClipsDataBuffer;
            #endif

            // ---------------------------------------------------------------------
            // Structs
            // ---------------------------------------------------------------------

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
                float3 normalWS   : TEXCOORD0;
                float2 uv         : TEXCOORD1;

                nointerpolation int textureSkinIndex : TEXCOORD2;
            };

            // ---------------------------------------------------------------------
            // Vertex
            // ---------------------------------------------------------------------

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
                    VATClipAtlasInfo clipInfo = _VATClipsDataBuffer[GetAnimationIndex(instanceData)];

                    float2 vatUV = CalculateVATUV(clipInfo, localVertexID, GetAnimationSpeed(instanceData));

                    positionOS = SampleAnimPosition(_PositionVATAtlas, sampler_PositionVATAtlas, vatUV);
                    normalOS   = SampleAnimNormal(_NormalVATAtlas, sampler_PositionVATAtlas, vatUV);
                #else
                    positionOS = input.positionOS;
                    normalOS   = input.normalOS;
                #endif

                float4 transformedLocalPos = mul(_GlobalTransform, float4(positionOS, 1.0));
                float4 positionWS = mul(instanceData.objectToWorld, transformedLocalPos);

                float3 transformedLocalNorm = mul((float3x3)_GlobalTransform, normalOS);
                float3 normalWS = normalize(mul((float3x3)instanceData.objectToWorld, transformedLocalNorm));
              
                Varyings output;
                output.positionCS = TransformWorldToHClip(positionWS.xyz);
                output.normalWS = normalWS;
                output.uv = input.uv;
                output.textureSkinIndex = GetTextureSkinIndex(instanceData);

                return output;
            }

            // ---------------------------------------------------------------------
            // Fragment
            // ---------------------------------------------------------------------

            half4 Frag(Varyings input) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D_ARRAY(_TextureSkins, sampler_TextureSkins, input.uv, input.textureSkinIndex);
               
                half3 lightDir = _MainLightPosition.xyz;
                half NdotL = saturate(dot(input.normalWS, lightDir));
                color.rgb *= NdotL;

                return color;
            }

            ENDHLSL
        }

        // ---------------------------------------------------------------------
        // Pass 2: DepthOnly — enables early-z rejection to reduce overdraw
        // ---------------------------------------------------------------------
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM

            #pragma target 4.5
            #pragma vertex   DepthVert
            #pragma fragment DepthFrag

            #pragma shader_feature ENABLE_VAT

            #define UNITY_INDIRECT_DRAW_ARGS IndirectDrawIndexedArgs

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "UnityIndirect.cginc"
            #include "Assets/MassRenderer/Source/Shaders/Common/InstanceData.hlsl"

            #ifdef ENABLE_VAT
                #include "Assets/MassRenderer/Source/Shaders/Common/VATSampling.hlsl"
            #endif

            CBUFFER_START(UnityPerMaterial)
                float4x4 _GlobalTransform;
            CBUFFER_END

            StructuredBuffer<InstanceData> _InstanceDataBuffer;
            StructuredBuffer<int> _InstanceIdOffset;

            #ifdef ENABLE_VAT
                TEXTURE2D(_PositionVATAtlas);
                SAMPLER(sampler_PositionVATAtlas);
                StructuredBuffer<VATClipAtlasInfo> _VATClipsDataBuffer;
            #endif

            struct DepthAttributes
            {
                float3 positionOS : POSITION;
                float2 uv2        : TEXCOORD1;
                nointerpolation uint instanceID : SV_InstanceID;
            };

            struct DepthVaryings
            {
                float4 positionCS : SV_POSITION;
            };

            DepthVaryings DepthVert(DepthAttributes input)
            {
                InitIndirectDrawArgs(0);

                uint cmdID = GetCommandID(0);
                uint instanceID = GetIndirectInstanceID(input.instanceID);
                int  index = _InstanceIdOffset[cmdID] + instanceID;

                InstanceData instanceData = _InstanceDataBuffer[index];

                float3 positionOS;

                #ifdef ENABLE_VAT
                    uint localVertexID = (uint)input.uv2.x;
                    VATClipAtlasInfo clipInfo = _VATClipsDataBuffer[GetAnimationIndex(instanceData)];
                    float2 vatUV = CalculateVATUV(clipInfo, localVertexID, GetAnimationSpeed(instanceData));
                    positionOS = SampleAnimPosition(_PositionVATAtlas, sampler_PositionVATAtlas, vatUV);
                #else
                    positionOS = input.positionOS;
                #endif

                float4 transformedLocalPos = mul(_GlobalTransform, float4(positionOS, 1.0));
                float4 positionWS = mul(instanceData.objectToWorld, transformedLocalPos);

                DepthVaryings output;
                output.positionCS = TransformWorldToHClip(positionWS.xyz);
                return output;
            }

            half4 DepthFrag(DepthVaryings input) : SV_Target
            {
                return 0;
            }

            ENDHLSL
        }
    }
}
