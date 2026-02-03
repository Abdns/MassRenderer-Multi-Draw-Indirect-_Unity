using UnityEngine;

namespace MassRendererSystem.Data
{
    public struct MassRendererParams
    {
        public bool IsVATEnable { get; set; }
        public int InstanceCount { get; set; }
        public Bounds RenderBounds { get; set; }
        public MassRenderShaderType ShaderType { get; set; }

        public MassRendererParams(int instanceCount, Bounds renderBounds )
        {
            this.InstanceCount = instanceCount;
            this.RenderBounds = renderBounds;
            IsVATEnable = false;
            ShaderType = MassRenderShaderType.Unlit;
        }
    }
}   