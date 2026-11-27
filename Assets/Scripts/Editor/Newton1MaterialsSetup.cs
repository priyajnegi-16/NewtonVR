#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Creates and assigns high-fidelity PBR and emissive materials for Newton's First Law experiment model.
/// </summary>
public static class Newton1MaterialsSetup
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

    [MenuItem("Newton VR/Generate Experiment 1 Materials", false, 5)]
    public static void GenerateAllMaterials()
    {
        // 1. Brushed Silver (Track rails, metallic frame)
        GetOrCreateMaterial("Brushed Silver.002", new Color(0.65f, 0.72f, 0.82f), 0.85f, 0.75f, Color.black);

        // 2. Callout Purple (Instruction & title callouts)
        GetOrCreateMaterial("Callout Purple.002", new Color(0.35f, 0.08f, 0.65f), 0.1f, 0.6f, new Color(0.25f, 0.05f, 0.5f));

        // 3. Chrome Ball (The inertia ball - highly reflective chrome)
        GetOrCreateMaterial("Chrome Ball.002", new Color(0.85f, 0.92f, 1.0f), 0.95f, 0.98f, Color.black);

        // 4. Deep Navy (Panels, console casing)
        GetOrCreateMaterial("Deep Navy.002", new Color(0.015f, 0.035f, 0.08f), 0.2f, 0.4f, Color.black);

        // 5. Graphite Metal (Deck, chassis frame, wheel hubs)
        GetOrCreateMaterial("Graphite Metal.002", new Color(0.06f, 0.08f, 0.12f), 0.7f, 0.65f, Color.black);

        // 6. Hazard Black (Hazard stripes)
        GetOrCreateMaterial("Hazard Black.002", new Color(0.01f, 0.015f, 0.02f), 0.1f, 0.3f, Color.black);

        // 7. Hazard Yellow (Hazard stripes & obstacle warning)
        GetOrCreateMaterial("Hazard Yellow.002", new Color(1.0f, 0.65f, 0.02f), 0.2f, 0.5f, new Color(0.8f, 0.45f, 0.02f) * 1.2f);

        // 8. Lab Floor (Station platform floor inlay)
        GetOrCreateMaterial("Lab Floor.002", new Color(0.04f, 0.06f, 0.1f), 0.3f, 0.5f, Color.black);

        // 9. Material (Default neutral)
        GetOrCreateMaterial("Material", new Color(0.8f, 0.8f, 0.8f), 0.1f, 0.5f, Color.black);

        // 10. Neon Cyan (Track glow, side light, UI accents)
        GetOrCreateMaterial("Neon Cyan.002", new Color(0.05f, 0.75f, 1.0f), 0.1f, 0.8f, new Color(0.1f, 0.85f, 1.2f) * 1.5f);

        // 11. UI White (Readable 3D text labels)
        GetOrCreateMaterial("UI White.002", new Color(0.9f, 0.95f, 1.0f), 0.0f, 0.4f, new Color(0.5f, 0.55f, 0.6f));

        // 12. Warning Red (Stop button, obstacle warning lights)
        GetOrCreateMaterial("Warning Red.002", new Color(0.95f, 0.05f, 0.08f), 0.1f, 0.7f, new Color(1.2f, 0.05f, 0.1f) * 1.5f);

        // 13. Liquid Glass UI (Console and panel translucent edges, matching Lab 2)
        GetOrCreateMaterial("Liquid Glass UI - translucent blue", new Color(0.04f, 0.12f, 0.22f, 0.75f), 0.2f, 0.98f, new Color(0.05f, 0.85f, 1.0f) * 0.18f, true);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Newton VR] All 13 Experiment 1 materials generated successfully.");
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

                // Match by material name or game object name
                string bestMatch = null;

                if (matName.Contains("Glass") || goName.Contains("BorderTop") || goName.Contains("Console_Border")) bestMatch = "Liquid Glass UI - translucent blue";
                else if (matName.Contains("Chrome") || goName.Contains("Inertia Ball") || goName == "Ball") bestMatch = "Chrome Ball.002";
                else if (matName.Contains("Neon") || goName.Contains("Cyan") || goName.Contains("Glow") || goName.Contains("Inlay") || goName.Contains("Rib")) bestMatch = "Neon Cyan.002";
                else if (matName.Contains("Yellow") || goName.Contains("Hazard Stripe")) bestMatch = "Hazard Yellow.002";
                else if (matName.Contains("Warning") || goName.Contains("Warning Light") || goName.Contains("STOP Button") || goName.Contains("OBSTACLE Button")) bestMatch = "Warning Red.002";
                else if (goName.Contains("RESET Button")) bestMatch = "Callout Purple.002";
                else if (matName.Contains("Silver") || goName.Contains("Rail") || goName.Contains("Deck") || goName.Contains("Top Plate") || goName.Contains("Slider Track") || goName.Contains("Slider Knob")) bestMatch = "Brushed Silver.002";
                else if (matName.Contains("Purple") || goName.Contains("Callout")) bestMatch = "Callout Purple.002";
                else if (matName.Contains("Navy") || goName.Contains("Panel") || goName.Contains("Console") || goName.Contains("Housing") || goName.Contains("Rear Wall")) bestMatch = "Deep Navy.002";
                else if (matName.Contains("Graphite") || goName.Contains("Chassis") || goName.Contains("Wheel") || goName.Contains("Foot") || goName.Contains("Leg") || goName.Contains("Side Support")) bestMatch = "Graphite Metal.002";
                else if (matName.Contains("UI") || goName.Contains("Text") || goName.Contains("Title") || goName.Contains("Label")) bestMatch = "UI White.002";
                else if (matName.Contains("Floor") || goName.Contains("Floor")) bestMatch = "Lab Floor.002";
                else if (matName.Contains("Hazard Black")) bestMatch = "Hazard Black.002";

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

        Debug.Log("[Newton VR] Materials successfully applied to NewtonsFirstLawExp meshes.");
    }
}
#endif
