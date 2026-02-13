using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace MassRendererSystem.Data
{
    /// <summary>
    /// Per-instance data structure for MDI rendering.
    /// Layout must match the shader's StructuredBuffer definition.
    /// Packed format: MeshIndex+TextureSkinIndex in one uint, AnimationIndex+AnimationSpeed(half) in one uint.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct InstanceData
    {
        /// <summary>
        /// Packed: lower 16 bits = MeshIndex, upper 16 bits = TextureSkinIndex.
        /// </summary>
        public uint PackedMeshSkin;

        /// <summary>
        /// Packed: lower 16 bits = AnimationIndex, upper 16 bits = AnimationSpeed as half-float.
        /// </summary>
        public uint PackedAnimData;

        /// <summary>
        /// Object-to-world transformation matrix for this instance.
        /// </summary>
        public float4x4 ObjectToWorld;

        /// <summary>
        /// Gets or sets the prototype mesh index (lower 16 bits of PackedMeshSkin).
        /// </summary>
        public int MeshIndex
        {
            get => (int)(PackedMeshSkin & 0xFFFF);
            set => PackedMeshSkin = (PackedMeshSkin & 0xFFFF0000) | ((uint)value & 0xFFFF);
        }

        /// <summary>
        /// Gets or sets the texture skin index (upper 16 bits of PackedMeshSkin).
        /// </summary>
        public int TextureSkinIndex
        {
            get => (int)((PackedMeshSkin >> 16) & 0xFFFF);
            set => PackedMeshSkin = (PackedMeshSkin & 0x0000FFFF) | (((uint)value & 0xFFFF) << 16);
        }

        /// <summary>
        /// Gets or sets the animation clip index (lower 16 bits of PackedAnimData).
        /// </summary>
        public int AnimationIndex
        {
            get => (int)(PackedAnimData & 0xFFFF);
            set => PackedAnimData = (PackedAnimData & 0xFFFF0000) | ((uint)value & 0xFFFF);
        }

        /// <summary>
        /// Gets or sets the animation speed (stored as half-float in upper 16 bits of PackedAnimData).
        /// </summary>
        public float AnimationSpeed
        {
            get => math.f16tof32(PackedAnimData >> 16);
            set => PackedAnimData = (PackedAnimData & 0x0000FFFF) | (math.f32tof16(value) << 16);
        }

        /// <summary>
        /// Sets mesh index and skin index in one call to avoid double bit manipulation.
        /// </summary>
        public void SetMeshAndSkin(int meshIndex, int skinIndex)
        {
            PackedMeshSkin = ((uint)meshIndex & 0xFFFF) | (((uint)skinIndex & 0xFFFF) << 16);
        }

        /// <summary>
        /// Sets animation index and speed in one call to avoid double bit manipulation.
        /// </summary>
        public void SetAnimation(int animIndex, float speed)
        {
            PackedAnimData = ((uint)animIndex & 0xFFFF) | (math.f32tof16(speed) << 16);
        }
    }
}

