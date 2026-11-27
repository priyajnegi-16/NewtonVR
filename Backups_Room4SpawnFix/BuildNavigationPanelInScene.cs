#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Editor tool that constructs and saves the brand-new, high-fidelity World Space VR Navigation Panel
/// directly into the active scene 'Assets/Scenes/NewtonEnvironment.unity' and as a Prefab.
/// Guarantees the panel physically exists in the scene hierarchy and is immediately visible in Scene View,
/// Game View, and Play mode.
/// </summary>
[InitializeOnLoad]
public static class BuildNavigationPanelInScene
{
    private const string ScenePath = "Assets/Scenes/NewtonEnvironment.unity";
    private const string PrefabPath = "Assets/Prefabs/FloatingVRDestinationPanel.prefab";
    private const string TriggerFile = "Assets/Screenshots/TriggerBuildNavPanel.txt";

    static BuildNavigationPanelInScene()
    {
        EditorApplication.update += CheckAutoTrigger;
    }

    private static void CheckAutoTrigger()
    {
        if (File.Exists(TriggerFile))
        {
            try { File.Delete(TriggerFile); } catch {}
            ExecuteBuildOnce();
        }

        if (File.Exists("Assets/Screenshots/TriggerSpawnTest.txt"))
        {
            try { File.Delete("Assets/Screenshots/TriggerSpawnTest.txt"); } catch {}
            ScreenshotCaptureTool.RunSpawnAndTeleportVerification();
        }

        if (File.Exists("Assets/Screenshots/TriggerQuest3STest.txt"))
        {
            try { File.Delete("Assets/Screenshots/TriggerQuest3STest.txt"); } catch {}
            ScreenshotCaptureTool.RunQuest3SVerificationSuite();
        }

        if (File.Exists("Assets/Screenshots/TriggerDebug.txt"))
        {
            try { File.Delete("Assets/Screenshots/TriggerDebug.txt"); } catch {}
            CheckDebugPanels();
        }
    }

    public static void CheckDebugPanels()
    {
        string rpt = "=== DEBUG PANELS REPORT ===\n";
        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        rpt += $"Total canvases in scene: {canvases.Length}\n";
        foreach (var c in canvases)
        {
            string pName = c.transform.parent != null ? c.transform.parent.name : "null";
            rpt += $"Canvas: '{c.gameObject.name}', Active: {c.gameObject.activeInHierarchy}, Parent: '{pName}', WorldPos: {c.transform.position}, LocalPos: {c.transform.localPosition}\n";
        }

        var panels = Object.FindObjectsByType<FloatingVRDestinationPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        rpt += $"\nTotal FloatingVRDestinationPanel: {panels.Length}\n";
        foreach (var p in panels)
        {
            rpt += $"Panel: '{p.gameObject.name}', Active: {p.gameObject.activeInHierarchy}, WorldPos: {p.transform.position}, Children: {p.transform.childCount}\n";
            for (int i = 0; i < p.transform.childCount; i++)
            {
                Transform child = p.transform.GetChild(i);
                rpt += $"  Child {i}: '{child.name}', Active: {child.gameObject.activeInHierarchy}, LocalPos: {child.localPosition}\n";
            }
        }

        var welcomes = Object.FindObjectsByType<VRWelcomePanel>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        rpt += $"\nTotal VRWelcomePanel: {welcomes.Length}\n";
        foreach (var w in welcomes)
        {
            rpt += $"WelcomePanel: '{w.gameObject.name}', Active: {w.gameObject.activeInHierarchy}, WorldPos: {w.transform.position}, Children: {w.transform.childCount}\n";
            for (int i = 0; i < w.transform.childCount; i++)
            {
                Transform child = w.transform.GetChild(i);
                rpt += $"  Child {i}: '{child.name}', Active: {child.gameObject.activeInHierarchy}, LocalPos: {child.localPosition}\n";
            }
        }

        File.WriteAllText("Assets/Screenshots/DebugReport.txt", rpt);
        Debug.Log(rpt);
    }

