#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor utility to construct, configure, and open the Newton Simulation Environment scene.
/// Complete with 3 Dedicated Experiment Labs (Lab 1: North, Lab 2: East, Lab 3: West),
/// prominent futuristic glowing portal signs, VR Rig & Desktop Player, Shading & Lighting optimization.
/// </summary>
public static class SetupNewtonScene
{
    private const string ScenePath = "Assets/Scenes/NewtonEnvironment.unity";
    private const string FbxPath = "Assets/Environment/UnityNewtonFullEnv.fbx";
    private const string BlendPath = "Assets/Environment/UnityNewtonFullEnv.blend";

    [MenuItem("Newton VR/Setup or Rebuild Scene (Apply All Fixes & VR Setup)", false, 1)]
    public static void BuildEnvironmentScene()
    {
        Debug.Log("[Newton VR] Constructing Reorganized Newton Simulation Environment Scene...");

        // Ensure the FBX asset is freshly imported from disk
        AssetDatabase.ImportAsset(FbxPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.Refresh();

        if (!Directory.Exists("Assets/Scenes"))
        {
            Directory.CreateDirectory("Assets/Scenes");
        }

        // Create new clean scene
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 1. Lighting Setup (Atmospheric, futuristic indoor illumination with rich contrast)
        GameObject lightingRoot = new GameObject("Environment Lighting");

        // Interior Hall Fill Lights throughout MainLobby
        float[] fillZPositions = new float[] { -8f, -2f, 2f, 8f };
        for (int i = 0; i < fillZPositions.Length; i++)
        {
            GameObject fillLightGo = new GameObject($"Hall Fill Light {i + 1}");
            fillLightGo.transform.SetParent(lightingRoot.transform);
            fillLightGo.transform.position = new Vector3(0f, 4.2f, fillZPositions[i]);

            Light pLight = fillLightGo.AddComponent<Light>();
            pLight.type = LightType.Point;
            pLight.color = new Color(0.85f, 0.94f, 1.0f);
            pLight.intensity = 0.75f;
            pLight.range = 22f;
            pLight.shadows = LightShadows.None;
        }

        // North Corridor Fill Lights leading to Lab 1 (North is -Z in Unity)
        float[] northCorrZ = new float[] { -18f, -24f, -30f };
        for (int i = 0; i < northCorrZ.Length; i++)
        {
            GameObject ncLightGo = new GameObject($"Corridor North Light {i + 1}");
            ncLightGo.transform.SetParent(lightingRoot.transform);
            ncLightGo.transform.position = new Vector3(0f, 3.2f, northCorrZ[i]);

            Light ncLight = ncLightGo.AddComponent<Light>();
            ncLight.type = LightType.Point;
            ncLight.color = new Color(0.65f, 0.90f, 1.0f);
            ncLight.intensity = 0.70f;
            ncLight.range = 14f;
            ncLight.shadows = LightShadows.None;
        }

        // Lab 1 Dedicated Ambient Ceiling Light (at Z = -48)
        GameObject lab1FillGo = new GameObject("Lab 1 Ceiling Light");
        lab1FillGo.transform.SetParent(lightingRoot.transform);
        lab1FillGo.transform.position = new Vector3(0f, 5.5f, -48f);
        Light l1Light = lab1FillGo.AddComponent<Light>();
        l1Light.type = LightType.Point;
        l1Light.color = new Color(0.85f, 0.95f, 1.0f);
        l1Light.intensity = 0.90f;
        l1Light.range = 25f;
        l1Light.shadows = LightShadows.None;

        // East Corridor Fill Lights leading to Lab 2 (East is -X in Unity)
        float[] eastCorrX = new float[] { -18f, -24f, -30f };
        for (int i = 0; i < eastCorrX.Length; i++)
        {
            GameObject ecLightGo = new GameObject($"Corridor East Light {i + 1}");
            ecLightGo.transform.SetParent(lightingRoot.transform);
            ecLightGo.transform.position = new Vector3(eastCorrX[i], 3.2f, 0f);

            Light ecLight = ecLightGo.AddComponent<Light>();
            ecLight.type = LightType.Point;
            ecLight.color = new Color(0.65f, 0.90f, 1.0f);
            ecLight.intensity = 0.70f;
            ecLight.range = 14f;
            ecLight.shadows = LightShadows.None;
        }

        // Lab 2 Dedicated Ambient Ceiling Light (at X = -48)
        GameObject lab2FillGo = new GameObject("Lab 2 Ceiling Light");
        lab2FillGo.transform.SetParent(lightingRoot.transform);
        lab2FillGo.transform.position = new Vector3(-48f, 5.5f, 0f);
        Light l2Light = lab2FillGo.AddComponent<Light>();
        l2Light.type = LightType.Point;
        l2Light.color = new Color(0.85f, 0.95f, 1.0f);
        l2Light.intensity = 0.90f;
        l2Light.range = 25f;
        l2Light.shadows = LightShadows.None;

        // West Corridor Fill Lights leading to Lab 3 (West is +X in Unity)
        float[] westCorrX = new float[] { 18f, 24f, 30f };
        for (int i = 0; i < westCorrX.Length; i++)
        {
            GameObject wcLightGo = new GameObject($"Corridor West Light {i + 1}");
            wcLightGo.transform.SetParent(lightingRoot.transform);
            wcLightGo.transform.position = new Vector3(westCorrX[i], 3.2f, 0f);

            Light wcLight = wcLightGo.AddComponent<Light>();
            wcLight.type = LightType.Point;
            wcLight.color = new Color(0.65f, 0.90f, 1.0f);
            wcLight.intensity = 0.70f;
            wcLight.range = 14f;
            wcLight.shadows = LightShadows.None;
        }

        // Lab 3 Dedicated Ambient Ceiling Light (at X = +48)
        GameObject lab3FillGo = new GameObject("Lab 3 Ceiling Light");
        lab3FillGo.transform.SetParent(lightingRoot.transform);
        lab3FillGo.transform.position = new Vector3(48f, 5.5f, 0f);
        Light l3Light = lab3FillGo.AddComponent<Light>();
        l3Light.type = LightType.Point;
        l3Light.color = new Color(0.85f, 0.95f, 1.0f);
        l3Light.intensity = 0.90f;
        l3Light.range = 25f;
        l3Light.shadows = LightShadows.None;

        // Ambient Lighting for deep futuristic tones
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.20f, 0.24f, 0.32f);
        RenderSettings.ambientEquatorColor = new Color(0.10f, 0.12f, 0.18f);
        RenderSettings.ambientGroundColor = new Color(0.04f, 0.05f, 0.08f);
        RenderSettings.ambientIntensity = 1.0f;

        // Global Reflection Probe
        GameObject probeGo = new GameObject("Global Reflection Probe");
        probeGo.transform.SetParent(lightingRoot.transform);
        probeGo.transform.position = new Vector3(0f, 3.5f, 0f);
        ReflectionProbe probe = probeGo.AddComponent<ReflectionProbe>();
        probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime;
        probe.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.OnAwake;
        probe.size = new Vector3(120f, 30f, 140f);

        // 2. Instantiate Environment 3D Model
        GameObject envPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
        if (envPrefab == null)
        {
            envPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BlendPath);
        }

