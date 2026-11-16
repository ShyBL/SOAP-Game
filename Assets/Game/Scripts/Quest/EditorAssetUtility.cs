using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class EditorAssetUtility
{
    public static void EnsureFolder(string folder)
    {
        if (string.IsNullOrEmpty(folder) || AssetDatabase.IsValidFolder(folder)) return;

        string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
        string leaf = Path.GetFileName(folder);

        if (string.IsNullOrEmpty(parent)) return;
        if (!AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent, leaf);
    }

    /// <summary>Saves a new ScriptableObject under folder with a unique, filename-safe name.</summary>
    public static T CreateAsset<T>(T asset, string folder, string baseName) where T : ScriptableObject
    {
        EnsureFolder(folder);

        string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{Sanitize(baseName)}.asset");
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        return asset;
    }

    public static List<T> FindAssets<T>() where T : UnityEngine.Object
    {
        var results = new List<T>();

        foreach (string guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}"))
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
            if (asset != null) results.Add(asset);
        }

        return results;
    }

    public static string Sanitize(string value)
    {
        if (string.IsNullOrEmpty(value)) return "Unnamed";

        var builder = new StringBuilder(value.Length);
        foreach (char c in value)
            builder.Append(char.IsLetterOrDigit(c) || c == '_' || c == '-' ? c : '_');

        return builder.ToString();
    }
}
