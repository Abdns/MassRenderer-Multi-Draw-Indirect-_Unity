using UnityEngine;

namespace MassRendererSystem.Data
{
    /// <summary>
    /// Configuration parameters for MassRenderer initialization.
    /// </summary>
    public struct MassRendererParams
    {
        /// <summary>
        /// Enables Vertex Animation Texture (VAT).
        /// </summary>
        public bool IsVATEnable { get; set; }

        /// <summary>
        /// Total number of instances to render.
        /// </summary>
        public int InstanceCount { get; set; }

        /// <summary>
        /// World-space bounds for frustum culling.
        /// </summary>
        public Bounds RenderBounds { get; set; }

        /// <summary>
        /// Shader type to use for rendering.
        /// </summary>
        public MassRenderShaderType ShaderType { get; set; }

        /// <summary>
        /// Creates a new MassRendererParams with default settings (VAT disabled, Unlit shader).
        /// </summary>
        /// <param name="instanceCount">Total number of instances to render.</param>
        /// <param name="renderBounds">World-space bounds for frustum culling.</param>
        public MassRendererParams(int instanceCount, Bounds renderBounds)
        {
            this.InstanceCount = instanceCount;
            this.RenderBounds = renderBounds;
            IsVATEnable = false;
            ShaderType = MassRenderShaderType.Unlit;
        }
    }
}