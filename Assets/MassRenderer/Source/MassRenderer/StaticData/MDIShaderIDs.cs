using UnityEngine;

namespace MassRendererSystem.Data
{
    public static class MDIShaderIDs
    {
        public static readonly string KEYWORD_VAT_ON = "ENABLE_VAT";

        public static readonly int InstanceDataBufferID = Shader.PropertyToID("_InstanceDataBuffer");
        public static readonly int InstanceIdOffsetID = Shader.PropertyToID("_InstanceIdOffset");
        public static readonly int GlobalTransformID = Shader.PropertyToID("_GlobalTransform");
        public static readonly int TextureSkinsID = Shader.PropertyToID("_TextureSkins");

        public static readonly int PositionVATAtlasID = Shader.PropertyToID("_PositionVATAtlas");
        public static readonly int NormalVATAtlasID = Shader.PropertyToID("_NormalVATAtlas");
        public static readonly int VATClipsBufferID = Shader.PropertyToID("_VATClipsDataBuffer");

        private const string NAME_UNLIT = "MassSimulation/MDI/Unlit";
        private const string NAME_SIMPLE = "MassSimulation/MDI/Simplelit";
        private const string NAME_LIT = "MassSimulation/MDI/Lit";

        public static Shader GetShader(MassRenderShaderType quality)
        {
            return quality switch
            {
                MassRenderShaderType.Lit => Shader.Find(NAME_LIT),
                MassRenderShaderType.SimpleLit => Shader.Find(NAME_SIMPLE),
                MassRenderShaderType.Unlit => Shader.Find(NAME_UNLIT),

                _ => Shader.Find("Hidden/InternalErrorShader")
            };
        }
    }

    public enum MassRenderShaderType
    {
        Unlit,
        SimpleLit,
        Lit
    }
}