        if (envPrefab != null)
        {
            GameObject envInstance = (GameObject)PrefabUtility.InstantiatePrefab(envPrefab);
            envInstance.name = "NewtonEnvironment";
            envInstance.transform.position = Vector3.zero;
            envInstance.transform.rotation = Quaternion.identity;
            envInstance.transform.localScale = Vector3.one;

            // Disable any internal model cameras (e.g. Walkthrough_Camera)
            Camera[] modelCameras = envInstance.GetComponentsInChildren<Camera>(true);
            foreach (Camera mc in modelCameras)
            {
                Debug.Log($"[Newton VR] Disabled model-imported camera: '{mc.gameObject.name}'");
                mc.enabled = false;
                mc.gameObject.SetActive(false);
            }

            // Generate MeshColliders on solid meshes
            MeshFilter[] meshFilters = envInstance.GetComponentsInChildren<MeshFilter>(true);
            int collidersAdded = 0;
            foreach (MeshFilter mf in meshFilters)
            {
                if (mf.sharedMesh != null && mf.GetComponent<Collider>() == null)
                {
                    string lowerName = mf.gameObject.name.ToLower();
                    if (!lowerName.Contains("glow") && !lowerName.Contains("particle") && !lowerName.Contains("beam"))
                    {
                        mf.gameObject.AddComponent<MeshCollider>();
                        collidersAdded++;
                    }
                }
            }
            Debug.Log($"[Newton VR] Added {collidersAdded} colliders to environment meshes.");

            // 2b. Configure Prominent Futuristic Signs outside Lab 1, Lab 2, and Lab 3
            ConfigureFuturisticSigns(envInstance);

            // 2c. Add Newton's First Law Experiment Model into dedicated Lab 1 room
            Transform station1Target = FindLab1StationTarget(envInstance);

            if (station1Target != null)
            {
                GameObject expInstance = null;
                Transform existingExp = station1Target.Find("NewtonsFirstLawExperimentModel");
                if (existingExp != null)
                {
                    expInstance = existingExp.gameObject;
                }
                else if (station1Target.Find("Cart Chassis") != null)
                {
                    expInstance = station1Target.gameObject;
                }
                else
                {
                    foreach (Transform child in station1Target)
                    {
                        child.gameObject.SetActive(false);
                    }

                    GameObject expPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Environment/NewtonsFirstLawExp.fbx");
                    if (expPrefab == null)
                    {
                        expPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Environment/Newtonsfirstlawexpmodel.blend");
                    }

                    if (expPrefab != null)
                    {
                        expInstance = (GameObject)PrefabUtility.InstantiatePrefab(expPrefab);
                        expInstance.name = "NewtonsFirstLawExperimentModel";
                        expInstance.transform.SetParent(station1Target, false);
                        expInstance.transform.localPosition = Vector3.zero;
                        expInstance.transform.localRotation = Quaternion.identity;
                        // Scale factor 0.01 compensates for FBX 100x parent unit scale so model is true 1:1 meter scale
                        expInstance.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                    }
                }

                if (expInstance != null)
                {
                    // Apply custom PBR & Emissive materials matching Lab 2's dark navy, cyan, violet, liquid-glass aesthetic
                    Newton1MaterialsSetup.ApplyMaterialsToModel(expInstance);

                    // Add colliders to experiment model meshes
                    foreach (MeshFilter emf in expInstance.GetComponentsInChildren<MeshFilter>(true))
                    {
                        if (emf.sharedMesh != null && emf.GetComponent<Collider>() == null)
                        {
                            string lName = emf.gameObject.name.ToLower();
                            if (!lName.Contains("glow") && !lName.Contains("text") && !lName.Contains("label") && !lName.Contains("callout"))
                            {
                                emf.gameObject.AddComponent<MeshCollider>();
                            }
                        }
                    }

                    // Attach interactive simulation controller
                    if (expInstance.GetComponent<NewtonsFirstLawExperiment>() == null)
                    {
                        expInstance.AddComponent<NewtonsFirstLawExperiment>();
                    }

                    // Dedicated Spot/Point Light for Lab 1 Experiment Desk
                    Transform existingLight = station1Target.Find("Lab 1 Experiment Desk Spotlight");
                    if (existingLight == null)
                    {
                        GameObject stationLightGo = new GameObject("Lab 1 Experiment Desk Spotlight");
                        stationLightGo.transform.SetParent(station1Target, false);
                        stationLightGo.transform.localPosition = new Vector3(0f, 4.2f, 0f);
                        Light sLight = stationLightGo.AddComponent<Light>();
                        sLight.type = LightType.Point;
                        sLight.color = new Color(0.96f, 0.98f, 1.0f);
                        sLight.intensity = 1.35f;
                        sLight.range = 16f;
                        sLight.shadows = LightShadows.Soft;
                    }

                    Debug.Log($"[Newton VR] Successfully installed Newton's First Law apparatus into Lab 1 at world position {station1Target.position}!");
                }
                else
                {
                    Debug.LogWarning("[Newton VR] NewtonsFirstLawExp model asset not found!");
                }
            }
            else
            {
                Debug.LogWarning("[Newton VR] Lab 1 station target not found in environment hierarchy!");
            }

            // 2d. Add Newton's Second Law Experiment Model into dedicated Lab 2 room
            Transform station2Target = FindLab2StationTarget(envInstance);

            if (station2Target != null)
            {
                // Check if NewtonsSecondLawExperimentModel or Cart A already exists
                GameObject exp2Instance = null;
                Transform existingExp = station2Target.Find("NewtonsSecondLawExperimentModel");
                if (existingExp != null)
                {
                    exp2Instance = existingExp.gameObject;
                }
                else if (station2Target.Find("Cart A - 1kg_Chassis") != null)
                {
                    exp2Instance = station2Target.gameObject;
                }
                else
                {
                    foreach (Transform child in station2Target)
                    {
                        child.gameObject.SetActive(false);
                    }

                    GameObject exp2Prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Environment/NewtonsSecondLawExp.fbx");
                    if (exp2Prefab == null)
                    {
                        exp2Prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Environment/NewtonsSecondLawExp.blend");
                    }

                    if (exp2Prefab != null)
                    {
                        exp2Instance = (GameObject)PrefabUtility.InstantiatePrefab(exp2Prefab);
                        exp2Instance.name = "NewtonsSecondLawExperimentModel";
                        exp2Instance.transform.SetParent(station2Target, false);
                        exp2Instance.transform.localPosition = Vector3.zero;
                        exp2Instance.transform.localRotation = Quaternion.identity;
                        // Scale factor 0.01 compensates for FBX 100x parent unit scale so model is true 1:1 meter scale
                        exp2Instance.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                    }
                }

                if (exp2Instance != null)
                {
                    // Apply custom PBR & Emissive materials matching Lab 1's dark navy, cyan, violet, liquid-glass aesthetic
                    Newton2MaterialsSetup.ApplyMaterialsToModel(exp2Instance);

                    // Add colliders to experiment model meshes
                    foreach (MeshFilter emf in exp2Instance.GetComponentsInChildren<MeshFilter>(true))
                    {
                        if (emf.sharedMesh != null && emf.GetComponent<Collider>() == null)
                        {
                            string lName = emf.gameObject.name.ToLower();
                            if (!lName.Contains("glow") && !lName.Contains("text") && !lName.Contains("label") && !lName.Contains("caption") && !lName.Contains("heading") && !lName.Contains("line"))
                            {
                                emf.gameObject.AddComponent<MeshCollider>();
                            }
                        }
                    }

                    // Attach interactive simulation controller
                    if (exp2Instance.GetComponent<NewtonsSecondLawExperiment>() == null)
                    {
                        exp2Instance.AddComponent<NewtonsSecondLawExperiment>();
                    }

                    // Dedicated Spot/Point Light for Lab 2 Experiment Desk
                    Transform existingLight = station2Target.Find("Lab 2 Experiment Desk Spotlight");
                    if (existingLight == null)
                    {
                        GameObject stationLightGo = new GameObject("Lab 2 Experiment Desk Spotlight");
                        stationLightGo.transform.SetParent(station2Target, false);
                        stationLightGo.transform.localPosition = new Vector3(0f, 4.2f, 0f);
                        Light sLight = stationLightGo.AddComponent<Light>();
                        sLight.type = LightType.Point;
                        sLight.color = new Color(0.96f, 0.98f, 1.0f);
                        sLight.intensity = 1.35f;
                        sLight.range = 16f;
                        sLight.shadows = LightShadows.Soft;
                    }

                    Debug.Log($"[Newton VR] Successfully installed Newton's Second Law apparatus into Lab 2 at world position {station2Target.position}!");
                }
                else
                {
                    Debug.LogWarning("[Newton VR] NewtonsSecondLawExp model asset not found!");
                }
            }
            else
            {
                Debug.LogWarning("[Newton VR] Lab 2 station target not found in environment hierarchy!");
            }

            // 2e. Add Newton's Third Law Experiment Model into dedicated Lab 3 room
            Transform station3Target = FindLab3StationTarget(envInstance);

            if (station3Target != null)
            {
                GameObject exp3Instance = null;
                Transform existingExp = station3Target.Find("NewtonsThirdLawExperimentModel");
                if (existingExp != null)
                {
                    exp3Instance = existingExp.gameObject;
                }
                else if (station3Target.Find("Cart A Cyan Chassis") != null || station3Target.Find("PUSH LEFT Button") != null)
                {
                    exp3Instance = station3Target.gameObject;
                }
                else
                {
                    foreach (Transform child in station3Target)
                    {
                        child.gameObject.SetActive(false);
                    }

                    GameObject exp3Prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Environment/NewtonsThirdLawExp.fbx");
                    if (exp3Prefab == null)
                    {
                        exp3Prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Environment/NewtonsThirdLaw_Experiment3_VR.blend");
                    }

                    if (exp3Prefab != null)
                    {
                        exp3Instance = (GameObject)PrefabUtility.InstantiatePrefab(exp3Prefab);
                        exp3Instance.name = "NewtonsThirdLawExperimentModel";
                        exp3Instance.transform.SetParent(station3Target, false);
                        exp3Instance.transform.localPosition = Vector3.zero;
                        exp3Instance.transform.localRotation = Quaternion.identity;
                        exp3Instance.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                    }
                }

                if (exp3Instance != null)
                {
                    // Apply custom PBR & Emissive materials matching Lab 1 & 2's dark navy, cyan, violet, liquid-glass aesthetic
                    Newton3MaterialsSetup.ApplyMaterialsToModel(exp3Instance);

                    // Add colliders to experiment model meshes
                    foreach (MeshFilter emf in exp3Instance.GetComponentsInChildren<MeshFilter>(true))
                    {
                        if (emf.sharedMesh != null && emf.GetComponent<Collider>() == null)
                        {
                            string lName = emf.gameObject.name.ToLower();
                            if (!lName.Contains("glow") && !lName.Contains("text") && !lName.Contains("label") && !lName.Contains("title") && !lName.Contains("equation") && !lName.Contains("line"))
                            {
                                emf.gameObject.AddComponent<MeshCollider>();
                            }
                        }
                    }

                    // Attach interactive simulation controller
                    if (exp3Instance.GetComponent<NewtonsThirdLawExperiment>() == null)
                    {
                        exp3Instance.AddComponent<NewtonsThirdLawExperiment>();
                    }

                    // Dedicated Spot/Point Light for Lab 3 Experiment Desk
                    Transform existingLight = station3Target.Find("Lab 3 Experiment Desk Spotlight");
                    if (existingLight == null)
                    {
                        GameObject stationLightGo = new GameObject("Lab 3 Experiment Desk Spotlight");
                        stationLightGo.transform.SetParent(station3Target, false);
                        stationLightGo.transform.localPosition = new Vector3(0f, 4.2f, 0f);
                        Light sLight = stationLightGo.AddComponent<Light>();
                        sLight.type = LightType.Point;
                        sLight.color = new Color(0.96f, 0.98f, 1.0f);
                        sLight.intensity = 1.35f;
                        sLight.range = 16f;
                        sLight.shadows = LightShadows.Soft;
                    }

                    Debug.Log($"[Newton VR] Successfully installed Newton's Third Law apparatus into Lab 3 at world position {station3Target.position}!");
                }
                else
                {
                    Debug.LogWarning("[Newton VR] NewtonsThirdLawExp model asset not found!");
                }
            }
            else
            {
                Debug.LogWarning("[Newton VR] Lab 3 station target not found in environment hierarchy!");
            }
        }
        else
        {
            Debug.LogWarning("[Newton VR] Environment model not imported yet: " + FbxPath);
        }

