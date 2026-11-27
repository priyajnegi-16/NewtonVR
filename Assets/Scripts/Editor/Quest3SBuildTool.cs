#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Editor build automation tool for Meta Quest 3S standalone Android deployment.
/// </summary>
public static class Quest3SBuildTool
{
    private const string ScenePath = "Assets/Scenes/NewtonEnvironment.unity";
    private const string OutputDirectory = "Builds/Quest3S";
    private const string ApkFileName = "Newton_VR_Simulation_Quest3S.apk";

    [MenuItem("Newton VR/Meta Quest 3S/Prepare Quest 3S Build Settings", false, 20)]
    public static void ConfigureQuestBuildSettings()
    {
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

        PlayerSettings.companyName = "NewtonVRLab";
        PlayerSettings.productName = "Newton VR Simulation";
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.newtonvr.simulation");

        // Quest 3S Snapdragon XR2 Gen 2 requirement: ARM64 + IL2CPP
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29; // Android 10+
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel32;

        // Color Space & Graphics
        PlayerSettings.colorSpace = ColorSpace.Linear;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;

        AssetDatabase.SaveAssets();
        Debug.Log("[Newton VR] Successfully configured Android Player Settings for Meta Quest 3S (ARM64, IL2CPP, API 29+).");
    }

    [MenuItem("Newton VR/Meta Quest 3S/Build Standalone Quest 3S APK", false, 21)]
    public static void BuildQuestApk()
    {
        ConfigureQuestBuildSettings();

        if (!Directory.Exists(OutputDirectory))
        {
            Directory.CreateDirectory(OutputDirectory);
        }

        string fullApkPath = Path.Combine(OutputDirectory, ApkFileName);

        BuildPlayerOptions buildOptions = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = fullApkPath,
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            options = BuildOptions.None
        };

        Debug.Log($"[Newton VR] Starting Meta Quest 3S Android Build to: '{fullApkPath}'...");
        BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[Newton VR] Build SUCCEEDED: '{fullApkPath}' ({summary.totalSize / (1024 * 1024)} MB in {summary.totalTime.TotalSeconds:F1}s).");
            EditorUtility.RevealInFinder(fullApkPath);
        }
        else
        {
            Debug.LogError($"[Newton VR] Build FAILED with result: {summary.result}. Check Editor console for compiler/shader details.");
        }
    }
}
#endif

