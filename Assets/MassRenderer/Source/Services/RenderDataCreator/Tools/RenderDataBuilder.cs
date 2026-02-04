using System;
using UnityEditor;
using UnityEngine;
using VATBakerSystem;

namespace MassRendererSystem.Data
{
    /// <summary>
    /// Static utility class for building RenderStaticData from prototype definitions.
    /// Handles mesh merging, texture array creation, and VAT atlas baking.
    /// </summary>
    public static class RenderDataBuilder
    {
        /// <summary>
        /// Builds complete render data from prototype definitions.
        /// Processes meshes, textures, and animations into GPU-ready format.
        /// </summary>
        /// <param name="prototypes">Array of prototype definitions.</param>
        /// <param name="bakerSettings">VAT baking configuration.</param>
        /// <returns>Complete RenderStaticData ready for MassRenderer.</returns>
        /// <exception cref="ArgumentException">Thrown if prototypes array is null or empty.</exception>
        /// <exception cref="ArgumentNullException">Thrown if bakerSettings is null.</exception>
        public static RenderStaticData BuildRenderData(PrototypeData[] prototypes, VATBakerSettings bakerSettings)
        {
            if (prototypes == null || prototypes.Length == 0)
                throw new ArgumentException("Prototype list is empty or null.", nameof(prototypes));

            if (bakerSettings == null)
                throw new ArgumentNullException(nameof(bakerSettings));

            try
            {
                EditorUtility.DisplayProgressBar("Render Data Builder", "Baking VAT Atlases...", 0.1f);

                var atlasData = BuildVatAtlasSet(prototypes, bakerSettings);

                EditorUtility.DisplayProgressBar("Render Data Builder", "Processing Meshes & Textures...", 0.6f);

                var textureSkins = prototypes.CreateTextureArray();

                var prototypeMeshes = prototypes.GetAllMeshes();
                var mergedMesh = prototypeMeshes.CreateMergedMesh(out var meshesData);

                if (mergedMesh.vertexCount > 65535)
                {
                    mergedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                }

                var prototypesRenderData = new PrototypesRenderData
                {
                    skinOffsets = prototypes.GetSkinOffsets(),
                    skinsForMeshCount = prototypes.GetSkinsPerMesh(),
                    mergedMeshData = meshesData,
                };

                var asset = ScriptableObject.CreateInstance<RenderStaticData>();
                asset.Initialize(prototypeMeshes, mergedMesh, textureSkins, atlasData, prototypesRenderData);

                return asset;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RenderDataBuilder] Failed to build RenderData: {ex.ToString()}");
                throw;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }


        private static VATAtlasData BuildVatAtlasSet(PrototypeData[] prototypes, VATBakerSettings bakerSettings)
        {
            var vatBaker = new VATBakerService(bakerSettings);
            var vatAtlasBaker = new VATAtlasBakerService(bakerSettings);

            var results = new VATBakeResult[prototypes.Length];
            try
            {
                for (int i = 0; i < prototypes.Length; i++)
                {
                    float progress = (float)i / prototypes.Length;
                    var meshName = prototypes[i].Mesh != null ? prototypes[i].Mesh.name : "Unknown";
                    EditorUtility.DisplayProgressBar("Baking VAT", $"Processing {meshName}...", progress);

                    results[i] = vatBaker.Bake(prototypes[i].AnimationBakerData);   
                }

                EditorUtility.DisplayProgressBar("Baking VAT", "Packing Atlas...", 1f);

                return vatAtlasBaker.BakeAtlas(results);
            }
            finally
            {
                if (results != null)
                {
                    foreach (var res in results) res?.Dispose();
                }
            }


        }
    }
}