    private static void ExecuteBuildOnce()
    {
        // Avoid running during play mode
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        // Check if we need to build or refresh the scene panel
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.path != ScenePath)
        {
            if (File.Exists(ScenePath))
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }
            else
            {
                return;
            }
        }

        BuildPanelHierarchy();
    }

    [MenuItem("Newton VR/Build and Place Navigation Panel in Scene", false, 5)]
    public static void ManualBuild()
    {
        if (File.Exists(ScenePath) && SceneManager.GetActiveScene().path != ScenePath)
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        BuildPanelHierarchy();
    }

    public static void BuildPanelHierarchy()
    {
        Debug.Log("[Newton VR] Constructing Brand-New World Space VR Navigation Panel in Scene...");
        Scene activeScene = SceneManager.GetActiveScene();

        // Thoroughly remove ALL old/duplicate panel roots, prefab instances and canvases in the scene
        var allRoots = activeScene.GetRootGameObjects();
        for (int i = allRoots.Length - 1; i >= 0; i--)
        {
            GameObject go = allRoots[i];
            if (go == null) continue;
            string n = go.name;
            bool isPanelObj = n.Contains("VR_NavigationPanel") || n.Contains("FloatingVRDestinationPanel") ||
                              n.Contains("VR_WorldSpaceNavCanvas") || n.Contains("VR_WelcomePanel") ||
                              n.Contains("VR_WorldSpaceWelcomeCanvas") || n == "Panel_GoldFrame" ||
                              n == "Panel_SciFiFrame" || n == "Btn_Accent" || n == "Btn_Frame";

            if (!isPanelObj && PrefabUtility.IsPartOfPrefabInstance(go))
            {
                string pPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
                if (!string.IsNullOrEmpty(pPath) && (pPath.Contains("FloatingVRDestinationPanel") || pPath.Contains("VRWelcomePanel")))
                {
                    isPanelObj = true;
                }
            }

            if (isPanelObj)
            {
                if (PrefabUtility.IsPartOfPrefabInstance(go))
                {
                    GameObject pRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(go);
                    if (pRoot != null)
                    {
                        PrefabUtility.UnpackPrefabInstance(pRoot, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                        Object.DestroyImmediate(pRoot);
                        continue;
                    }
                }
                Object.DestroyImmediate(go);
            }
        }

        var allGos = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var go in allGos)
        {
            if (go == null) continue;
            string n = go.name;
            if (n.Contains("VR_NavigationPanel") || n.Contains("FloatingVRDestinationPanel") ||
                n.Contains("VR_WorldSpaceNavCanvas") || n.Contains("VR_WelcomePanel") ||
                n.Contains("VR_WorldSpaceWelcomeCanvas") || n == "Panel_GoldFrame" ||
                n == "Panel_SciFiFrame" || n == "Btn_Accent" || n == "Btn_Frame" ||
                n == "VR_NavTab_StableAnchor" || n == "VR_WorldSpaceNavTabCanvas" ||
                n.StartsWith("VR_RoomNavTab_") || n == "Environment_StationaryNavTabs")
            {
                Transform cur = go.transform;
                while (cur.parent != null && cur.parent.name != "Player" && !cur.parent.name.StartsWith("Newton"))
                {
                    cur = cur.parent;
                }
                if (cur != null && cur.gameObject != null)
                {
                    if (PrefabUtility.IsPartOfPrefabInstance(cur.gameObject))
                    {
                        GameObject pRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(cur.gameObject);
                        if (pRoot != null)
                        {
                            PrefabUtility.UnpackPrefabInstance(pRoot, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                            Object.DestroyImmediate(pRoot);
                            continue;
                        }
                    }
                    Object.DestroyImmediate(cur.gameObject);
                }
            }
        }

        var allTmp = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var tmp in allTmp)
        {
            if (tmp == null || tmp.gameObject == null) continue;
            if (tmp.gameObject.name == "TMP_NavSub" || tmp.text == "SELECT ROOM")
            {
                Object.DestroyImmediate(tmp.gameObject);
            }
        }

        // Find Player references in scene
        PlayerController player = Object.FindFirstObjectByType<PlayerController>();
        CharacterController cc = player != null ? player.GetComponent<CharacterController>() : Object.FindFirstObjectByType<CharacterController>();
        Camera cam = player != null ? player.playerCamera : Camera.main;
        if (cam == null) cam = Object.FindFirstObjectByType<Camera>();
        Transform leftHand = player != null ? player.leftHandController : null;
        Transform rightHand = player != null ? player.rightHandController : null;

        // Load LiberationSans SDF font
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        if (font == null) font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (font == null && TMP_Settings.defaultFontAsset != null)
        {
            font = TMP_Settings.defaultFontAsset;
        }

        // Room preview textures for authentic VR card visual
        Texture2D lobbyTex = LoadScreenshotTexture("Assets/Screenshots/MainLobby_PlayerSpawn_View.png");
        Texture2D lab1Tex = LoadScreenshotTexture("Assets/Screenshots/Inside_Lab1_FirstLaw.png");
        Texture2D lab2Tex = LoadScreenshotTexture("Assets/Screenshots/Inside_Lab2_SecondLaw.png");
        Texture2D lab3Tex = LoadScreenshotTexture("Assets/Screenshots/Inside_Lab3_ThirdLaw.png");
        Texture2D room4Tex = LoadScreenshotTexture("Assets/Textures/UI/Room4_Thumbnail.png");

        // 1. Root GameObject in Scene: VR_NavigationPanel
        GameObject panelRoot = new GameObject("VR_NavigationPanel");
        // Position on the player's LEFT (38° left from (0, 0, 5.8), arm's reach 1.35m, chest-to-eye height)
        // Keeps both panels completely clear and unobstructed side-by-side!
        panelRoot.transform.position = new Vector3(0.83f, 1.66f, 4.74f);
        panelRoot.transform.rotation = Quaternion.Euler(10f, 142f, 0f);

        FloatingVRDestinationPanel comp = panelRoot.AddComponent<FloatingVRDestinationPanel>();
        comp.characterController = cc;
        comp.playerCamera = cam;
        comp.leftHandController = leftHand;
        comp.rightHandController = rightHand;
        comp.armReachDistance = 1.35f;
        comp.leftAngleOffset = -38.0f;
        comp.verticalOffset = 0.02f;
        comp.upwardTiltAngle = 10.0f;
        comp.deadzoneAngle = 50.0f;
        comp.deadzoneDistance = 0.85f;

        // 2. World Space Canvas
        GameObject canvasGo = new GameObject("VR_WorldSpaceNavCanvas");
        canvasGo.transform.SetParent(panelRoot.transform, false);
        canvasGo.transform.localPosition = Vector3.zero;
        canvasGo.transform.localRotation = Quaternion.identity;
        canvasGo.transform.localScale = Vector3.one * 0.001f; // 1 unit in Canvas = 1 mm

        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = cam;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;
        scaler.referencePixelsPerUnit = 100f;

        canvasGo.AddComponent<GraphicRaycaster>();

        RectTransform canvasRect = canvasGo.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(680f, 580f);

        // Colors
        Color champagneGold = new Color(0.855f, 0.725f, 0.447f, 1.0f); // #DAB972
        Color warmIvory     = new Color(1.000f, 0.973f, 0.910f, 1.0f); // #FFF8E8
        Color pureWhite     = new Color(1.000f, 1.000f, 1.000f, 1.0f); // #FFFFFF
        Color emeraldGreen  = new Color(0.125f, 0.880f, 0.520f, 1.0f); // #20E085
        Color smokedBronze  = new Color(0.110f, 0.090f, 0.078f, 0.96f); // #1C1714
        Color buttonBase    = new Color(0.160f, 0.135f, 0.118f, 0.98f); // #29221E

        // Outer Champagne-Gold Frame
        GameObject frameObj = CreateUI("Panel_GoldFrame", canvasRect, new Vector2(686f, 586f), Vector2.zero);
        Image frameImg = frameObj.AddComponent<Image>();
        frameImg.color = champagneGold;

        // Main Background (Warm Smoked Bronze)
        GameObject bgObj = CreateUI("Panel_Background", frameObj.transform, new Vector2(680f, 580f), Vector2.zero);
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = smokedBronze;

        // Header Emblem Icon: Geometric Diamond in Champagne Gold
        GameObject emblemObj = CreateUI("Header_Emblem", bgObj.transform, new Vector2(16f, 16f), new Vector2(-300f, 252f));
        emblemObj.transform.localEulerAngles = new Vector3(0f, 0f, 45f);
        Image emblemImg = emblemObj.AddComponent<Image>();
        emblemImg.color = champagneGold;

        // Header Title: NEWTONIAN VR LABORATORY (Bold Pure White)
        GameObject titleObj = CreateUI("Header_Title", bgObj.transform, new Vector2(500f, 34f), new Vector2(-20f, 260f));
        TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
        if (font != null) titleTmp.font = font;
        titleTmp.text = "NEWTONIAN VR LABORATORY";
        titleTmp.fontSize = 26f;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Left;
        titleTmp.color = pureWhite;

        // Subtitle: SELECT A ROOM TO EXPLORE (Champagne Gold)
        GameObject subObj = CreateUI("Header_Subtitle", bgObj.transform, new Vector2(500f, 24f), new Vector2(-20f, 234f));
        TextMeshProUGUI subTmp = subObj.AddComponent<TextMeshProUGUI>();
        if (font != null) subTmp.font = font;
        subTmp.text = "SELECT A ROOM TO EXPLORE";
        subTmp.fontSize = 15f;
        subTmp.fontStyle = FontStyles.Normal;
        subTmp.alignment = TextAlignmentOptions.Left;
        subTmp.color = champagneGold;

        // Upper-Right Close Button (✕)
        GameObject closeObj = CreateUI("Btn_Close", bgObj.transform, new Vector2(40f, 40f), new Vector2(305f, 252f));
        GameObject closeFrame = CreateUI("Close_Frame", closeObj.transform, new Vector2(42f, 42f), Vector2.zero);
        Image cfImg = closeFrame.AddComponent<Image>();
        cfImg.color = champagneGold * 0.8f;
        GameObject closeFill = CreateUI("Close_Fill", closeFrame.transform, new Vector2(38f, 38f), Vector2.zero);
        Image cFillImg = closeFill.AddComponent<Image>();
        cFillImg.color = new Color(0.20f, 0.16f, 0.14f);
        GameObject closeTxt = CreateUI("TMP_Close", closeFill.transform, new Vector2(38f, 38f), Vector2.zero);
        TextMeshProUGUI cTmp = closeTxt.AddComponent<TextMeshProUGUI>();
        if (font != null) cTmp.font = font;
        cTmp.text = "X";
        cTmp.fontSize = 20f;
        cTmp.fontStyle = FontStyles.Bold;
        cTmp.alignment = TextAlignmentOptions.Center;
        cTmp.color = pureWhite;
        BoxCollider closeCol = closeObj.AddComponent<BoxCollider>();
        closeCol.size = new Vector3(60f, 60f, 30f);

        // Five Large Destination Buttons
        float startY = 160f;
        float spacingY = 70f;

        CreateButton(bgObj.transform, "MAIN LOBBY", "Return to the Central Exhibition Hall", new Vector2(0f, startY), font, champagneGold, buttonBase, emeraldGreen, pureWhite, warmIvory, lobbyTex);
        CreateButton(bgObj.transform, "ROOM 1 — NEWTON'S FIRST LAW", "Inertia and Motion", new Vector2(0f, startY - spacingY), font, champagneGold, buttonBase, emeraldGreen, pureWhite, warmIvory, lab1Tex);
        CreateButton(bgObj.transform, "ROOM 2 — NEWTON'S SECOND LAW", "Force, Mass and Acceleration", new Vector2(0f, startY - spacingY * 2), font, champagneGold, buttonBase, emeraldGreen, pureWhite, warmIvory, lab2Tex);
        CreateButton(bgObj.transform, "ROOM 3 — NEWTON'S THIRD LAW", "Action and Reaction", new Vector2(0f, startY - spacingY * 3), font, champagneGold, buttonBase, emeraldGreen, pureWhite, warmIvory, lab3Tex);
        CreateButton(bgObj.transform, "ROOM 4 — OBSERVATION & DATA ANALYSIS", "Review, Compare and Understand", new Vector2(0f, startY - spacingY * 4), font, champagneGold, buttonBase, emeraldGreen, pureWhite, warmIvory, room4Tex);

        // Bottom Replay Narration Button
        GameObject replayObj = CreateUI("Btn_ReplayNarration", bgObj.transform, new Vector2(340f, 38f), new Vector2(0f, -220f));
        GameObject rFrame = CreateUI("Replay_Frame", replayObj.transform, new Vector2(342f, 40f), Vector2.zero);
        Image rfImg = rFrame.AddComponent<Image>();
        rfImg.color = champagneGold * 0.65f;
        GameObject rFill = CreateUI("Replay_Fill", rFrame.transform, new Vector2(338f, 36f), Vector2.zero);
        Image rFillImg = rFill.AddComponent<Image>();
        rFillImg.color = new Color(0.18f, 0.15f, 0.13f);
        GameObject rTxt = CreateUI("TMP_Replay", rFill.transform, new Vector2(338f, 36f), Vector2.zero);
        TextMeshProUGUI rTmp = rTxt.AddComponent<TextMeshProUGUI>();
        if (font != null) rTmp.font = font;
        rTmp.text = "REPLAY AUDIO NARRATION";
        rTmp.fontSize = 14f;
        rTmp.fontStyle = FontStyles.Bold;
        rTmp.alignment = TextAlignmentOptions.Center;
        rTmp.color = warmIvory;
        BoxCollider rCol = replayObj.AddComponent<BoxCollider>();
        rCol.size = new Vector3(340f, 38f, 20f);

        if (player != null)
        {
            Transform pAnchor = player.transform.Find("VR_NavTab_StableAnchor");
            if (pAnchor != null) Object.DestroyImmediate(pAnchor.gameObject);
            Transform pTab = player.transform.Find("VR_WorldSpaceNavTabCanvas");
            if (pTab != null) Object.DestroyImmediate(pTab.gameObject);
        }

        // 3. Four Permanently Stationary Room-Anchored LABS Buttons
        // Placed inside EACH room: MainLobby, Lab 1, Lab 2, Lab 3
        // Anchored to room environment, strictly independent of player or headset!
        GameObject envTabsRoot = new GameObject("Environment_StationaryNavTabs");
        envTabsRoot.transform.position = Vector3.zero;
        envTabsRoot.transform.rotation = Quaternion.identity;

        CreateStationaryRoomNavTab("VR_RoomNavTab_MainLobby", new Vector3(0.38f, 1.55f, 5.25f), Quaternion.Euler(0.0f, 180.0f, 0.0f), envTabsRoot.transform, cam, font);
        CreateStationaryRoomNavTab("VR_RoomNavTab_Lab1", new Vector3(0.38f, 1.55f, -41.55f), Quaternion.Euler(0.0f, 180.0f, 0.0f), envTabsRoot.transform, cam, font);
        CreateStationaryRoomNavTab("VR_RoomNavTab_Lab2", new Vector3(-42.35f, 1.55f, -0.38f), Quaternion.Euler(0.0f, -90.0f, 0.0f), envTabsRoot.transform, cam, font);
        CreateStationaryRoomNavTab("VR_RoomNavTab_Lab3", new Vector3(42.35f, 1.55f, 0.38f), Quaternion.Euler(0.0f, 90.0f, 0.0f), envTabsRoot.transform, cam, font);
        CreateStationaryRoomNavTab("VR_RoomNavTab_Room4", new Vector3(-0.38f, 1.55f, 41.55f), Quaternion.Euler(0.0f, 0.0f, 0.0f), envTabsRoot.transform, cam, font);

        // 4. Save as Prefab cleanly
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }
        if (File.Exists(PrefabPath))
        {
            AssetDatabase.DeleteAsset(PrefabPath);
        }
        PrefabUtility.SaveAsPrefabAssetAndConnect(panelRoot, PrefabPath, InteractionMode.AutomatedAction);
        Debug.Log($"[Newton VR] Saved Navigation Panel Prefab to: {PrefabPath}");

        // 4.5. Construct and place Brand-New Glowing Blue Welcome Panel on the RIGHT
        BuildWelcomePanelHierarchy();

        // 4.6. Construct Room 4 Observation & Data Analysis Centre inside TutorialRoom
        BuildRoom4ObservationHierarchy();

        // 5. Ensure Player and Camera are always cleanly parked at Main Lobby spawn in front of START LAB sign before saving
        if (player != null)
        {
            player.transform.position = new Vector3(0f, 0.0f, 5.80f);
            player.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        }
        if (cam != null)
        {
            cam.transform.localPosition = new Vector3(0f, 1.70f, 0f);
            cam.transform.localRotation = Quaternion.identity;
        }

        // 6. Save Scene
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
        Debug.Log($"[Newton VR] Successfully placed and saved VR_NavigationPanel and VR_WelcomePanel in scene: {ScenePath}");
    }

    [MenuItem("Newton VR/Build and Place Welcome Panel in Scene", false, 6)]
    public static GameObject BuildWelcomePanelHierarchy()
    {
        Debug.Log("[Newton VR] Constructing Grand Futuristic Glowing Blue VR Welcome Panel Centered in Front of Player...");
        Scene activeScene = SceneManager.GetActiveScene();

        // 1. Root GameObject in Scene: VR_WelcomePanel
        GameObject welcomeRoot = new GameObject("VR_WelcomePanel");
        // Centered directly in front of player spawn (0, 0, 5.80) facing (0, 180, 0)
        // Position: (0.00, 1.58, 3.80) facing Euler(8.0, 180.0, 0.0) at natural eye level, distance ~2.0m
        welcomeRoot.transform.position = new Vector3(0.00f, 1.58f, 3.80f);
        welcomeRoot.transform.rotation = Quaternion.Euler(8.0f, 180.0f, 0.0f);

        PlayerController player = Object.FindFirstObjectByType<PlayerController>();
        CharacterController cc = player != null ? player.GetComponent<CharacterController>() : Object.FindFirstObjectByType<CharacterController>();
        Camera cam = player != null ? player.playerCamera : Camera.main;
        if (cam == null) cam = Object.FindFirstObjectByType<Camera>();
        Transform leftHand = player != null ? player.leftHandController : null;
        Transform rightHand = player != null ? player.rightHandController : null;

        VRWelcomePanel comp = welcomeRoot.AddComponent<VRWelcomePanel>();
        comp.characterController = cc;
        comp.playerCamera = cam;
        comp.leftHandController = leftHand;
        comp.rightHandController = rightHand;
        comp.viewingDistance = 2.00f;
        comp.centerHeight = 1.58f;
        comp.upwardTiltAngle = 8.0f;

        // 2. Build World Space Welcome Canvas (1280x800, scale 0.0015 -> ~1.92m wide x 1.20m tall)
        GameObject canvasGo = new GameObject("VR_WorldSpaceWelcomeCanvas");
        canvasGo.transform.SetParent(welcomeRoot.transform, false);
        canvasGo.transform.localPosition = Vector3.zero;
        canvasGo.transform.localRotation = Quaternion.identity;
        canvasGo.transform.localScale = Vector3.one * 0.0015f;

        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = cam;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;
        scaler.referencePixelsPerUnit = 100f;
        canvasGo.AddComponent<GraphicRaycaster>();

        CanvasGroup canvasGroup = canvasGo.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1.0f;

        RectTransform rootRt = canvasGo.GetComponent<RectTransform>();
        rootRt.sizeDelta = new Vector2(1280f, 800f);

        // Load custom textures & font
        Sprite mainFrameSprite = LoadSpriteAsset("Assets/Textures/UI/SciFi_Main_Frame.png");
        Sprite cardFrameSprite = LoadSpriteAsset("Assets/Textures/UI/SciFi_Card_Frame.png");
        Sprite pillFrameSprite = LoadSpriteAsset("Assets/Textures/UI/SciFi_Pill_Frame.png");
        Sprite iconInertia = LoadSpriteAsset("Assets/Textures/UI/Icon_FirstLaw_Inertia.png");
        Sprite iconForce = LoadSpriteAsset("Assets/Textures/UI/Icon_SecondLaw_Force.png");
        Sprite iconActionReaction = LoadSpriteAsset("Assets/Textures/UI/Icon_ThirdLaw_ActionReaction.png");
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        if (font == null) font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (font == null && TMP_Settings.defaultFontAsset != null) font = TMP_Settings.defaultFontAsset;

        Color neonCyan = new Color(0.00f, 0.90f, 1.00f, 1.00f);
        Color iceWhite = new Color(0.85f, 0.96f, 1.00f, 1.00f);
        Color pureWhite = Color.white;
        Color subtextColor = new Color(0.68f, 0.88f, 0.98f, 1.00f);
        Color buttonNormal = new Color(0.015f, 0.110f, 0.200f, 0.96f);

        // 3. Sci-Fi Outer Glowing Frame (1280x800)
        GameObject frameObj = CreateUI("Panel_SciFiFrame", canvasGo.transform, new Vector2(1280f, 800f), Vector2.zero);
        Image frameImg = frameObj.AddComponent<Image>();
        if (mainFrameSprite != null)
        {
            frameImg.sprite = mainFrameSprite;
            frameImg.type = Image.Type.Simple;
            frameImg.color = Color.white;
        }
        else
        {
            frameImg.color = neonCyan;
        }

        // 4. Header: →   W E L C O M E   T O   ←
        GameObject welcomeObj = CreateUI("Header_WelcomeTo", frameObj.transform, new Vector2(750f, 32f), new Vector2(0f, 335f));
        TextMeshProUGUI welcomeTmp = welcomeObj.AddComponent<TextMeshProUGUI>();
        if (font != null) welcomeTmp.font = font;
        welcomeTmp.text = "→   W E L C O M E   T O   ←";
        welcomeTmp.fontSize = 21f;
        welcomeTmp.characterSpacing = 28f;
        welcomeTmp.fontStyle = FontStyles.Bold;
        welcomeTmp.alignment = TextAlignmentOptions.Center;
        welcomeTmp.color = iceWhite;

        // Header: NEWTONIAN VR LABORATORY (Bold Pure White inside orbital ring)
        GameObject titleObj = CreateUI("Header_MainTitle", frameObj.transform, new Vector2(1000f, 110f), new Vector2(0f, 260f));
        TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
        if (font != null) titleTmp.font = font;
        titleTmp.text = "NEWTONIAN VR\nLABORATORY";
        titleTmp.fontSize = 46f;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = pureWhite;
        titleTmp.lineSpacing = -6f;

        // Pill Badge: EXPLORE THE THREE LAWS OF MOTION
        GameObject pillBadgeObj = CreateUI("Header_PillBadge", frameObj.transform, new Vector2(640f, 44f), new Vector2(0f, 175f));
        Image pillBadgeImg = pillBadgeObj.AddComponent<Image>();
        if (pillFrameSprite != null)
        {
            pillBadgeImg.sprite = pillFrameSprite;
            pillBadgeImg.color = Color.white;
        }
        else
        {
            pillBadgeImg.color = new Color(0.00f, 0.25f, 0.40f, 0.85f);
        }

        GameObject pillTextObj = CreateUI("TMP_PillText", pillBadgeObj.transform, new Vector2(620f, 38f), Vector2.zero);
        TextMeshProUGUI pillTmp = pillTextObj.AddComponent<TextMeshProUGUI>();
        if (font != null) pillTmp.font = font;
        pillTmp.text = "EXPLORE THE THREE LAWS OF MOTION";
        pillTmp.fontSize = 18f;
        pillTmp.fontStyle = FontStyles.Bold;
        pillTmp.alignment = TextAlignmentOptions.Center;
        pillTmp.color = neonCyan;

        // Description
        GameObject descObj = CreateUI("Header_Description", frameObj.transform, new Vector2(1040f, 65f), new Vector2(0f, 105f));
        TextMeshProUGUI descTmp = descObj.AddComponent<TextMeshProUGUI>();
        if (font != null) descTmp.font = font;
        descTmp.text = "Discover the science behind motion, force and interaction\nthrough immersive virtual experiments.";
        descTmp.fontSize = 15.5f;
        descTmp.fontStyle = FontStyles.Normal;
        descTmp.alignment = TextAlignmentOptions.Center;
        descTmp.color = subtextColor;

        // 5. Three Cards (360x256 each, spaced 390 apart)
        float cardY = -48f;
        float cardW = 360f;
        float cardH = 256f;
        float spacingX = 390f;

        CreateWelcomeCard(frameObj.transform, "Card_FirstLaw", new Vector2(-spacingX, cardY), new Vector2(cardW, cardH),
            cardFrameSprite, "NEWTON'S", "FIRST LAW", iconInertia, "INERTIA", font, iceWhite, pureWhite, neonCyan);

        CreateWelcomeCard(frameObj.transform, "Card_SecondLaw", new Vector2(0f, cardY), new Vector2(cardW, cardH),
            cardFrameSprite, "NEWTON'S", "SECOND LAW", iconForce, "FORCE &\nACCELERATION", font, iceWhite, pureWhite, neonCyan);

        CreateWelcomeCard(frameObj.transform, "Card_ThirdLaw", new Vector2(spacingX, cardY), new Vector2(cardW, cardH),
            cardFrameSprite, "NEWTON'S", "THIRD LAW", iconActionReaction, "ACTION &\nREACTION", font, iceWhite, pureWhite, neonCyan);

        // 6. Action Button: ENTER LABORATORY →
        GameObject enterBtn = CreateUI("Btn_EnterLaboratory", frameObj.transform, new Vector2(460f, 72f), new Vector2(0f, -315f));
        GameObject enterFrame = CreateUI("Enter_Frame", enterBtn.transform, new Vector2(460f, 72f), Vector2.zero);
        Image enterFrameImg = enterFrame.AddComponent<Image>();
        if (pillFrameSprite != null)
        {
            enterFrameImg.sprite = pillFrameSprite;
            enterFrameImg.color = Color.white;
        }
        else
        {
            enterFrameImg.color = neonCyan;
        }

        GameObject enterFill = CreateUI("Enter_Fill", enterFrame.transform, new Vector2(450f, 62f), Vector2.zero);
        Image enterFillImg = enterFill.AddComponent<Image>();
        enterFillImg.color = buttonNormal;

        GameObject enterTxt = CreateUI("TMP_Enter", enterFill.transform, new Vector2(430f, 54f), Vector2.zero);
        TextMeshProUGUI enterTmp = enterTxt.AddComponent<TextMeshProUGUI>();
        if (font != null) enterTmp.font = font;
        enterTmp.text = "ENTER LABORATORY →";
        enterTmp.fontSize = 24f;
        enterTmp.fontStyle = FontStyles.Bold;
        enterTmp.alignment = TextAlignmentOptions.Center;
        enterTmp.color = pureWhite;

        BoxCollider enterCol = enterBtn.AddComponent<BoxCollider>();
        enterCol.size = new Vector3(480f, 85f, 60f);
        enterCol.center = Vector3.zero;

        // 7. Corner Hash Decals
        GameObject decalL = CreateUI("Tech_Decal_Left", frameObj.transform, new Vector2(160f, 28f), new Vector2(-490f, -325f));
        TextMeshProUGUI dLTmp = decalL.AddComponent<TextMeshProUGUI>();
        if (font != null) dLTmp.font = font;
        dLTmp.text = "[ ///// ///// ]";
        dLTmp.fontSize = 14f;
        dLTmp.fontStyle = FontStyles.Bold;
        dLTmp.alignment = TextAlignmentOptions.Center;
        dLTmp.color = new Color(0.00f, 0.75f, 0.95f, 0.60f);

        GameObject decalR = CreateUI("Tech_Decal_Right", frameObj.transform, new Vector2(160f, 28f), new Vector2(490f, -325f));
        TextMeshProUGUI dRTmp = decalR.AddComponent<TextMeshProUGUI>();
        if (font != null) dRTmp.font = font;
        dRTmp.text = "[ \\\\\\\\\\ \\\\\\\\\\ ]";
        dRTmp.fontSize = 14f;
        dRTmp.fontStyle = FontStyles.Bold;
        dRTmp.alignment = TextAlignmentOptions.Center;
        dRTmp.color = new Color(0.00f, 0.75f, 0.95f, 0.60f);

        comp.EnsureInitialized(false);

        // 8. Save Prefab
        string welcomePrefabPath = "Assets/Prefabs/VRWelcomePanel.prefab";
        if (File.Exists(welcomePrefabPath))
        {
            AssetDatabase.DeleteAsset(welcomePrefabPath);
        }
        PrefabUtility.SaveAsPrefabAssetAndConnect(welcomeRoot, welcomePrefabPath, InteractionMode.AutomatedAction);
        Debug.Log($"[Newton VR] Saved Grand Welcome Panel Prefab to: {welcomePrefabPath}");

        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
        return welcomeRoot;
    }

    private static void CreateWelcomeCard(Transform parent, string name, Vector2 pos, Vector2 size, Sprite frameSprite, string subHead, string mainHead, Sprite iconSprite, string footerLabel, TMP_FontAsset font, Color iceWhite, Color pureWhite, Color neonCyan)
    {
        GameObject cardObj = CreateUI(name, parent, size, pos);
        Image cardImg = cardObj.AddComponent<Image>();
        if (frameSprite != null)
        {
            cardImg.sprite = frameSprite;
            cardImg.color = Color.white;
        }
        else
        {
            cardImg.color = new Color(0.027f, 0.078f, 0.141f, 0.92f);
        }

        GameObject subObj = CreateUI("TMP_SubHead", cardObj.transform, new Vector2(300f, 22f), new Vector2(0f, 96f));
        TextMeshProUGUI subTmp = subObj.AddComponent<TextMeshProUGUI>();
        if (font != null) subTmp.font = font;
        subTmp.text = subHead;
        subTmp.fontSize = 14f;
        subTmp.fontStyle = FontStyles.Bold;
        subTmp.alignment = TextAlignmentOptions.Center;
        subTmp.color = iceWhite;

        GameObject mainObj = CreateUI("TMP_MainHead", cardObj.transform, new Vector2(300f, 26f), new Vector2(0f, 72f));
        TextMeshProUGUI mainTmp = mainObj.AddComponent<TextMeshProUGUI>();
        if (font != null) mainTmp.font = font;
        mainTmp.text = mainHead;
        mainTmp.fontSize = 18f;
        mainTmp.fontStyle = FontStyles.Bold;
        mainTmp.alignment = TextAlignmentOptions.Center;
        mainTmp.color = pureWhite;

        GameObject iconObj = CreateUI("Icon_Graphic", cardObj.transform, new Vector2(136f, 136f), new Vector2(0f, 5f));
        Image iconImg = iconObj.AddComponent<Image>();
        if (iconSprite != null)
        {
            iconImg.sprite = iconSprite;
            iconImg.color = Color.white;
        }
        else
        {
            iconImg.color = neonCyan;
        }

        GameObject footObj = CreateUI("TMP_Footer", cardObj.transform, new Vector2(320f, 44f), new Vector2(0f, -88f));
        TextMeshProUGUI footTmp = footObj.AddComponent<TextMeshProUGUI>();
        if (font != null) footTmp.font = font;
        footTmp.text = footerLabel;
        footTmp.fontSize = 15f;
        footTmp.fontStyle = FontStyles.Bold;
        footTmp.alignment = TextAlignmentOptions.Center;
        footTmp.color = neonCyan;
        footTmp.lineSpacing = -8f;
    }

    private static Sprite LoadSpriteAsset(string path)
    {
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (tex != null)
        {
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }
        if (File.Exists(path))
        {
            byte[] bytes = File.ReadAllBytes(path);
            Texture2D diskTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (diskTex.LoadImage(bytes))
            {
                return Sprite.Create(diskTex, new Rect(0, 0, diskTex.width, diskTex.height), new Vector2(0.5f, 0.5f));
            }
        }
        return null;
    }

    private static void CreateButton(Transform parent, string heading, string description, Vector2 anchoredPos, TMP_FontAsset font, Color champagneGold, Color buttonBase, Color emeraldGreen, Color pureWhite, Color warmIvory, Texture2D previewTex = null)
    {
        string cleanName = heading.Replace(" ", "").Replace("—", "").Replace("'", "").Replace("&", "AND");
        GameObject btnObj = CreateUI($"Btn_{cleanName}", parent, new Vector2(630f, 70f), anchoredPos);
        RectTransform btnRect = btnObj.GetComponent<RectTransform>();

        // Outer Gold Frame
        GameObject frame = CreateUI("Btn_Frame", btnRect, new Vector2(632f, 72f), Vector2.zero);
        Image frameImg = frame.AddComponent<Image>();
        frameImg.color = champagneGold * 0.70f;

        // Background Fill
        GameObject fill = CreateUI("Btn_Fill", frame.transform, new Vector2(628f, 68f), Vector2.zero);
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = buttonBase;

        // Left Emerald Accent Bar
        GameObject accent = CreateUI("Btn_Accent", fill.transform, new Vector2(8f, 68f), new Vector2(-310f, 0f));
        Image accentImg = accent.AddComponent<Image>();
        accentImg.color = emeraldGreen;

        float textStartX = -290f;
        if (previewTex != null)
        {
            // Room Preview Thumbnail Frame
            GameObject thumbFrame = CreateUI("Btn_ThumbFrame", fill.transform, new Vector2(76f, 54f), new Vector2(-260f, 0f));
            Image tfImg = thumbFrame.AddComponent<Image>();
            tfImg.color = champagneGold * 0.85f;

            // Room Preview Thumbnail RawImage
            GameObject thumbGo = CreateUI("Btn_Preview", thumbFrame.transform, new Vector2(72f, 50f), Vector2.zero);
            RawImage rawImg = thumbGo.AddComponent<RawImage>();
            rawImg.texture = previewTex;
            rawImg.color = Color.white;

            textStartX = -205f;
        }

        // Heading Text: Pure White (#FFFFFF), Bold
        float textWidth = 275f - textStartX;
        GameObject headObj = CreateUI("TMP_Heading", fill.transform, new Vector2(textWidth, 26f), new Vector2(textStartX + textWidth * 0.5f, 13f));
        TextMeshProUGUI headTmp = headObj.AddComponent<TextMeshProUGUI>();
        if (font != null)
        {
            headTmp.font = font;
            if (font.material != null) headTmp.fontSharedMaterial = font.material;
        }
        headTmp.text = heading;
        headTmp.fontSize = 20f;
        headTmp.fontStyle = FontStyles.Bold;
        headTmp.alignment = TextAlignmentOptions.Left;
        headTmp.color = pureWhite;

        // Description Text: Warm Ivory (#FFF8E8)
        GameObject descObj = CreateUI("TMP_Description", fill.transform, new Vector2(textWidth, 22f), new Vector2(textStartX + textWidth * 0.5f, -14f));
        TextMeshProUGUI descTmp = descObj.AddComponent<TextMeshProUGUI>();
        if (font != null)
        {
            descTmp.font = font;
            if (font.material != null) descTmp.fontSharedMaterial = font.material;
        }
        descTmp.text = description;
        descTmp.fontSize = 14f;
        descTmp.fontStyle = FontStyles.Normal;
        descTmp.alignment = TextAlignmentOptions.Left;
        descTmp.color = warmIvory;

        // Right Action Button Badge (Circular Frame with Arrow)
        GameObject actionObj = CreateUI("Btn_ActionBadge", fill.transform, new Vector2(34f, 34f), new Vector2(285f, 0f));
        GameObject actionFrame = CreateUI("Action_Frame", actionObj.transform, new Vector2(34f, 34f), Vector2.zero);
        Image afImg = actionFrame.AddComponent<Image>();
        afImg.color = champagneGold * 0.90f;

        GameObject actionFill = CreateUI("Action_Fill", actionFrame.transform, new Vector2(30f, 30f), Vector2.zero);
        Image aFillImg = actionFill.AddComponent<Image>();
        aFillImg.color = new Color(0.18f, 0.15f, 0.13f);

        GameObject arrowObj = CreateUI("TMP_Arrow", actionFill.transform, new Vector2(30f, 30f), new Vector2(1f, 0f));
        TextMeshProUGUI arrowTmp = arrowObj.AddComponent<TextMeshProUGUI>();
        if (font != null)
        {
            arrowTmp.font = font;
            if (font.material != null) arrowTmp.fontSharedMaterial = font.material;
        }
        arrowTmp.text = ">";
        arrowTmp.fontSize = 20f;
        arrowTmp.fontStyle = FontStyles.Bold;
        arrowTmp.alignment = TextAlignmentOptions.Center;
        arrowTmp.color = champagneGold;

        // BoxCollider for Raycast Hit
        BoxCollider col = btnObj.AddComponent<BoxCollider>();
        col.size = new Vector3(630f, 70f, 20f);
        col.center = Vector3.zero;
    }

    public static GameObject CreateStationaryRoomNavTab(string name, Vector3 worldPos, Quaternion worldRot, Transform parent, Camera cam, TMP_FontAsset font)
    {
        GameObject tabCanvasGo = new GameObject(name);
        if (parent != null) tabCanvasGo.transform.SetParent(parent, false);
        tabCanvasGo.transform.position = worldPos;
        tabCanvasGo.transform.rotation = worldRot;
        tabCanvasGo.transform.localScale = Vector3.one * 0.001f;

        Canvas tabCanvas = tabCanvasGo.AddComponent<Canvas>();
        tabCanvas.renderMode = RenderMode.WorldSpace;
        tabCanvas.worldCamera = cam;
        CanvasScaler tabScaler = tabCanvasGo.AddComponent<CanvasScaler>();
        tabScaler.dynamicPixelsPerUnit = 10f;
        tabCanvasGo.AddComponent<GraphicRaycaster>();

        RectTransform tabRect = tabCanvasGo.GetComponent<RectTransform>();
        tabRect.sizeDelta = new Vector2(300f, 90f);

        Color champagneGold = new Color(0.92f, 0.76f, 0.45f, 1.0f);
        Color buttonBase = new Color(0.14f, 0.12f, 0.10f, 0.95f);
        Color emeraldGreen = new Color(0.05f, 0.85f, 0.45f, 1.0f);
        Color pureWhite = Color.white;

        GameObject tabFrame = CreateUI("Tab_Frame", tabRect, new Vector2(304f, 94f), Vector2.zero);
        Image tfImg = tabFrame.AddComponent<Image>();
        tfImg.color = champagneGold;

        GameObject tabFill = CreateUI("Tab_Fill", tabFrame.transform, new Vector2(298f, 88f), Vector2.zero);
        Image tabFillImg = tabFill.AddComponent<Image>();
        tabFillImg.color = buttonBase;

        GameObject tabDot = CreateUI("Tab_Dot", tabFill.transform, new Vector2(18f, 18f), new Vector2(-115f, 0f));
        Image dotImg = tabDot.AddComponent<Image>();
        dotImg.color = emeraldGreen;

        GameObject tabTxt = CreateUI("TMP_NavTab", tabFill.transform, new Vector2(210f, 45f), new Vector2(15f, 10f));
        TextMeshProUGUI tabTmp = tabTxt.AddComponent<TextMeshProUGUI>();
        if (font != null) tabTmp.font = font;
        tabTmp.text = "LABS";
        tabTmp.fontSize = 34f;
        tabTmp.fontStyle = FontStyles.Bold;
        tabTmp.alignment = TextAlignmentOptions.Center;
        tabTmp.color = pureWhite;

        GameObject tabSub = CreateUI("TMP_NavSub", tabFill.transform, new Vector2(210f, 24f), new Vector2(15f, -22f));
        TextMeshProUGUI tabSubTmp = tabSub.AddComponent<TextMeshProUGUI>();
        if (font != null) tabSubTmp.font = font;
        tabSubTmp.text = "SELECT ROOM";
        tabSubTmp.fontSize = 14f;
        tabSubTmp.fontStyle = FontStyles.Bold;
        tabSubTmp.alignment = TextAlignmentOptions.Center;
        tabSubTmp.color = champagneGold;

        BoxCollider tabCol = tabCanvasGo.AddComponent<BoxCollider>();
        tabCol.size = new Vector3(450f, 180f, 250f);
        tabCol.center = Vector3.zero;

        tabCanvasGo.SetActive(true);
        return tabCanvasGo;
    }

    private static GameObject CreateUI(string name, Transform parent, Vector2 size, Vector2 anchoredPos)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        return go;
    }

    private static Texture2D LoadScreenshotTexture(string path)
    {
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (tex != null) return tex;
        if (File.Exists(path))
        {
            byte[] data = File.ReadAllBytes(path);
            Texture2D loaded = new Texture2D(2, 2);
            if (loaded.LoadImage(data)) return loaded;
        }
        return null;
    }

    private static Sprite LoadSprite(string path)
    {
        Texture2D tex = LoadScreenshotTexture(path);
        if (tex != null)
        {
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }
        return null;
    }

    [MenuItem("Newton VR/Build Room 4 Observation Centre", false, 7)]
    public static GameObject BuildRoom4ObservationHierarchy()
    {
        Debug.Log("[Newton VR] Constructing Room 4 — Observation & Data Analysis Centre...");

        // 1. Root Container (Preserves existing TutorialRoom contents)
        GameObject room4Root = GameObject.Find("Room4_ObservationCentre");
        if (room4Root != null) Object.DestroyImmediate(room4Root);

        room4Root = new GameObject("Room4_ObservationCentre");
        room4Root.transform.position = Vector3.zero;
        room4Root.transform.rotation = Quaternion.identity;

        GameObject tutRoom = GameObject.Find("TutorialRoom");
        if (tutRoom != null)
        {
            room4Root.transform.SetParent(tutRoom.transform, true);
        }

        TMP_FontAsset font = TMP_Settings.defaultFontAsset;
        if (font == null) font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        // Futuristic Colors
        Color deepNavy = new Color(0.025f, 0.065f, 0.140f, 0.94f);
        Color cyanNeon = new Color(0.00f, 0.88f, 1.00f, 1.0f);
        Color brightWhite = Color.white;
        Color mutedCyan = new Color(0.40f, 0.80f, 0.95f, 0.90f);
        Color cardBacking = new Color(0.04f, 0.10f, 0.22f, 0.92f);

        Sprite bannerSprite = LoadSprite("Assets/Textures/UI/Room4_Header_Banner.png");
        Sprite s1Sprite = LoadSprite("Assets/Textures/UI/Room4_Station1_Inertia_Diagram.png");
        Sprite s3Sprite = LoadSprite("Assets/Textures/UI/Room4_Station3_Collision_Diagram.png");

        // 2. Room 4 Dedicated Lighting & Atmosphere
        GameObject lightingRoot = GameObject.Find("Environment Lighting");
        if (lightingRoot != null)
        {
            if (lightingRoot.transform.Find("Room 4 Ceiling Light") == null)
            {
                GameObject lGo = new GameObject("Room 4 Ceiling Light");
                lGo.transform.SetParent(lightingRoot.transform, false);
                lGo.transform.position = new Vector3(0f, 5.5f, 48.0f);
                Light l = lGo.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color(0.85f, 0.95f, 1.0f);
                l.intensity = 0.95f;
                l.range = 28f;
                l.shadows = LightShadows.None;
            }

            if (lightingRoot.transform.Find("Room 4 Center Spotlight") == null)
            {
                GameObject spotGo = new GameObject("Room 4 Center Spotlight");
                spotGo.transform.SetParent(lightingRoot.transform, false);
                spotGo.transform.position = new Vector3(0f, 5.2f, 45.8f);
                spotGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                Light spot = spotGo.AddComponent<Light>();
                spot.type = LightType.Spot;
                spot.color = new Color(0.0f, 0.88f, 1.0f);
                spot.intensity = 1.25f;
                spot.range = 16f;
                spot.spotAngle = 60f;
            }

            float[] southZ = new float[] { 18f, 24f, 30f };
            for (int i = 0; i < southZ.Length; i++)
            {
                string sName = $"Corridor South Light {i + 1}";
                if (lightingRoot.transform.Find(sName) == null)
                {
                    GameObject clGo = new GameObject(sName);
                    clGo.transform.SetParent(lightingRoot.transform, false);
                    clGo.transform.position = new Vector3(0f, 3.2f, southZ[i]);
                    Light cl = clGo.AddComponent<Light>();
                    cl.type = LightType.Point;
                    cl.color = new Color(0.65f, 0.90f, 1.0f);
                    cl.intensity = 0.70f;
                    cl.range = 14f;
                }
            }
        }

        // 3. Central Holographic Pedestal & Globe
        GameObject pedestalRoot = new GameObject("HoloPedestal_Center");
        pedestalRoot.transform.SetParent(room4Root.transform, false);
        pedestalRoot.transform.position = new Vector3(0f, 0f, 45.8f);

        GameObject pBase = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pBase.name = "Pedestal_Base";
        pBase.transform.SetParent(pedestalRoot.transform, false);
        pBase.transform.localPosition = new Vector3(0f, 0.25f, 0f);
        pBase.transform.localScale = new Vector3(3.6f, 0.25f, 3.6f);
        Material baseMat = new Material(Shader.Find("Standard"));
        baseMat.color = new Color(0.05f, 0.08f, 0.14f);
        pBase.GetComponent<Renderer>().material = baseMat;

        GameObject pRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pRing.name = "Pedestal_NeonRing";
        pRing.transform.SetParent(pedestalRoot.transform, false);
        pRing.transform.localPosition = new Vector3(0f, 0.51f, 0f);
        pRing.transform.localScale = new Vector3(3.2f, 0.02f, 3.2f);
        Material ringMat = new Material(Shader.Find("Standard"));
        ringMat.color = cyanNeon;
        ringMat.EnableKeyword("_EMISSION");
        ringMat.SetColor("_EmissionColor", cyanNeon * 1.5f);
        pRing.GetComponent<Renderer>().material = ringMat;

        GameObject globeSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        globeSphere.name = "Holographic_Globe";
        globeSphere.transform.SetParent(pedestalRoot.transform, false);
        globeSphere.transform.localPosition = new Vector3(0f, 1.35f, 0f);
        globeSphere.transform.localScale = Vector3.one * 1.5f;
        Material globeMat = new Material(Shader.Find("Standard"));
        globeMat.color = new Color(0.0f, 0.75f, 1.0f, 0.45f);
        globeMat.EnableKeyword("_EMISSION");
        globeMat.SetColor("_EmissionColor", new Color(0.0f, 0.75f, 1.0f) * 1.2f);
        globeSphere.GetComponent<Renderer>().material = globeMat;

        // 4. Central Angled Experiment Summary Console
        GameObject consoleGo = new GameObject("Console_ExperimentSummary");
        consoleGo.transform.SetParent(room4Root.transform, false);
        consoleGo.transform.position = new Vector3(0f, 0.95f, 44.5f);
        consoleGo.transform.rotation = Quaternion.Euler(32f, 0f, 0f);
        consoleGo.transform.localScale = Vector3.one * 0.0018f;

        Canvas cCanvas = consoleGo.AddComponent<Canvas>();
        cCanvas.renderMode = RenderMode.WorldSpace;
        consoleGo.AddComponent<CanvasScaler>();
        consoleGo.AddComponent<GraphicRaycaster>();
        RectTransform cRect = consoleGo.GetComponent<RectTransform>();
        cRect.sizeDelta = new Vector2(740f, 340f);

        GameObject cFrame = CreateUI("Console_Frame", cRect, new Vector2(746f, 346f), Vector2.zero);
        Image cfImg = cFrame.AddComponent<Image>();
        cfImg.color = cyanNeon;

        GameObject cBg = CreateUI("Console_Bg", cFrame.transform, new Vector2(740f, 340f), Vector2.zero);
        Image cBgImg = cBg.AddComponent<Image>();
        cBgImg.color = deepNavy;

        // Title: EXPERIMENT SUMMARY
        GameObject cTitle = CreateUI("TMP_Title", cBg.transform, new Vector2(700f, 36f), new Vector2(0f, 135f));
        TextMeshProUGUI cTTmp = cTitle.AddComponent<TextMeshProUGUI>();
        if (font != null) cTTmp.font = font;
        cTTmp.text = "EXPERIMENT SUMMARY";
        cTTmp.fontSize = 22f;
        cTTmp.fontStyle = FontStyles.Bold;
        cTTmp.alignment = TextAlignmentOptions.Center;
        cTTmp.color = cyanNeon;

        // 3 Console Tabs
        GameObject tab1Obj = CreateUI("Tab_Results", cBg.transform, new Vector2(180f, 40f), new Vector2(-190f, 85f));
        Image t1Img = tab1Obj.AddComponent<Image>();
        t1Img.color = new Color(0.00f, 0.45f, 0.75f, 1.0f);
        BoxCollider t1Col = tab1Obj.AddComponent<BoxCollider>();
        t1Col.size = new Vector3(180f, 40f, 20f);
        GameObject t1Txt = CreateUI("TMP_T1", tab1Obj.transform, new Vector2(180f, 40f), Vector2.zero);
        TextMeshProUGUI t1Tmp = t1Txt.AddComponent<TextMeshProUGUI>();
        if (font != null) t1Tmp.font = font;
        t1Tmp.text = "RESULTS";
        t1Tmp.fontSize = 15f;
        t1Tmp.fontStyle = FontStyles.Bold;
        t1Tmp.alignment = TextAlignmentOptions.Center;
        t1Tmp.color = brightWhite;

        GameObject tab2Obj = CreateUI("Tab_Comparison", cBg.transform, new Vector2(180f, 40f), new Vector2(0f, 85f));
        Image t2Img = tab2Obj.AddComponent<Image>();
        t2Img.color = cardBacking;
        BoxCollider t2Col = tab2Obj.AddComponent<BoxCollider>();
        t2Col.size = new Vector3(180f, 40f, 20f);
        GameObject t2Txt = CreateUI("TMP_T2", tab2Obj.transform, new Vector2(180f, 40f), Vector2.zero);
        TextMeshProUGUI t2Tmp = t2Txt.AddComponent<TextMeshProUGUI>();
        if (font != null) t2Tmp.font = font;
        t2Tmp.text = "COMPARISON";
        t2Tmp.fontSize = 15f;
        t2Tmp.fontStyle = FontStyles.Bold;
        t2Tmp.alignment = TextAlignmentOptions.Center;
        t2Tmp.color = brightWhite;

        GameObject tab3Obj = CreateUI("Tab_Conclusion", cBg.transform, new Vector2(180f, 40f), new Vector2(190f, 85f));
        Image t3Img = tab3Obj.AddComponent<Image>();
        t3Img.color = cardBacking;
        BoxCollider t3Col = tab3Obj.AddComponent<BoxCollider>();
        t3Col.size = new Vector3(180f, 40f, 20f);
        GameObject t3Txt = CreateUI("TMP_T3", tab3Obj.transform, new Vector2(180f, 40f), Vector2.zero);
        TextMeshProUGUI t3Tmp = t3Txt.AddComponent<TextMeshProUGUI>();
        if (font != null) t3Tmp.font = font;
        t3Tmp.text = "CONCLUSION";
        t3Tmp.fontSize = 15f;
        t3Tmp.fontStyle = FontStyles.Bold;
        t3Tmp.alignment = TextAlignmentOptions.Center;
        t3Tmp.color = brightWhite;

        // Console Dynamic Text
        GameObject cHead = CreateUI("TMP_ConsoleHead", cBg.transform, new Vector2(680f, 32f), new Vector2(0f, 35f));
        TextMeshProUGUI cHeadTmp = cHead.AddComponent<TextMeshProUGUI>();
        if (font != null) cHeadTmp.font = font;
        cHeadTmp.text = "EXPERIMENTAL RESULTS SUMMARY";
        cHeadTmp.fontSize = 18f;
        cHeadTmp.fontStyle = FontStyles.Bold;
        cHeadTmp.alignment = TextAlignmentOptions.Left;
        cHeadTmp.color = cyanNeon;

        GameObject cBody = CreateUI("TMP_ConsoleBody", cBg.transform, new Vector2(680f, 130f), new Vector2(0f, -50f));
        TextMeshProUGUI cBodyTmp = cBody.AddComponent<TextMeshProUGUI>();
        if (font != null) cBodyTmp.font = font;
        cBodyTmp.text = "• Law 1 (Inertia): Cart maintains constant velocity until external barrier exerts contact force.\n" +
                        "• Law 2 (Dynamics): Measured acceleration verifies a = F/m with perfect linear proportionality.\n" +
                        "• Law 3 (Interaction): Impact sensors confirm equal peak force magnitudes (F₁ = F₂ = 10.0 N) in opposite directions.";
        cBodyTmp.fontSize = 15f;
        cBodyTmp.alignment = TextAlignmentOptions.TopLeft;
        cBodyTmp.color = brightWhite;

        // 5. Top Header Banner
        GameObject bannerGo = new GameObject("Banner_Room4Header");
        bannerGo.transform.SetParent(room4Root.transform, false);
        bannerGo.transform.position = new Vector3(0f, 3.85f, 47.8f);
        bannerGo.transform.rotation = Quaternion.identity;
        bannerGo.transform.localScale = Vector3.one * 0.003f;

        Canvas bCanvas = bannerGo.AddComponent<Canvas>();
        bCanvas.renderMode = RenderMode.WorldSpace;
        bannerGo.AddComponent<CanvasScaler>();
        RectTransform bRect = bannerGo.GetComponent<RectTransform>();
        bRect.sizeDelta = new Vector2(1024f, 256f);

        GameObject bImgObj = CreateUI("Banner_Image", bRect, new Vector2(1024f, 256f), Vector2.zero);
        Image bImg = bImgObj.AddComponent<Image>();
        if (bannerSprite != null) bImg.sprite = bannerSprite;
        bImg.color = Color.white;

        GameObject bH1 = CreateUI("TMP_R4", bImgObj.transform, new Vector2(900f, 32f), new Vector2(0f, 55f));
        TextMeshProUGUI bH1Tmp = bH1.AddComponent<TextMeshProUGUI>();
        if (font != null) bH1Tmp.font = font;
        bH1Tmp.text = "ROOM 4";
        bH1Tmp.fontSize = 24f;
        bH1Tmp.fontStyle = FontStyles.Bold;
        bH1Tmp.alignment = TextAlignmentOptions.Center;
        bH1Tmp.color = cyanNeon;

        GameObject bH2 = CreateUI("TMP_MainTitle", bImgObj.transform, new Vector2(900f, 48f), new Vector2(0f, 12f));
        TextMeshProUGUI bH2Tmp = bH2.AddComponent<TextMeshProUGUI>();
        if (font != null) bH2Tmp.font = font;
        bH2Tmp.text = "OBSERVATION & DATA ANALYSIS";
        bH2Tmp.fontSize = 38f;
        bH2Tmp.fontStyle = FontStyles.Bold;
        bH2Tmp.alignment = TextAlignmentOptions.Center;
        bH2Tmp.color = brightWhite;

        GameObject bH3 = CreateUI("TMP_SubTitle", bImgObj.transform, new Vector2(900f, 28f), new Vector2(0f, -32f));
        TextMeshProUGUI bH3Tmp = bH3.AddComponent<TextMeshProUGUI>();
        if (font != null) bH3Tmp.font = font;
        bH3Tmp.text = "Review • Compare • Understand";
        bH3Tmp.fontSize = 18f;
        bH3Tmp.alignment = TextAlignmentOptions.Center;
        bH3Tmp.color = mutedCyan;

        // 6. Station 1: Newton's First Law (Far Left)
        CreateStation1(room4Root.transform, font, deepNavy, cyanNeon, brightWhite, mutedCyan, s1Sprite);

        // 7. Station 2: Newton's Second Law (Center-Left Interactive)
        var s2Refs = CreateStation2(room4Root.transform, font, deepNavy, cyanNeon, brightWhite, mutedCyan);

        // 8. Station 3: Newton's Third Law (Center-Right)
        var s3Refs = CreateStation3(room4Root.transform, font, deepNavy, cyanNeon, brightWhite, mutedCyan, s3Sprite);

        // 9. Station 4: Overall Results (Far Right)
        CreateStation4(room4Root.transform, font, deepNavy, cyanNeon, brightWhite, mutedCyan);

        // 10. Attach Controller Component & Link Fields
        VRRoom4ObservationController ctrl = room4Root.AddComponent<VRRoom4ObservationController>();
        ctrl.massValueText = s2Refs.massText;
        ctrl.forceValueText = s2Refs.forceText;
        ctrl.accelerationValueText = s2Refs.accelText;
        ctrl.graphLineTransform = s2Refs.graphLine;
        ctrl.graphPointTransform = s2Refs.graphPoint;
        ctrl.graphSlopeText = s2Refs.slopeText;

        ctrl.cart1ForceText = s3Refs.f1Text;
        ctrl.cart2ForceText = s3Refs.f2Text;
        ctrl.actionReactionStatusText = s3Refs.statusText;

        ctrl.summaryHeadingText = cHeadTmp;
        ctrl.summaryBodyText = cBodyTmp;
        ctrl.tabResultsBg = t1Img;
        ctrl.tabComparisonBg = t2Img;
        ctrl.tabConclusionBg = t3Img;

        Debug.Log("[Newton VR] Successfully constructed Room 4 Observation Centre with 4 Interactive Data Stations.");
        return room4Root;
    }

    private static void CreateStation1(Transform parent, TMP_FontAsset font, Color bgCol, Color neonCol, Color whiteCol, Color mutedCol, Sprite diagram)
    {
        GameObject go = new GameObject("Station1_FirstLaw_Observation");
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(-4.60f, 2.10f, 46.80f);
        go.transform.rotation = Quaternion.Euler(0f, 22f, 0f);
        go.transform.localScale = Vector3.one * 0.0028f;

        Canvas cv = go.AddComponent<Canvas>();
        cv.renderMode = RenderMode.WorldSpace;
        go.AddComponent<CanvasScaler>();
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(820f, 560f);

        GameObject frame = CreateUI("Frame", rt, new Vector2(826f, 566f), Vector2.zero);
        frame.AddComponent<Image>().color = neonCol;
        GameObject bg = CreateUI("Bg", frame.transform, new Vector2(820f, 560f), Vector2.zero);
        bg.AddComponent<Image>().color = bgCol;

        // Title
        GameObject h = CreateUI("TMP_Header", bg.transform, new Vector2(760f, 36f), new Vector2(0f, 240f));
        TextMeshProUGUI hTmp = h.AddComponent<TextMeshProUGUI>();
        if (font != null) hTmp.font = font;
        hTmp.text = "NEWTON'S FIRST LAW";
        hTmp.fontSize = 28f;
        hTmp.fontStyle = FontStyles.Bold;
        hTmp.color = neonCol;

        GameObject sub = CreateUI("TMP_Sub", bg.transform, new Vector2(760f, 24f), new Vector2(0f, 210f));
        TextMeshProUGUI subTmp = sub.AddComponent<TextMeshProUGUI>();
        if (font != null) subTmp.font = font;
        subTmp.text = "Inertia and Motion";
        subTmp.fontSize = 16f;
        subTmp.color = mutedCol;

        // Diagram
        if (diagram != null)
        {
            GameObject diag = CreateUI("Diagram", bg.transform, new Vector2(760f, 280f), new Vector2(0f, 50f));
            Image dImg = diag.AddComponent<Image>();
            dImg.sprite = diagram;
            dImg.color = Color.white;
        }

        // Labels underneath diagram
        GameObject l1 = CreateUI("TMP_L1", bg.transform, new Vector2(340f, 24f), new Vector2(-190f, -80f));
        TextMeshProUGUI l1Tmp = l1.AddComponent<TextMeshProUGUI>();
        if (font != null) l1Tmp.font = font;
        l1Tmp.text = "Obstacle Raised (Stops)";
        l1Tmp.fontSize = 15f;
        l1Tmp.fontStyle = FontStyles.Bold;
        l1Tmp.alignment = TextAlignmentOptions.Center;
        l1Tmp.color = whiteCol;

        GameObject l2 = CreateUI("TMP_L2", bg.transform, new Vector2(340f, 24f), new Vector2(190f, -80f));
        TextMeshProUGUI l2Tmp = l2.AddComponent<TextMeshProUGUI>();
        if (font != null) l2Tmp.font = font;
        l2Tmp.text = "Obstacle Lowered (Continues)";
        l2Tmp.fontSize = 15f;
        l2Tmp.fontStyle = FontStyles.Bold;
        l2Tmp.alignment = TextAlignmentOptions.Center;
        l2Tmp.color = whiteCol;

        // Key Observation Box
        GameObject obsBox = CreateUI("ObsBox", bg.transform, new Vector2(760f, 95f), new Vector2(0f, -195f));
        obsBox.AddComponent<Image>().color = new Color(0.00f, 0.25f, 0.45f, 0.40f);
        GameObject obsTxt = CreateUI("TMP_Obs", obsBox.transform, new Vector2(720f, 85f), Vector2.zero);
        TextMeshProUGUI obsTmp = obsTxt.AddComponent<TextMeshProUGUI>();
        if (font != null) obsTmp.font = font;
        obsTmp.text = "<b>Key Observation:</b>\nAn object maintains its state of rest or uniform motion in a straight line unless acted on by a net external force.";
        obsTmp.fontSize = 15f;
        obsTmp.alignment = TextAlignmentOptions.TopLeft;
        obsTmp.color = whiteCol;
    }

    private struct Station2Refs
    {
        public TextMeshProUGUI massText;
        public TextMeshProUGUI forceText;
        public TextMeshProUGUI accelText;
        public RectTransform graphLine;
        public RectTransform graphPoint;
        public TextMeshProUGUI slopeText;
    }

    private static Station2Refs CreateStation2(Transform parent, TMP_FontAsset font, Color bgCol, Color neonCol, Color whiteCol, Color mutedCol)
    {
        Station2Refs refs = new Station2Refs();

        GameObject go = new GameObject("Station2_SecondLaw_Observation");
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(-1.60f, 2.10f, 48.00f);
        go.transform.rotation = Quaternion.Euler(0f, 8f, 0f);
        go.transform.localScale = Vector3.one * 0.0028f;

        Canvas cv = go.AddComponent<Canvas>();
        cv.renderMode = RenderMode.WorldSpace;
        go.AddComponent<CanvasScaler>();
        go.AddComponent<GraphicRaycaster>();
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(840f, 560f);

        GameObject frame = CreateUI("Frame", rt, new Vector2(846f, 566f), Vector2.zero);
        frame.AddComponent<Image>().color = neonCol;
        GameObject bg = CreateUI("Bg", frame.transform, new Vector2(840f, 560f), Vector2.zero);
        bg.AddComponent<Image>().color = bgCol;

        // Title
        GameObject h = CreateUI("TMP_Header", bg.transform, new Vector2(780f, 36f), new Vector2(0f, 240f));
        TextMeshProUGUI hTmp = h.AddComponent<TextMeshProUGUI>();
        if (font != null) hTmp.font = font;
        hTmp.text = "NEWTON'S SECOND LAW";
        hTmp.fontSize = 28f;
        hTmp.fontStyle = FontStyles.Bold;
        hTmp.color = neonCol;

        GameObject sub = CreateUI("TMP_Sub", bg.transform, new Vector2(780f, 24f), new Vector2(0f, 210f));
        TextMeshProUGUI subTmp = sub.AddComponent<TextMeshProUGUI>();
        if (font != null) subTmp.font = font;
        subTmp.text = "F = ma";
        subTmp.fontSize = 18f;
        subTmp.fontStyle = FontStyles.Bold;
        subTmp.color = mutedCol;

        // Left Column (Interactive Selectors)
        // 1. Mass Selector
        GameObject mCard = CreateUI("Card_Mass", bg.transform, new Vector2(340f, 75f), new Vector2(-200f, 130f));
        mCard.AddComponent<Image>().color = new Color(0.04f, 0.12f, 0.24f, 0.85f);
        GameObject mLbl = CreateUI("TMP_MassLbl", mCard.transform, new Vector2(160f, 40f), new Vector2(-75f, 0f));
        TextMeshProUGUI mLblTmp = mLbl.AddComponent<TextMeshProUGUI>();
        if (font != null) mLblTmp.font = font;
        mLblTmp.text = "Mass (kg)";
        mLblTmp.fontSize = 17f;
        mLblTmp.color = whiteCol;

        GameObject mBtn = CreateUI("Btn_MassSelector", mCard.transform, new Vector2(130f, 48f), new Vector2(80f, 0f));
        mBtn.AddComponent<Image>().color = new Color(0.00f, 0.35f, 0.65f, 1.0f);
        BoxCollider mCol = mBtn.AddComponent<BoxCollider>();
        mCol.size = new Vector3(130f, 48f, 25f);
        GameObject mVal = CreateUI("TMP_MassVal", mBtn.transform, new Vector2(130f, 48f), Vector2.zero);
        refs.massText = mVal.AddComponent<TextMeshProUGUI>();
        if (font != null) refs.massText.font = font;
        refs.massText.text = "5.0  >";
        refs.massText.fontSize = 20f;
        refs.massText.fontStyle = FontStyles.Bold;
        refs.massText.alignment = TextAlignmentOptions.Center;
        refs.massText.color = whiteCol;

        // 2. Force Selector
        GameObject fCard = CreateUI("Card_Force", bg.transform, new Vector2(340f, 75f), new Vector2(-200f, 40f));
        fCard.AddComponent<Image>().color = new Color(0.04f, 0.12f, 0.24f, 0.85f);
        GameObject fLbl = CreateUI("TMP_ForceLbl", fCard.transform, new Vector2(160f, 40f), new Vector2(-75f, 0f));
        TextMeshProUGUI fLblTmp = fLbl.AddComponent<TextMeshProUGUI>();
        if (font != null) fLblTmp.font = font;
        fLblTmp.text = "Force (N)";
        fLblTmp.fontSize = 17f;
        fLblTmp.color = whiteCol;

        GameObject fBtn = CreateUI("Btn_ForceSelector", fCard.transform, new Vector2(130f, 48f), new Vector2(80f, 0f));
        fBtn.AddComponent<Image>().color = new Color(0.00f, 0.35f, 0.65f, 1.0f);
        BoxCollider fCol = fBtn.AddComponent<BoxCollider>();
        fCol.size = new Vector3(130f, 48f, 25f);
        GameObject fVal = CreateUI("TMP_ForceVal", fBtn.transform, new Vector2(130f, 48f), Vector2.zero);
        refs.forceText = fVal.AddComponent<TextMeshProUGUI>();
        if (font != null) refs.forceText.font = font;
        refs.forceText.text = "10.0  >";
        refs.forceText.fontSize = 20f;
        refs.forceText.fontStyle = FontStyles.Bold;
        refs.forceText.alignment = TextAlignmentOptions.Center;
        refs.forceText.color = whiteCol;

        // 3. Acceleration Readout
        GameObject aCard = CreateUI("Card_Accel", bg.transform, new Vector2(340f, 85f), new Vector2(-200f, -60f));
        aCard.AddComponent<Image>().color = new Color(0.02f, 0.20f, 0.38f, 0.90f);
        GameObject aLbl = CreateUI("TMP_AccelLbl", aCard.transform, new Vector2(180f, 40f), new Vector2(-65f, 0f));
        TextMeshProUGUI aLblTmp = aLbl.AddComponent<TextMeshProUGUI>();
        if (font != null) aLblTmp.font = font;
        aLblTmp.text = "Acceleration (m/s²)";
        aLblTmp.fontSize = 16f;
        aLblTmp.color = whiteCol;

        GameObject aVal = CreateUI("TMP_AccelVal", aCard.transform, new Vector2(110f, 50f), new Vector2(95f, 0f));
        refs.accelText = aVal.AddComponent<TextMeshProUGUI>();
        if (font != null) refs.accelText.font = font;
        refs.accelText.text = "2.0";
        refs.accelText.fontSize = 36f;
        refs.accelText.fontStyle = FontStyles.Bold;
        refs.accelText.alignment = TextAlignmentOptions.Center;
        refs.accelText.color = neonCol;

        // 4. Key Observation Box (bottom)
        GameObject obsBox = CreateUI("ObsBox", bg.transform, new Vector2(780f, 85f), new Vector2(0f, -200f));
        obsBox.AddComponent<Image>().color = new Color(0.00f, 0.25f, 0.45f, 0.40f);
        GameObject obsTxt = CreateUI("TMP_Obs", obsBox.transform, new Vector2(740f, 75f), Vector2.zero);
        TextMeshProUGUI obsTmp = obsTxt.AddComponent<TextMeshProUGUI>();
        if (font != null) obsTmp.font = font;
        obsTmp.text = "<b>Key Principle:</b> Acceleration is directly proportional to applied net force (a ∝ F) and inversely proportional to mass (a ∝ 1/m).";
        obsTmp.fontSize = 15f;
        obsTmp.alignment = TextAlignmentOptions.TopLeft;
        obsTmp.color = whiteCol;

        // Right Column: Dynamic Graph Area
        GameObject graphArea = CreateUI("GraphArea", bg.transform, new Vector2(380f, 270f), new Vector2(185f, 35f));
        graphArea.AddComponent<Image>().color = new Color(0.03f, 0.09f, 0.18f, 0.92f);

        GameObject gTitle = CreateUI("TMP_GTitle", graphArea.transform, new Vector2(340f, 26f), new Vector2(0f, 115f));
        TextMeshProUGUI gtTmp = gTitle.AddComponent<TextMeshProUGUI>();
        if (font != null) gtTmp.font = font;
        gtTmp.text = "Acceleration vs Force";
        gtTmp.fontSize = 16f;
        gtTmp.fontStyle = FontStyles.Bold;
        gtTmp.alignment = TextAlignmentOptions.Center;
        gtTmp.color = neonCol;

        // Graph Plot Canvas/Origin
        GameObject plotOrigin = CreateUI("PlotOrigin", graphArea.transform, new Vector2(240f, 180f), new Vector2(25f, -15f));
        RectTransform plotRt = plotOrigin.GetComponent<RectTransform>();
        plotRt.pivot = new Vector2(0f, 0f);

        // Axes Lines
        GameObject xAxis = CreateUI("X_Axis", plotOrigin.transform, new Vector2(240f, 2f), new Vector2(120f, 0f));
        xAxis.AddComponent<Image>().color = mutedCol;
        GameObject yAxis = CreateUI("Y_Axis", plotOrigin.transform, new Vector2(2f, 180f), new Vector2(0f, 90f));
        yAxis.AddComponent<Image>().color = mutedCol;

        // Axis Labels
        GameObject xLbl = CreateUI("TMP_XLbl", graphArea.transform, new Vector2(100f, 20f), new Vector2(30f, -115f));
        TextMeshProUGUI xlTmp = xLbl.AddComponent<TextMeshProUGUI>();
        if (font != null) xlTmp.font = font;
        xlTmp.text = "F (N) →";
        xlTmp.fontSize = 13f;
        xlTmp.color = mutedCol;

        GameObject yLbl = CreateUI("TMP_YLbl", graphArea.transform, new Vector2(120f, 20f), new Vector2(-155f, 0f));
        yLbl.transform.localEulerAngles = new Vector3(0f, 0f, 90f);
        TextMeshProUGUI ylTmp = yLbl.AddComponent<TextMeshProUGUI>();
        if (font != null) ylTmp.font = font;
        ylTmp.text = "a (m/s²) →";
        ylTmp.fontSize = 13f;
        ylTmp.color = mutedCol;

        // Dynamic Slope Line
        GameObject slopeLine = CreateUI("DynamicSlopeLine", plotOrigin.transform, new Vector2(240f, 3f), Vector2.zero);
        refs.graphLine = slopeLine.GetComponent<RectTransform>();
        refs.graphLine.pivot = new Vector2(0f, 0.5f);
        slopeLine.AddComponent<Image>().color = neonCol;

        // Dynamic Data Point Dot
        GameObject pointDot = CreateUI("DataPointDot", plotOrigin.transform, new Vector2(16f, 16f), Vector2.zero);
        refs.graphPoint = pointDot.GetComponent<RectTransform>();
        pointDot.AddComponent<Image>().color = whiteCol;

        // Slope Text
        GameObject slopeTxt = CreateUI("TMP_Slope", graphArea.transform, new Vector2(300f, 20f), new Vector2(0f, -85f));
        refs.slopeText = slopeTxt.AddComponent<TextMeshProUGUI>();
        if (font != null) refs.slopeText.font = font;
        refs.slopeText.text = "Slope = 1/m = 0.20 kg⁻¹";
        refs.slopeText.fontSize = 14f;
        refs.slopeText.alignment = TextAlignmentOptions.Center;
        refs.slopeText.color = neonCol;

        return refs;
    }

    private struct Station3Refs
    {
        public TextMeshProUGUI f1Text;
        public TextMeshProUGUI f2Text;
        public TextMeshProUGUI statusText;
    }

    private static Station3Refs CreateStation3(Transform parent, TMP_FontAsset font, Color bgCol, Color neonCol, Color whiteCol, Color mutedCol, Sprite diagram)
    {
        Station3Refs refs = new Station3Refs();

        GameObject go = new GameObject("Station3_ThirdLaw_Observation");
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(1.60f, 2.10f, 48.00f);
        go.transform.rotation = Quaternion.Euler(0f, -8f, 0f);
        go.transform.localScale = Vector3.one * 0.0028f;

        Canvas cv = go.AddComponent<Canvas>();
        cv.renderMode = RenderMode.WorldSpace;
        go.AddComponent<CanvasScaler>();
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(820f, 560f);

        GameObject frame = CreateUI("Frame", rt, new Vector2(826f, 566f), Vector2.zero);
        frame.AddComponent<Image>().color = neonCol;
        GameObject bg = CreateUI("Bg", frame.transform, new Vector2(820f, 560f), Vector2.zero);
        bg.AddComponent<Image>().color = bgCol;

        // Title
        GameObject h = CreateUI("TMP_Header", bg.transform, new Vector2(760f, 36f), new Vector2(0f, 240f));
        TextMeshProUGUI hTmp = h.AddComponent<TextMeshProUGUI>();
        if (font != null) hTmp.font = font;
        hTmp.text = "NEWTON'S THIRD LAW";
        hTmp.fontSize = 28f;
        hTmp.fontStyle = FontStyles.Bold;
        hTmp.color = neonCol;

        GameObject sub = CreateUI("TMP_Sub", bg.transform, new Vector2(760f, 24f), new Vector2(0f, 210f));
        TextMeshProUGUI subTmp = sub.AddComponent<TextMeshProUGUI>();
        if (font != null) subTmp.font = font;
        subTmp.text = "Action and Reaction";
        subTmp.fontSize = 16f;
        subTmp.color = mutedCol;

        // Diagram
        if (diagram != null)
        {
            GameObject diag = CreateUI("Diagram", bg.transform, new Vector2(760f, 260f), new Vector2(0f, 60f));
            Image dImg = diag.AddComponent<Image>();
            dImg.sprite = diagram;
            dImg.color = Color.white;
        }

        // Telemetry Callouts
        GameObject f1Obj = CreateUI("TMP_F1", bg.transform, new Vector2(220f, 36f), new Vector2(-180f, 150f));
        refs.f1Text = f1Obj.AddComponent<TextMeshProUGUI>();
        if (font != null) refs.f1Text.font = font;
        refs.f1Text.text = "F₁ = 10.0 N →";
        refs.f1Text.fontSize = 22f;
        refs.f1Text.fontStyle = FontStyles.Bold;
        refs.f1Text.alignment = TextAlignmentOptions.Center;
        refs.f1Text.color = neonCol;

        GameObject f2Obj = CreateUI("TMP_F2", bg.transform, new Vector2(220f, 36f), new Vector2(180f, 150f));
        refs.f2Text = f2Obj.AddComponent<TextMeshProUGUI>();
        if (font != null) refs.f2Text.font = font;
        refs.f2Text.text = "← F₂ = 10.0 N";
        refs.f2Text.fontSize = 22f;
        refs.f2Text.fontStyle = FontStyles.Bold;
        refs.f2Text.alignment = TextAlignmentOptions.Center;
        refs.f2Text.color = new Color(1.0f, 0.45f, 0.55f);

        // Vector Symmetry Callout
        GameObject statObj = CreateUI("TMP_Status", bg.transform, new Vector2(760f, 30f), new Vector2(0f, -70f));
        refs.statusText = statObj.AddComponent<TextMeshProUGUI>();
        if (font != null) refs.statusText.font = font;
        refs.statusText.text = "|F₁| = |F₂| = 10.0 N   |   F₁ = -F₂   (Equal Magnitude, Opposite Direction)";
        refs.statusText.fontSize = 16f;
        refs.statusText.fontStyle = FontStyles.Bold;
        refs.statusText.alignment = TextAlignmentOptions.Center;
        refs.statusText.color = whiteCol;

        // Key Observation Box
        GameObject obsBox = CreateUI("ObsBox", bg.transform, new Vector2(760f, 95f), new Vector2(0f, -195f));
        obsBox.AddComponent<Image>().color = new Color(0.00f, 0.25f, 0.45f, 0.40f);
        GameObject obsTxt = CreateUI("TMP_Obs", obsBox.transform, new Vector2(720f, 85f), Vector2.zero);
        TextMeshProUGUI obsTmp = obsTxt.AddComponent<TextMeshProUGUI>();
        if (font != null) obsTmp.font = font;
        obsTmp.text = "<b>Key Observation:</b>\nFor every action force exerted on a body, there is an equal and opposite reaction force exerted on the other body. Individual isolated forces do not exist in nature.";
        obsTmp.fontSize = 15f;
        obsTmp.alignment = TextAlignmentOptions.TopLeft;
        obsTmp.color = whiteCol;

        return refs;
    }

    private static void CreateStation4(Transform parent, TMP_FontAsset font, Color bgCol, Color neonCol, Color whiteCol, Color mutedCol)
    {
        GameObject go = new GameObject("Station4_OverallResults_Observation");
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(4.60f, 2.10f, 46.80f);
        go.transform.rotation = Quaternion.Euler(0f, -22f, 0f);
        go.transform.localScale = Vector3.one * 0.0028f;

        Canvas cv = go.AddComponent<Canvas>();
        cv.renderMode = RenderMode.WorldSpace;
        go.AddComponent<CanvasScaler>();
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(820f, 560f);

        GameObject frame = CreateUI("Frame", rt, new Vector2(826f, 566f), Vector2.zero);
        frame.AddComponent<Image>().color = neonCol;
        GameObject bg = CreateUI("Bg", frame.transform, new Vector2(820f, 560f), Vector2.zero);
        bg.AddComponent<Image>().color = bgCol;

        // Title
        GameObject h = CreateUI("TMP_Header", bg.transform, new Vector2(760f, 36f), new Vector2(0f, 240f));
        TextMeshProUGUI hTmp = h.AddComponent<TextMeshProUGUI>();
        if (font != null) hTmp.font = font;
        hTmp.text = "OVERALL RESULTS";
        hTmp.fontSize = 28f;
        hTmp.fontStyle = FontStyles.Bold;
        hTmp.color = neonCol;

        GameObject sub = CreateUI("TMP_Sub", bg.transform, new Vector2(760f, 24f), new Vector2(0f, 210f));
        TextMeshProUGUI subTmp = sub.AddComponent<TextMeshProUGUI>();
        if (font != null) subTmp.font = font;
        subTmp.text = "Three Laws of Motion Synthesis";
        subTmp.fontSize = 16f;
        subTmp.color = mutedCol;

        // Comparison Table Container
        GameObject table = CreateUI("Table_Container", bg.transform, new Vector2(760f, 240f), new Vector2(0f, 60f));
        table.AddComponent<Image>().color = new Color(0.04f, 0.12f, 0.24f, 0.90f);

        // Table Header
        GameObject th = CreateUI("TableHeader", table.transform, new Vector2(760f, 45f), new Vector2(0f, 97f));
        th.AddComponent<Image>().color = new Color(0.00f, 0.40f, 0.70f, 0.95f);
        GameObject th1 = CreateUI("TMP_TH1", th.transform, new Vector2(200f, 35f), new Vector2(-260f, 0f));
        TextMeshProUGUI th1Tmp = th1.AddComponent<TextMeshProUGUI>();
        if (font != null) th1Tmp.font = font;
        th1Tmp.text = "LAW";
        th1Tmp.fontSize = 16f;
        th1Tmp.fontStyle = FontStyles.Bold;
        th1Tmp.color = whiteCol;

        GameObject th2 = CreateUI("TMP_TH2", th.transform, new Vector2(520f, 35f), new Vector2(100f, 0f));
        TextMeshProUGUI th2Tmp = th2.AddComponent<TextMeshProUGUI>();
        if (font != null) th2Tmp.font = font;
        th2Tmp.text = "KEY SCIENTIFIC RESULT";
        th2Tmp.fontSize = 16f;
        th2Tmp.fontStyle = FontStyles.Bold;
        th2Tmp.color = whiteCol;

        // Row 1: First Law
        GameObject r1 = CreateUI("Row1", table.transform, new Vector2(760f, 55f), new Vector2(0f, 45f));
        GameObject r1c1 = CreateUI("TMP_R1C1", r1.transform, new Vector2(200f, 45f), new Vector2(-260f, 0f));
        TextMeshProUGUI r1c1Tmp = r1c1.AddComponent<TextMeshProUGUI>();
        if (font != null) r1c1Tmp.font = font;
        r1c1Tmp.text = "First Law";
        r1c1Tmp.fontSize = 16f;
        r1c1Tmp.fontStyle = FontStyles.Bold;
        r1c1Tmp.color = neonCol;

        GameObject r1c2 = CreateUI("TMP_R1C2", r1.transform, new Vector2(520f, 45f), new Vector2(100f, 0f));
        TextMeshProUGUI r1c2Tmp = r1c2.AddComponent<TextMeshProUGUI>();
        if (font != null) r1c2Tmp.font = font;
        r1c2Tmp.text = "Motion changes only with external force (v = const if ΣF = 0).";
        r1c2Tmp.fontSize = 15f;
        r1c2Tmp.color = whiteCol;

        // Row 2: Second Law
        GameObject r2 = CreateUI("Row2", table.transform, new Vector2(760f, 55f), new Vector2(0f, -15f));
        r2.AddComponent<Image>().color = new Color(0.00f, 0.20f, 0.40f, 0.35f);
        GameObject r2c1 = CreateUI("TMP_R2C1", r2.transform, new Vector2(200f, 45f), new Vector2(-260f, 0f));
        TextMeshProUGUI r2c1Tmp = r2c1.AddComponent<TextMeshProUGUI>();
        if (font != null) r2c1Tmp.font = font;
        r2c1Tmp.text = "Second Law";
        r2c1Tmp.fontSize = 16f;
        r2c1Tmp.fontStyle = FontStyles.Bold;
        r2c1Tmp.color = neonCol;

        GameObject r2c2 = CreateUI("TMP_R2C2", r2.transform, new Vector2(520f, 45f), new Vector2(100f, 0f));
        TextMeshProUGUI r2c2Tmp = r2c2.AddComponent<TextMeshProUGUI>();
        if (font != null) r2c2Tmp.font = font;
        r2c2Tmp.text = "a ∝ F and a ∝ 1/m (F = ma, fundamental equation of motion).";
        r2c2Tmp.fontSize = 15f;
        r2c2Tmp.color = whiteCol;

        // Row 3: Third Law
        GameObject r3 = CreateUI("Row3", table.transform, new Vector2(760f, 55f), new Vector2(0f, -75f));
        GameObject r3c1 = CreateUI("TMP_R3C1", r3.transform, new Vector2(200f, 45f), new Vector2(-260f, 0f));
        TextMeshProUGUI r3c1Tmp = r3c1.AddComponent<TextMeshProUGUI>();
        if (font != null) r3c1Tmp.font = font;
        r3c1Tmp.text = "Third Law";
        r3c1Tmp.fontSize = 16f;
        r3c1Tmp.fontStyle = FontStyles.Bold;
        r3c1Tmp.color = neonCol;

        GameObject r3c2 = CreateUI("TMP_R3C2", r3.transform, new Vector2(520f, 45f), new Vector2(100f, 0f));
        TextMeshProUGUI r3c2Tmp = r3c2.AddComponent<TextMeshProUGUI>();
        if (font != null) r3c2Tmp.font = font;
        r3c2Tmp.text = "Forces are equal in magnitude and opposite in direction (F₁ = -F₂).";
        r3c2Tmp.fontSize = 15f;
        r3c2Tmp.color = whiteCol;

        // Key Observation Box (bottom)
        GameObject obsBox = CreateUI("ObsBox", bg.transform, new Vector2(760f, 95f), new Vector2(0f, -195f));
        obsBox.AddComponent<Image>().color = new Color(0.00f, 0.25f, 0.45f, 0.40f);
        GameObject obsTxt = CreateUI("TMP_Obs", obsBox.transform, new Vector2(720f, 85f), Vector2.zero);
        TextMeshProUGUI obsTmp = obsTxt.AddComponent<TextMeshProUGUI>();
        if (font != null) obsTmp.font = font;
        obsTmp.text = "<b>Final Synthesis:</b>\nTogether, the three laws provide a complete description of classical motion: First Law identifies inertia, Second Law determines acceleration under net force, and Third Law governs interaction conservation.";
        obsTmp.fontSize = 15f;
        obsTmp.alignment = TextAlignmentOptions.TopLeft;
        obsTmp.color = whiteCol;
    }
}
#endif
