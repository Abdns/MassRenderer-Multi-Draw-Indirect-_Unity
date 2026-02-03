using System.Text;
using UnityEngine;

public static class TransformExtensions
{
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

        Debug.LogWarning($"[TransformExtensions] Не удалось найти соответствие для {originalComponent.name} в {ghostRoot.name}");
        return null;
    }

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
