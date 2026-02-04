using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace VATBakerSystem
{
    /// <summary>
    /// Combined VAT atlas data containing packed textures for multiple meshes.
    /// All mesh animations are packed into single position and normal atlas textures.
    /// </summary>
    [Serializable]
    public sealed class VATAtlasData
    {
        /// <summary>
        /// Atlas texture containing all baked vertex positions.
        /// </summary>
        public Texture2D PositionAtlas;

        /// <summary>
        /// Atlas texture containing all baked vertex normals.
        /// </summary>
        public Texture2D NormalAtlas;

        /// <summary>
        /// Width of the atlas textures in pixels.
        /// </summary>
        public int AtlasWidth;

        /// <summary>
        /// Height of the atlas textures in pixels.
        /// </summary>
        public int AtlasHeight;

        /// <summary>
        /// Per-mesh segment information for atlas UV lookups.
        /// </summary>
        public VATAtlasSegmentsInfo[] vatAtlasSegs;

        /// <summary>
        /// Flattened array of all animation clips across all meshes.
        /// </summary>
        public VATAtlasAnimationClip[] allClips;
    }

    /// <summary>
    /// Describes a mesh's region within the VAT atlas.
    /// Used to locate animation data for a specific prototype mesh.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct VATAtlasSegmentsInfo
    {
        /// <summary>
        /// Normalized X offset (0-1) of this mesh's data in the atlas.
        /// </summary>
        public float NormalizedOffsetX;

        /// <summary>
        /// Normalized width (0-1) of this mesh's data in the atlas.
        /// </summary>
        public float NormalizedWidth;

        /// <summary>
        /// Number of vertices in this mesh.
        /// </summary>
        public int VertexCount;

        /// <summary>
        /// Total animation frames for this mesh.
        /// </summary>
        public int AnimationsFramesCount;

        /// <summary>
        /// Starting index in the allClips array for this mesh's clips.
        /// </summary>
        public int ClipsStartIndex;

        /// <summary>
        /// Number of animation clips for this mesh.
        /// </summary>
        public int ClipCount;
    }

    /// <summary>
    /// Animation clip data with atlas UV coordinates for GPU sampling.
    /// Layout must match the shader's StructuredBuffer definition.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct VATAtlasAnimationClip
    {
        /// <summary>
        /// Number of vertices (texture width for this clip).
        /// </summary>
        public int VertexCount;

        /// <summary>
        /// Number of frames in this animation.
        /// </summary>
        public int FrameCount;

        /// <summary>
        /// Duration of the animation in seconds.
        /// </summary>
        public float Duration;

        /// <summary>
        /// Normalized X offset in the atlas (0-1).
        /// </summary>
        public float NormalizedOffsetX;

        /// <summary>
        /// Normalized Y offset in the atlas (0-1).
        /// </summary>
        public float NormalizedOffsetY;

        /// <summary>
        /// Normalized width in the atlas (0-1).
        /// </summary>
        public float NormalizedWidth;

        /// <summary>
        /// Normalized height/length in the atlas (0-1).
        /// </summary>
        public float NormalizedLength;
    }
}