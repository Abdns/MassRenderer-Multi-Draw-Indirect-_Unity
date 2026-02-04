using UnityEngine;

namespace MassRendererSystem.Data
{
    /// <summary>
    /// Cached shader property IDs and shader keywords for MDI rendering.
    /// Using cached IDs avoids string hashing overhead at runtime.
    /// </summary>
    public static class MDIShaderIDs
    {
        /// <summary>
        /// Shader keyword to enable VAT animation sampling.
        /// </summary>
        public static readonly string KEYWORD_VAT_ON = "ENABLE_VAT";

        /// <summary>
        /// Property ID for the per-instance data structured buffer.
        /// </summary>
        public static readonly int InstanceDataBufferID = Shader.PropertyToID("_InstanceDataBuffer");

        /// <summary>
        /// Property ID for the instance ID offset buffer (for multi-draw).
        /// </summary>
        public static readonly int InstanceIdOffsetID = Shader.PropertyToID("_InstanceIdOffset");

        /// <summary>
        /// Property ID for the global transformation matrix.
        /// </summary>
        public static readonly int GlobalTransformID = Shader.PropertyToID("_GlobalTransform");

        /// <summary>
        /// Property ID for the texture array containing all skin textures.
        /// </summary>
        public static readonly int TextureSkinsID = Shader.PropertyToID("_TextureSkins");

        /// <summary>
        /// Property ID for the VAT position atlas texture.
        /// </summary>
        public static readonly int PositionVATAtlasID = Shader.PropertyToID("_PositionVATAtlas");

        /// <summary>
        /// Property ID for the VAT normal atlas texture.
        /// </summary>
        public static readonly int NormalVATAtlasID = Shader.PropertyToID("_NormalVATAtlas");

        /// <summary>
        /// Property ID for the VAT animation clips data buffer.
        /// </summary>
        public static readonly int VATClipsBufferID = Shader.PropertyToID("_VATClipsDataBuffer");

        private const string NAME_UNLIT = "MassSimulation/MDI/Unlit";
        private const string NAME_SIMPLE = "MassSimulation/MDI/Simplelit";
        private const string NAME_LIT = "MassSimulation/MDI/Lit";

        /// <summary>
        /// Gets the appropriate shader based on the requested quality level.
        /// </summary>
        /// <param name="quality">The shader quality/complexity level.</param>
        /// <returns>The shader for the specified quality level, or error shader if not found.</returns>
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

    /// <summary>
    /// Shader quality levels for MDI rendering, ordered by complexity/performance cost.
    /// </summary>
    public enum MassRenderShaderType
    {
        /// <summary>
        /// Unlit shader - fastest, no lighting calculations.
        /// </summary>
        Unlit,

        /// <summary>
        /// Simple lit shader - basic lighting with reduced features.
        /// </summary>
        SimpleLit,

        /// <summary>
        /// Full lit shader - complete PBR lighting.
        /// </summary>
        Lit
    }
}
