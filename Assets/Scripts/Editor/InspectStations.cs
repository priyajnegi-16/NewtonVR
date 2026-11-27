using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class InspectStations
{
    [MenuItem("NewtonVR/Inspect Stations")]
    public static void Inspect()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/NewtonEnvironment.unity");
        GameObject env = GameObject.Find("UnityNewtonFullEnv");
        if (env == null)
        {
            Debug.LogError("UnityNewtonFullEnv not found!");
            return;
        }

        Debug.Log("=== INSPECTING STATIONS ===");

        // STATION 1
        Transform s1 = env.transform.Find("Station1_FirstLaw");
        if (s1 != null)
        {
            Debug.Log($"[S1] World Pos: {s1.position}, Rot: {s1.eulerAngles}");
            foreach (Transform c in s1)
            {
                if (c.name.Contains("Cart") || c.name.Contains("Obstacle") || c.name.Contains("Track") || c.name.Contains("Friction") || c.name.Contains("Button"))
                {
                    Debug.Log($"  S1 Child: '{c.name}' LocalPos={c.localPosition}, WorldPos={c.position}");
                }
            }
        }

        // STATION 2
        Transform s2 = env.transform.Find("Station2_SecondLaw");
        if (s2 != null)
        {
            Debug.Log($"[S2] World Pos: {s2.position}, Rot: {s2.eulerAngles}");
            foreach (Transform c in s2)
            {
                if (c.name.Contains("Cart") || c.name.Contains("Panel") || c.name.Contains("Track") || c.name.Contains("Button") || c.name.Contains("Control") || c.name.Contains("Mass") || c.name.Contains("Force"))
                {
                    Debug.Log($"  S2 Child: '{c.name}' LocalPos={c.localPosition}, WorldPos={c.position}");
                }
            }
        }

        // STATION 3
        Transform s3 = env.transform.Find("Station3_ThirdLaw");
        if (s3 != null)
        {
            Debug.Log($"[S3] World Pos: {s3.position}, Rot: {s3.eulerAngles}");
            foreach (Transform c in s3)
            {
                if (c.name.Contains("Cart") || c.name.Contains("Spring") || c.name.Contains("Track") || c.name.Contains("Button") || c.name.Contains("Display") || c.name.Contains("Force"))
                {
                    Debug.Log($"  S3 Child: '{c.name}' LocalPos={c.localPosition}, WorldPos={c.position}");
                }
            }
        }
    }
}
