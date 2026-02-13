using UnityEngine;

namespace MassRendererSystem.Data
{
    /// <summary>
    /// Configuration parameters for MassRenderer initialization.
    /// Defines rendering settings including VAT support, instance count, shader type,
    /// and GPU frustum culling options.
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
        /// Enables GPU-based per-instance frustum culling via compute shader.
        /// When enabled, only instances visible to the camera are rendered.
        /// </summary>
        public bool IsFrustumCullingEnabled { get; set; }

        /// <summary>
        /// Compute shader used for GPU frustum culling.
        /// Required when <see cref="IsFrustumCullingEnabled"/> is true.
        /// Assign the FrustumCulling.compute asset.
        /// </summary>
        public ComputeShader FrustumCullingShader { get; set; }

        /// <summary>
        /// Bounding sphere radius for individual instances in object space.
        /// Used by the frustum culling compute shader to determine visibility.
        /// Should be large enough to encompass the instance mesh.
        /// Default: 1.0
        /// </summary>
        public float BoundingSphereRadius { get; set; }

        /// <summary>
        /// Maximum distance from the camera at which instances are rendered.
        /// Instances beyond this distance are culled by the compute shader.
        /// Set to 0 to disable distance culling. Default: 0 (disabled).
        /// </summary>
        public float MaxRenderDistance { get; set; }

        /// <summary>
        /// Camera used for frustum culling plane extraction.
        /// If null, Camera.main will be used at runtime.
        /// </summary>
        public Camera CullingCamera { get; set; }

        /// <summary>
        /// Creates a new MassRendererParams with default settings (VAT disabled, Unlit shader, frustum culling disabled).
        /// </summary>
        /// <param name="instanceCount">Total number of instances to render.</param>
        /// <param name="renderBounds">World-space bounds for frustum culling.</param>
        public MassRendererParams(int instanceCount, Bounds renderBounds)
        {
            InstanceCount = instanceCount;
            RenderBounds = renderBounds;
            IsVATEnable = false;
            ShaderType = MassRenderShaderType.Unlit;
            IsFrustumCullingEnabled = false;
            FrustumCullingShader = null;
            BoundingSphereRadius = 1f;
            MaxRenderDistance = 0f;
            CullingCamera = null;
        }

        /// <summary>
        /// Creates a new MassRendererParams with full configuration.
        /// </summary>
        /// <param name="instanceCount">Total number of instances to render.</param>
        /// <param name="renderBounds">World-space bounds for frustum culling.</param>
        /// <param name="isVATEnable">Whether to enable VAT animation support.</param>
        /// <param name="shaderType">Shader type for rendering.</param>
        public MassRendererParams(int instanceCount, Bounds renderBounds, bool isVATEnable, MassRenderShaderType shaderType)
        {
            InstanceCount = instanceCount;
            RenderBounds = renderBounds;
            IsVATEnable = isVATEnable;
            ShaderType = shaderType;
            IsFrustumCullingEnabled = false;
            FrustumCullingShader = null;
            BoundingSphereRadius = 1f;
            MaxRenderDistance = 0f;
            CullingCamera = null;
        }
    }
}