        // 3. Construct Complete VR Rig & Desktop Player
        GameObject player = new GameObject("Player");
        // Player starts in MainLobby on open floor in front of Newtonian Lab entrance, facing the entrance (North, -Z)
        player.transform.position = new Vector3(0f, 1.2f, -10.0f);
        player.transform.rotation = Quaternion.Euler(0f, 180f, 0f); // Face North (-Z)

        CharacterController cc = player.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.4f;
        cc.center = new Vector3(0f, 0.9f, 0f);
        cc.stepOffset = 0.35f;
        cc.slopeLimit = 45f;
        cc.skinWidth = 0.05f;
        cc.minMoveDistance = 0f;

        PlayerController pc = player.AddComponent<PlayerController>();

        // Camera Offset node
        GameObject offsetGo = new GameObject("CameraOffset");
        offsetGo.transform.SetParent(player.transform, false);
        offsetGo.transform.localPosition = Vector3.zero;
        pc.cameraOffset = offsetGo.transform;

        // 4. Main Camera
        GameObject camObj = new GameObject("Main Camera");
        camObj.tag = "MainCamera";
        camObj.transform.SetParent(offsetGo.transform, false);
        camObj.transform.localPosition = new Vector3(0f, 1.7f, 0f);

        Camera cam = camObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.nearClipPlane = 0.08f;
        cam.farClipPlane = 1000f;
        cam.fieldOfView = 65f;
        cam.depth = 1;

