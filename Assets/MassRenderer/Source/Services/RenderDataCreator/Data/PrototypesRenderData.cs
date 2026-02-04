using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace MassRendererSystem.Data
{
    /// <summary>
    /// Describes a mesh segment within the merged prototype mesh.
    /// Used by MDI to locate vertex/index data for each prototype.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct PrototypesMeshSegment
    {
        /// <summary>
        /// Base vertex offset in the merged mesh vertex buffer.
        /// </summary>
        public int BaseVertex;

        /// <summary>
        /// Starting index in the merged mesh index buffer.
        /// </summary>
        public int StartIndex;

        /// <summary>
        /// Number of indices for this mesh segment.
        /// </summary>
        public int IndexCount;

        /// <summary>
        /// Original mesh index in the prototype array.
        /// </summary>
        public int MeshIndex;
    }

    /// <summary>
    /// Configuration data for prototype rendering.
    /// Contains texture offsets and merged mesh segment information.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct PrototypesRenderData
    {
        /// <summary>
        /// Starting texture index for each prototype in the texture array.
        /// </summary>
        [SerializeField]
        public int[] skinOffsets;

        /// <summary>
        /// Number of texture skins available for each prototype.
        /// </summary>
        [SerializeField]
        public int[] skinsForMeshCount;

        /// <summary>
        /// Segment data for locating each prototype within the merged mesh.
        /// </summary>
        [SerializeField]
        public PrototypesMeshSegment[] mergedMeshData;
    }
}

