using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace MassRendererSystem.Data
{
    [StructLayout(LayoutKind.Sequential)]
    public struct InstanceData
    {
        public int MeshIndex;
        public int TextureSkinIndex;
        public int AnimationIndex;
        public float AnimationSpeed;
        public float4x4 ObjectToWorld;
    }
}

