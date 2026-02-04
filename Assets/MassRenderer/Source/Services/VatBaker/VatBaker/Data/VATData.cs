using System;
using System.Runtime.InteropServices;
using UnityEngine;
using Object = UnityEngine.Object;

namespace VATBakerSystem
{
    /// <summary>
    /// Request data for baking a skinned mesh animation into VAT textures.
    /// </summary>
    [Serializable]
    public sealed class VATBakeRequest
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private SkinnedMeshRenderer _skinnedMeshRenderer;

        /// <summary>
        /// The animator component containing animation clips to bake.
        /// </summary>
        public Animator Animator => _animator;

        /// <summary>
        /// The skinned mesh renderer to sample vertex positions from.
        /// </summary>
        public SkinnedMeshRenderer SkinnedMeshRenderer => _skinnedMeshRenderer;
    }

    /// <summary>
    /// Result of VAT baking containing position and normal textures for a single mesh.
    /// Implements IDisposable pattern for proper texture cleanup.
    /// </summary>
    public sealed class VATBakeResult : IDisposable
    {
        private Texture2D _positionMap;
        private Texture2D _normalMap;
        private int _totalFrames;
        private int _vertexCount;
        private VATAnimationClip[] _clipInfos;

        /// <summary>
        /// Texture containing baked vertex positions (RGB = XYZ).
        /// </summary>
        public Texture2D PositionMap => _positionMap;

        /// <summary>
        /// Texture containing baked vertex normals (RGB = XYZ).
        /// </summary>
        public Texture2D NormalMap => _normalMap;

        /// <summary>
        /// Total number of animation frames across all clips.
        /// </summary>
        public int TotalFrames => _totalFrames;

        /// <summary>
        /// Number of vertices in the source mesh.
        /// </summary>
        public int VertexCount => _vertexCount;

        /// <summary>
        /// Metadata for each animation clip (timing, UV coordinates).
        /// </summary>
        public VATAnimationClip[] ClipInfos => _clipInfos;

        /// <summary>
        /// Creates a new VAT bake result.
        /// </summary>
        /// <param name="positionMap">Baked vertex position texture.</param>
        /// <param name="normalMap">Baked vertex normal texture.</param>
        /// <param name="totalFrames">Total frame count.</param>
        /// <param name="vertexCount">Vertex count of source mesh.</param>
        /// <param name="clipInfos">Animation clip metadata array.</param>
        public VATBakeResult(
            Texture2D positionMap,
            Texture2D normalMap,
            int totalFrames,
            int vertexCount,
            VATAnimationClip[] clipInfos)
        {
            _positionMap = positionMap;
            _normalMap = normalMap;
            _clipInfos = clipInfos;
            _totalFrames = totalFrames;
            _vertexCount = vertexCount;
        }

        /// <summary>
        /// Releases the baked textures. Call when the result is no longer needed.
        /// </summary>
        public void Dispose()
        {
            if (PositionMap != null)
            {
                Object.DestroyImmediate(PositionMap);
                _positionMap = null;
            }
            if (NormalMap != null)
            {
                Object.DestroyImmediate(NormalMap);
                _normalMap = null;
            }
        }
    }

    /// <summary>
    /// Metadata for a single animation clip within a VAT texture.
    /// Layout must match shader struct definition.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct VATAnimationClip
    {
        /// <summary>
        /// Number of frames in this animation clip.
        /// </summary>
        public int FrameCount;

        /// <summary>
        /// Starting frame index in the VAT texture.
        /// </summary>
        public int StartFrame;

        /// <summary>
        /// Duration of the animation in seconds.
        /// </summary>
        public float Duration;

        /// <summary>
        /// Normalized V coordinate for the first frame (0-1 range).
        /// </summary>
        public float NormalizedStart;

        /// <summary>
        /// Normalized V span of the animation (0-1 range).
        /// </summary>
        public float NormalizedLength;
    }
}
