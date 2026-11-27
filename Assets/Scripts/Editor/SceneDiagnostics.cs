#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

public static class SceneDiagnostics
{
    [MenuItem("Newton VR/Run Scene Diagnostics", false, 10)]
    public static void RunDiagnostics()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/NewtonEnvironment.unity", OpenSceneMode.Single);
        StringBuilder sb = new StringBuilder();
        void Log(string msg) { sb.AppendLine(msg); Debug.Log(msg); }

        Log("==== [Newton VR Scene Diagnostics] ====");
        
        // 1. Check Scene
        Scene currentScene = SceneManager.GetActiveScene();
        Log($"Active Scene: {currentScene.name} (Path: {currentScene.path})");

        // 2. Find Player and Camera
        GameObject player = GameObject.Find("Player");
        if (player != null)
        {
            Log($"Player found at position: {player.transform.position}");
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null)
            {
                Log($"CharacterController: enabled={cc.enabled}, height={cc.height}, radius={cc.radius}, center={cc.center}");
            }
            else
            {
                Log("CharacterController missing on Player!");
            }

            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null)
            {
                Log($"PlayerController: enabled={pc.enabled}, speed={pc.walkSpeed}, camera={(pc.playerCamera != null ? pc.playerCamera.name : "null")}");
            }
            else
            {
                Log("PlayerController missing on Player!");
            }
        }
        else
        {
            Log("Player GameObject not found in scene!");
        }

        // 3. Find Environment and Bounds
        GameObject env = GameObject.Find("NewtonEnvironment");
        if (env != null)
        {
            Bounds totalBounds = new Bounds(env.transform.position, Vector3.zero);
            MeshRenderer[] renderers = env.GetComponentsInChildren<MeshRenderer>();
            Log($"NewtonEnvironment found with {renderers.Length} MeshRenderers.");
            bool hasBounds = false;
            foreach (var r in renderers)
            {
                if (!hasBounds)
                {
                    totalBounds = r.bounds;
                    hasBounds = true;
                }
                else
                {
                    totalBounds.Encapsulate(r.bounds);
                }
            }
            // Inspect distinct materials and shaders
            System.Collections.Generic.HashSet<string> matInfo = new System.Collections.Generic.HashSet<string>();
            foreach (var r in renderers)
            {
                foreach (var m in r.sharedMaterials)
                {
                    if (m != null)
                    {
                        string sName = m.shader != null ? m.shader.name : "null";
                        string colStr = m.HasProperty("_Color") ? m.color.ToString() : "N/A";
                        matInfo.Add($"{m.name} (Shader: {sName}, color={colStr})");
                    }
                    else
                    {
                        matInfo.Add("<null material>");
                    }
                }
            }
            Log($"Unique Materials ({matInfo.Count}):");
            foreach (var mi in matInfo)
            {
                Log($"  - {mi}");
            }

            // Check Lights
            Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            Log($"Lights in scene ({lights.Length}):");
            foreach (var l in lights)
            {
                Log($"  - Light '{l.gameObject.name}': type={l.type}, color={l.color}, intensity={l.intensity}, shadows={l.shadows}, rot={l.transform.eulerAngles}");
            }

            // Check Ambient
            Log($"Ambient: mode={RenderSettings.ambientMode}, skyColor={RenderSettings.ambientSkyColor}, equatorColor={RenderSettings.ambientEquatorColor}, groundColor={RenderSettings.ambientGroundColor}, intensity={RenderSettings.ambientIntensity}");
            
            // Check raycast below player
            if (player != null)
            {
                RaycastHit[] hits = Physics.RaycastAll(player.transform.position + Vector3.up * 0.1f, Vector3.down, 50f);
                Log($"Raycast down hits count: {hits.Length}");
                foreach (var h in hits)
                {
                    Log($"Hit: '{h.collider.gameObject.name}', point={h.point}, dist={h.distance}");
                }
            }
        }
        else
        {
            Log("NewtonEnvironment GameObject not found in scene!");
        }

        // 4. Check Cameras
        Camera[] cams = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
        Log($"Total Cameras in scene: {cams.Length}");
        foreach (var c in cams)
        {
            var comps = string.Join(", ", System.Array.ConvertAll(c.GetComponents<Component>(), comp => comp.GetType().Name));
            Log($"Camera: '{c.gameObject.name}', enabled={c.enabled}, depth={c.depth}, clearFlags={c.clearFlags}, tag={c.tag}, targetDisplay={c.targetDisplay}, pos={c.transform.position}, comps=[{comps}]");
        }

        // 5. Check Render Pipeline
        var currentRP = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
        Log($"Current Render Pipeline: {(currentRP != null ? currentRP.GetType().Name : "Built-in Render Pipeline")}");

        Log("==== [End Diagnostics] ====");

        try
        {
            File.WriteAllText("diagnostics_output.txt", sb.ToString());
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Failed to write diagnostics_output.txt: " + ex.Message);
        }
    }
}
#endif
