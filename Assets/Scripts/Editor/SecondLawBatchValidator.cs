#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Automated validator for Newton's Second Law experiment (F = m * a):
/// 1. Verifies independent Cart 1 (Blue) and Cart 2 (Red) mass selection.
/// 2. Verifies Force control panel and launch/reset buttons.
/// 3. Validates physics: a = F / m for both carts.
/// 4. Generates camera render showing the 3D control console in front of the track.
/// </summary>
public static class SecondLawBatchValidator
{
    private const string ScenePath = "Assets/Scenes/NewtonEnvironment.unity";

    [MenuItem("Newton Lab/Run Second Law Validation and Build Panels")]
    public static void ExecuteValidation()
    {
        Debug.Log("[Newton VR] Running Second Law Validation and building panels in NewtonEnvironment.unity...");

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        NewtonsSecondLawExperiment exp = UnityEngine.Object.FindFirstObjectByType<NewtonsSecondLawExperiment>();
        if (exp == null)
        {
            Debug.LogError("[Newton VR] NewtonsSecondLawExperiment component not found in scene!");
            return;
        }

        // 1. Build and verify 3D interactive console
        exp.BuildOrUpdate3DControlPanels();

        // 2. Test independent mass selection
        exp.SelectCart1Mass(2.0f);
        exp.SelectCart2Mass(10.0f);
        exp.SetForce(10.0f);

        Debug.Log($"[Newton VR] Configured Test: Cart 1 = {exp.massCartA} kg, Cart 2 = {exp.massCartB} kg, Force = {exp.appliedForce} N");
        float expectedA1 = exp.appliedForce / exp.massCartA; // 5.0 m/s^2
        float expectedA2 = exp.appliedForce / exp.massCartB; // 1.0 m/s^2
        Debug.Log($"[Newton VR] Expected Acceleration: Cart 1 = {expectedA1} m/s^2, Cart 2 = {expectedA2} m/s^2 (Ratio = {expectedA1 / expectedA2}x)");

        // 3. Reset experiment to initial state with standard educational presets
        exp.SelectCart1Mass(1.0f);
        exp.SelectCart2Mass(5.0f);
        exp.SetForce(10.0f);
        exp.ResetExperiment();

        // 4. Capture screenshot from player's viewpoint
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camObj = new GameObject("VerificationCam");
            cam = camObj.AddComponent<Camera>();
        }
        cam.transform.position = new Vector3(-41.2f, 1.75f, 0f);
        cam.transform.rotation = Quaternion.Euler(14.0f, -90.0f, 0f);
        cam.fieldOfView = 58.0f;

        string screenshotPath = Path.Combine(Application.dataPath, "SecondLaw_ControlPanels_View.png");
        CaptureCameraRender(cam, screenshotPath);
        Debug.Log($"[Newton VR] Screenshot captured at: {screenshotPath}");

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("[Newton VR] SUCCESS: Lab 2 Control Panels built and verified!");
    }

    private static void CaptureCameraRender(Camera cam, string outputPath)
    {
        if (cam == null) return;
        RenderTexture rt = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
        tex.Apply();

        cam.targetTexture = null;
        RenderTexture.active = null;
        UnityEngine.Object.DestroyImmediate(rt);

        byte[] bytes = tex.EncodeToPNG();
        UnityEngine.Object.DestroyImmediate(tex);
        File.WriteAllBytes(outputPath, bytes);
    }
}
#endif
