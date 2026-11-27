#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor utility to generate the FloatingVRDestinationPanel prefab.
/// </summary>
public static class CreateDestinationPanelPrefab
{
    private const string PrefabPath = "Assets/Prefabs/FloatingVRDestinationPanel.prefab";

    [MenuItem("Newton VR/Create Floating Destination Panel Prefab", false, 10)]
    public static void GeneratePrefab()
    {
        // Check if directory exists
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        GameObject root = new GameObject("FloatingVRDestinationPanel");
        FloatingVRDestinationPanel panel = root.AddComponent<FloatingVRDestinationPanel>();

        // Save as Prefab
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[Newton VR] Successfully created and saved prefab at: {PrefabPath}");
    }
}
#endif

