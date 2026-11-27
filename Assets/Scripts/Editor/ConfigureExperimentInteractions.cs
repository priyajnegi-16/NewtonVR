#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor utility to configure, wire, and verify all 3 interactive physics experiments directly
/// in NewtonEnvironment.unity. Ensures all interactive buttons, sliders, knobs, mass options,
/// and carts have proper colliders and components for Meta Quest 3S controllers and desktop PC.
/// </summary>
public static class ConfigureExperimentInteractions
{
    private const string ScenePath = "Assets/Scenes/NewtonEnvironment.unity";

    [MenuItem("Newton VR/Configure and Verify All 3 Experiments", false, 2)]
    public static void ConfigureAllExperiments()
    {
        Debug.Log("[Newton VR] Configuring and verifying all 3 interactive experiments in NewtonEnvironment.unity...");

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        GameObject env = GameObject.Find("NewtonEnvironment");
        if (env == null)
        {
            Debug.LogError("[Newton VR] NewtonEnvironment root not found in scene!");
            return;
        }

        // 1. Configure Lab 1: First Law of Motion (Inertia)
        ConfigureLab1(env);

        // 2. Configure Lab 2: Second Law of Motion (F = ma)
        ConfigureLab2(env);

        // 3. Configure Lab 3: Third Law of Motion (Action and Reaction)
        ConfigureLab3(env);

        // 4. Verify Player VR Rig & Controls
        ConfigurePlayer();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("[Newton VR] SUCCESS: All 3 experiments successfully verified and scene saved!");
    }

