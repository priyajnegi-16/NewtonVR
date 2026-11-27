using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using TMPro;

// Explicit aliases for XR Input
using XRInputDevice = UnityEngine.XR.InputDevice;
using XRInputDevices = UnityEngine.XR.InputDevices;
using XRCommonUsages = UnityEngine.XR.CommonUsages;
using XRNode = UnityEngine.XR.XRNode;
using XRDeviceCharacteristics = UnityEngine.XR.InputDeviceCharacteristics;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Brand-New, High-Fidelity 3D VR World Space Navigation Panel for Meta Quest 3S & PC.
/// Built from scratch with a dedicated World Space Canvas and TextMeshProUGUI:
/// - True World Space Canvas with RectTransform scaling and UI ordering (NO 3D cubes, NO distorted text).
/// - Crisp, readable pure bright white (#FFFFFF) TextMeshProUGUI typography on warm smoked-bronze cards.
/// - Champagne-gold metallic outer frame with glowing emerald-green hover and selection highlights.
/// - Four separated, non-overlapping destination buttons (Main Lobby, Lab 1, Lab 2, Lab 3).
/// - Positioned comfortably to the LEFT (30° offset, arm's reach, chest-to-eye height, not obstructing center view).
/// - Smooth horizontal lazy-follow without jitter, face-attachment, or obscuring forward view.
/// - Full panel opens automatically at headset session startup.
/// - Meta Quest 3S Hand Tracking: detected hand joints, pointing ray, index-to-thumb pinch with emerald orb feedback.
/// - Touch Plus Controllers: Left [X] button to toggle, laser pointer ray, trigger click with haptics.
/// - Small floating "🧭 LABS" tab when hidden (pinch or click to reopen).
/// - Auto-hides after successful room teleportation.
/// - Desktop testing: [N] key, mouse hover & click, [M], [T], [K], [L] shortcuts.
/// - 100% immune to InvalidOperationException via NewtonInput.
/// </summary>
public class FloatingVRDestinationPanel : MonoBehaviour
{
    [Header("Player & XR Rig References")]
    public CharacterController characterController;
    public Camera playerCamera;
    public Transform rightHandController;
    public Transform leftHandController;

    [Header("Panel Positioning & Follow")]
    [Tooltip("Comfortable arm's reach distance in meters.")]
    public float armReachDistance = 1.35f;

    [Tooltip("Angular offset to the LEFT of the user's forward view (degrees).")]
    public float leftAngleOffset = -38.0f; // Positioned cleanly to the left, clear of central Welcome Panel

    [Tooltip("Vertical height offset relative to headset eye level.")]
    public float verticalOffset = 0.02f; // Chest to eye height

    [Tooltip("Upward tilt angle (degrees) toward player eyes.")]
    public float upwardTiltAngle = 10.0f;

    [Tooltip("Smooth follow catchup speed.")]
    public float followSpeed = 4.2f;

    [Tooltip("Angular threshold in degrees before panel catches up.")]
    public float deadzoneAngle = 50.0f;

    [Tooltip("Distance threshold in meters before panel catches up.")]
    public float deadzoneDistance = 0.85f;

    private static readonly InputFeatureUsage<Vector3> s_PointerPosition = new InputFeatureUsage<Vector3>("PointerPosition");
    private static readonly InputFeatureUsage<Quaternion> s_PointerRotation = new InputFeatureUsage<Quaternion>("PointerRotation");

    // Singleton Instance
    public static FloatingVRDestinationPanel Instance { get; private set; }

    // Hand Tracking Public Properties
    public bool IsHandTrackingActive => isHandTrackingActive;
    public bool IsPinching => (leftHandPinching || rightHandPinching || isTestPinching);
    public bool IsPinchJustPressed { get; private set; }
    public Vector3 LastPinchWorldPos => lastPinchWorldPos;
    public Transform ActiveHandTransform => (rightHandVisualObj != null && rightHandVisualObj.activeSelf) ? rightHandVisualObj.transform : ((leftHandVisualObj != null) ? leftHandVisualObj.transform : null);
    public bool HasActiveHandRay { get; private set; }
    public Ray ActiveHandRay { get; private set; }

    // Test Injection Fields for Verification Suite
    private bool isTestHandActive = false;
    private bool isTestPinching = false;
    private Ray testHandRay;
    private bool isTestControllerActive = false;
    private Ray testControllerRay;
    private bool isTestControllerTrigger = false;

    // START LAB in-scene interaction
    private GameObject currentHoveredStartLab = null;
    private Color originalStartLabColor = Color.black;
    private Renderer startLabRenderer = null;

    [Header("Color Palette")]
    public static readonly Color ColorChampagneGold  = new Color(0.855f, 0.725f, 0.447f, 1.0f); // #DAB972
    public static readonly Color ColorWarmIvory      = new Color(1.000f, 0.973f, 0.910f, 1.0f); // #FFF8E8
    public static readonly Color ColorPureWhite      = new Color(1.000f, 1.000f, 1.000f, 1.0f); // #FFFFFF
    public static readonly Color ColorEmeraldGreen   = new Color(0.125f, 0.880f, 0.520f, 1.0f); // #20E085
    public static readonly Color ColorSmokedBronze   = new Color(0.110f, 0.090f, 0.078f, 0.95f); // #1C1714
    public static readonly Color ColorButtonNormal   = new Color(0.160f, 0.135f, 0.118f, 0.98f); // #29221E
    public static readonly Color ColorButtonHover    = new Color(0.120f, 0.280f, 0.200f, 0.98f); // Emerald hover tint
    public static readonly Color ColorButtonActive   = new Color(0.080f, 0.240f, 0.160f, 0.98f); // Active room emerald

    [Header("Destinations & Spawns")]
    public Vector3 mainLobbySpawnPosition = new Vector3(0f, 0f, 5.80f);
    public Vector3 mainLobbySpawnRotation = new Vector3(0f, 180f, 0f);

    public Vector3 lab1SpawnPosition = new Vector3(0f, 0f, -41.0f);
    public Vector3 lab1SpawnRotation = new Vector3(0f, 180f, 0f);

    public Vector3 lab2SpawnPosition = new Vector3(-41.8f, 0f, 0f);
    public Vector3 lab2SpawnRotation = new Vector3(0f, -90f, 0f);

    public Vector3 lab3SpawnPosition = new Vector3(41.8f, 0f, 0f);
    public Vector3 lab3SpawnRotation = new Vector3(0f, 90f, 0f);

    public Vector3 room4SpawnPosition = new Vector3(0f, 0f, 41.0f);
    public Vector3 room4SpawnRotation = new Vector3(0f, 0f, 0f);

    public enum DestinationRoom
    {
        MainLobby = 0,
        Lab1_FirstLaw = 1,
        Lab2_SecondLaw = 2,
        Lab3_ThirdLaw = 3,
        Room4_Observation = 4
    }

    private DestinationRoom currentActiveRoom = DestinationRoom.MainLobby;

    // Full panel visibility: Open automatically at startup!
    private bool isPanelVisible = true;
    public bool IsPanelVisible => isPanelVisible;

    // Canvas Roots & Stable Anchor
    private GameObject panelCanvasGo;
    private RectTransform panelRootRect;
    private GameObject navTabCanvasGo;
    private RectTransform navTabRootRect;
    private GameObject navTabAnchorGo;
    private Transform xrOriginTransform;

    // Room-Anchored Stationary Buttons (Inside MainLobby, Lab 1, Lab 2, Lab 3)
    private List<CanvasDestinationButton> roomNavTabButtons = new List<CanvasDestinationButton>();
    private List<GameObject> roomNavTabCanvasObjects = new List<GameObject>();

    // Ray & Reticle Visuals
    private LineRenderer interactionRayRenderer;
    private GameObject rayReticleDot;
    private GameObject pinchFeedbackOrb;
    private TMP_FontAsset sharedFontAsset;

    // Tracked Hand Visuals
    private GameObject leftHandVisualObj;
    private GameObject rightHandVisualObj;
    private bool isHandTrackingActive = false;

    // Button UI Representation
    public class CanvasDestinationButton
    {
        public DestinationRoom room;
        public GameObject rootObj;
        public RectTransform rect;
        public BoxCollider collider;
        public Image bgImage;
        public Image frameImage;
        public Image accentBarImage;
        public TextMeshProUGUI headingTmp;
        public TextMeshProUGUI descriptionTmp;
        public TextMeshProUGUI arrowTmp;
        public Action onClick;
        public bool isHovered;
    }

    private List<CanvasDestinationButton> destinationButtons = new List<CanvasDestinationButton>();
    public int DestinationButtonCount => destinationButtons != null ? destinationButtons.Count : 0;

    public bool HasDestinationRoom(DestinationRoom room)
    {
        if (destinationButtons == null) return false;
        foreach (var b in destinationButtons)
        {
            if (b.room == room) return true;
        }
        return false;
    }

    public CanvasDestinationButton GetButtonForRoom(DestinationRoom room)
    {
        if (destinationButtons == null) return null;
        foreach (var b in destinationButtons)
        {
            if (b.room == room) return b;
        }
        return null;
    }

    public bool SimulateButtonClick(DestinationRoom room)
    {
        var btn = GetButtonForRoom(room);
        if (btn != null)
        {
            SetButtonHover(btn);
            btn.onClick?.Invoke();
            return true;
        }
        return false;
    }

    private CanvasDestinationButton closeButton;
    private CanvasDestinationButton replayButton;
    private CanvasDestinationButton navTabButton;
    private CanvasDestinationButton currentHoveredButton = null;

    // Inputs & Gestures
    private bool prevSelectionState = false;
    private bool prevLeftX = false;
    private bool prevLeftY = false;
    private bool prevLeftMenu = false;
    private float lastToggleTime = -1f;

    // Hand Pinch Tracking
    private bool leftHandPinching = false;
    private bool rightHandPinching = false;
    private bool prevAnyPinching = false;
    private Vector3 lastPinchWorldPos = Vector3.zero;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInitialize()
    {
        if (FindFirstObjectByType<FloatingVRDestinationPanel>() == null)
        {
            GameObject sys = new GameObject("FloatingVRDestinationPanel_System");
            sys.AddComponent<FloatingVRDestinationPanel>();
            Debug.Log("[Newton VR] World Space 3D Navigation Panel initialized.");
        }
    }

    public void EnsureInitialized()
    {
        FindRigReferences();
        LoadFontAsset();

        if (panelCanvasGo == null)
        {
            Transform existingCanvas = transform.Find("VR_WorldSpaceNavCanvas");
            if (existingCanvas != null)
            {
                panelCanvasGo = existingCanvas.gameObject;
                panelRootRect = panelCanvasGo.GetComponent<RectTransform>();
                BindExistingCanvasButtons();
            }
            else
            {
                BuildWorldSpaceCanvas();
            }
        }

        if (destinationButtons == null || destinationButtons.Count == 0)
        {
            if (panelCanvasGo != null) BindExistingCanvasButtons();
        }

        EnsureRoomNavTabs(false);
    }

