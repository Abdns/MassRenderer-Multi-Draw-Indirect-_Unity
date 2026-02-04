using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MassRendererSystem.Utils
{
    /// <summary>
    /// Utility class for saving Unity assets with sub-assets to the AssetDatabase.
    /// Editor-only functionality.
    /// </summary>
    public static class AssetSaver
    {
        /// <summary>
        /// Saves a main asset with optional sub-assets to the specified path.
        /// Creates the folder if it doesn't exist.
        /// </summary>
        /// <param name="mainAsset">The primary asset to save.</param>
        /// <param name="folderPath">Folder path (must be inside Assets folder).</param>
        /// <param name="fileName">Name for the asset file (without extension).</param>
        /// <param name="subAssets">Optional collection of sub-assets to embed.</param>
        /// <exception cref="ArgumentNullException">Thrown if mainAsset is null.</exception>
        /// <exception cref="ArgumentException">Thrown if path is not inside Assets folder.</exception>
        public static void SaveAsset(Object mainAsset, string folderPath, string fileName, IEnumerable<Object> subAssets = null)
        {
            if (mainAsset == null)
            {
                throw new ArgumentNullException(nameof(mainAsset), "[AssetSaver] Cannot save a null asset.");
            }

            string relativePath = ConvertToRelativePath(folderPath);
            if (string.IsNullOrEmpty(relativePath))
            {
                throw new ArgumentException($"[AssetSaver] The path must be inside the 'Assets' folder. Received: {folderPath}", nameof(folderPath));
            }

            if (!AssetDatabase.IsValidFolder(relativePath))
            {
                string parent = Path.GetDirectoryName(relativePath);

                Directory.CreateDirectory(relativePath);
                AssetDatabase.ImportAsset(relativePath);
            }

            string fullPath = $"{relativePath}/{fileName}.asset";

            AssetDatabase.CreateAsset(mainAsset, fullPath);

            if (subAssets != null)
            {
                foreach (var subObj in subAssets)
                {
                    if (subObj != null)
                    {
                        if (!AssetDatabase.Contains(subObj))
                        {
                            AssetDatabase.AddObjectToAsset(subObj, mainAsset);
                        }
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.SetDirty(mainAsset);

            Debug.Log($"<color=green>Saved:</color> {fullPath}");
            EditorGUIUtility.PingObject(mainAsset);
        }

        private static string ConvertToRelativePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            path = path.Replace("\\", "/");

            if (path.StartsWith("Assets")) return path;

            string dataPath = Application.dataPath.Replace("\\", "/");
            if (path.StartsWith(dataPath))
            {
                return "Assets" + path.Substring(dataPath.Length);
            }

            return null; 
        }
    }
}