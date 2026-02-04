using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace MassRendererSystem.Data
{
    /// <summary>
    /// Per-instance data structure for MDI rendering.
    /// Layout must match the shader's StructuredBuffer definition.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct InstanceData
    {
        /// <summary>
        /// Index of the prototype mesh to use for this instance.
        /// </summary>
        public int MeshIndex;

        /// <summary>
        /// Index into the texture array for this instance's skin/albedo.
        /// </summary>
        public int TextureSkinIndex;

        /// <summary>
        /// Index of the VAT animation clip to play.
        /// </summary>
        public int AnimationIndex;

        /// <summary>
        /// Playback speed multiplier for the animation.
        /// </summary>
        public float AnimationSpeed;

        /// <summary>
        /// Object-to-world transformation matrix for this instance.
        /// </summary>
        public float4x4 ObjectToWorld;
    }
}

