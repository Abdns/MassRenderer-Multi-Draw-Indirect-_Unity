Shader "MassSimulation/MDI/Unlit" 
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
            // Instance Data
            // ---------------------------------------------------------------------

            struct InstanceData
            {
                int    meshIndex;
                int    textureSkinIndex;
                int    animationIndex;
                float  animationSpeed;
                float4x4 localToWorldMatrix;
            };

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

                uint   instanceID : TEXCOORD2;
                uint   cmdID      : TEXCOORD3;

                float  lightDot   : TEXCOORD4;
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
                    VATClipAtlasInfo clipInfo = _VATClipsDataBuffer[instanceData.animationIndex];

                    float2 vatUV = CalculateVATUV(clipInfo, localVertexID, instanceData.animationSpeed);

                    positionOS = SampleAnimPosition(_PositionVATAtlas, sampler_PositionVATAtlas, vatUV);
                    normalOS   = SampleAnimNormal(_NormalVATAtlas, sampler_PositionVATAtlas, vatUV);
                #else
                    positionOS = input.positionOS;
                    normalOS   = input.normalOS;
                #endif

                float4 transformedLocalPos = mul(_GlobalTransform, float4(positionOS, 1.0));
                float4 positionWS = mul(instanceData.localToWorldMatrix, transformedLocalPos);

                float3 transformedLocalNorm = mul((float3x3)_GlobalTransform, normalOS);
                float3 normalWS = normalize(mul((float3x3)instanceData.localToWorldMatrix, transformedLocalNorm));
              
                Varyings output;
                output.positionCS = TransformWorldToHClip(positionWS.xyz);;
                output.normalWS = normalWS;
                output.uv = input.uv;
                output.instanceID = instanceID;
                output.cmdID = cmdID;

                return output;
            }

            // ---------------------------------------------------------------------
            // Fragment
            // ---------------------------------------------------------------------

            half4 Frag(Varyings input) : SV_Target
            {
                int index = _InstanceIdOffset[input.cmdID] + input.instanceID;
                InstanceData instanceData = _InstanceDataBuffer[index];

                half4 color = SAMPLE_TEXTURE2D_ARRAY(_TextureSkins, sampler_TextureSkins, input.uv, instanceData.textureSkinIndex);

                return color;
            }

            ENDHLSL
        }
    }
}
