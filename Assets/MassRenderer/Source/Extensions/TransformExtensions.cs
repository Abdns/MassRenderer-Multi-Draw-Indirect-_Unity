using System.Text;
using UnityEngine;

/// <summary>
/// Extension methods for Unity Transform and Component hierarchy operations.
/// </summary>
public static class TransformExtensions
{
    /// <summary>
    /// Finds a matching component in a cloned hierarchy by traversing the same relative path.
    /// Useful for finding corresponding components in instantiated prefab copies.
    /// </summary>
    /// <typeparam name="T">The type of component to find.</typeparam>
    /// <param name="originalComponent">The original component to find a match for.</param>
    /// <param name="ghostRoot">The root of the cloned hierarchy.</param>
    /// <param name="originalRoot">The root of the original hierarchy.</param>
    /// <returns>The matching component in the cloned hierarchy, or null if not found.</returns>
    public static T FindMatchingComponentIn<T>(this T originalComponent, GameObject ghostRoot, GameObject originalRoot) where T : Component
    {
        if (originalComponent.gameObject == originalRoot)
        {
            return ghostRoot.GetComponent<T>();
        }

        string path = GetHierarchyPath(originalComponent.transform, originalRoot.transform);

        Transform foundTransform = ghostRoot.transform.Find(path);

        if (foundTransform != null)
        {
            return foundTransform.GetComponent<T>();
        }

        Debug.LogWarning($"[TransformExtensions] Not found {originalComponent.name} / {ghostRoot.name}");

        return null;
    }

    /// <summary>
    /// Gets the relative hierarchy path from a root transform to a target transform.
    /// The path uses "/" as separator, compatible with Transform.Find().
    /// </summary>
    /// <param name="target">The target transform to get the path to.</param>
    /// <param name="root">The root transform to calculate the path from.</param>
    /// <returns>A "/" separated path string, or empty string if target equals root.</returns>
    public static string GetHierarchyPath(this Transform target, Transform root)
    {
        if (target == root) return "";

        var sb = new StringBuilder();
        sb.Append(target.name);

        Transform current = target.parent;
        while (current != null && current != root)
        {
            sb.Insert(0, "/");
            sb.Insert(0, current.name);

            current = current.parent;
        }

        return sb.ToString();
    }
}