    void Awake()
    {
        Instance = this;
        EnsureInitialized();
        SetupPinchFeedbackVisuals();
        SetupHandVisualizers();
        SetupInteractionRay();
    }

    public void BindExistingCanvasButtons()
    {
        if (panelCanvasGo == null)
        {
            Canvas existingCanvas = GetComponentInChildren<Canvas>(true);
            if (existingCanvas != null)
            {
                panelCanvasGo = existingCanvas.gameObject;
                panelRootRect = panelCanvasGo.GetComponent<RectTransform>();
            }
            else
            {
                GameObject found = GameObject.Find("VR_WorldSpaceNavCanvas");
                if (found != null)
                {
                    panelCanvasGo = found;
                    panelRootRect = panelCanvasGo.GetComponent<RectTransform>();
                }
            }
        }

        if (panelCanvasGo == null)
        {
            Debug.LogWarning("[Newton VR] FloatingVRDestinationPanel: panelCanvasGo is null, cannot bind canvas buttons!");
            return;
        }

        destinationButtons.Clear();

        Transform bg = panelCanvasGo.transform.Find("Panel_GoldFrame/Panel_Background");
        if (bg == null)
        {
            Debug.LogWarning("[Newton VR] FloatingVRDestinationPanel: Panel_GoldFrame/Panel_Background not found on canvas!");
            return;
        }

        Transform b1 = null;
        Transform b2 = null;
        Transform b3 = null;
        Transform b4 = null;
        Transform b5 = null;
        Transform closeT = null;
        Transform repT = null;

        // Robust matching across all child elements to prevent naming discrepancies
        foreach (Transform child in bg)
        {
            string n = child.name.ToUpper();
            if (b1 == null && (n.Contains("MAINLOBBY") || (n.Contains("LOBBY") && !n.Contains("START")))) b1 = child;
            else if (b2 == null && (n.Contains("ROOM1") || n.Contains("FIRSTLAW"))) b2 = child;
            else if (b3 == null && (n.Contains("ROOM2") || n.Contains("SECONDLAW"))) b3 = child;
            else if (b4 == null && (n.Contains("ROOM3") || n.Contains("THIRDLAW"))) b4 = child;
            else if (b5 == null && (n.Contains("ROOM4") || n.Contains("OBSERVATION"))) b5 = child;
            else if (closeT == null && (n.Contains("CLOSE") || n.Contains("BTN_CLOSE"))) closeT = child;
            else if (repT == null && (n.Contains("REPLAY") || n.Contains("NARRATION"))) repT = child;
        }

        // Direct path fallbacks if needed
        if (b1 == null) b1 = bg.Find("Btn_MAINLOBBY");
        if (b2 == null) b2 = bg.Find("Btn_ROOM1NEWTONSFIRSTLAW");
        if (b3 == null) b3 = bg.Find("Btn_ROOM2NEWTONSSECONDLAW");
        if (b4 == null) b4 = bg.Find("Btn_ROOM3NEWTONSTHIRDLAW");
        if (b5 == null) b5 = bg.Find("Btn_ROOM4OBSERVATION&DATAANALYSIS") ?? bg.Find("Btn_ROOM4OBSERVATIONDATAANALYSIS");
        if (closeT == null) closeT = bg.Find("Btn_Close");
        if (repT == null) repT = bg.Find("Btn_ReplayNarration");

        if (b1 != null)
        {
            destinationButtons.Add(CreateButtonData(DestinationRoom.MainLobby, b1.gameObject,
                () => TeleportTo(mainLobbySpawnPosition, Quaternion.Euler(mainLobbySpawnRotation), DestinationRoom.MainLobby, "Main Lobby")));
        }

        if (b2 != null)
        {
            destinationButtons.Add(CreateButtonData(DestinationRoom.Lab1_FirstLaw, b2.gameObject,
                () => TeleportTo(lab1SpawnPosition, Quaternion.Euler(lab1SpawnRotation), DestinationRoom.Lab1_FirstLaw, "Room 1 — First Law")));
        }

        if (b3 != null)
        {
            destinationButtons.Add(CreateButtonData(DestinationRoom.Lab2_SecondLaw, b3.gameObject,
                () => TeleportTo(lab2SpawnPosition, Quaternion.Euler(lab2SpawnRotation), DestinationRoom.Lab2_SecondLaw, "Room 2 — Second Law")));
        }

        if (b4 != null)
        {
            destinationButtons.Add(CreateButtonData(DestinationRoom.Lab3_ThirdLaw, b4.gameObject,
                () => TeleportTo(lab3SpawnPosition, Quaternion.Euler(lab3SpawnRotation), DestinationRoom.Lab3_ThirdLaw, "Room 3 — Third Law")));
        }

        if (b5 != null)
        {
            destinationButtons.Add(CreateButtonData(DestinationRoom.Room4_Observation, b5.gameObject,
                () => TeleportTo(room4SpawnPosition, Quaternion.Euler(room4SpawnRotation), DestinationRoom.Room4_Observation, "Room 4 — Observation & Data Analysis")));
            Debug.Log($"[Newton VR] Bound Room 4 Destination Button successfully: {b5.name} (Total Destination Buttons: {destinationButtons.Count})");
        }
        else
        {
            Debug.LogError("[Newton VR] FAILED to locate Room 4 Destination Button under Panel_Background!");
        }

        if (closeT != null)
        {
            closeButton = CreateButtonData((DestinationRoom)(-1), closeT.gameObject, () => SetPanelVisibility(false));
        }

        if (repT != null)
        {
            replayButton = CreateButtonData((DestinationRoom)(-2), repT.gameObject, () =>
            {
                if (NarrationManager.Instance != null) NarrationManager.Instance.ReplayCurrentStation();
            });
        }
    }

    private CanvasDestinationButton CreateButtonData(DestinationRoom room, GameObject go, Action onClick)
    {
        BoxCollider col = go.GetComponent<BoxCollider>();
        if (col == null) col = go.AddComponent<BoxCollider>();

        Image bg = go.transform.Find("Btn_Frame/Btn_Fill")?.GetComponent<Image>()
                   ?? go.transform.Find("Close_Frame/Close_Fill")?.GetComponent<Image>()
                   ?? go.transform.Find("Replay_Frame/Replay_Fill")?.GetComponent<Image>();
        Image fr = go.transform.Find("Btn_Frame")?.GetComponent<Image>()
                   ?? go.transform.Find("Close_Frame")?.GetComponent<Image>()
                   ?? go.transform.Find("Replay_Frame")?.GetComponent<Image>();
        Image ac = go.transform.Find("Btn_Frame/Btn_Fill/Btn_Accent")?.GetComponent<Image>();
        TextMeshProUGUI h = go.transform.Find("Btn_Frame/Btn_Fill/TMP_Heading")?.GetComponent<TextMeshProUGUI>()
                            ?? go.transform.Find("Close_Frame/Close_Fill/TMP_Close")?.GetComponent<TextMeshProUGUI>()
                            ?? go.transform.Find("Replay_Frame/Replay_Fill/TMP_Replay")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI d = go.transform.Find("Btn_Frame/Btn_Fill/TMP_Description")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI a = go.transform.Find("Btn_Frame/Btn_Fill/TMP_Arrow")?.GetComponent<TextMeshProUGUI>()
                            ?? go.transform.Find("Btn_Frame/Btn_Fill/Btn_ActionBadge/Action_Frame/Action_Fill/TMP_Arrow")?.GetComponent<TextMeshProUGUI>();

        return new CanvasDestinationButton
        {
            room = room,
            rootObj = go,
            rect = go.GetComponent<RectTransform>(),
            collider = col,
            bgImage = bg,
            frameImage = fr,
            accentBarImage = ac,
            headingTmp = h,
            descriptionTmp = d,
            arrowTmp = a,
            onClick = onClick,
            isHovered = false
        };
    }

    private CanvasDestinationButton BindRoomNavTabButton(GameObject tabCanvasGo)
    {
        if (tabCanvasGo == null) return null;

        RectTransform rootRt = tabCanvasGo.GetComponent<RectTransform>();
        if (rootRt == null) rootRt = tabCanvasGo.AddComponent<RectTransform>();
        rootRt.sizeDelta = new Vector2(300f, 90f);

        BoxCollider col = tabCanvasGo.GetComponent<BoxCollider>();
        if (col == null) col = tabCanvasGo.AddComponent<BoxCollider>();
        col.size = new Vector3(450f, 180f, 250f);
        col.center = Vector3.zero;

        Image fr = tabCanvasGo.transform.Find("Tab_Frame")?.GetComponent<Image>();
        Image bg = tabCanvasGo.transform.Find("Tab_Frame/Tab_Fill")?.GetComponent<Image>();
        Image dot = tabCanvasGo.transform.Find("Tab_Frame/Tab_Fill/Tab_Dot")?.GetComponent<Image>();
        TextMeshProUGUI txt = tabCanvasGo.transform.Find("Tab_Frame/Tab_Fill/TMP_NavTab")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI sub = tabCanvasGo.transform.Find("Tab_Frame/Tab_Fill/TMP_NavSub")?.GetComponent<TextMeshProUGUI>();

        CanvasDestinationButton btn = new CanvasDestinationButton
        {
            room = (DestinationRoom)(-3),
            rootObj = tabCanvasGo,
            rect = rootRt,
            collider = col,
            bgImage = bg,
            frameImage = fr,
            accentBarImage = dot,
            headingTmp = txt,
            descriptionTmp = sub,
            arrowTmp = null,
            onClick = () =>
            {
                SetPanelVisibility(true);
                SnapToLeftPosition();
            },
            isHovered = false
        };

        return btn;
    }

    private void BindExistingTabButton()
    {
        EnsureRoomNavTabs();
    }


    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        Instance = this;
        FindRigReferences();
        EnsureNavTabAnchor(true);

        // Suppress any legacy basic cube menu from VRTeleportController
        VRTeleportController vtc = FindFirstObjectByType<VRTeleportController>();
        if (vtc != null)
        {
            Transform oldMenu = vtc.transform.Find("VR_DestinationMenu");
            if (oldMenu != null) oldMenu.gameObject.SetActive(false);
        }