        camObj.AddComponent<AudioListener>();

        // Color Grading Effect
        ColorGradingEffect grading = camObj.AddComponent<ColorGradingEffect>();
        grading.exposure = 0.05f;
        grading.contrast = 1.05f;
        grading.saturation = 1.05f;
        grading.temperature = 0.02f;
        grading.vignetteIntensity = 0.15f;

        pc.playerCamera = cam;

        // 5. Left VR Controller
        GameObject leftCtrlGo = new GameObject("LeftHandController");
        leftCtrlGo.transform.SetParent(offsetGo.transform, false);
        leftCtrlGo.transform.localPosition = new Vector3(-0.25f, 1.25f, 0.35f);
        VRControllerVisual leftVis = leftCtrlGo.AddComponent<VRControllerVisual>();
        leftVis.hand = VRControllerVisual.HandType.Left;
        pc.leftHandController = leftCtrlGo.transform;

        // 6. Right VR Controller
        GameObject rightCtrlGo = new GameObject("RightHandController");
        rightCtrlGo.transform.SetParent(offsetGo.transform, false);
        rightCtrlGo.transform.localPosition = new Vector3(0.25f, 1.25f, 0.35f);
        VRControllerVisual rightVis = rightCtrlGo.AddComponent<VRControllerVisual>();
        rightVis.hand = VRControllerVisual.HandType.Right;
        pc.rightHandController = rightCtrlGo.transform;

