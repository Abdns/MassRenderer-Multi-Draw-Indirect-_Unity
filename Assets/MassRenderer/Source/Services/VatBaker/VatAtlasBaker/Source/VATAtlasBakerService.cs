using System;
using System.Collections.Generic;
using UnityEngine;

namespace VATBakerSystem
{
    /// <summary>
    /// Service for packing multiple VAT bake results into a single texture atlas.
    /// Combines position and normal textures from multiple meshes into unified atlases.
    /// </summary>
    public class VATAtlasBakerService
    {
        private const string ATLAS_NAME = "VAT_Atlas";
        private readonly VATBakerSettings _defaults;

        /// <summary>
        /// Creates a new VAT atlas baker service.
        /// </summary>
        /// <param name="defaults">Baker configuration settings.</param>
        /// <exception cref="ArgumentNullException">Thrown if defaults is null.</exception>
        public VATAtlasBakerService(VATBakerSettings defaults)
        {
            _defaults = defaults ?? throw new ArgumentNullException(nameof(defaults));
        }

        /// <summary>
        /// Packs multiple VAT bake results into a single atlas.
        /// Textures are arranged horizontally, with normalized UV coordinates calculated for each segment.
        /// </summary>
        /// <param name="bakeResults">Array of individual mesh VAT bake results.</param>
        /// <returns>Combined atlas data with packed textures and segment metadata.</returns>
        /// <exception cref="InvalidOperationException">Thrown if atlas size exceeds GPU limits.</exception>
        public VATAtlasData BakeAtlas(VATBakeResult[] bakeResults)
        {
            CalculateAtlasSize(bakeResults, out int atlasWidth, out int atlasHeight);

            int maxTextureSize = SystemInfo.maxTextureSize;
            if (atlasWidth > maxTextureSize || atlasHeight > maxTextureSize)
            {
                throw new InvalidOperationException($"Generated atlas size ({atlasWidth}x{atlasHeight}) exceeds the GPU limit of {maxTextureSize}px.");
            }

            if (atlasWidth == 0 || atlasHeight == 0)
            {
                return BakeAllVATSets(bakeResults, null, null, 0, 0);
            }

            var posAtlas = CreateAtlasTexture(atlasWidth, atlasHeight);
            var normAtlas = CreateAtlasTexture(atlasWidth, atlasHeight);

            return BakeAllVATSets(bakeResults, posAtlas, normAtlas, atlasWidth, atlasHeight);
        }

        private void CalculateAtlasSize(VATBakeResult[] results, out int width, out int height)
        {
            width = 0;
            height = 0;

            foreach (var result in results)
            {
                if (result == null || result.PositionMap == null) continue;

                width += result.PositionMap.width;
                height = Mathf.Max(height, result.PositionMap.height);
            }
        }

        private Texture2D CreateAtlasTexture(int width, int height)
        {
            return new Texture2D(width, height, TextureFormat.RGBAFloat, false)
            {
                filterMode = _defaults.filter,
                wrapMode = _defaults.wrap,
                anisoLevel = 0,
                name = ATLAS_NAME
            };
        }

        private VATAtlasData BakeAllVATSets(VATBakeResult[] bakeResults, Texture2D posAtlas, Texture2D normAtlas, int atlasWidth, int atlasHeight)
        {
            var vatList = new List<VATAtlasSegmentsInfo>(bakeResults.Length);
            var clipList = new List<VATAtlasAnimationClip>();

            Color[] posAtlasPixels = (posAtlas != null) ? new Color[atlasWidth * atlasHeight] : null;
            Color[] normAtlasPixels = (normAtlas != null) ? new Color[atlasWidth * atlasHeight] : null;

            int currentX = 0;

            foreach (var vatData in bakeResults)
            {
                if (vatData == null)
                {
                    vatList.Add(default(VATAtlasSegmentsInfo));
                    continue;
                }

                if (vatData.PositionMap == null)
                {
                    vatList.Add(default(VATAtlasSegmentsInfo));
                    continue;
                }

                if (posAtlasPixels == null) continue;

                Texture2D posTex = vatData.PositionMap;
                Texture2D normTex = vatData.NormalMap;

                int yOffset = atlasHeight - posTex.height;

                CopyPixelsToBuffer(posTex, posAtlasPixels, atlasWidth, currentX, yOffset);
                CopyPixelsToBuffer(normTex, normAtlasPixels, atlasWidth, currentX, yOffset);

                float normalizedOffsetX = (float)currentX / atlasWidth;
                float normalizedWidth = (float)posTex.width / atlasWidth;
                float yScale = (float)posTex.height / atlasHeight;
                float normalizedYOffset = (float)yOffset / atlasHeight;

                vatList.Add(new VATAtlasSegmentsInfo
                {
                    NormalizedOffsetX = normalizedOffsetX,
                    NormalizedWidth = normalizedWidth,
                    VertexCount = vatData.VertexCount,
                    AnimationsFramesCount = vatData.TotalFrames,
                    ClipsStartIndex = clipList.Count,
                    ClipCount = vatData.ClipInfos.Length,
                });

                foreach (var clip in vatData.ClipInfos)
                {
                    clipList.Add(new VATAtlasAnimationClip
                    {
                        NormalizedOffsetX = normalizedOffsetX,
                        NormalizedWidth = normalizedWidth,
                        VertexCount = vatData.VertexCount,

                        NormalizedOffsetY = normalizedYOffset + (clip.NormalizedStart * yScale),
                        NormalizedLength = clip.NormalizedLength * yScale,

                        Duration = clip.Duration,
                        FrameCount = clip.FrameCount
                    });
                }

                currentX += posTex.width;
            }

            if (posAtlas != null && posAtlasPixels != null)
            {
                posAtlas.SetPixels(posAtlasPixels);
                posAtlas.Apply();
            }

            if (normAtlas != null && normAtlasPixels != null)
            {
                normAtlas.SetPixels(normAtlasPixels);
                normAtlas.Apply();
            }

            return new VATAtlasData
            {
                PositionAtlas = posAtlas,
                NormalAtlas = normAtlas,
                vatAtlasSegs = vatList.ToArray(),
                allClips = clipList.ToArray(),
                AtlasWidth = atlasWidth,
                AtlasHeight = atlasHeight
            };
        }

        private void CopyPixelsToBuffer(Texture2D source, Color[] targetBuffer, int atlasWidth, int startX, int yOffset)
        {
            Color[] srcPixels = source.GetPixels();
            int srcWidth = source.width;
            int srcHeight = source.height;

            for (int y = 0; y < srcHeight; y++)
            {
                int srcRowIndex = y * srcWidth;
                int dstRowIndex = ((y + yOffset) * atlasWidth) + startX;

                Array.Copy(srcPixels, srcRowIndex, targetBuffer, dstRowIndex, srcWidth);
            }
        }
    }
}