        // Start with stable LABS button visible and reachable in front of player!
        SetPanelVisibility(false);
        SnapToLeftPosition();
        RefreshActiveRoomVisuals();
    }

    private void FindRigReferences()
    {
        if (characterController == null) characterController = FindFirstObjectByType<CharacterController>();

        if (playerCamera == null)
        {
            Camera main = Camera.main;
            if (main != null) playerCamera = main;
            else playerCamera = FindFirstObjectByType<Camera>();
        }

        if (rightHandController == null || leftHandController == null)
        {
            PlayerController pc = FindFirstObjectByType<PlayerController>();
            if (pc != null)
            {
                if (rightHandController == null) rightHandController = pc.rightHandController;
                if (leftHandController == null) leftHandController = pc.leftHandController;
            }
        }

        if (xrOriginTransform == null)
        {
            if (characterController != null) xrOriginTransform = characterController.transform;
            else if (playerCamera != null) xrOriginTransform = playerCamera.transform.parent != null ? playerCamera.transform.parent : playerCamera.transform;
        }
    }

    public void EnsureRoomNavTabs(bool forceUpdate = false)
    {
        if (characterController == null || playerCamera == null) FindRigReferences();

        // STRICT USER REQUIREMENT: Absolutely NO LABS button attached to Player, XR Origin, Camera, or hands!
        if (xrOriginTransform != null)
        {
            Transform oldAnchor = xrOriginTransform.Find("VR_NavTab_StableAnchor");
            if (oldAnchor != null) DestroyImmediate(oldAnchor.gameObject);
            Transform oldTab = xrOriginTransform.Find("VR_WorldSpaceNavTabCanvas");
            if (oldTab != null) DestroyImmediate(oldTab.gameObject);
        }
        GameObject strayAnchor = GameObject.Find("VR_NavTab_StableAnchor");
        if (strayAnchor != null) DestroyImmediate(strayAnchor);
        Transform tabOnSelf = transform.Find("VR_WorldSpaceNavTabCanvas");
        if (tabOnSelf != null) DestroyImmediate(tabOnSelf.gameObject);

        // Fixed Room Spawn Positions and Orientations
        // Button is placed inside EACH room near spawn: 0.55m forward, 0.38m to the left, standing chest height 1.55m (+17cm raised, upright facing player)
        // Permanently anchored to room environment!
        var roomDefs = new (string name, Vector3 pos, Quaternion rot, DestinationRoom room)[]
        {
            ("VR_RoomNavTab_MainLobby", new Vector3(0.38f, 1.55f, 5.25f), Quaternion.Euler(0.0f, 180.0f, 0.0f), DestinationRoom.MainLobby),
            ("VR_RoomNavTab_Lab1", new Vector3(0.38f, 1.55f, -41.55f), Quaternion.Euler(0.0f, 180.0f, 0.0f), DestinationRoom.Lab1_FirstLaw),
            ("VR_RoomNavTab_Lab2", new Vector3(-42.35f, 1.55f, -0.38f), Quaternion.Euler(0.0f, -90.0f, 0.0f), DestinationRoom.Lab2_SecondLaw),
            ("VR_RoomNavTab_Lab3", new Vector3(42.35f, 1.55f, 0.38f), Quaternion.Euler(0.0f, 90.0f, 0.0f), DestinationRoom.Lab3_ThirdLaw),
            ("VR_RoomNavTab_Room4", new Vector3(-0.38f, 1.55f, 41.55f), Quaternion.Euler(0.0f, 0.0f, 0.0f), DestinationRoom.Room4_Observation)
        };

        // Environment parent root
        GameObject envRoot = GameObject.Find("Environment_StationaryNavTabs");
        if (envRoot == null)
        {
            envRoot = new GameObject("Environment_StationaryNavTabs");
            envRoot.transform.position = Vector3.zero;
            envRoot.transform.rotation = Quaternion.identity;
        }

        roomNavTabButtons.Clear();
        roomNavTabCanvasObjects.Clear();

        foreach (var def in roomDefs)
        {
            GameObject tabGo = GameObject.Find(def.name);
            if (tabGo == null && envRoot != null)
            {
                Transform foundChild = envRoot.transform.Find(def.name);
                if (foundChild != null) tabGo = foundChild.gameObject;
            }

            if (tabGo == null)
            {
                tabGo = BuildRoomNavTab(def.name, def.pos, def.rot, envRoot.transform);
            }
            else
            {
                // Ensure firmly anchored to environment, NEVER to player
                if (tabGo.transform.parent != envRoot.transform)
                {
                    tabGo.transform.SetParent(envRoot.transform, true);
                }
                tabGo.transform.position = def.pos;
                tabGo.transform.rotation = def.rot;
                tabGo.transform.localScale = Vector3.one * 0.001f;
            }

            CanvasDestinationButton btn = BindRoomNavTabButton(tabGo);
            if (btn != null)
            {
                roomNavTabButtons.Add(btn);
                roomNavTabCanvasObjects.Add(tabGo);
            }
        }

        if (roomNavTabButtons.Count > 0)
        {
            navTabButton = roomNavTabButtons[0];
            navTabCanvasGo = roomNavTabCanvasObjects[0];
            navTabRootRect = navTabCanvasGo.GetComponent<RectTransform>();
        }
    }

    public void EnsureNavTabAnchor(bool forceUpdate = false)
    {
        EnsureRoomNavTabs(forceUpdate);
    }

    private void LoadFontAsset()
    {
        if (TMP_Settings.defaultFontAsset != null)
        {
            sharedFontAsset = TMP_Settings.defaultFontAsset;
            return;
        }

        sharedFontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (sharedFontAsset == null)
        {
            TMP_FontAsset[] fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            if (fonts != null && fonts.Length > 0)
            {
                sharedFontAsset = fonts[0];
            }
        }
    }

    #region World Space Canvas Construction

    private void BuildWorldSpaceCanvas()
    {
        panelCanvasGo = new GameObject("VR_WorldSpaceNavCanvas");
        panelCanvasGo.transform.SetParent(transform, true);

        Canvas canvas = panelCanvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = playerCamera;

        CanvasScaler scaler = panelCanvasGo.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;
        scaler.referencePixelsPerUnit = 100f;

        panelCanvasGo.AddComponent<GraphicRaycaster>();

        panelRootRect = panelCanvasGo.GetComponent<RectTransform>();
        panelRootRect.sizeDelta = new Vector2(680f, 580f);
        panelRootRect.localScale = Vector3.one * 0.001f; // Compact: 0.68m wide x 0.58m tall in world space

        // 1. Outer Champagne-Gold Glowing Frame
        GameObject frameObj = CreateUIElement("Panel_GoldFrame", panelRootRect, new Vector2(686f, 586f), Vector2.zero);
        Image frameImg = frameObj.AddComponent<Image>();
        frameImg.color = ColorChampagneGold;

        // 2. Main Background: Warm Smoked-Bronze
        GameObject bgObj = CreateUIElement("Panel_Background", frameObj.transform, new Vector2(680f, 580f), Vector2.zero);
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = ColorSmokedBronze;

        // 3. Top Header Bar
        // Emblem Icon: Geometric Diamond in Champagne Gold
        GameObject emblemObj = CreateUIElement("Header_Emblem", bgObj.transform, new Vector2(16f, 16f), new Vector2(-300f, 252f));
        emblemObj.transform.localEulerAngles = new Vector3(0f, 0f, 45f);
        Image emblemImg = emblemObj.AddComponent<Image>();
        emblemImg.color = ColorChampagneGold;

        // Header Title: NEWTONIAN VR LABORATORY (Bold Pure White)
        GameObject titleObj = CreateUIElement("Header_Title", bgObj.transform, new Vector2(500f, 34f), new Vector2(-20f, 260f));
        TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
        if (sharedFontAsset != null) titleTmp.font = sharedFontAsset;
        titleTmp.text = "NEWTONIAN VR LABORATORY";
        titleTmp.fontSize = 26f;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Left;
        titleTmp.color = ColorPureWhite;

        // Subtitle: SELECT A ROOM TO EXPLORE (Champagne Gold)
        GameObject subObj = CreateUIElement("Header_Subtitle", bgObj.transform, new Vector2(500f, 24f), new Vector2(-20f, 234f));
        TextMeshProUGUI subTmp = subObj.AddComponent<TextMeshProUGUI>();
        if (sharedFontAsset != null) subTmp.font = sharedFontAsset;
        subTmp.text = "SELECT A ROOM TO EXPLORE";
        subTmp.fontSize = 15f;
        subTmp.fontStyle = FontStyles.Normal;
        subTmp.alignment = TextAlignmentOptions.Left;
        subTmp.color = ColorChampagneGold;

        // Upper-Right Close Button (✕)
        CreateCanvasCloseButton(bgObj.transform, new Vector2(305f, 252f));

        // 4. Five Large, Clearly Separated Functional Destination Buttons
        float startY = 160f;
        float spacingY = 70f;

        // Button 1: Main Lobby
        CreateCanvasDestinationButton(
            bgObj.transform,
            DestinationRoom.MainLobby,
            "MAIN LOBBY",
            "Return to the Central Exhibition Hall",
            new Vector2(0f, startY),
            () => TeleportTo(mainLobbySpawnPosition, Quaternion.Euler(mainLobbySpawnRotation), DestinationRoom.MainLobby, "Main Lobby")
        );

        // Button 2: Room 1 — Newton's First Law
        CreateCanvasDestinationButton(
            bgObj.transform,
            DestinationRoom.Lab1_FirstLaw,
            "ROOM 1 — NEWTON'S FIRST LAW",
            "Inertia and Motion",
            new Vector2(0f, startY - spacingY),
            () => TeleportTo(lab1SpawnPosition, Quaternion.Euler(lab1SpawnRotation), DestinationRoom.Lab1_FirstLaw, "Room 1 — First Law")
        );

        // Button 3: Room 2 — Newton's Second Law
        CreateCanvasDestinationButton(
            bgObj.transform,
            DestinationRoom.Lab2_SecondLaw,
            "ROOM 2 — NEWTON'S SECOND LAW",
            "Force, Mass and Acceleration",
            new Vector2(0f, startY - spacingY * 2),
            () => TeleportTo(lab2SpawnPosition, Quaternion.Euler(lab2SpawnRotation), DestinationRoom.Lab2_SecondLaw, "Room 2 — Second Law")
        );

        // Button 4: Room 3 — Newton's Third Law
        CreateCanvasDestinationButton(
            bgObj.transform,
            DestinationRoom.Lab3_ThirdLaw,
            "ROOM 3 — NEWTON'S THIRD LAW",
            "Action and Reaction",
            new Vector2(0f, startY - spacingY * 3),
            () => TeleportTo(lab3SpawnPosition, Quaternion.Euler(lab3SpawnRotation), DestinationRoom.Lab3_ThirdLaw, "Room 3 — Third Law")
        );

        // Button 5: Room 4 — Observation & Data Analysis
        CreateCanvasDestinationButton(
            bgObj.transform,
            DestinationRoom.Room4_Observation,
            "ROOM 4 — OBSERVATION & DATA ANALYSIS",
            "Review, Compare and Understand",
            new Vector2(0f, startY - spacingY * 4),
            () => TeleportTo(room4SpawnPosition, Quaternion.Euler(room4SpawnRotation), DestinationRoom.Room4_Observation, "Room 4 — Observation & Data Analysis")
        );

        // 5. Bottom Bar: Replay Narration
        CreateCanvasReplayButton(bgObj.transform, new Vector2(0f, -220f));
    }

    private void CreateCanvasDestinationButton(Transform parent, DestinationRoom roomType, string heading, string description, Vector2 anchoredPos, Action onClickAction)
    {
        // Button Container
        GameObject btnObj = CreateUIElement($"Btn_{heading.Replace(" ", "").Replace("—", "")}", parent, new Vector2(630f, 70f), anchoredPos);
        RectTransform btnRect = btnObj.GetComponent<RectTransform>();

        // Outer Gold Frame
        GameObject frameObj = CreateUIElement("Btn_Frame", btnRect, new Vector2(632f, 72f), Vector2.zero);
        Image frameImg = frameObj.AddComponent<Image>();
        frameImg.color = ColorChampagneGold * 0.70f;

        // Button Background Image
        GameObject fillObj = CreateUIElement("Btn_Fill", frameObj.transform, new Vector2(628f, 68f), Vector2.zero);
        Image fillImg = fillObj.AddComponent<Image>();
        fillImg.color = ColorButtonNormal;

        // Left Emerald Status Indicator Bar
        GameObject accentObj = CreateUIElement("Btn_Accent", fillObj.transform, new Vector2(8f, 68f), new Vector2(-310f, 0f));
        Image accentImg = accentObj.AddComponent<Image>();
        accentImg.color = ColorEmeraldGreen;

        // Bright Pure White Heading (#FFFFFF)
        GameObject headObj = CreateUIElement("TMP_Heading", fillObj.transform, new Vector2(530f, 26f), new Vector2(-25f, 13f));
        TextMeshProUGUI headTmp = headObj.AddComponent<TextMeshProUGUI>();
        if (sharedFontAsset != null) headTmp.font = sharedFontAsset;
        headTmp.text = heading;
        headTmp.fontSize = 20f;
        headTmp.fontStyle = FontStyles.Bold;
        headTmp.alignment = TextAlignmentOptions.Left;
        headTmp.color = ColorPureWhite;

        // Warm Ivory Description (#FFF8E8)
        GameObject descObj = CreateUIElement("TMP_Description", fillObj.transform, new Vector2(530f, 22f), new Vector2(-25f, -14f));
        TextMeshProUGUI descTmp = descObj.AddComponent<TextMeshProUGUI>();
        if (sharedFontAsset != null) descTmp.font = sharedFontAsset;
        descTmp.text = description;
        descTmp.fontSize = 14f;
        descTmp.fontStyle = FontStyles.Normal;
        descTmp.alignment = TextAlignmentOptions.Left;
        descTmp.color = ColorWarmIvory;

        // Right Champagne-Gold Arrow
        GameObject arrowObj = CreateUIElement("TMP_Arrow", fillObj.transform, new Vector2(30f, 30f), new Vector2(285f, 0f));
        TextMeshProUGUI arrowTmp = arrowObj.AddComponent<TextMeshProUGUI>();
        if (sharedFontAsset != null) arrowTmp.font = sharedFontAsset;
        arrowTmp.text = ">";
        arrowTmp.fontSize = 24f;
        arrowTmp.fontStyle = FontStyles.Bold;
        arrowTmp.alignment = TextAlignmentOptions.Center;
        arrowTmp.color = ColorChampagneGold;

        // Physical BoxCollider for precise 3D Raycasting
        BoxCollider col = btnObj.AddComponent<BoxCollider>();
        col.size = new Vector3(630f, 70f, 20f);
        col.center = Vector3.zero;

        CanvasDestinationButton btnData = new CanvasDestinationButton
        {
            room = roomType,
            rootObj = btnObj,
            rect = btnRect,
            collider = col,
            bgImage = fillImg,
            frameImage = frameImg,
            accentBarImage = accentImg,
            headingTmp = headTmp,
            descriptionTmp = descTmp,
            arrowTmp = arrowTmp,
            onClick = onClickAction,
            isHovered = false
        };

        destinationButtons.Add(btnData);
    }

    private void CreateCanvasCloseButton(Transform parent, Vector2 anchoredPos)
    {
        GameObject closeObj = CreateUIElement("Btn_Close", parent, new Vector2(40f, 40f), anchoredPos);
        RectTransform closeRect = closeObj.GetComponent<RectTransform>();

        GameObject frameObj = CreateUIElement("Close_Frame", closeRect, new Vector2(42f, 42f), Vector2.zero);
        Image frameImg = frameObj.AddComponent<Image>();
        frameImg.color = ColorChampagneGold * 0.8f;

        GameObject fillObj = CreateUIElement("Close_Fill", frameObj.transform, new Vector2(38f, 38f), Vector2.zero);
        Image fillImg = fillObj.AddComponent<Image>();
        fillImg.color = new Color(0.20f, 0.16f, 0.14f);

        GameObject textObj = CreateUIElement("TMP_Close", fillObj.transform, new Vector2(38f, 38f), Vector2.zero);
        TextMeshProUGUI closeTmp = textObj.AddComponent<TextMeshProUGUI>();
        if (sharedFontAsset != null) closeTmp.font = sharedFontAsset;
        closeTmp.text = "X";
        closeTmp.fontSize = 20f;
        closeTmp.fontStyle = FontStyles.Bold;
        closeTmp.alignment = TextAlignmentOptions.Center;
        closeTmp.color = ColorPureWhite;

        BoxCollider col = closeObj.AddComponent<BoxCollider>();
        col.size = new Vector3(60f, 60f, 30f);
        col.center = Vector3.zero;

        closeButton = new CanvasDestinationButton
        {
            room = (DestinationRoom)(-1),
            rootObj = closeObj,
            rect = closeRect,
            collider = col,
            bgImage = fillImg,
            frameImage = frameImg,
            accentBarImage = null,
            headingTmp = closeTmp,
            descriptionTmp = null,
            arrowTmp = null,
            onClick = () => SetPanelVisibility(false),
            isHovered = false
        };
    }

    private void CreateCanvasReplayButton(Transform parent, Vector2 anchoredPos)
    {
        GameObject replayObj = CreateUIElement("Btn_ReplayNarration", parent, new Vector2(340f, 38f), anchoredPos);
        RectTransform replayRect = replayObj.GetComponent<RectTransform>();

        GameObject frameObj = CreateUIElement("Replay_Frame", replayRect, new Vector2(342f, 40f), Vector2.zero);
        Image frameImg = frameObj.AddComponent<Image>();
        frameImg.color = ColorChampagneGold * 0.65f;

        GameObject fillObj = CreateUIElement("Replay_Fill", frameObj.transform, new Vector2(338f, 36f), Vector2.zero);
        Image fillImg = fillObj.AddComponent<Image>();
        fillImg.color = new Color(0.18f, 0.15f, 0.13f);

        GameObject textObj = CreateUIElement("TMP_Replay", fillObj.transform, new Vector2(338f, 36f), Vector2.zero);
        TextMeshProUGUI replayTmp = textObj.AddComponent<TextMeshProUGUI>();
        if (sharedFontAsset != null) replayTmp.font = sharedFontAsset;
        replayTmp.text = "REPLAY AUDIO NARRATION";
        replayTmp.fontSize = 14f;
        replayTmp.fontStyle = FontStyles.Bold;
        replayTmp.alignment = TextAlignmentOptions.Center;
        replayTmp.color = ColorWarmIvory;

        BoxCollider col = replayObj.AddComponent<BoxCollider>();
        col.size = new Vector3(340f, 38f, 20f);
        col.center = Vector3.zero;

        replayButton = new CanvasDestinationButton
        {
            room = (DestinationRoom)(-2),
            rootObj = replayObj,
            rect = replayRect,
            collider = col,
            bgImage = fillImg,
            frameImage = frameImg,
            accentBarImage = null,
            headingTmp = replayTmp,
            descriptionTmp = null,
            arrowTmp = null,
            onClick = () =>
            {
                if (NarrationManager.Instance != null)
                {
                    NarrationManager.Instance.ReplayCurrentStation();
                }
            },
            isHovered = false
        };
    }

    /// <summary>
    /// Builds a permanently stationary, room-anchored World Space LABS navigation button inside a designated room.
    /// Anchored to the room's environment, strictly independent of the player body or headset.
    /// </summary>
    private GameObject BuildRoomNavTab(string name, Vector3 worldPos, Quaternion worldRot, Transform parent)
    {
        LoadFontAsset();
        GameObject tabCanvasGo = new GameObject(name);
        if (parent != null) tabCanvasGo.transform.SetParent(parent, false);
        tabCanvasGo.transform.position = worldPos;
        tabCanvasGo.transform.rotation = worldRot;
        tabCanvasGo.transform.localScale = Vector3.one * 0.001f;

        Canvas canvas = tabCanvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = playerCamera;

        CanvasScaler scaler = tabCanvasGo.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;
        scaler.referencePixelsPerUnit = 100f;

        tabCanvasGo.AddComponent<GraphicRaycaster>();

        RectTransform rootRt = tabCanvasGo.GetComponent<RectTransform>();
        rootRt.sizeDelta = new Vector2(300f, 90f);

        GameObject frameObj = CreateUIElement("Tab_Frame", rootRt, new Vector2(304f, 94f), Vector2.zero);
        Image frameImg = frameObj.AddComponent<Image>();
        frameImg.color = ColorChampagneGold;

        GameObject fillObj = CreateUIElement("Tab_Fill", frameObj.transform, new Vector2(298f, 88f), Vector2.zero);
        Image fillImg = fillObj.AddComponent<Image>();
        fillImg.color = ColorButtonNormal;

        // Emerald Indicator Dot
        GameObject dotObj = CreateUIElement("Tab_Dot", fillObj.transform, new Vector2(18f, 18f), new Vector2(-115f, 0f));
        Image dotImg = dotObj.AddComponent<Image>();
        dotImg.color = ColorEmeraldGreen;

        // Primary Label: LABS (Bold Pure White)
        GameObject textObj = CreateUIElement("TMP_NavTab", fillObj.transform, new Vector2(210f, 45f), new Vector2(15f, 10f));
        TextMeshProUGUI tm = textObj.AddComponent<TextMeshProUGUI>();
        if (sharedFontAsset != null) tm.font = sharedFontAsset;
        tm.text = "LABS";
        tm.fontSize = 34f;
        tm.fontStyle = FontStyles.Bold;
        tm.alignment = TextAlignmentOptions.Center;
        tm.color = ColorPureWhite;

        // Subtitle: SELECT ROOM (Bold Champagne Gold)
        GameObject subObj = CreateUIElement("TMP_NavSub", fillObj.transform, new Vector2(210f, 24f), new Vector2(15f, -22f));
        TextMeshProUGUI subTm = subObj.AddComponent<TextMeshProUGUI>();
        if (sharedFontAsset != null) subTm.font = sharedFontAsset;
        subTm.text = "SELECT ROOM";
        subTm.fontSize = 14f;
        subTm.fontStyle = FontStyles.Bold;
        subTm.alignment = TextAlignmentOptions.Center;
        subTm.color = ColorChampagneGold;

        BoxCollider col = tabCanvasGo.AddComponent<BoxCollider>();
        col.size = new Vector3(450f, 180f, 250f);
        col.center = Vector3.zero;

        tabCanvasGo.SetActive(!isPanelVisible);
        return tabCanvasGo;
    }

    private void BuildNavigationTabCanvas()
    {
        EnsureRoomNavTabs(true);
    }

    private GameObject CreateUIElement(string name, Transform parent, Vector2 size, Vector2 anchoredPos)
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

    #endregion

    #region Interaction Rays & Visuals

    private void SetupInteractionRay()
    {
        GameObject rayObj = new GameObject("VR_DestinationRay");
        rayObj.transform.SetParent(transform, false);

        interactionRayRenderer = rayObj.AddComponent<LineRenderer>();
        interactionRayRenderer.useWorldSpace = true;
        interactionRayRenderer.startWidth = 0.005f;
        interactionRayRenderer.endWidth = 0.002f;
        interactionRayRenderer.positionCount = 2;
        interactionRayRenderer.enabled = false;

        Material rayMat = new Material(Shader.Find("Sprites/Default"));
        Color startCol = new Color(0.0f, 0.88f, 1.0f, 0.90f); // Luminous electric cyan laser (reference image)
        Color endCol = new Color(0.125f, 0.95f, 0.75f, 0.35f); // Soft cyan/emerald tint
        rayMat.color = Color.white;
        interactionRayRenderer.material = rayMat;
        interactionRayRenderer.startColor = startCol;
        interactionRayRenderer.endColor = endCol;
        interactionRayRenderer.startWidth = 0.0055f;
        interactionRayRenderer.endWidth = 0.0020f;

        // Reticle Dot
        rayReticleDot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        rayReticleDot.name = "VR_DestinationRayReticle";
        rayReticleDot.transform.SetParent(transform, false);
        rayReticleDot.transform.localScale = Vector3.one * 0.024f;
        Collider col = rayReticleDot.GetComponent<Collider>();
        if (col) Destroy(col);

        Material dotMat = new Material(Shader.Find("Standard") ?? Shader.Find("Unlit/Color"));
        Color dotCol = new Color(0.0f, 0.92f, 1.0f, 1.0f);
        dotMat.color = dotCol;
        dotMat.EnableKeyword("_EMISSION");
        dotMat.SetColor("_EmissionColor", dotCol * 3.0f);
        rayReticleDot.GetComponent<Renderer>().sharedMaterial = dotMat;
        rayReticleDot.SetActive(false);
    }

    private void SetupPinchFeedbackVisuals()
    {
        pinchFeedbackOrb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pinchFeedbackOrb.name = "VR_PinchFeedbackOrb";
        pinchFeedbackOrb.transform.SetParent(transform, false);
        pinchFeedbackOrb.transform.localScale = Vector3.one * 0.028f;
        Collider col = pinchFeedbackOrb.GetComponent<Collider>();
        if (col) Destroy(col);

        Material pMat = new Material(Shader.Find("Standard") ?? Shader.Find("Unlit/Color")) { hideFlags = HideFlags.HideAndDontSave };
        pMat.color = ColorEmeraldGreen;
        pMat.EnableKeyword("_EMISSION");
        pMat.SetColor("_EmissionColor", ColorEmeraldGreen * 3.0f);
        pinchFeedbackOrb.GetComponent<Renderer>().sharedMaterial = pMat;
        pinchFeedbackOrb.SetActive(false);
    }

    private void SetupHandVisualizers()
    {
        Transform existingLeft = transform.Find("TrackedHand_Left");
        if (existingLeft != null) DestroyImmediate(existingLeft.gameObject);
        Transform existingRight = transform.Find("TrackedHand_Right");
        if (existingRight != null) DestroyImmediate(existingRight.gameObject);

        leftHandVisualObj = CreateHandMeshObject("TrackedHand_Left", false);
        rightHandVisualObj = CreateHandMeshObject("TrackedHand_Right", true);
        leftHandVisualObj.SetActive(false);
        rightHandVisualObj.SetActive(false);
    }

    private GameObject CreateHandMeshObject(string name, bool isRight)
    {
        GameObject handRoot = new GameObject(name);
        handRoot.transform.SetParent(transform, false);

        Material handMat = new Material(Shader.Find("Standard") ?? Shader.Find("Unlit/Color")) { hideFlags = HideFlags.HideAndDontSave };
        handMat.color = new Color(0.15f, 0.18f, 0.22f, 0.85f);
        handMat.SetFloat("_Metallic", 0.80f);
        handMat.SetFloat("_Glossiness", 0.75f);
        handMat.EnableKeyword("_EMISSION");
        handMat.SetColor("_EmissionColor", ColorChampagneGold * 0.35f);

        GameObject palm = GameObject.CreatePrimitive(PrimitiveType.Cube);
        palm.name = "Palm";
        palm.transform.SetParent(handRoot.transform, false);
        palm.transform.localPosition = new Vector3(0f, 0f, 0.04f);
        palm.transform.localScale = new Vector3(0.065f, 0.015f, 0.075f);
        Collider pc = palm.GetComponent<Collider>();
        if (pc) Destroy(pc);
        palm.GetComponent<Renderer>().sharedMaterial = handMat;

        GameObject indexTip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        indexTip.name = "IndexTip";
        indexTip.transform.SetParent(handRoot.transform, false);
        indexTip.transform.localPosition = new Vector3(isRight ? 0.02f : -0.02f, 0f, 0.12f);
        indexTip.transform.localScale = Vector3.one * 0.016f;
        Collider ic = indexTip.GetComponent<Collider>();
        if (ic) Destroy(ic);

        Material tipMat = new Material(Shader.Find("Standard") ?? Shader.Find("Unlit/Color")) { hideFlags = HideFlags.HideAndDontSave };
        tipMat.color = ColorEmeraldGreen;
        tipMat.EnableKeyword("_EMISSION");
        tipMat.SetColor("_EmissionColor", ColorEmeraldGreen * 1.8f);
        indexTip.GetComponent<Renderer>().sharedMaterial = tipMat;

        GameObject thumbTip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        thumbTip.name = "ThumbTip";
        thumbTip.transform.SetParent(handRoot.transform, false);
        thumbTip.transform.localPosition = new Vector3(isRight ? -0.028f : 0.028f, -0.015f, 0.08f);
        thumbTip.transform.localScale = Vector3.one * 0.015f;
        Collider tc = thumbTip.GetComponent<Collider>();
        if (tc) Destroy(tc);
        thumbTip.GetComponent<Renderer>().sharedMaterial = tipMat;

        return handRoot;
    }

    #endregion

    void Update()
    {
        HandleVisibilityToggleInputs();
        UpdateHandTracking();

        if (isPanelVisible)
        {
            UpdateLazyFollow(leftAngleOffset);
            UpdateInteraction(true);
        }
        else
        {
            // Room-anchored stationary buttons (permanently in room environment, strictly zero drift)
            UpdateInteraction(false);
        }
    }

    #region Hand Tracking & Pinch Detection

    private bool IsTrackedHandDevice(XRInputDevice dev, bool isRight)
    {
        if (!dev.isValid) return false;

        // 1. Explicit HandTracking characteristic
        if ((dev.characteristics & XRDeviceCharacteristics.HandTracking) != 0) return true;

        // 2. Name contains "Hand" but not "Controller"
        string devName = dev.name;
        if (!string.IsNullOrEmpty(devName))
        {
            if (devName.IndexOf("Hand", StringComparison.OrdinalIgnoreCase) >= 0 &&
                devName.IndexOf("Controller", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return true;
            }
        }

        // 3. TrackedDevice at Hand node without Controller characteristic
        if ((dev.characteristics & XRDeviceCharacteristics.Controller) == 0 &&
            (dev.characteristics & XRDeviceCharacteristics.TrackedDevice) != 0)
        {
            return true;
        }

        return false;
    }

    private void UpdateHandTracking()
    {
        isHandTrackingActive = false;

        // 1. Check UnityEngine.XR.InputDevices
        XRInputDevice leftHandDev = XRInputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        bool leftIsHand = IsTrackedHandDevice(leftHandDev, false);
        if (leftIsHand)
        {
            isHandTrackingActive = true;
            UpdateHandPose(leftHandDev, leftHandVisualObj, false, ref leftHandPinching);
        }
        else
        {
            if (leftHandVisualObj != null) leftHandVisualObj.SetActive(false);
            leftHandPinching = false;
        }

        XRInputDevice rightHandDev = XRInputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        bool rightIsHand = IsTrackedHandDevice(rightHandDev, true);
        if (rightIsHand)
        {
            isHandTrackingActive = true;
            UpdateHandPose(rightHandDev, rightHandVisualObj, true, ref rightHandPinching);
        }
        else
        {
            if (rightHandVisualObj != null) rightHandVisualObj.SetActive(false);
            rightHandPinching = false;
        }

#if ENABLE_INPUT_SYSTEM
        // 2. Check UnityEngine.InputSystem.devices for Quest 3S Hand Tracking
        UpdateInputSystemHandTracking();
#endif

        bool isAnyPinching = leftHandPinching || rightHandPinching || isTestPinching;
        IsPinchJustPressed = isAnyPinching && !prevAnyPinching;
        prevAnyPinching = isAnyPinching;

        if (pinchFeedbackOrb != null)
        {
            if (isAnyPinching)
            {
                pinchFeedbackOrb.SetActive(true);
                pinchFeedbackOrb.transform.position = lastPinchWorldPos;
                pinchFeedbackOrb.transform.localScale = Vector3.one * (0.030f + Mathf.PingPong(Time.time * 0.08f, 0.012f));
            }
            else
            {
                pinchFeedbackOrb.SetActive(false);
            }
        }
    }

#if ENABLE_INPUT_SYSTEM
    private void UpdateInputSystemHandTracking()
    {
        foreach (var dev in InputSystem.devices)
        {
            if (!dev.added) continue;

            string name = dev.name;
            string layout = dev.layout;
            bool isHand = name.IndexOf("Hand", StringComparison.OrdinalIgnoreCase) >= 0 ||
                          layout.IndexOf("Hand", StringComparison.OrdinalIgnoreCase) >= 0;
            if (!isHand) continue;

            bool isRight = false;
            for (int u = 0; u < dev.usages.Count; u++)
            {
                if (dev.usages[u] == UnityEngine.InputSystem.CommonUsages.RightHand) { isRight = true; break; }
            }
            if (!isRight && name.IndexOf("right", StringComparison.OrdinalIgnoreCase) >= 0) isRight = true;

            isHandTrackingActive = true;
            GameObject handObj = isRight ? rightHandVisualObj : leftHandVisualObj;
            if (handObj != null)
            {
                handObj.SetActive(true);

                // Read position & rotation
                var posCtrl = dev.GetChildControl<UnityEngine.InputSystem.Controls.Vector3Control>("devicePosition")
                              ?? dev.GetChildControl<UnityEngine.InputSystem.Controls.Vector3Control>("pointerPosition");
                var rotCtrl = dev.GetChildControl<UnityEngine.InputSystem.Controls.QuaternionControl>("deviceRotation")
                              ?? dev.GetChildControl<UnityEngine.InputSystem.Controls.QuaternionControl>("pointerRotation");

                if (posCtrl != null && rotCtrl != null)
                {
                    Vector3 pos = posCtrl.ReadValue();
                    Quaternion rot = rotCtrl.ReadValue();
                    if (pos != Vector3.zero)
                    {
                        Transform parentRig = characterController != null ? characterController.transform : transform;
                        handObj.transform.position = parentRig.TransformPoint(pos);
                        handObj.transform.rotation = parentRig.rotation * rot;
                    }
                }

                // Read pinch controls
                bool pinch = false;
                var trigAxis = dev.GetChildControl<UnityEngine.InputSystem.Controls.AxisControl>("trigger")
                               ?? dev.GetChildControl<UnityEngine.InputSystem.Controls.AxisControl>("pinch");
                if (trigAxis != null && trigAxis.ReadValue() > 0.55f) pinch = true;

                var trigBtn = dev.GetChildControl<UnityEngine.InputSystem.Controls.ButtonControl>("triggerPressed")
                              ?? dev.GetChildControl<UnityEngine.InputSystem.Controls.ButtonControl>("triggerButton")
                              ?? dev.GetChildControl<UnityEngine.InputSystem.Controls.ButtonControl>("pinchButton")
                              ?? dev.GetChildControl<UnityEngine.InputSystem.Controls.ButtonControl>("primaryButton");
                if (trigBtn != null && trigBtn.isPressed) pinch = true;

                if (isRight) rightHandPinching = pinch;
                else leftHandPinching = pinch;

                Transform indexTip = handObj.transform.Find("IndexTip");
                Transform thumbTip = handObj.transform.Find("ThumbTip");
                if (indexTip != null && thumbTip != null && pinch)
                {
                    lastPinchWorldPos = (indexTip.position + thumbTip.position) * 0.5f;
                }
            }
        }
    }
#endif

    private void UpdateHandPose(XRInputDevice device, GameObject handObj, bool isRight, ref bool isPinching)
    {
        if (handObj == null) return;
        handObj.SetActive(true);

        Transform parentRig = characterController != null ? characterController.transform : transform;

        // Position & Rotation from XRInputDevice
        if (device.TryGetFeatureValue(XRCommonUsages.devicePosition, out Vector3 pos) &&
            device.TryGetFeatureValue(XRCommonUsages.deviceRotation, out Quaternion rot))
        {
            if (pos != Vector3.zero)
            {
                handObj.transform.position = parentRig.TransformPoint(pos);
                handObj.transform.rotation = parentRig.rotation * rot;
            }
        }

        // Pinch Detection:
        // A. Trigger value (0..1 pinch float on Quest hand tracking)
        bool pinch = false;
        if (device.TryGetFeatureValue(XRCommonUsages.trigger, out float tVal) && tVal > 0.55f) pinch = true;
        if (device.TryGetFeatureValue(XRCommonUsages.triggerButton, out bool tBtn) && tBtn) pinch = true;

        // B. PrimaryButton (Index pinch bool on Meta Quest OpenXR)
        if (device.TryGetFeatureValue(XRCommonUsages.primaryButton, out bool pBtn) && pBtn) pinch = true;

        Transform indexTip = handObj.transform.Find("IndexTip");
        Transform thumbTip = handObj.transform.Find("ThumbTip");

        // C. Real Meta Quest 3S Joint Tracking from UnityEngine.XR.CommonUsages.handData
        if (device.TryGetFeatureValue(XRCommonUsages.handData, out Hand xrHand))
        {
            List<Bone> indexBones = new List<Bone>();
            List<Bone> thumbBones = new List<Bone>();
            xrHand.TryGetFingerBones(HandFinger.Index, indexBones);
            xrHand.TryGetFingerBones(HandFinger.Thumb, thumbBones);

            if (indexBones.Count > 0 && thumbBones.Count > 0)
            {
                Bone indexTipBone = indexBones[indexBones.Count - 1];
                Bone thumbTipBone = thumbBones[thumbBones.Count - 1];

                if (indexTipBone.TryGetPosition(out Vector3 idxPos) && thumbTipBone.TryGetPosition(out Vector3 thmPos))
                {
                    Vector3 worldIdx = parentRig.TransformPoint(idxPos);
                    Vector3 worldThm = parentRig.TransformPoint(thmPos);

                    if (indexTip != null) indexTip.position = worldIdx;
                    if (thumbTip != null) thumbTip.position = worldThm;

                    float realJointDist = Vector3.Distance(worldIdx, worldThm);
                    if (realJointDist < 0.038f) pinch = true;
                    lastPinchWorldPos = (worldIdx + worldThm) * 0.5f;
                }
            }
        }

        // Visual articulation: when pinching, animate thumb and index tip moving together
        if (indexTip != null && thumbTip != null)
        {
            Vector3 restIndexLocal = new Vector3(isRight ? 0.02f : -0.02f, 0f, 0.12f);
            Vector3 restThumbLocal = new Vector3(isRight ? -0.028f : 0.028f, -0.015f, 0.08f);

            if (pinch)
            {
                Vector3 pinchLocalMid = (restIndexLocal + restThumbLocal) * 0.5f;
                indexTip.localPosition = Vector3.Lerp(indexTip.localPosition, pinchLocalMid, Time.deltaTime * 20f);
                thumbTip.localPosition = Vector3.Lerp(thumbTip.localPosition, pinchLocalMid, Time.deltaTime * 20f);
            }
            else
            {
                indexTip.localPosition = Vector3.Lerp(indexTip.localPosition, restIndexLocal, Time.deltaTime * 12f);
                thumbTip.localPosition = Vector3.Lerp(thumbTip.localPosition, restThumbLocal, Time.deltaTime * 12f);
            }

            lastPinchWorldPos = (indexTip.position + thumbTip.position) * 0.5f;

            float jointDist = Vector3.Distance(indexTip.position, thumbTip.position);
            if (jointDist < 0.035f) pinch = true;
        }
        else
        {
            lastPinchWorldPos = handObj.transform.position + handObj.transform.forward * 0.08f;
        }

        isPinching = pinch;
    }

    #endregion

    #region Input & Visibility Toggle

    private void HandleVisibilityToggleInputs()
    {
        bool toggleTriggered = false;

        // 1. Meta Quest Left Controller: [X] Button (Primary Button on Left Hand) or [Y] / [Menu]
        XRInputDevice leftHand = XRInputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        bool leftX = false;
        bool leftY = false;
        bool leftMenu = false;

        if (leftHand.isValid)
        {
            leftHand.TryGetFeatureValue(XRCommonUsages.primaryButton, out leftX);
            leftHand.TryGetFeatureValue(XRCommonUsages.secondaryButton, out leftY);
            leftHand.TryGetFeatureValue(XRCommonUsages.menuButton, out leftMenu);
        }

#if ENABLE_INPUT_SYSTEM
        if (!leftX)
        {
            foreach (var dev in InputSystem.devices)
            {
                if (!dev.added) continue;
                bool isLeft = false;
                for (int u = 0; u < dev.usages.Count; u++)
                {
                    if (dev.usages[u] == UnityEngine.InputSystem.CommonUsages.LeftHand) { isLeft = true; break; }
                }
                if (dev.name.IndexOf("left", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    dev.name.IndexOf("right", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    isLeft = true;
                }

                if (isLeft)
                {
                    var xBtn = dev.GetChildControl<UnityEngine.InputSystem.Controls.ButtonControl>("primaryButton")
                               ?? dev.GetChildControl<UnityEngine.InputSystem.Controls.ButtonControl>("buttonSouth")
                               ?? dev.GetChildControl<UnityEngine.InputSystem.Controls.ButtonControl>("xButton");
                    if (xBtn != null && xBtn.isPressed) { leftX = true; break; }
                }
            }
        }
#endif

        if ((leftX && !prevLeftX) || (leftY && !prevLeftY) || (leftMenu && !prevLeftMenu))
        {
            toggleTriggered = true;
            Debug.Log("[Newton VR] Left Controller [X] pressed: Toggle Navigation Panel");
        }
        prevLeftX = leftX;
        prevLeftY = leftY;
        prevLeftMenu = leftMenu;

        // 2. Desktop Testing: [N] Key
        if (NewtonInput.GetKeyDown(KeyCode.N) || NewtonInput.GetKeyDown(KeyCode.Tab) || NewtonInput.GetKeyDown(KeyCode.V))
        {
            toggleTriggered = true;
            Debug.Log("[Newton VR] Desktop [N] pressed: Toggle Navigation Panel");
        }

        if (toggleTriggered)
        {
            TogglePanel();
        }
    }

    public void TogglePanel()
    {
        if (Time.unscaledTime - lastToggleTime < 0.25f) return;
        lastToggleTime = Time.unscaledTime;
        SetPanelVisibility(!isPanelVisible);
        if (isPanelVisible) SnapToLeftPosition();
    }

    public void SetPanelVisibility(bool visible)
    {
        isPanelVisible = visible;
        if (panelCanvasGo != null)
        {
            panelCanvasGo.SetActive(visible);
            if (visible)
            {
                panelCanvasGo.transform.localPosition = Vector3.zero;
                panelCanvasGo.transform.localRotation = Quaternion.identity;
                panelCanvasGo.transform.localScale = Vector3.one * 0.001f;
                SnapToLeftPosition();
            }
        }
        // Room-anchored buttons: visible when panel is closed, hidden when full panel is open so it never overlaps!
        foreach (var tabGo in roomNavTabCanvasObjects)
        {
            if (tabGo != null)
            {
                tabGo.SetActive(!visible);
            }
        }
        if (navTabCanvasGo != null)
        {
            navTabCanvasGo.SetActive(!visible);
        }

        if (!visible)
        {
            if (interactionRayRenderer != null) interactionRayRenderer.enabled = false;
            if (rayReticleDot != null) rayReticleDot.SetActive(false);
            if (currentHoveredButton != null)
            {
                ResetButtonVisual(currentHoveredButton);
                currentHoveredButton = null;
            }
        }
    }

    public void SetCurrentActiveRoom(int roomIndex)
    {
        currentActiveRoom = (DestinationRoom)Mathf.Clamp(roomIndex, 0, 4);
        RefreshActiveRoomVisuals();
    }

    #endregion

    #region Smooth Lazy Follow (Left-Side Placement)

    public void SnapToLeftPosition()
    {
        if (playerCamera == null) FindRigReferences();
        if (playerCamera == null) return;

        Vector3 headPos = playerCamera.transform.position;
        Vector3 headFwd = Vector3.ProjectOnPlane(playerCamera.transform.forward, Vector3.up).normalized;
        if (headFwd.sqrMagnitude < 0.01f) headFwd = Vector3.forward;

        // Rotate forward direction 28 degrees to the left
        Vector3 leftDir = Quaternion.Euler(0f, leftAngleOffset, 0f) * headFwd;
        Vector3 targetPos = headPos + leftDir * armReachDistance + Vector3.up * verticalOffset;
        Quaternion targetRot = Quaternion.LookRotation(targetPos - headPos) * Quaternion.Euler(upwardTiltAngle, 0f, 0f);

        transform.position = targetPos;
        transform.rotation = targetRot;

        if (panelCanvasGo != null)
        {
            panelCanvasGo.transform.localPosition = Vector3.zero;
            panelCanvasGo.transform.localRotation = Quaternion.identity;
            panelCanvasGo.transform.localScale = Vector3.one * 0.001f;
        }
    }

    public void SnapToPlayerFront()
    {
        SnapToLeftPosition();
    }

    private void UpdateLazyFollow(float angleOffsetDeg)
    {
        if (playerCamera == null) return;

        Vector3 headPos = playerCamera.transform.position;
        Vector3 headFwd = Vector3.ProjectOnPlane(playerCamera.transform.forward, Vector3.up).normalized;
        if (headFwd.sqrMagnitude < 0.01f) headFwd = Vector3.forward;

        Vector3 leftDir = Quaternion.Euler(0f, angleOffsetDeg, 0f) * headFwd;
        Vector3 targetPos = headPos + leftDir * armReachDistance + Vector3.up * verticalOffset;
        Quaternion targetRot = Quaternion.LookRotation(targetPos - headPos) * Quaternion.Euler(upwardTiltAngle, 0f, 0f);

        float distDiff = Vector3.Distance(transform.position, targetPos);
        if (distDiff > 2.5f)
        {
            // Instant snap if teleported or initial spawn was distant
            transform.position = targetPos;
            transform.rotation = targetRot;
        }
        else
        {
            Vector3 toCurrent = Vector3.ProjectOnPlane(transform.position - headPos, Vector3.up).normalized;
            float angleDiff = Vector3.Angle(leftDir, toCurrent);

            if (angleDiff > deadzoneAngle || distDiff > deadzoneDistance)
            {
                transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSpeed);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * followSpeed);
            }
        }

        if (panelCanvasGo != null)
        {
            panelCanvasGo.transform.localPosition = Vector3.zero;
            panelCanvasGo.transform.localRotation = Quaternion.identity;
        }
    }

    #endregion

    #region Interaction & Selection

    private void UpdateInteraction(bool isFullPanel)
    {
        Ray ray = default;
        bool hasRay = false;
        bool selectionRequested = false;

        // 0. Test Injection Overrides for Verification Suite & Automated Testing
        if (isTestHandActive)
        {
            ray = testHandRay;
            hasRay = true;
            ActiveHandRay = ray;
            HasActiveHandRay = true;
            if (isTestPinching) selectionRequested = true;
        }
        else if (isTestControllerActive)
        {
            ray = testControllerRay;
            hasRay = true;
            HasActiveHandRay = false;
            if (isTestControllerTrigger) selectionRequested = true;
        }
        // 1. Meta Quest 3S Real Hand Tracking: Pointer Aim Ray & Pinch Selection
        else if (isHandTrackingActive)
        {
            GameObject activeHand = null;
            bool activePinching = false;

            // Prefer right hand if active, or whichever hand is pinching
            if (rightHandVisualObj != null && rightHandVisualObj.activeSelf)
            {
                activeHand = rightHandVisualObj;
                activePinching = rightHandPinching;
            }
            if (leftHandVisualObj != null && leftHandVisualObj.activeSelf && (leftHandPinching || activeHand == null))
            {
                activeHand = leftHandVisualObj;
                activePinching = leftHandPinching;
            }

            if (activeHand != null)
            {
                Transform indexTip = activeHand.transform.Find("IndexTip");
                Vector3 origin = (indexTip != null) ? indexTip.position : activeHand.transform.position;
                Vector3 direction = activeHand.transform.forward;

                // Check for Quest OpenXR Pointer Pose (Aim Ray)
                XRNode node = (activeHand == rightHandVisualObj) ? XRNode.RightHand : XRNode.LeftHand;
                XRInputDevice dev = XRInputDevices.GetDeviceAtXRNode(node);
                if (dev.isValid && dev.TryGetFeatureValue(s_PointerPosition, out Vector3 pPos) && pPos != Vector3.zero)
                {
                    Transform parentRig = characterController != null ? characterController.transform : transform;
                    origin = parentRig.TransformPoint(pPos);
                    if (dev.TryGetFeatureValue(s_PointerRotation, out Quaternion pRot) && pRot != Quaternion.identity)
                    {
                        direction = parentRig.rotation * pRot * Vector3.forward;
                    }
                }

                ray = new Ray(origin, direction);
                hasRay = true;
                ActiveHandRay = ray;
                HasActiveHandRay = true;

                if (activePinching)
                {
                    selectionRequested = true;
                }
            }
        }
        else
        {
            HasActiveHandRay = false;
        }

        // 2. Touch Plus Controller Ray & Trigger Selection
        if (!hasRay && rightHandController != null)
        {
            ray = new Ray(rightHandController.position, rightHandController.forward);
            hasRay = true;
        }

        XRInputDevice rightHand = XRInputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if (rightHand.isValid)
        {
            if (rightHand.TryGetFeatureValue(XRCommonUsages.trigger, out float tVal) && tVal > 0.40f) selectionRequested = true;
            if (rightHand.TryGetFeatureValue(XRCommonUsages.triggerButton, out bool tBtn) && tBtn) selectionRequested = true;
            if (rightHand.TryGetFeatureValue(XRCommonUsages.primaryButton, out bool pBtn) && pBtn) selectionRequested = true;
        }

        XRInputDevice leftHand = XRInputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        if (leftHand.isValid)
        {
            if (leftHand.TryGetFeatureValue(XRCommonUsages.trigger, out float ltVal) && ltVal > 0.40f) selectionRequested = true;
            if (leftHand.TryGetFeatureValue(XRCommonUsages.triggerButton, out bool ltBtn) && ltBtn) selectionRequested = true;
        }

        // 3. Desktop Testing Fallback (Mouse Hover & Left Click, or Space for simulated pinch)
        if (!hasRay || (!XRSettings.isDeviceActive && !isHandTrackingActive))
        {
            if (playerCamera != null)
            {
                if (Cursor.lockState == CursorLockMode.None)
                {
                    ray = playerCamera.ScreenPointToRay(NewtonInput.GetMousePosition());
                }
                else
                {
                    ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
                }
                hasRay = true;
            }
        }

        if (NewtonInput.GetMouseButton(0) || NewtonInput.GetKey(KeyCode.E) || NewtonInput.GetKey(KeyCode.Space))
        {
            selectionRequested = true;
        }

        bool selectClickThisFrame = selectionRequested && !prevSelectionState;
        prevSelectionState = selectionRequested;

        if (!hasRay) return;

        // Perform Raycast against Button Colliders
        CanvasDestinationButton hitBtn = null;
        RaycastHit hitInfo = default;
        float closestDist = float.MaxValue;

        if (isFullPanel)
        {
            foreach (var b in destinationButtons)
            {
                if (b.collider != null && b.collider.Raycast(ray, out RaycastHit hit, 10.0f))
                {
                    if (hit.distance < closestDist)
                    {
                        closestDist = hit.distance;
                        hitBtn = b;
                        hitInfo = hit;
                    }
                }
            }

            if (closeButton != null && closeButton.collider != null && closeButton.collider.Raycast(ray, out RaycastHit cHit, 10.0f))
            {
                if (cHit.distance < closestDist)
                {
                    closestDist = cHit.distance;
                    hitBtn = closeButton;
                    hitInfo = cHit;
                }
            }

            if (replayButton != null && replayButton.collider != null && replayButton.collider.Raycast(ray, out RaycastHit rHit, 10.0f))
            {
                if (rHit.distance < closestDist)
                {
                    closestDist = rHit.distance;
                    hitBtn = replayButton;
                    hitInfo = rHit;
                }
            }
        }
        else
        {
            foreach (var tb in roomNavTabButtons)
            {
                if (tb != null && tb.collider != null && tb.collider.Raycast(ray, out RaycastHit tHit, 10.0f))
                {
                    if (tHit.distance < closestDist)
                    {
                        closestDist = tHit.distance;
                        hitBtn = tb;
                        hitInfo = tHit;
                    }
                }
            }
            if (hitBtn == null && navTabButton != null && navTabButton.collider != null && navTabButton.collider.Raycast(ray, out RaycastHit fallbackHit, 10.0f))
            {
                hitBtn = navTabButton;
                hitInfo = fallbackHit;
            }
        }

        // Direct Touch / Poke Check for Hand Tracking (Physical fingertip penetration)
        if (hitBtn == null && isHandTrackingActive)
        {
            GameObject[] activeHands = new GameObject[] { rightHandVisualObj, leftHandVisualObj };
            foreach (var h in activeHands)
            {
                if (h == null || !h.activeSelf) continue;
                Transform indexTip = h.transform.Find("IndexTip");
                if (indexTip == null) continue;

                Vector3 tipPos = indexTip.position;
                if (isFullPanel)
                {
                    foreach (var b in destinationButtons)
                    {
                        if (b.collider != null && b.collider.bounds.Contains(tipPos))
                        {
                            hitBtn = b;
                            selectClickThisFrame = true; // direct poke triggers selection!
                            break;
                        }
                    }
                    if (closeButton != null && closeButton.collider != null && closeButton.collider.bounds.Contains(tipPos))
                    {
                        hitBtn = closeButton;
                        selectClickThisFrame = true;
                    }
                }
                else
                {
                    foreach (var tb in roomNavTabButtons)
                    {
                        if (tb != null && tb.collider != null && tb.collider.bounds.Contains(tipPos))
                        {
                            hitBtn = tb;
                            selectClickThisFrame = true;
                            break;
                        }
                    }
                    if (hitBtn == null && navTabButton != null && navTabButton.collider != null && navTabButton.collider.bounds.Contains(tipPos))
                    {
                        hitBtn = navTabButton;
                        selectClickThisFrame = true;
                    }
                }
            }
        }

        // START LAB In-Scene Sign Raycast Check (Aiming & Selection via Controller Ray or Hand Pinch)
        bool isAimingAtStartLab = false;
        RaycastHit startLabHit = default;
        if (hitBtn == null)
        {
            if (CheckStartLabRaycast(ray, selectClickThisFrame, out startLabHit))
            {
                isAimingAtStartLab = true;
                hitInfo = startLabHit;
            }
        }
        else
        {
            ResetStartLabHover();
        }

        // Pointer Ray & Reticle Updates (Matching luminous laser pointer HUD in reference visual)
        if (interactionRayRenderer != null)
        {
            bool hasTarget = (hitBtn != null || isAimingAtStartLab);
            if (hasTarget || XRSettings.isDeviceActive || isHandTrackingActive || isTestHandActive || isTestControllerActive)
            {
                interactionRayRenderer.enabled = true;
                Vector3 startPos = ray.origin;
                Vector3 endPos = hasTarget ? hitInfo.point : (ray.origin + ray.direction * 2.5f);
                interactionRayRenderer.SetPosition(0, startPos);
                interactionRayRenderer.SetPosition(1, endPos);
            }
            else
            {
                interactionRayRenderer.enabled = false;
            }
        }

        if (rayReticleDot != null)
        {
            if (hitBtn != null || isAimingAtStartLab)
            {
                rayReticleDot.SetActive(true);
                rayReticleDot.transform.position = hitInfo.point + hitInfo.normal * 0.005f;
                rayReticleDot.transform.rotation = Quaternion.LookRotation(hitInfo.normal);
            }
            else
            {
                rayReticleDot.SetActive(false);
            }
        }

        // Hover Feedback
        if (hitBtn != currentHoveredButton)
        {
            if (currentHoveredButton != null) ResetButtonVisual(currentHoveredButton);
            currentHoveredButton = hitBtn;
            if (currentHoveredButton != null) SetButtonHover(currentHoveredButton);
        }

        // Click / Pinch Selection
        if (currentHoveredButton != null && selectClickThisFrame)
        {
            TriggerHapticPulse();
            StartCoroutine(AnimateButtonClick(currentHoveredButton));
            currentHoveredButton.onClick?.Invoke();
        }
    }

    private void SetButtonHover(CanvasDestinationButton b)
    {
        if (b == null) return;

        if (b == navTabButton || roomNavTabButtons.Contains(b) || (int)b.room == -3)
        {
            if (b.frameImage != null) b.frameImage.color = new Color(0.125f, 1.0f, 0.55f, 1.0f); // Radiant Emerald Highlight
            if (b.bgImage != null) b.bgImage.color = new Color(0.12f, 0.35f, 0.20f, 0.95f);      // Emerald bronze fill
            if (b.accentBarImage != null) b.accentBarImage.color = ColorEmeraldGreen;
            b.rootObj.transform.localScale = Vector3.one * 0.00115f; // tactile 15% scale pop
            return;
        }

        if (b.bgImage != null)
        {
            b.bgImage.color = ColorButtonHover;
        }
        if (b.frameImage != null)
        {
            b.frameImage.color = ColorEmeraldGreen;
        }
        b.rect.anchoredPosition += new Vector2(4f, 0f);
    }

    private void ResetButtonVisual(CanvasDestinationButton b)
    {
        if (b == null) return;

        if (b == navTabButton || roomNavTabButtons.Contains(b) || (int)b.room == -3)
        {
            if (b.frameImage != null) b.frameImage.color = ColorChampagneGold;
            if (b.bgImage != null) b.bgImage.color = ColorButtonNormal;
            if (b.accentBarImage != null) b.accentBarImage.color = ColorEmeraldGreen;
            b.rootObj.transform.localScale = Vector3.one * 0.001f;
            return;
        }

        if (b.room == currentActiveRoom)
        {
            if (b.bgImage != null) b.bgImage.color = ColorButtonActive;
            if (b.frameImage != null) b.frameImage.color = ColorEmeraldGreen;
        }
        else
        {
            if (b.bgImage != null) b.bgImage.color = ColorButtonNormal;
            if (b.frameImage != null) b.frameImage.color = ColorChampagneGold * 0.70f;
        }

        b.rect.anchoredPosition = new Vector2(0f, b.rect.anchoredPosition.y);
    }

    private void RefreshActiveRoomVisuals()
    {
        foreach (var b in destinationButtons)
        {
            if (b.room == currentActiveRoom)
            {
                if (b.bgImage != null) b.bgImage.color = ColorButtonActive;
                if (b.frameImage != null) b.frameImage.color = ColorEmeraldGreen;
            }
            else
            {
                if (b.bgImage != null) b.bgImage.color = ColorButtonNormal;
                if (b.frameImage != null) b.frameImage.color = ColorChampagneGold * 0.70f;
            }
        }
    }

    private IEnumerator AnimateButtonClick(CanvasDestinationButton b)
    {
        if (b == null) yield break;

        if (b == navTabButton || roomNavTabButtons.Contains(b) || (int)b.room == -3)
        {
            if (b.bgImage != null) b.bgImage.color = ColorEmeraldGreen;
            if (b.frameImage != null) b.frameImage.color = ColorWarmIvory;
            yield return new WaitForSeconds(0.12f);
            if (b != null) ResetButtonVisual(b);
            yield break;
        }

        if (b.bgImage != null) b.bgImage.color = ColorEmeraldGreen;
        yield return new WaitForSeconds(0.12f);
        if (b != null)
        {
            if (b == currentHoveredButton) SetButtonHover(b);
            else ResetButtonVisual(b);
        }
    }

    private void TriggerHapticPulse()
    {
        if (!XRSettings.isDeviceActive) return;
        XRInputDevice rDev = XRInputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if (rDev.isValid) rDev.SendHapticImpulse(0, 0.5f, 0.08f);

        XRInputDevice lDev = XRInputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        if (lDev.isValid) lDev.SendHapticImpulse(0, 0.5f, 0.08f);
    }

    #region In-Scene START LAB Raycast & Hover

    private bool CheckStartLabRaycast(Ray ray, bool selectClickThisFrame, out RaycastHit hit)
    {
        hit = default;
        // In Main Lobby: StartButton & StartText are around (0, 1.12, 2.90)
        if (Physics.Raycast(ray, out RaycastHit rHit, 25.0f))
        {
            string hitName = rHit.collider.gameObject.name.ToLower();
            string parentName = rHit.collider.transform.parent != null ? rHit.collider.transform.parent.name.ToLower() : "";
            bool isStartLab = hitName.Contains("startbutton") || hitName.Contains("starttext") || hitName.Contains("start lab") ||
                              parentName.Contains("startbutton") || parentName.Contains("start lab");

            if (isStartLab)
            {
                hit = rHit;
                GameObject targetGo = rHit.collider.gameObject;

                // Handle hover highlight
                if (currentHoveredStartLab != targetGo)
                {
                    ResetStartLabHover();
                    currentHoveredStartLab = targetGo;
                    startLabRenderer = targetGo.GetComponent<Renderer>();
                    if (startLabRenderer != null && startLabRenderer.material != null)
                    {
                        if (startLabRenderer.material.HasProperty("_EmissionColor"))
                        {
                            originalStartLabColor = startLabRenderer.material.GetColor("_EmissionColor");
                            startLabRenderer.material.EnableKeyword("_EMISSION");
                            startLabRenderer.material.SetColor("_EmissionColor", ColorEmeraldGreen * 1.5f);
                        }
                    }
                }

                // Handle selection click / pinch
                if (selectClickThisFrame)
                {
                    TriggerHapticPulse();
                    if (startLabRenderer != null)
                    {
                        StartCoroutine(FlashStartLabActivation(startLabRenderer));
                    }
                    // Open the navigation panel smoothly on the left without teleporting the player!
                    SetPanelVisibility(true);
                    SnapToLeftPosition();
                    if (NarrationManager.Instance != null)
                    {
                        NarrationManager.Instance.OnEnterFromStartLab();
                    }
                    ResetStartLabHover();
                }

                return true;
            }
        }

        ResetStartLabHover();
        return false;
    }

    private void ResetStartLabHover()
    {
        if (currentHoveredStartLab != null)
        {
            if (startLabRenderer != null && startLabRenderer.material != null)
            {
                if (startLabRenderer.material.HasProperty("_EmissionColor"))
                {
                    startLabRenderer.material.SetColor("_EmissionColor", originalStartLabColor);
                }
            }
            currentHoveredStartLab = null;
            startLabRenderer = null;
        }
    }

    private IEnumerator FlashStartLabActivation(Renderer rend)
    {
        if (rend != null && rend.material != null && rend.material.HasProperty("_EmissionColor"))
        {
            rend.material.EnableKeyword("_EMISSION");
            rend.material.SetColor("_EmissionColor", ColorEmeraldGreen * 3.5f);
            yield return new WaitForSeconds(0.18f);
            if (rend != null && rend.material != null)
            {
                rend.material.SetColor("_EmissionColor", originalStartLabColor);
            }
        }
    }

    #endregion

    #region Automated Test Injection

    public void SetTestHandTracking(bool active, Vector3 origin, Vector3 direction, bool pinching)
    {
        isTestHandActive = active;
        testHandRay = new Ray(origin, direction);
        isTestPinching = pinching;
        if (active)
        {
            isHandTrackingActive = true;
            ActiveHandRay = testHandRay;
            HasActiveHandRay = true;
            lastPinchWorldPos = origin + direction * 0.05f;
        }
    }

    public void SetTestController(bool active, Vector3 origin, Vector3 direction, bool trigger)
    {
        isTestControllerActive = active;
        testControllerRay = new Ray(origin, direction);
        isTestControllerTrigger = trigger;
    }

    #endregion

    #endregion

    #region Room Teleportation

    public void SelectDestination(DestinationRoom room)
    {
        EnsureInitialized();
        foreach (var b in destinationButtons)
        {
            if (b.room == room)
            {
                b.onClick?.Invoke();
                return;
            }
        }

        // Failsafe direct teleport
        PlayerController pc = (characterController != null) ? characterController.GetComponent<PlayerController>() : FindFirstObjectByType<PlayerController>();
        if (pc != null) pc.TeleportToStationIndex((int)room);
    }

    private void TeleportTo(Vector3 destination, Quaternion orientation, DestinationRoom room, string destName)
    {
        currentActiveRoom = room;

        if (characterController != null) characterController.enabled = false;

        GameObject player = (characterController != null) ? characterController.gameObject : gameObject;
        Vector3 headOffset = Vector3.zero;
        if (playerCamera != null)
        {
            headOffset = playerCamera.transform.position - player.transform.position;
            headOffset.y = 0f;
        }

        Vector3 targetRoot = destination - headOffset;
        targetRoot.y = destination.y;
        player.transform.position = targetRoot;
        player.transform.rotation = orientation;

        if (characterController != null) characterController.enabled = true;

        PlayerController pc = player.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.ResetVerticalVelocity();
            pc.SetCurrentStationIndex((int)room);
        }

        SnapToLeftPosition();
        SetPanelVisibility(false);
        RefreshActiveRoomVisuals();

        if (NarrationManager.Instance != null)
        {
            if (room == DestinationRoom.Lab1_FirstLaw) NarrationManager.Instance.OnEnterLab1();
            else if (room == DestinationRoom.Lab2_SecondLaw) NarrationManager.Instance.OnEnterLab2();
            else if (room == DestinationRoom.Lab3_ThirdLaw) NarrationManager.Instance.OnEnterLab3();
            else if (room == DestinationRoom.MainLobby) NarrationManager.Instance.OnEnterMainLobby();
        }

        Debug.Log($"[Newton VR] Teleported XR Origin to {destName} at {destination}. Full panel auto-hidden; navigation tab available.");
    }

    #endregion
}