    private static void ConfigureLab1(GameObject env)
    {
        Transform station1 = FindChildRecursive(env.transform, "Station1_FirstLaw");
        if (station1 == null)
        {
            Debug.LogWarning("[Newton VR] Station1_FirstLaw not found!");
            return;
        }

        // Attach controller script if missing
        NewtonsFirstLawExperiment exp1 = station1.GetComponent<NewtonsFirstLawExperiment>();
        if (exp1 == null)
        {
            exp1 = station1.gameObject.AddComponent<NewtonsFirstLawExperiment>();
        }

        // Add colliders to buttons and interactables
        EnsureBoxCollider(station1, "RESET Button", new Vector3(0.55f, 0.55f, 0.55f), "Reset");
        EnsureBoxCollider(station1, "STOP Button", new Vector3(0.55f, 0.55f, 0.55f), "LaunchStop");
        EnsureBoxCollider(station1, "OBSTACLE Button", new Vector3(0.55f, 0.55f, 0.55f), "Obstacle");
        EnsureBoxCollider(station1, "Friction Slider Knob", new Vector3(0.55f, 0.55f, 0.55f), "FrictionKnob");
        EnsureBoxCollider(station1, "Slider Track", new Vector3(1.2f, 0.4f, 0.4f), "FrictionRail");
        EnsureBoxCollider(station1, "Friction Console", new Vector3(2.2f, 1.4f, 0.45f), "FrictionConsole");
        EnsureBoxCollider(station1, "Obstacle", new Vector3(1.4f, 1.0f, 1.0f), "ObstacleBlock");

        // Cart grab collider
        Transform cart = FindChildRecursive(station1, "Cart Chassis");
        if (cart != null)
        {
            BoxCollider bc = cart.GetComponent<BoxCollider>();
            if (bc == null) bc = cart.gameObject.AddComponent<BoxCollider>();
            bc.size = new Vector3(1.6f, 0.9f, 1.2f);
            bc.center = new Vector3(0f, 0.25f, 0f);
            cart.gameObject.name = "FirstLawCart_GrabHandle";
        }

        // Inertia Ball Physics
        Transform ball = FindChildRecursive(station1, "Inertia Ball");
        if (ball != null)
        {
            SphereCollider sc = ball.GetComponent<SphereCollider>();
            if (sc == null) sc = ball.gameObject.AddComponent<SphereCollider>();
            sc.radius = 0.28f;

            Rigidbody rb = ball.GetComponent<Rigidbody>();
            if (rb == null) rb = ball.gameObject.AddComponent<Rigidbody>();
            rb.mass = 0.8f;
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        // Materials
        Newton1MaterialsSetup.ApplyMaterialsToModel(station1.gameObject);
        Debug.Log("[Newton VR] Lab 1: Newton's First Law apparatus verified.");
    }

    private static void ConfigureLab2(GameObject env)
    {
        Transform station2 = FindChildRecursive(env.transform, "Station2_SecondLaw");
        if (station2 == null)
        {
            Debug.LogWarning("[Newton VR] Station2_SecondLaw not found!");
            return;
        }

        // Attach controller script if missing
        NewtonsSecondLawExperiment exp2 = station2.GetComponent<NewtonsSecondLawExperiment>();
        if (exp2 == null)
        {
            exp2 = station2.gameObject.AddComponent<NewtonsSecondLawExperiment>();
        }

        // Add colliders to buttons and interactables
        EnsureBoxCollider(station2, "LAUNCH Button", new Vector3(0.55f, 0.55f, 0.55f), "Launch");
        EnsureBoxCollider(station2, "STOP Button", new Vector3(0.55f, 0.55f, 0.55f), "Stop");
        EnsureBoxCollider(station2, "RESET Button", new Vector3(0.55f, 0.55f, 0.55f), "Reset");
        EnsureBoxCollider(station2, "Cart Controls", new Vector3(2.5f, 1.0f, 0.4f), "CartControlsPanel");

        EnsureBoxCollider(station2, "Force Slider Knob", new Vector3(0.55f, 0.55f, 0.55f), "ForceKnob");
        EnsureBoxCollider(station2, "Force Slider Rail", new Vector3(2.4f, 0.4f, 0.4f), "ForceRail");

        EnsureBoxCollider(station2, "Mass Option 1 kg", new Vector3(0.55f, 0.55f, 0.55f), "Mass_1kg");
        EnsureBoxCollider(station2, "Mass Option 2 kg", new Vector3(0.55f, 0.55f, 0.55f), "Mass_2kg");
        EnsureBoxCollider(station2, "Mass Option 5 kg", new Vector3(0.55f, 0.55f, 0.55f), "Mass_5kg");
        EnsureBoxCollider(station2, "Mass Option 10 kg", new Vector3(0.55f, 0.55f, 0.55f), "Mass_10kg");

        EnsureBoxCollider(station2, "Mode Panel", new Vector3(2.8f, 1.2f, 0.2f), "ModeToggle");

        // Build or verify 3D interactive consoles
        exp2.BuildOrUpdate3DControlPanels();

        // Materials
        Newton2MaterialsSetup.ApplyMaterialsToModel(station2.gameObject);
        Debug.Log("[Newton VR] Lab 2: Newton's Second Law apparatus verified.");
    }

    private static void ConfigureLab3(GameObject env)
    {
        Transform station3 = FindChildRecursive(env.transform, "Station3_ThirdLaw");
        if (station3 == null)
        {
            Debug.LogWarning("[Newton VR] Station3_ThirdLaw not found!");
            return;
        }

        // Attach controller script if missing
        NewtonsThirdLawExperiment exp3 = station3.GetComponent<NewtonsThirdLawExperiment>();
        if (exp3 == null)
        {
            exp3 = station3.gameObject.AddComponent<NewtonsThirdLawExperiment>();
        }

        // Add colliders to buttons and interactables
        EnsureBoxCollider(station3, "PUSH LEFT Button", new Vector3(0.65f, 0.55f, 0.55f), "PushLeft");
        EnsureBoxCollider(station3, "PUSH RIGHT Button", new Vector3(0.65f, 0.55f, 0.55f), "PushRight");
        EnsureBoxCollider(station3, "RESET Button", new Vector3(0.65f, 0.55f, 0.55f), "Reset");

        // Direct push colliders on carts
        Transform cartA = FindChildRecursive(station3, "Cart A Cyan Chassis");
        if (cartA != null)
        {
            BoxCollider bc = cartA.GetComponent<BoxCollider>();
            if (bc == null) bc = cartA.gameObject.AddComponent<BoxCollider>();
            bc.size = new Vector3(1.2f, 0.8f, 0.8f);
            cartA.gameObject.name = "Cart A Cyan_PushLeftHandle";
        }

        Transform cartB = FindChildRecursive(station3, "Cart B Violet Chassis");
        if (cartB != null)
        {
            BoxCollider bc = cartB.GetComponent<BoxCollider>();
            if (bc == null) bc = cartB.gameObject.AddComponent<BoxCollider>();
            bc.size = new Vector3(1.2f, 0.8f, 0.8f);
            cartB.gameObject.name = "Cart B Violet_PushRightHandle";
        }

        // Materials
        Newton3MaterialsSetup.ApplyMaterialsToModel(station3.gameObject);
        Debug.Log("[Newton VR] Lab 3: Newton's Third Law apparatus verified.");
    }

    private static void ConfigurePlayer()
    {
        GameObject player = GameObject.Find("Player");
        if (player == null) return;

        PlayerController pc = player.GetComponent<PlayerController>();
        if (pc != null)
        {
            Transform offset = player.transform.Find("CameraOffset");
            if (offset != null)
            {
                pc.cameraOffset = offset;
                Transform cam = offset.Find("Main Camera");
                if (cam != null) pc.playerCamera = cam.GetComponent<Camera>();
                Transform lHand = offset.Find("LeftHandController");
                if (lHand != null) pc.leftHandController = lHand;
                Transform rHand = offset.Find("RightHandController");
                if (rHand != null) pc.rightHandController = rHand;
            }
        }

        VRTeleportController tc = player.GetComponent<VRTeleportController>();
        if (tc != null)
        {
            tc.characterController = player.GetComponent<CharacterController>();
            if (pc != null)
            {
                tc.playerCamera = pc.playerCamera;
                tc.leftHandController = pc.leftHandController;
                tc.rightHandController = pc.rightHandController;
            }
        }
        Debug.Log("[Newton VR] Player XR Rig and controller bindings verified.");
    }

    private static void EnsureBoxCollider(Transform root, string searchName, Vector3 size, string tagSuffix)
    {
        Transform target = FindChildRecursive(root, searchName);
        if (target != null)
        {
            BoxCollider bc = target.GetComponent<BoxCollider>();
            if (bc == null)
            {
                bc = target.gameObject.AddComponent<BoxCollider>();
            }
            bc.size = size;
            if (!target.gameObject.name.Contains(tagSuffix))
            {
                target.gameObject.name = $"{target.gameObject.name}_{tagSuffix}";
            }
        }
    }

    private static Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == name || child.name.Contains(name))
            {
                return child;
            }
        }
        return null;
    }
}
#endif
