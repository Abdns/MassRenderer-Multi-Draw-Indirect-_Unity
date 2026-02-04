using System;
using System.Linq;
using UnityEngine;

namespace VATBakerSystem
{
    /// <summary>
    /// Service for baking skinned mesh animations into Vertex Animation Textures (VAT).
    /// Samples animation frames and stores vertex positions/normals in textures for GPU playback.
    /// </summary>
    public class VATBakerService
    {
        private const float TEXEL_SAMPLE_OFFSET = 0.5f;
        private readonly VATBakerSettings _defaults;

        /// <summary>
        /// Creates a new VAT baker service with the specified settings.
        /// </summary>
        /// <param name="defaults">Baker configuration settings.</param>
        /// <exception cref="ArgumentNullException">Thrown if defaults is null.</exception>
        public VATBakerService(VATBakerSettings defaults)
        {
            _defaults = defaults ?? throw new ArgumentNullException(nameof(defaults));
        }

        /// <summary>
        /// Bakes all animation clips from the request into VAT textures.
        /// Creates a temporary copy of the GameObject to sample animations without affecting the original.
        /// </summary>
        /// <param name="requestData">The bake request containing animator and skinned mesh renderer.</param>
        /// <returns>Bake result with position/normal textures, or null if baking failed.</returns>
        public VATBakeResult Bake(VATBakeRequest requestData)
        {
            if (!IsValidInput(requestData))
            {
                return null;
            }

            GameObject copyObj = null;
            Mesh bakedMesh = null;

            try
            {
                if (requestData.Animator.runtimeAnimatorController == null)
                {
                    Debug.LogWarning($"[VATBaker] Animator '{requestData.Animator.name}' missing Controller. Skipping.");
                    return null;
                }

                AnimationClip[] clips = requestData.Animator.runtimeAnimatorController.animationClips
                    .Distinct()
                    .ToArray();

                if (clips.Length == 0)
                {
                    return null;
                }

                copyObj = UnityEngine.Object.Instantiate(requestData.Animator.gameObject);
                copyObj.hideFlags = HideFlags.HideAndDontSave;
                copyObj.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                copyObj.transform.localScale = Vector3.one;

                SkinnedMeshRenderer copySMR = requestData.SkinnedMeshRenderer.FindMatchingComponentIn(copyObj, requestData.Animator.gameObject);

                if (copySMR == null)
                {
                    Debug.LogError($"[VATBaker] SMR not found in copy of {requestData.Animator.name}.");
                    return null;
                }

                bakedMesh = new Mesh();
                int vertCount = requestData.SkinnedMeshRenderer.sharedMesh.vertexCount;

                VATAnimationClip[] clipInfos = CalculateClipInfos(clips, out int totalFrames);

                Texture2D positionTex = CreateTexture($"{requestData.Animator.name}_Pos_VAT", vertCount, totalFrames);
                Texture2D normalTex = CreateTexture($"{requestData.Animator.name}_Norm_VAT", vertCount, totalFrames);

                Color[] posPixels = new Color[vertCount * totalFrames];
                Color[] normPixels = new Color[vertCount * totalFrames];

                for (int clipIndex = 0; clipIndex < clips.Length; clipIndex++)
                {
                    AnimationClip clip = clips[clipIndex];
                    VATAnimationClip info = clipInfos[clipIndex];

                    for (int frame = 0; frame < info.FrameCount; frame++)
                    {
                        float t = info.FrameCount > 1 ? (float)frame / (info.FrameCount - 1) : 0f;
                        float time = t * clip.length;

                        clip.SampleAnimation(copyObj, time);

                        copySMR.BakeMesh(bakedMesh);

                        Vector3[] verts = bakedMesh.vertices;
                        Vector3[] normals = bakedMesh.normals;

                        int globalFrameRow = info.StartFrame + frame;
                        int pixelOffset = globalFrameRow * vertCount;

                        for (int v = 0; v < verts.Length; v++)
                        {
                            Vector3 pos = verts[v];
                            posPixels[pixelOffset + v] = new Color(pos.x, pos.y, pos.z, 1f);

                            Vector3 norm = normals[v];
                            normPixels[pixelOffset + v] = new Color(norm.x, norm.y, norm.z, 1f);
                        }
                    }
                }

                positionTex.SetPixels(posPixels);
                positionTex.Apply();

                normalTex.SetPixels(normPixels);
                normalTex.Apply();

                return new VATBakeResult(positionTex, normalTex, totalFrames, vertCount, clipInfos);

            }
            finally
            {
                if (copyObj != null)
                {
                    UnityEngine.Object.DestroyImmediate(copyObj);
                }
                if (bakedMesh != null)
                {
                    UnityEngine.Object.DestroyImmediate(bakedMesh);
                }
            }
        }

        private bool IsValidInput(VATBakeRequest data)
        {
            if (data == null)
            {
                return false;
            }
                
            if (data.Animator == null)
            {
                return false;
            }
                
            if (data.SkinnedMeshRenderer == null)
            {
                return false;
            }
                
            return true;
        }

        private Texture2D CreateTexture(string name, int width, int height)
        {
            return new Texture2D(width, height, _defaults.TextureFormat, false)
            {
                filterMode = _defaults.filter,
                wrapMode = _defaults.wrap,
                name = name
            };
        }

        private VATAnimationClip[] CalculateClipInfos(AnimationClip[] clips, out int totalFrames)
        {
            totalFrames = 0;
            var infos = new VATAnimationClip[clips.Length];

            for (int i = 0; i < clips.Length; i++)
            {
                int frameCount = Mathf.CeilToInt(clips[i].length * _defaults.samplesPerSecond) + 1;
                infos[i] = new VATAnimationClip
                {
                    StartFrame = totalFrames,
                    FrameCount = frameCount,
                    Duration = clips[i].length
                };
                totalFrames += frameCount;
            }

            for (int i = 0; i < infos.Length; i++)
            {
                float firstFrameUV = (infos[i].StartFrame + TEXEL_SAMPLE_OFFSET) / totalFrames;
                float lastFrameUV = (infos[i].StartFrame + infos[i].FrameCount - TEXEL_SAMPLE_OFFSET) / totalFrames;

                infos[i].NormalizedStart = firstFrameUV;
                infos[i].NormalizedLength = lastFrameUV - firstFrameUV;
            }

            return infos;
        }
    }
}