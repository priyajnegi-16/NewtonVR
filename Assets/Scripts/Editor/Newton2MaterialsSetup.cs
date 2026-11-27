#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Creates and assigns high-fidelity PBR and emissive materials for Newton's Second Law experiment model (F = ma).
/// Matches the futuristic dark navy, cyan, violet, and liquid-glass aesthetic of Lab 1.
/// </summary>
public static class Newton2MaterialsSetup
{
    private const string MatDir = "Assets/Environment/Materials";

    public static Material GetOrCreateMaterial(string matName, Color albedo, float metallic, float smoothness, Color emissionColor, bool isTransparent = false)
    {
        if (!Directory.Exists(MatDir))
        {
            Directory.CreateDirectory(MatDir);
        }

        string path = $"{MatDir}/{matName}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (mat == null)
        {
            Shader standardShader = Shader.Find("Standard");
            mat = new Material(standardShader);
            mat.name = matName;

            mat.color = albedo;
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Glossiness", smoothness);

            if (emissionColor.maxColorComponent > 0.05f)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emissionColor);
            }

            if (isTransparent)
            {
                mat.SetFloat("_Mode", 3); // Transparent
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;
            }

            AssetDatabase.CreateAsset(mat, path);
            Debug.Log($"[Newton VR] Created material '{matName}' at '{path}'.");
        }
        else
        {
            mat.color = albedo;
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Glossiness", smoothness);

            if (emissionColor.maxColorComponent > 0.05f)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emissionColor);
            }
            EditorUtility.SetDirty(mat);
        }

        return mat;
    }

    [MenuItem("Newton VR/Generate Experiment 2 Materials", false, 7)]
    public static void GenerateAllMaterials()
    {
        // 1. Blue Cart (Cart A - 1kg mass)
        GetOrCreateMaterial("Blue Cart", new Color(0.08f, 0.38f, 0.95f), 0.4f, 0.82f, new Color(0.05f, 0.3f, 0.9f) * 1.4f);

        // 2. Red Cart (Cart B - 5kg mass)
        GetOrCreateMaterial("Red Cart", new Color(0.95f, 0.12f, 0.16f), 0.4f, 0.82f, new Color(0.85f, 0.05f, 0.1f) * 1.4f);

        // 3. Deep Navy (Panels, console casing, backboard frame)
        GetOrCreateMaterial("Deep Navy", new Color(0.015f, 0.035f, 0.08f), 0.2f, 0.5f, Color.black);

        // 4. Graphite Metal (Deck, chassis frame, wheel hubs, support legs)
        GetOrCreateMaterial("Graphite Metal", new Color(0.06f, 0.08f, 0.12f), 0.7f, 0.65f, Color.black);

        // 5. Brushed Silver (Track rails, metallic frame)
        GetOrCreateMaterial("Brushed Silver", new Color(0.65f, 0.72f, 0.82f), 0.85f, 0.75f, Color.black);

        // 6. Neon Cyan (Track glow, side light, UI accents)
        GetOrCreateMaterial("Neon Cyan", new Color(0.05f, 0.85f, 1.0f), 0.1f, 0.9f, new Color(0.1f, 0.85f, 1.2f) * 1.8f);

        // 7. Purple Accent (Callouts, button accents, secondary highlights)
        GetOrCreateMaterial("Purple Accent", new Color(0.42f, 0.10f, 0.78f), 0.1f, 0.75f, new Color(0.35f, 0.08f, 0.70f) * 1.5f);

        // 8. Liquid Glass UI - translucent blue
        GetOrCreateMaterial("Liquid Glass UI - translucent blue", new Color(0.04f, 0.14f, 0.28f, 0.75f), 0.2f, 0.98f, new Color(0.05f, 0.45f, 0.85f) * 0.3f, true);

        // 9. UI White (Readable 3D text labels)
        GetOrCreateMaterial("UI White", new Color(0.92f, 0.96f, 1.0f), 0.0f, 0.4f, new Color(0.6f, 0.7f, 0.8f));

        // 10. Lab Floor (Station platform floor inlay)
        GetOrCreateMaterial("Lab Floor", new Color(0.04f, 0.06f, 0.1f), 0.3f, 0.5f, Color.black);

        // 11. Launch Green (Launch button)
        GetOrCreateMaterial("Launch Green", new Color(0.1f, 0.92f, 0.25f), 0.1f, 0.8f, new Color(0.1f, 1.2f, 0.3f) * 1.6f);

        // 12. Warning Red (Stop button)
        GetOrCreateMaterial("Warning Red", new Color(0.95f, 0.05f, 0.08f), 0.1f, 0.7f, new Color(1.2f, 0.05f, 0.1f) * 1.6f);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Newton VR] All 12 Experiment 2 materials generated successfully.");
    }

    public static void ApplyMaterialsToModel(GameObject expInstance)
    {
        GenerateAllMaterials();

        MeshRenderer[] renderers = expInstance.GetComponentsInChildren<MeshRenderer>(true);
        foreach (MeshRenderer r in renderers)
        {
            string goName = r.gameObject.name;
            Material[] mats = r.sharedMaterials;
            bool modified = false;

            for (int i = 0; i < mats.Length; i++)
            {
                string matName = (mats[i] != null) ? mats[i].name.Replace(" (Instance)", "") : "";
                string bestMatch = null;

                if (matName.Contains("Blue Cart") || goName.Contains("Cart A")) bestMatch = "Blue Cart";
                else if (matName.Contains("Red Cart") || goName.Contains("Cart B")) bestMatch = "Red Cart";
                else if (matName.Contains("Neon Cyan") || matName.Contains("Neon") || goName.Contains("SideGlow") || goName.Contains("TopGlow") || goName.Contains("Underline") || goName.Contains("Rib")) bestMatch = "Neon Cyan";
                else if (matName.Contains("Purple Accent") || matName.Contains("Purple") || goName.Contains("Caption") || goName.Contains("GlowHalo")) bestMatch = "Purple Accent";
                else if (matName.Contains("Liquid Glass") || goName.Contains("GlassRim")) bestMatch = "Liquid Glass UI - translucent blue";
                else if (matName.Contains("Brushed Silver") || goName.Contains("Rail") || goName.Contains("Top")) bestMatch = "Brushed Silver";
                else if (matName.Contains("Deep Navy") || goName.Contains("Back Wall") || goName.Contains("Panel") || goName.Contains("Board")) bestMatch = "Deep Navy";
                else if (matName.Contains("Graphite Metal") || goName.Contains("Chassis") || goName.Contains("Wheel") || goName.Contains("Hub") || goName.Contains("Leg") || goName.Contains("Foot") || goName.Contains("Base")) bestMatch = "Graphite Metal";
                else if (matName.Contains("UI White") || goName.Contains("Heading") || goName.Contains("Line") || goName.Contains("Label") || goName.Contains("Text") || goName.Contains("MassLabel")) bestMatch = "UI White";
                else if (goName.Contains("LAUNCH Button") && !goName.Contains("Base") && !goName.Contains("Label")) bestMatch = "Launch Green";
                else if (goName.Contains("STOP Button") && !goName.Contains("Base") && !goName.Contains("Label")) bestMatch = "Warning Red";
                else if (goName.Contains("RESET Button") && !goName.Contains("Base") && !goName.Contains("Label")) bestMatch = "Purple Accent";
                else if (matName.Contains("Lab Floor") || goName.Contains("Floor")) bestMatch = "Lab Floor";
                else if (!string.IsNullOrEmpty(matName)) bestMatch = matName;

                if (bestMatch != null)
                {
                    Material targetMat = AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/{bestMatch}.mat");
                    if (targetMat != null)
                    {
                        mats[i] = targetMat;
                        modified = true;
                    }
                }
            }

            if (modified)
            {
                r.sharedMaterials = mats;
            }
        }

        Debug.Log("[Newton VR] Materials successfully applied to NewtonsSecondLawExp meshes.");
    }
}
#endif