        // 7. VR Teleportation & Destination Menu Controller
        VRTeleportController tc = player.AddComponent<VRTeleportController>();
        tc.characterController = cc;
        tc.playerCamera = cam;
        tc.leftHandController = leftCtrlGo.transform;
        tc.rightHandController = rightCtrlGo.transform;
        tc.lab1SpawnPosition = new Vector3(0f, 1.2f, -41.0f);
        tc.lab1SpawnRotation = new Vector3(0f, 180f, 0f);
        tc.lab2SpawnPosition = new Vector3(-41.8f, 1.2f, 0f);
        tc.lab2SpawnRotation = new Vector3(0f, -90f, 0f);
        tc.lab3SpawnPosition = new Vector3(42.0f, 1.2f, 0f);
        tc.lab3SpawnRotation = new Vector3(0f, 90f, 0f);
        tc.mainLobbySpawnPosition = new Vector3(0f, 1.2f, 2.0f);
        tc.mainLobbySpawnRotation = new Vector3(0f, 180f, 0f);

        // 8. Save Scene and configure Build Settings
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorBuildSettingsScene[] buildScenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene(ScenePath, true)
        };
        EditorBuildSettings.scenes = buildScenes;

        Debug.Log($"[Newton VR] Reorganized environment scene successfully built at '{ScenePath}'!");

        // 9. Capture Verification Screenshots after next editor update tick
        // (scene must be fully serialized and re-loaded before rendering)
        EditorApplication.delayCall += () =>
        {
            EditorApplication.delayCall += () =>
            {
                EditorApplication.delayCall += CaptureVerificationScreenshots;
            };
        };
    }

    private static Transform FindLab1StationTarget(GameObject envInstance)
    {
        // Search for Station1_FirstLaw under Lab1_FirstLaw
        Transform lab1 = null;
        foreach (Transform t in envInstance.GetComponentsInChildren<Transform>(true))
        {
            if (t.name.Contains("Lab1"))
            {
                lab1 = t;
                break;
            }
        }

        if (lab1 != null)
        {
            foreach (Transform t in lab1.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "Station1_FirstLaw")
                {
                    return t;
                }
            }
            return lab1;
        }

        // If Lab1 node itself has station name
        foreach (Transform t in envInstance.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == "Station1_FirstLaw")
            {
                return t;
            }
        }

        return null;
    }

    private static Transform FindLab2StationTarget(GameObject envInstance)
    {
        // Search for Station2_SecondLaw under BriefingRoom or entire environment
        foreach (Transform t in envInstance.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == "Station2_SecondLaw" || t.name.Contains("Station2"))
            {
                return t;
            }
        }

        // Fallback: look for BriefingRoom / Lab2 node
        foreach (Transform t in envInstance.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == "BriefingRoom" || t.name.Contains("BriefingRoom") || t.name.Contains("Lab2"))
            {
                return t;
            }
        }

        return null;
    }

    private static Transform FindLab3StationTarget(GameObject envInstance)
    {
        // Search for Station3_ThirdLaw under ResultsRoom or entire environment
        foreach (Transform t in envInstance.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == "Station3_ThirdLaw" || t.name.Contains("Station3"))
            {
                return t;
            }
        }

        // Fallback: look for ResultsRoom / Lab3 node
        foreach (Transform t in envInstance.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == "ResultsRoom" || t.name.Contains("ResultsRoom") || t.name.Contains("Lab3"))
            {
                return t;
            }
        }

        return null;
    }

    private static void ConfigureFuturisticSigns(GameObject envInstance)
    {
        // Deactivate all placeholder/FBX-imported text signs
        foreach (Transform t in envInstance.GetComponentsInChildren<Transform>(true))
        {
            if (t.name.StartsWith("Sign.") || t.name == "Sign" || t.name.StartsWith("SignBG"))
            {
                t.gameObject.SetActive(false);
            }
        }

        Color cyanGlow = new Color(0.05f, 0.85f, 1.0f);

        // Sign 1: LAB 1 — Outside North Entrance (at Z = -15.85, facing +Z towards central MainLobby)
        CreateFuturisticSign(
            envInstance.transform,
            new Vector3(0f, 4.38f, -15.85f),
            Quaternion.Euler(0f, 180f, 0f), // Front of TextMesh faces +Z (towards viewer in MainLobby)
            "EXPERIMENT 1",
            "NEWTON'S FIRST LAW",
            cyanGlow
        );

        // Sign 2: LAB 2 — Outside East Entrance (at X = -15.85, facing +X towards central MainLobby)
        CreateFuturisticSign(
            envInstance.transform,
            new Vector3(-15.85f, 4.38f, 0f),
            Quaternion.Euler(0f, -90f, 0f), // Front of TextMesh faces +X (towards viewer in MainLobby)
            "EXPERIMENT 2",
            "NEWTON'S SECOND LAW",
            cyanGlow
        );

        // Sign 3: LAB 3 — Outside West Entrance (at X = +15.85, facing -X towards central MainLobby)
        CreateFuturisticSign(
            envInstance.transform,
            new Vector3(15.85f, 4.38f, 0f),
            Quaternion.Euler(0f, 90f, 0f), // Front of TextMesh faces -X (towards viewer in MainLobby)
            "EXPERIMENT 3",
            "NEWTON'S THIRD LAW",
            cyanGlow
        );

        Debug.Log("[Newton VR] Prominent futuristic signs created outside Lab 1, Lab 2, and Lab 3.");
    }

    private static void CreateFuturisticSign(Transform parent, Vector3 worldPos, Quaternion worldRot, string experimentNum, string lawTitle, Color glowColor)
    {
        GameObject signRoot = new GameObject($"FuturisticSign_{experimentNum.Replace(" ", "")}");
        signRoot.transform.SetParent(parent, true);
        signRoot.transform.position = worldPos;
        signRoot.transform.rotation = worldRot;

        // Dark Navy Backing Panel
        GameObject backPanel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        backPanel.name = "Sign_DarkNavyBackplate";
        backPanel.transform.SetParent(signRoot.transform, false);
        backPanel.transform.localPosition = Vector3.zero;
        backPanel.transform.localScale = new Vector3(5.2f, 1.35f, 0.08f);
        Collider bc = backPanel.GetComponent<Collider>();
        if (bc) Object.DestroyImmediate(bc);
        Material navyMat = Newton1MaterialsSetup.GetOrCreateMaterial("Sign_DarkNavy", new Color(0.015f, 0.035f, 0.08f), 0.3f, 0.85f, Color.black);
        backPanel.GetComponent<MeshRenderer>().sharedMaterial = navyMat;

        // Glowing Cyan Frame Trim
        GameObject trim = GameObject.CreatePrimitive(PrimitiveType.Cube);
        trim.name = "Sign_CyanFrameTrim";
        trim.transform.SetParent(signRoot.transform, false);
        trim.transform.localPosition = new Vector3(0f, 0f, -0.01f);
        trim.transform.localScale = new Vector3(5.35f, 1.45f, 0.04f);
        Collider tc = trim.GetComponent<Collider>();
        if (tc) Object.DestroyImmediate(tc);
        Material trimMat = Newton1MaterialsSetup.GetOrCreateMaterial("Sign_CyanFrameMat", glowColor * 0.8f, 0.1f, 0.9f, glowColor * 1.6f);
        trim.GetComponent<MeshRenderer>().sharedMaterial = trimMat;

        // Liquid-Glass Reflective Front Plate
        GameObject glassPanel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        glassPanel.name = "Sign_LiquidGlassPlate";
        glassPanel.transform.SetParent(signRoot.transform, false);
        glassPanel.transform.localPosition = new Vector3(0f, 0f, 0.045f);
        glassPanel.transform.localScale = new Vector3(5.05f, 1.25f, 0.02f);
        Collider gc = glassPanel.GetComponent<Collider>();
        if (gc) Object.DestroyImmediate(gc);
        Material glassMat = Newton1MaterialsSetup.GetOrCreateMaterial("Sign_LiquidGlassMat", new Color(0.04f, 0.12f, 0.22f, 0.75f), 0.2f, 0.98f, glowColor * 0.18f, true);
        glassPanel.GetComponent<MeshRenderer>().sharedMaterial = glassMat;

        // Line 1: EXPERIMENT X (Vivid Glowing Cyan)
        GameObject line1Obj = new GameObject("Text_ExperimentNumber");
        line1Obj.transform.SetParent(signRoot.transform, false);
        line1Obj.transform.localPosition = new Vector3(0f, 0.30f, 0.07f);
        TextMesh tm1 = line1Obj.AddComponent<TextMesh>();
        tm1.text = experimentNum;
        tm1.fontSize = 52;
        tm1.characterSize = 0.052f;
        tm1.alignment = TextAlignment.Center;
        tm1.anchor = TextAnchor.MiddleCenter;
        tm1.color = glowColor;
        tm1.fontStyle = FontStyle.Bold;

        // Glowing Cyan Divider Line
        GameObject divider = GameObject.CreatePrimitive(PrimitiveType.Cube);
        divider.name = "Sign_DividerLine";
        divider.transform.SetParent(signRoot.transform, false);
        divider.transform.localPosition = new Vector3(0f, 0.02f, 0.07f);
        divider.transform.localScale = new Vector3(4.2f, 0.025f, 0.01f);
        Collider dc = divider.GetComponent<Collider>();
        if (dc) Object.DestroyImmediate(dc);
        divider.GetComponent<MeshRenderer>().sharedMaterial = trimMat;

        // Line 2: LAW TITLE (Crisp UI White)
        GameObject line2Obj = new GameObject("Text_LawTitle");
        line2Obj.transform.SetParent(signRoot.transform, false);
        line2Obj.transform.localPosition = new Vector3(0f, -0.28f, 0.07f);
        TextMesh tm2 = line2Obj.AddComponent<TextMesh>();
        tm2.text = lawTitle;
        tm2.fontSize = 42;
        tm2.characterSize = 0.044f;
        tm2.alignment = TextAlignment.Center;
        tm2.anchor = TextAnchor.MiddleCenter;
        tm2.color = new Color(0.96f, 0.98f, 1.0f);
        tm2.fontStyle = FontStyle.Bold;
    }

    [MenuItem("Newton VR/Capture Verification Screenshots", false, 4)]
    public static void CaptureVerificationScreenshots()
    {
        // Ensure the Newton environment scene is open before rendering
        string currentScenePath = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
        if (currentScenePath != ScenePath)
        {
            if (!System.IO.File.Exists(ScenePath))
            {
                Debug.LogError("[Newton VR] Scene not found at " + ScenePath + ". Run 'Newton VR > Build Scene' first.");
                return;
            }
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        string dir = "Assets/Screenshots";
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        GameObject camGo = new GameObject("ScreenshotCam");
        Camera cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.04f, 0.06f, 0.12f);
        cam.fieldOfView = 65f;
        cam.nearClipPlane = 0.08f;
        cam.farClipPlane = 1000f;

        // ---- Temporarily lower ambient so editor screenshots are not blown out ----
        Color savedSky = RenderSettings.ambientSkyColor;
        Color savedEq = RenderSettings.ambientEquatorColor;
        Color savedGnd = RenderSettings.ambientGroundColor;
        float savedIntensity = RenderSettings.ambientIntensity;

        // Very dark ambient so point / spot lights define the scene mood
        RenderSettings.ambientSkyColor = new Color(0.10f, 0.12f, 0.16f);
        RenderSettings.ambientEquatorColor = new Color(0.05f, 0.06f, 0.09f);
        RenderSettings.ambientGroundColor = new Color(0.02f, 0.02f, 0.04f);
        RenderSettings.ambientIntensity = 0.55f;
        // -------------------------------------------------------------------------

        RenderTexture rt = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 4;
        cam.targetTexture = rt;
        Texture2D screenShot = new Texture2D(1920, 1080, TextureFormat.RGB24, false);

        void RenderAndSave(Vector3 pos, Quaternion rot, string fileName)
        {
            camGo.transform.position = pos;
            camGo.transform.rotation = rot;
            cam.Render();
            RenderTexture.active = rt;
            screenShot.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            screenShot.Apply();
            byte[] bytes = screenShot.EncodeToPNG();
            string path = $"{dir}/{fileName}.png";
            File.WriteAllBytes(path, bytes);
            Debug.Log($"[Newton VR] Saved screenshot: '{path}'");
        }

        // Shot 1: Standing in central MainLobby looking North directly at Lab 1 entrance and prominent sign
        // Player at (0,1.2,0), North wall at Z=-15.85. Camera at +Z side facing -Z to see the North wall + sign.
        // Use Z=+6 so camera is well inside the lobby with full view of the north wall arch and sign.
        RenderAndSave(new Vector3(0f, 1.65f, 6f), Quaternion.Euler(2f, 180f, 0f), "CentralHall_Facing_Lab1");

        // Shot 1b: Exact initial player spawn view in MainLobby (open floor at Z=-10.0, outside globe, directly facing Newtonian Lab entrance)
        RenderAndSave(new Vector3(0f, 1.65f, -10.0f), Quaternion.Euler(0f, 180f, 0f), "MainLobby_PlayerSpawn_View");

        // Shot 2: Standing in central MainLobby looking East at Lab 2 entrance and sign (East wall at X=-15.85)
        // Camera offset slightly West (+X) looking East (-90 deg on Y)
        RenderAndSave(new Vector3(4f, 1.65f, 0f), Quaternion.Euler(2f, -90f, 0f), "CentralHall_Facing_Lab2");

        // Shot 3: Standing in central MainLobby looking West at Lab 3 entrance and sign (West wall at X=+15.85)
        // Camera offset slightly East (-X) looking West (+90 deg on Y)
        RenderAndSave(new Vector3(-4f, 1.65f, 0f), Quaternion.Euler(2f, 90f, 0f), "CentralHall_Facing_Lab3");

        // Shot 4: Inside Lab 1 – camera at Z=-38 facing into Lab1 (Z=-48), tilted down 12 degrees to see full apparatus
        // The Blender apparatus occupies roughly X=-9..+9, Y=-2..+5 (in Blender), translating to Unity X=-9..9, Y=-2..5, Z=-48
        // With scale 0.01 on the FBX instance... apparatus coords get scaled down. Let's use Z=-37 pulling back further.
        RenderAndSave(new Vector3(0f, 2.2f, -38f), Quaternion.Euler(15f, 180f, 0f), "Inside_Lab1_FirstLaw");

        // Shot 5: Wide side-diagonal view from inside Lab 1 showing full track length
        RenderAndSave(new Vector3(-4f, 2.0f, -39f), Quaternion.Euler(12f, 148f, 0f), "Inside_Lab1_Apparatus_Detail");

        // Shot 6: Overview of the Central Exhibition Hall from South entrance – camera at South, facing North
        // South wall at Z=+15.85 so camera at Z=+12 facing 180 (North)
        RenderAndSave(new Vector3(0f, 1.65f, 12f), Quaternion.Euler(2f, 180f, 0f), "CentralHall_Overview_From_Entrance");

        // Shot 7: Inside Lab 2 – exact player spawn position in front of apparatus (X=-43.2f, Y=1.7f, Z=0f), facing directly toward the experiment (-90 deg on Y)
        RenderAndSave(new Vector3(-43.2f, 1.7f, 0f), Quaternion.Euler(0f, -90f, 0f), "Inside_Lab2_SecondLaw");

        // Shot 8: Detail view of Newton's Second Law Apparatus in Lab 2
        RenderAndSave(new Vector3(-43.5f, 1.8f, 2.2f), Quaternion.Euler(14f, -135f, 0f), "Inside_Lab2_Apparatus_Detail");

        // Shot 9: Inside Lab 3 – exact player spawn position in front of apparatus (X=41.8f, Y=1.7f, Z=0f), facing directly toward experiment (+90 deg on Y)
        RenderAndSave(new Vector3(41.8f, 1.7f, 0f), Quaternion.Euler(0f, 90f, 0f), "Inside_Lab3_ThirdLaw");

        // Shot 10: Detail view of Newton's Third Law Apparatus in Lab 3
        RenderAndSave(new Vector3(43.5f, 1.8f, 2.2f), Quaternion.Euler(14f, 45f, 0f), "Inside_Lab3_Apparatus_Detail");

        // ---- Restore ambient settings ----
        RenderSettings.ambientSkyColor = savedSky;
        RenderSettings.ambientEquatorColor = savedEq;
        RenderSettings.ambientGroundColor = savedGnd;
        RenderSettings.ambientIntensity = savedIntensity;
        // ----------------------------------

        RenderTexture.active = null;
        cam.targetTexture = null;
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(screenShot);
        Object.DestroyImmediate(camGo);

        AssetDatabase.Refresh();
        Debug.Log("[Newton VR] All verification screenshots captured successfully.");
    }

    [MenuItem("Newton VR/Open Environment Scene", false, 2)]
    public static void OpenEnvironmentScene()
    {
        if (File.Exists(ScenePath))
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }
        else
        {
            BuildEnvironmentScene();
        }
    }

    [MenuItem("Newton VR/Align Scene View to Player", false, 3)]
    public static void AlignSceneViewToPlayer()
    {
        GameObject player = GameObject.Find("Player");
        if (player != null && SceneView.lastActiveSceneView != null)
        {
            SceneView sv = SceneView.lastActiveSceneView;
            sv.pivot = player.transform.position + Vector3.up * 1.7f;
            sv.rotation = player.transform.rotation;
            sv.Repaint();
            Debug.Log("[Newton VR] Scene View aligned to Player position.");
        }
    }
}
#endif
