using System.Collections.Generic;
using UnityEngine;
using VATBakerSystem;

namespace MassRendererSystem.Data
{
    /// <summary>
    /// ScriptableObject containing all static data required for MassRenderer.
    /// Includes merged meshes, texture arrays, VAT atlases, and configuration data.
    /// Create via Assets > Create > Mass Renderer > Static Data.
    /// </summary>
    [CreateAssetMenu(fileName = "NewRenderStaticData", menuName = "Mass Renderer/Static Data")]
    public class RenderStaticData : ScriptableObject
    {
        [Header("Meshes")]
        [SerializeField] private List<Mesh> _prototypeMeshes;
        [SerializeField] private Mesh _mergedPrototypeMeshes;

        [Header("Textures")]
        [SerializeField] private Texture2DArray _textureSkins;

        [Header("Atlas Data")]
        [SerializeField] private VATAtlasData _atlasData;

        [Header("Configuration")]
        [SerializeField] private PrototypesRenderData _prototypesData;

        /// <summary>
        /// List of individual prototype meshes before merging.
        /// </summary>
        public IReadOnlyList<Mesh> PrototypeMeshes => _prototypeMeshes;

        /// <summary>
        /// Single merged mesh containing all prototypes for MDI rendering.
        /// </summary>
        public Mesh MergedPrototypeMeshes => _mergedPrototypeMeshes;

        /// <summary>
        /// Texture array containing all skin textures for all prototypes.
        /// </summary>
        public Texture2DArray TextureSkins => _textureSkins;

        /// <summary>
        /// VAT atlas data containing baked animation textures and clip metadata.
        /// </summary>
        public VATAtlasData AtlasData => _atlasData;

        /// <summary>
        /// Configuration data for prototype rendering (offsets, segment info).
        /// </summary>
        public PrototypesRenderData PrototypesData => _prototypesData;

        /// <summary>
        /// Initializes the render data with all required components.
        /// Called by RenderDataBuilder during asset creation.
        /// </summary>
        /// <param name="prototypeMeshes">List of individual prototype meshes.</param>
        /// <param name="mergedMesh">Combined mesh for MDI rendering.</param>
        /// <param name="textureSkins">Texture array with all skins.</param>
        /// <param name="atlasData">VAT atlas data.</param>
        /// <param name="meshConfig">Prototype configuration data.</param>
        public void Initialize(List<Mesh> prototypeMeshes, Mesh mergedMesh, Texture2DArray textureSkins, VATAtlasData atlasData, PrototypesRenderData meshConfig)
        {
            _prototypeMeshes = prototypeMeshes;
            _mergedPrototypeMeshes = mergedMesh;
            _textureSkins = textureSkins;
            _atlasData = atlasData;
            _prototypesData = meshConfig;
        }

        /// <summary>
        /// Gets the valid texture index range for a specific mesh prototype.
        /// </summary>
        /// <param name="meshIndex">Index of the prototype mesh.</param>
        /// <returns>Tuple with start (inclusive) and end (exclusive) indices.</returns>
        public (int start, int end) GetSkinTextureIndexRange(int meshIndex)
        {
            if (!IsValidIndex(meshIndex)) return (0, 0);

            int skinCount = _prototypesData.skinsForMeshCount[meshIndex];
            int offset = _prototypesData.skinOffsets[meshIndex];

            return (offset, offset + skinCount);
        }

        /// <summary>
        /// Gets the valid animation clip index range for a specific mesh prototype.
        /// </summary>
        /// <param name="meshIndex">Index of the prototype mesh.</param>
        /// <returns>Tuple with start (inclusive) and end (exclusive) indices into allClips array.</returns>
        public (int start, int end) GetAnimationIndexRange(int meshIndex)
        {
            if (_atlasData == null || _atlasData.vatAtlasSegs == null || !IsValidIndex(meshIndex))
            {
                return (0, 0);
            }

            VATAtlasSegmentsInfo segment = _atlasData.vatAtlasSegs[meshIndex];

            return (segment.ClipsStartIndex, segment.ClipsStartIndex + segment.ClipCount);
        }

        private bool IsValidIndex(int index)
        {
            if (_prototypesData.skinsForMeshCount == null)
            {
                return false;
            }

            return index >= 0 && index < _prototypesData.skinsForMeshCount.Length;
        }
    }
}