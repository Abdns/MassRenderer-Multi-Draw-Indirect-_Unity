using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using VATBakerSystem;

namespace MassRendererSystem.Data
{
    /// <summary>
    /// Data structure defining a single prototype (mesh variant) for mass rendering.
    /// Contains mesh, texture skins, and optional animation baking data.
    /// </summary>
    [Serializable]
    public struct PrototypeData
    {
        [SerializeField]
        private Mesh _mesh;
        [SerializeField]
        private Texture2D[] _textures;
        [SerializeField]
        private VATBakeRequest _animationBakerData;

        /// <summary>
        /// The mesh geometry for this prototype.
        /// </summary>
        public Mesh Mesh => _mesh;

        /// <summary>
        /// Array of texture skins/variants for this prototype.
        /// </summary>
        public Texture2D[] Textures => _textures;

        /// <summary>
        /// Animation baking configuration (animator + skinned mesh).
        /// </summary>
        public VATBakeRequest AnimationBakerData => _animationBakerData;
    }

    /// <summary>
    /// Extension methods for processing arrays of PrototypeData.
    /// Provides utilities for mesh merging, texture array creation, and offset calculations.
    /// </summary>
    public static class UnitPrototypeDataExtensions
    {
        /// <summary>
        /// Extracts all non-null meshes from the prototype array.
        /// </summary>
        /// <param name="unitPrototypes">Array of prototype data.</param>
        /// <returns>List of meshes from all prototypes.</returns>
        public static List<Mesh> GetAllMeshes(this PrototypeData[] unitPrototypes)
        {
            var meshes = new List<Mesh>(unitPrototypes.Length);
            foreach (var unit in unitPrototypes)
            {
                if (unit.Mesh != null) meshes.Add(unit.Mesh);
            }
            return meshes;
        }

        /// <summary>
        /// Creates a Texture2DArray from all prototype textures.
        /// All textures must have the same dimensions and format.
        /// </summary>
        /// <param name="prototypes">Array of prototype data.</param>
        /// <returns>Combined texture array, or null if no textures found.</returns>
        /// <exception cref="InvalidOperationException">Thrown if texture sizes or formats don't match.</exception>
        public static Texture2DArray CreateTextureArray(this PrototypeData[] prototypes)
        {
            Texture2D refTex = null;
            int totalSkins = 0;

            foreach (var unit in prototypes)
            {
                if (unit.Textures != null && unit.Textures.Length > 0)
                {
                    if (refTex == null) refTex = unit.Textures[0];
                    totalSkins += unit.Textures.Length;
                }
            }

            if (refTex == null) return null;

            int width = refTex.width;
            int height = refTex.height;
            TextureFormat format = refTex.format;

            var textureArray = new Texture2DArray(width, height, totalSkins, format, true);
            textureArray.name = "Prototype_TextureArray";

            textureArray.filterMode = refTex.filterMode;
            textureArray.wrapMode = refTex.wrapMode;

            int index = 0;
            foreach (var unit in prototypes)
            {
                if (unit.Textures == null) continue;

                foreach (var tex in unit.Textures)
                {
                    if (tex.width != width || tex.height != height)
                        throw new InvalidOperationException($"Texture {tex.name} size ({tex.width}x{tex.height}) does not match array size ({width}x{height})!");

                    if (tex.format != format)
                        throw new InvalidOperationException($"Texture {tex.name} format ({tex.format}) does not match array format ({format})!");

                    for (int mip = 0; mip < tex.mipmapCount; mip++)
                    {
                        Graphics.CopyTexture(tex, 0, mip, textureArray, index, mip);
                    }
                    index++;
                }
            }

            textureArray.Apply(false, true);

            return textureArray;
        }

        /// <summary>
        /// Calculates the starting index offset for each prototype's textures in the combined array.
        /// </summary>
        /// <param name="prototypes">Array of prototype data.</param>
        /// <returns>Array of starting indices for each prototype's textures.</returns>
        public static int[] GetSkinOffsets(this PrototypeData[] prototypes)
        {
            var skinOffsets = new int[prototypes.Length];
            int currentOffset = 0;

            for (int i = 0; i < prototypes.Length; i++)
            {
                skinOffsets[i] = currentOffset;
                if (prototypes[i].Textures != null)
                {
                    currentOffset += prototypes[i].Textures.Length;
                }
            }
            return skinOffsets;
        }

        /// <summary>
        /// Gets the number of texture skins for each prototype mesh.
        /// </summary>
        /// <param name="prototypes">Array of prototype data.</param>
        /// <returns>Array of skin counts per prototype.</returns>
        public static int[] GetSkinsPerMesh(this PrototypeData[] prototypes)
        {
            var skinsPerMesh = new int[prototypes.Length];
            for (int i = 0; i < prototypes.Length; i++)
            {
                skinsPerMesh[i] = prototypes[i].Textures?.Length ?? 0;
            }
            return skinsPerMesh;
        }

        /// <summary>
        /// Merges multiple meshes into a single mesh for efficient MDI rendering.
        /// Outputs segment data for locating each original mesh within the merged mesh.
        /// UV2 channel stores vertex indices for VAT sampling.
        /// </summary>
        /// <param name="meshes">List of meshes to merge.</param>
        /// <param name="segments">Output array of segment data for each merged mesh.</param>
        /// <returns>Single merged mesh containing all input meshes.</returns>
        public static Mesh CreateMergedMesh(this List<Mesh> meshes, out PrototypesMeshSegment[] segments)
        {
            long totalVerts = 0;
            long totalIndices = 0;

            foreach (var m in meshes)
            {
                totalVerts += m.vertexCount;
                totalIndices += m.triangles.Length;
            }

            IndexFormat indexFormat = totalVerts > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;

            Vector3[] vertices = new Vector3[totalVerts];
            Vector3[] normals = new Vector3[totalVerts];
            Vector2[] uv = new Vector2[totalVerts];
            Vector2[] uv2 = new Vector2[totalVerts]; 
            int[] indices = new int[totalIndices];

            segments = new PrototypesMeshSegment[meshes.Count];

            int vOffset = 0;
            int iOffset = 0;

            for (int i = 0; i < meshes.Count; i++)
            {
                Mesh m = meshes[i];
                int vCount = m.vertexCount;
                int[] mTris = m.triangles;
                int iCount = mTris.Length;

                Array.Copy(m.vertices, 0, vertices, vOffset, vCount);
                Array.Copy(m.normals, 0, normals, vOffset, vCount);
                Array.Copy(m.uv, 0, uv, vOffset, vCount);

                for (int v = 0; v < vCount; v++)
                {
                    uv2[vOffset + v] = new Vector2(v, 0); 
                }

                Array.Copy(mTris, 0, indices, iOffset, iCount);

                segments[i] = new PrototypesMeshSegment
                {
                    BaseVertex = vOffset,
                    StartIndex = iOffset,
                    IndexCount = iCount,
                    MeshIndex = i
                };

                vOffset += vCount;
                iOffset += iCount;
            }

            Mesh merged = new Mesh();
            merged.name = "Merged_Prototypes_Mesh";
            merged.indexFormat = indexFormat; 

            merged.vertices = vertices;
            merged.normals = normals;
            merged.uv = uv;
            merged.uv2 = uv2;
            merged.triangles = indices;

            merged.RecalculateTangents();
            merged.RecalculateBounds();

            return merged;
        }
    }
}