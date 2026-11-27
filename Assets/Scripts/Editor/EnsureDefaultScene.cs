#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Automatically opens Assets/Scenes/NewtonEnvironment.unity when Unity starts
/// if the active scene is untitled/empty.  This ensures any recipient who opens
/// Newton_VR_Simulation_Shareable_3 immediately sees the full VR environment
/// in the Scene and Game views rather than a blank new scene.
/// </summary>
[InitializeOnLoad]
public static class EnsureDefaultScene
{
    private const string ScenePath = "Assets/Scenes/NewtonEnvironment.unity";

    static EnsureDefaultScene()
    {
        // Defer to next editor frame so the AssetDatabase is fully loaded first
        EditorApplication.delayCall += OpenDefaultScene;
    }

    private static void OpenDefaultScene()
    {
        // Only act when no play session is running and we're in a project-open context
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        Scene active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();

        // Scene is "untitled" and completely empty → auto-open the Newton scene
        bool isUntitled = string.IsNullOrEmpty(active.path);
        bool isEmpty    = !active.isDirty && active.rootCount == 0;

        if (isUntitled || isEmpty)
        {
            if (System.IO.File.Exists(ScenePath))
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                UnityEngine.Debug.Log("[Newton VR] EnsureDefaultScene: auto-opened " + ScenePath);
            }
        }
    }
}
#endif

