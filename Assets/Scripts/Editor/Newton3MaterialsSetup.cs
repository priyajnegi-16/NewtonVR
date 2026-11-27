#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Creates and assigns high-fidelity PBR and emissive materials for Newton's Third Law experiment model (F1 = -F2).
/// Matches the futuristic dark navy, cyan, violet, and liquid-glass aesthetic of Labs 1 and 2.
/// </summary>
public static class Newton3MaterialsSetup
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
            else
            {
                mat.DisableKeyword("_EMISSION");
            }
            EditorUtility.SetDirty(mat);
        }

        return mat;
    }

    public static void ApplyMaterialsToModel(GameObject root)
    {
        // 1. Core Futuristic PBR Materials matching Lab 1 & 2
        Material darkNavy = GetOrCreateMaterial("N3_DeepNavy", new Color(0.015f, 0.035f, 0.08f), 0.2f, 0.85f, Color.black);
        Material midnightBlue = GetOrCreateMaterial("N3_MidnightBlue", new Color(0.025f, 0.055f, 0.14f), 0.15f, 0.85f, Color.black);
        Material trackSilver = GetOrCreateMaterial("N3_TrackSilver", new Color(0.45f, 0.55f, 0.68f), 0.85f, 0.8f, Color.black);
        Material springChrome = GetOrCreateMaterial("N3_SpringChrome", new Color(0.75f, 0.82f, 0.95f), 0.95f, 0.95f, new Color(0.1f, 0.2f, 0.4f) * 0.5f);
        Material graphite = GetOrCreateMaterial("N3_Graphite", new Color(0.025f, 0.035f, 0.055f), 0.2f, 0.6f, Color.black);
        Material displayGlass = GetOrCreateMaterial("N3_DisplayGlass", new Color(0.02f, 0.06f, 0.12f, 0.85f), 0.3f, 0.95f, new Color(0.02f, 0.1f, 0.2f), true);

        // 2. Vivid Glowing Emissives
        Material cyanGlow = GetOrCreateMaterial("N3_NeonCyan", new Color(0.05f, 0.85f, 1.0f), 0.1f, 0.9f, new Color(0.1f, 0.85f, 1.2f) * 1.8f);
        Material violetGlow = GetOrCreateMaterial("N3_NeonViolet", new Color(0.6f, 0.2f, 1.0f), 0.1f, 0.9f, new Color(0.65f, 0.2f, 1.2f) * 1.8f);
        Material greenGlow = GetOrCreateMaterial("N3_ReadoutGreen", new Color(0.08f, 0.92f, 0.28f), 0.1f, 0.85f, new Color(0.1f, 1.1f, 0.3f) * 1.6f);
        Material textWhite = GetOrCreateMaterial("N3_ReadableWhite", new Color(0.92f, 0.96f, 1.0f), 0.05f, 0.5f, new Color(0.6f, 0.7f, 0.8f) * 0.5f);

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in renderers)
        {
            string n = r.gameObject.name.ToLower();

            // Cart A (Cyan)
            if (n.Contains("cart a"))
            {
                if (n.Contains("top") || n.Contains("accent") || n.Contains("side"))
                    r.sharedMaterial = cyanGlow;
                else if (n.Contains("hub") || n.Contains("wheel"))
                    r.sharedMaterial = cyanGlow;
                else
                    r.sharedMaterial = graphite;
            }
            // Cart B (Violet)
            else if (n.Contains("cart b"))
            {
                if (n.Contains("top") || n.Contains("accent") || n.Contains("side"))
                    r.sharedMaterial = violetGlow;
                else if (n.Contains("hub") || n.Contains("wheel"))
                    r.sharedMaterial = violetGlow;
                else
                    r.sharedMaterial = graphite;
            }
            // Spring
            else if (n.Contains("spring"))
            {
                r.sharedMaterial = springChrome;
            }
            // Buttons
            else if (n.Contains("push left"))
            {
                if (n.Contains("base")) r.sharedMaterial = graphite;
                else r.sharedMaterial = cyanGlow;
            }
            else if (n.Contains("push right"))
            {
                if (n.Contains("base")) r.sharedMaterial = graphite;
                else r.sharedMaterial = violetGlow;
            }
            else if (n.Contains("reset"))
            {
                if (n.Contains("base")) r.sharedMaterial = graphite;
                else r.sharedMaterial = greenGlow;
            }
            // Displays & Readouts
            else if (n.Contains("f1_value") || n.Contains("f1 value"))
            {
                r.sharedMaterial = cyanGlow;
            }
            else if (n.Contains("f2_value") || n.Contains("f2 value"))
            {
                r.sharedMaterial = violetGlow;
            }
            else if (n.Contains("screen") || n.Contains("display"))
            {
                r.sharedMaterial = displayGlass;
            }
            else if (n.Contains("text") || n.Contains("label") || n.Contains("line") || n.Contains("header") || n.Contains("title") || n.Contains("equation") || n.Contains("statement"))
            {
                r.sharedMaterial = textWhite;
            }
            else if (n.Contains("trim") || n.Contains("glow") || n.Contains("accent") || n.Contains("indicator"))
            {
                if (n.Contains("violet") || n.Contains("right")) r.sharedMaterial = violetGlow;
                else r.sharedMaterial = cyanGlow;
            }
            // Track & Structure
            else if (n.Contains("rail") || n.Contains("deck") || n.Contains("foot") || n.Contains("support"))
            {
                r.sharedMaterial = trackSilver;
            }
            else if (n.Contains("console") || n.Contains("backboard") || n.Contains("base") || n.Contains("panel"))
            {
                r.sharedMaterial = darkNavy;
            }
            else
            {
                r.sharedMaterial = midnightBlue;
            }
        }

        Debug.Log($"[Newton VR] Applied Lab 3 materials to {renderers.Length} sub-objects.");
    }
}
#endif

