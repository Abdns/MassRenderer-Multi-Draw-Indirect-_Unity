using MassRendererSystem.Utils;
using System.Collections.Generic;
using UnityEngine;
using VATBakerSystem;
using Object = UnityEngine.Object;

namespace MassRendererSystem.Data
{
    /// <summary>
    /// MonoBehaviour component for creating and saving RenderStaticData assets.
    /// Attach to a GameObject and configure prototypes in the Inspector.
    /// </summary>
    public class RenderDataCreator : MonoBehaviour
    {
        [SerializeField] private PrototypeData[] _prototypes;
        [SerializeField] private VATBakerSettings _bakerSettings;

        /// <summary>
        /// Generates render data from configured prototypes and saves it as an asset.
        /// Includes merged meshes, texture arrays, and VAT atlases as sub-assets.
        /// </summary>
        /// <param name="savePath">Folder path where the asset will be saved.</param>
        /// <returns>The generated RenderStaticData asset.</returns>
        public RenderStaticData GenerateAndSave(string savePath)
        {
            var renderData = RenderDataBuilder.BuildRenderData(_prototypes, _bakerSettings);

#if UNITY_EDITOR

            var subAssets = new List<Object>();

            if (renderData.MergedPrototypeMeshes != null)
                subAssets.Add(renderData.MergedPrototypeMeshes);

            if (renderData.TextureSkins != null)
                subAssets.Add(renderData.TextureSkins);

            if (renderData.PrototypeMeshes != null)
            {
                foreach (var mesh in renderData.PrototypeMeshes)
                {
                    if (mesh != null)
                    {
                        subAssets.Add(mesh);
                    }
                }
            }

            if (renderData.AtlasData?.PositionAtlas != null)
            {
                renderData.AtlasData.PositionAtlas.name = "VAT_Position_Atlas";
                subAssets.Add(renderData.AtlasData.PositionAtlas);
            }

            if (renderData.AtlasData?.NormalAtlas != null)
            {
                renderData.AtlasData.NormalAtlas.name = "VAT_Normal_Atlas";
                subAssets.Add(renderData.AtlasData.NormalAtlas);
            }

            AssetSaver.SaveAsset(renderData, savePath, "RenderData", subAssets);
#endif

            return renderData;
        }
    }

}