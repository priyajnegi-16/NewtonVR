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
                n == "VR_NavTab_StableAnchor" || n == "VR_WorldSpaceNavTabCanvas")
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
        canvasRect.sizeDelta = new Vector2(680f, 520f);

        // Colors
        Color champagneGold = new Color(0.855f, 0.725f, 0.447f, 1.0f); // #DAB972
        Color warmIvory     = new Color(1.000f, 0.973f, 0.910f, 1.0f); // #FFF8E8
        Color pureWhite     = new Color(1.000f, 1.000f, 1.000f, 1.0f); // #FFFFFF
        Color emeraldGreen  = new Color(0.125f, 0.880f, 0.520f, 1.0f); // #20E085
        Color smokedBronze  = new Color(0.110f, 0.090f, 0.078f, 0.96f); // #1C1714
        Color buttonBase    = new Color(0.160f, 0.135f, 0.118f, 0.98f); // #29221E

        // Outer Champagne-Gold Frame
        GameObject frameObj = CreateUI("Panel_GoldFrame", canvasRect, new Vector2(686f, 526f), Vector2.zero);
        Image frameImg = frameObj.AddComponent<Image>();
        frameImg.color = champagneGold;

        // Main Background (Warm Smoked Bronze)
        GameObject bgObj = CreateUI("Panel_Background", frameObj.transform, new Vector2(680f, 520f), Vector2.zero);
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = smokedBronze;

        // Header Emblem Icon: Geometric Diamond in Champagne Gold
        GameObject emblemObj = CreateUI("Header_Emblem", bgObj.transform, new Vector2(16f, 16f), new Vector2(-300f, 222f));
        emblemObj.transform.localEulerAngles = new Vector3(0f, 0f, 45f);
        Image emblemImg = emblemObj.AddComponent<Image>();
        emblemImg.color = champagneGold;

        // Header Title: NEWTONIAN VR LABORATORY (Bold Pure White)
        GameObject titleObj = CreateUI("Header_Title", bgObj.transform, new Vector2(500f, 34f), new Vector2(-20f, 230f));
        TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
        if (font != null) titleTmp.font = font;
        titleTmp.text = "NEWTONIAN VR LABORATORY";
        titleTmp.fontSize = 26f;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Left;
        titleTmp.color = pureWhite;

        // Subtitle: SELECT A ROOM TO EXPLORE (Champagne Gold)
        GameObject subObj = CreateUI("Header_Subtitle", bgObj.transform, new Vector2(500f, 24f), new Vector2(-20f, 204f));
        TextMeshProUGUI subTmp = subObj.AddComponent<TextMeshProUGUI>();
        if (font != null) subTmp.font = font;
        subTmp.text = "SELECT A ROOM TO EXPLORE";
        subTmp.fontSize = 15f;
        subTmp.fontStyle = FontStyles.Normal;
        subTmp.alignment = TextAlignmentOptions.Left;
        subTmp.color = champagneGold;

        // Upper-Right Close Button (✕)
        GameObject closeObj = CreateUI("Btn_Close", bgObj.transform, new Vector2(40f, 40f), new Vector2(305f, 222f));
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

        // Four Large Destination Buttons
        float startY = 135f;
        float spacingY = 82f;

        CreateButton(bgObj.transform, "MAIN LOBBY", "Return to the Central Exhibition Hall", new Vector2(0f, startY), font, champagneGold, buttonBase, emeraldGreen, pureWhite, warmIvory, lobbyTex);
        CreateButton(bgObj.transform, "ROOM 1 — NEWTON'S FIRST LAW", "Inertia and Motion", new Vector2(0f, startY - spacingY), font, champagneGold, buttonBase, emeraldGreen, pureWhite, warmIvory, lab1Tex);
        CreateButton(bgObj.transform, "ROOM 2 — NEWTON'S SECOND LAW", "Force, Mass and Acceleration", new Vector2(0f, startY - spacingY * 2), font, champagneGold, buttonBase, emeraldGreen, pureWhite, warmIvory, lab2Tex);
        CreateButton(bgObj.transform, "ROOM 3 — NEWTON'S THIRD LAW", "Action and Reaction", new Vector2(0f, startY - spacingY * 3), font, champagneGold, buttonBase, emeraldGreen, pureWhite, warmIvory, lab3Tex);

        // Bottom Replay Narration Button
        GameObject replayObj = CreateUI("Btn_ReplayNarration", bgObj.transform, new Vector2(340f, 38f), new Vector2(0f, -218f));
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

        // 3. Small Stationary Navigation Tab (Attached to Player body root anchor at chest height)
        GameObject anchorGo = null;
        if (player != null)
        {
            Transform existingA = player.transform.Find("VR_NavTab_StableAnchor");
            if (existingA != null) anchorGo = existingA.gameObject;
            else
            {
                anchorGo = new GameObject("VR_NavTab_StableAnchor");
                anchorGo.transform.SetParent(player.transform, false);
            }
            anchorGo.transform.localPosition = new Vector3(-0.38f, 1.38f, 0.55f);
            anchorGo.transform.localRotation = Quaternion.Euler(12.0f, 35.0f, 0.0f);
            anchorGo.transform.localScale = Vector3.one;
        }

        GameObject tabCanvasGo = new GameObject("VR_WorldSpaceNavTabCanvas");
        Transform tabParent = (anchorGo != null) ? anchorGo.transform : panelRoot.transform;
        tabCanvasGo.transform.SetParent(tabParent, false);
        tabCanvasGo.transform.localPosition = Vector3.zero;
        tabCanvasGo.transform.localRotation = Quaternion.identity;
        tabCanvasGo.transform.localScale = Vector3.one * 0.001f;

        Canvas tabCanvas = tabCanvasGo.AddComponent<Canvas>();
        tabCanvas.renderMode = RenderMode.WorldSpace;
        tabCanvas.worldCamera = cam;
        CanvasScaler tabScaler = tabCanvasGo.AddComponent<CanvasScaler>();
        tabScaler.dynamicPixelsPerUnit = 10f;
        tabCanvasGo.AddComponent<GraphicRaycaster>();

        RectTransform tabRect = tabCanvasGo.GetComponent<RectTransform>();
        tabRect.sizeDelta = new Vector2(300f, 90f);

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

        tabCanvasGo.SetActive(true); // Active and ready when panel is closed

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
        string cleanName = heading.Replace(" ", "").Replace("—", "").Replace("'", "");
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
}
#endif
