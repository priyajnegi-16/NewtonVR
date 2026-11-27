using System;
using System.Collections;
using System.IO;
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

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// World Space 3D Grand Futuristic VR Welcome Introduction Panel for Newtonian VR Simulation.
/// Positioned CENTRALLY in front of the player in the Main Lobby, dominating the initial view
/// as the main visual attraction, while keeping the central scientific globe and circular platform
/// visible around and behind it.
/// Displays high-fidelity futuristic holographic aesthetics matching the reference design,
/// featuring the Three Laws of Motion overview and an interactive ENTER LABORATORY button.
/// Fully compatible with Meta Quest 3S controllers, natural hand tracking, and direct touch.
/// </summary>
public class VRWelcomePanel : MonoBehaviour
{
    public static VRWelcomePanel Instance { get; private set; }

    [Header("Rig References")]
    public CharacterController characterController;
    public Camera playerCamera;
    public Transform rightHandController;
    public Transform leftHandController;

    [Header("Placement & Viewing Settings")]
    public float viewingDistance = 2.00f; // 2.0m comfortable viewing distance in front
    public float centerHeight = 1.58f; // Natural eye level
    public float upwardTiltAngle = 8.0f; // Subtle upward tilt towards player's eyes

    [Header("Color Palette (Luminous Electric Cyan Futuristic Sci-Fi Theme)")]
    public static readonly Color ColorNeonCyan       = new Color(0.00f, 0.90f, 1.00f, 1.00f); // #00E5FF
    public static readonly Color ColorGlowCyan       = new Color(0.35f, 1.00f, 1.00f, 1.00f); // #59FFFF
    public static readonly Color ColorIceWhite       = new Color(0.85f, 0.96f, 1.00f, 1.00f); // #D9F5FF
    public static readonly Color ColorPureWhite      = new Color(1.00f, 1.00f, 1.00f, 1.00f); // #FFFFFF
    public static readonly Color ColorSubtext        = new Color(0.68f, 0.88f, 0.98f, 1.00f); // #ADE0FA
    public static readonly Color ColorDarkNavyGlass  = new Color(0.020f, 0.055f, 0.110f, 0.95f); // #050E1C
    public static readonly Color ColorCardBg         = new Color(0.027f, 0.078f, 0.141f, 0.92f); // #071424
    public static readonly Color ColorButtonNormal   = new Color(0.015f, 0.110f, 0.200f, 0.96f); // #041C33
    public static readonly Color ColorButtonHover    = new Color(0.040f, 0.280f, 0.440f, 1.00f); // #0A4770
    public static readonly Color ColorButtonActive   = new Color(0.00f, 0.85f, 1.00f, 1.00f); // #00D9FF
    public static readonly Color ColorEmeraldGreen   = new Color(0.125f, 0.880f, 0.520f, 1.00f); // #20E085

    // Visibility & Animation
    private bool isPanelVisible = true;
    public bool IsPanelVisible => isPanelVisible;
    private bool isDismissing = false;

    // Canvas Roots
    private GameObject panelCanvasGo;
    private RectTransform panelRootRect;
    private CanvasGroup panelCanvasGroup;
    private TMP_FontAsset sharedFontAsset;

    // Interactive Button
    private GameObject enterButtonObj;
    private RectTransform enterButtonRect;
    private BoxCollider enterButtonCollider;
    private Image enterButtonFrame;
    private Image enterButtonFill;
    private TextMeshProUGUI enterButtonTmp;
    private bool isButtonHovered = false;

    // Input States
    private bool prevSelectionState = false;
    private float lastClickTime = -1f;

    // Test Injection
    private bool isTestActive = false;
    private Ray testRay;
    private bool isTestTrigger = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInitialize()
    {
        if (FindFirstObjectByType<VRWelcomePanel>() == null)
        {
            GameObject sys = new GameObject("VR_WelcomePanel");
            sys.AddComponent<VRWelcomePanel>();
            Debug.Log("[Newton VR] World Space 3D Grand Futuristic Welcome Panel initialized centrally.");
        }
    }

    void Awake()
    {
        Instance = this;
        EnsureInitialized();
    }

    void Start()
    {
        Instance = this;
        FindRigReferences();
        SnapToCenterPosition();
        SetPanelVisibility(true); // Welcome panel opens automatically when VR experience starts!
        StartCoroutine(PlayEntranceAnimation());
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void EnsureInitialized(bool forceRebuild = false)
    {
        FindRigReferences();
        LoadFontAsset();

        if (forceRebuild && panelCanvasGo != null)
        {
            DestroyImmediate(panelCanvasGo);
            panelCanvasGo = null;
        }

        if (panelCanvasGo == null)
        {
            Transform existingCanvas = transform.Find("VR_WorldSpaceWelcomeCanvas");
            if (existingCanvas != null && forceRebuild)
            {
                DestroyImmediate(existingCanvas.gameObject);
                existingCanvas = null;
            }

            if (existingCanvas != null)
            {
                panelCanvasGo = existingCanvas.gameObject;
                panelRootRect = panelCanvasGo.GetComponent<RectTransform>();
                panelCanvasGroup = panelCanvasGo.GetComponent<CanvasGroup>();
                if (panelCanvasGroup == null) panelCanvasGroup = panelCanvasGo.AddComponent<CanvasGroup>();
                BindExistingElements();
            }
            else
            {
                BuildWorldSpaceWelcomeCanvas();
            }
        }
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

    #region Canvas Construction

    public void BuildWorldSpaceWelcomeCanvas()
    {
        panelCanvasGo = new GameObject("VR_WorldSpaceWelcomeCanvas");
        panelCanvasGo.transform.SetParent(transform, true);
        panelCanvasGo.transform.localPosition = Vector3.zero;
        panelCanvasGo.transform.localRotation = Quaternion.identity;
        panelCanvasGo.transform.localScale = Vector3.one * 0.0015f; // 1280x800 canvas -> ~1.92m wide x 1.20m tall

        Canvas canvas = panelCanvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = playerCamera;

        CanvasScaler scaler = panelCanvasGo.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;
        scaler.referencePixelsPerUnit = 100f;

        panelCanvasGo.AddComponent<GraphicRaycaster>();

        panelCanvasGroup = panelCanvasGo.AddComponent<CanvasGroup>();
        panelCanvasGroup.alpha = 1.0f;

        panelRootRect = panelCanvasGo.GetComponent<RectTransform>();
        panelRootRect.sizeDelta = new Vector2(1280f, 800f); // Grand wide holographic command display

        // Load custom textures
        Sprite mainFrameSprite = LoadSpriteAsset("Assets/Textures/UI/SciFi_Main_Frame.png");
        Sprite cardFrameSprite = LoadSpriteAsset("Assets/Textures/UI/SciFi_Card_Frame.png");
        Sprite pillFrameSprite = LoadSpriteAsset("Assets/Textures/UI/SciFi_Pill_Frame.png");
        Sprite iconInertia = LoadSpriteAsset("Assets/Textures/UI/Icon_FirstLaw_Inertia.png");
        Sprite iconForce = LoadSpriteAsset("Assets/Textures/UI/Icon_SecondLaw_Force.png");
        Sprite iconActionReaction = LoadSpriteAsset("Assets/Textures/UI/Icon_ThirdLaw_ActionReaction.png");

        // 1. Outer Glowing Neon Cyan Sci-Fi Frame (includes translucent dark navy glass fill & chamfer cut corners)
        GameObject frameObj = CreateUIElement("Panel_SciFiFrame", panelRootRect, new Vector2(1280f, 800f), Vector2.zero);
        Image frameImg = frameObj.AddComponent<Image>();
        if (mainFrameSprite != null)
        {
            frameImg.sprite = mainFrameSprite;
            frameImg.type = Image.Type.Simple;
            frameImg.color = Color.white;
        }
        else
        {
            frameImg.color = ColorNeonCyan;
        }

        // 2. Header Section
        // A. Top Tracking Text: →   W E L C O M E   T O   ← (Soft Cyan Ice)
        GameObject welcomeObj = CreateUIElement("Header_WelcomeTo", frameObj.transform, new Vector2(750f, 32f), new Vector2(0f, 335f));
        TextMeshProUGUI welcomeTmp = welcomeObj.AddComponent<TextMeshProUGUI>();
        if (sharedFontAsset != null) welcomeTmp.font = sharedFontAsset;
        welcomeTmp.text = "→   W E L C O M E   T O   ←";
        welcomeTmp.fontSize = 21f;
        welcomeTmp.characterSpacing = 28f;
        welcomeTmp.fontStyle = FontStyles.Bold;
        welcomeTmp.alignment = TextAlignmentOptions.Center;
        welcomeTmp.color = ColorIceWhite;

        // B. Large Main Title: NEWTONIAN VR LABORATORY (Bold Pure White, framed by the glowing orbital ellipse ring)
        GameObject titleObj = CreateUIElement("Header_MainTitle", frameObj.transform, new Vector2(1000f, 110f), new Vector2(0f, 260f));
        TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
        if (sharedFontAsset != null) titleTmp.font = sharedFontAsset;
        titleTmp.text = "NEWTONIAN VR\nLABORATORY";
        titleTmp.fontSize = 46f;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = ColorPureWhite;
        titleTmp.lineSpacing = -6f;

        // C. Subheader Pill Badge: EXPLORE THE THREE LAWS OF MOTION
        GameObject pillBadgeObj = CreateUIElement("Header_PillBadge", frameObj.transform, new Vector2(640f, 44f), new Vector2(0f, 175f));
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

        GameObject pillTextObj = CreateUIElement("TMP_PillText", pillBadgeObj.transform, new Vector2(620f, 38f), Vector2.zero);
        TextMeshProUGUI pillTmp = pillTextObj.AddComponent<TextMeshProUGUI>();
        if (sharedFontAsset != null) pillTmp.font = sharedFontAsset;
        pillTmp.text = "EXPLORE THE THREE LAWS OF MOTION";
        pillTmp.fontSize = 18f;
        pillTmp.fontStyle = FontStyles.Bold;
        pillTmp.alignment = TextAlignmentOptions.Center;
        pillTmp.color = ColorNeonCyan;

        // D. Description Text
        GameObject descObj = CreateUIElement("Header_Description", frameObj.transform, new Vector2(1040f, 65f), new Vector2(0f, 105f));
        TextMeshProUGUI descTmp = descObj.AddComponent<TextMeshProUGUI>();
        if (sharedFontAsset != null) descTmp.font = sharedFontAsset;
        descTmp.text = "Discover the science behind motion, force and interaction\nthrough immersive virtual experiments.";
        descTmp.fontSize = 15.5f;
        descTmp.fontStyle = FontStyles.Normal;
        descTmp.alignment = TextAlignmentOptions.Center;
        descTmp.color = ColorSubtext;

        // 3. Three Law Feature Cards (3 Columns)
        float cardY = -48f;
        float cardW = 360f;
        float cardH = 256f;
        float spacingX = 390f;

        // Card 1: Newton's First Law (Inertia)
        CreateFeatureCard(
            frameObj.transform,
            "Card_FirstLaw",
            new Vector2(-spacingX, cardY),
            new Vector2(cardW, cardH),
            cardFrameSprite,
            "NEWTON'S",
            "FIRST LAW",
            iconInertia,
            "INERTIA"
        );

        // Card 2: Newton's Second Law (Force & Acceleration)
        CreateFeatureCard(
            frameObj.transform,
            "Card_SecondLaw",
            new Vector2(0f, cardY),
            new Vector2(cardW, cardH),
            cardFrameSprite,
            "NEWTON'S",
            "SECOND LAW",
            iconForce,
            "FORCE &\nACCELERATION"
        );

        // Card 3: Newton's Third Law (Action & Reaction)
        CreateFeatureCard(
            frameObj.transform,
            "Card_ThirdLaw",
            new Vector2(spacingX, cardY),
            new Vector2(cardW, cardH),
            cardFrameSprite,
            "NEWTON'S",
            "THIRD LAW",
            iconActionReaction,
            "ACTION &\nREACTION"
        );

        // 4. Bottom Action Button: ENTER LABORATORY →
        CreateEnterLaboratoryButton(frameObj.transform, pillFrameSprite, new Vector2(0f, -315f));

        // 5. Corner Sci-Fi Tech Hash Decals (Bottom Left & Right)
        CreateTechDecals(frameObj.transform);
    }

    private void CreateFeatureCard(
        Transform parent,
        string name,
        Vector2 pos,
        Vector2 size,
        Sprite frameSprite,
        string subHead,
        string mainHead,
        Sprite iconSprite,
        string footerLabel)
    {
        // Container
        GameObject cardObj = CreateUIElement(name, parent, size, pos);
        RectTransform cardRect = cardObj.GetComponent<RectTransform>();

        // Background / Frame
        Image cardImg = cardObj.AddComponent<Image>();
        if (frameSprite != null)
        {
            cardImg.sprite = frameSprite;
            cardImg.color = Color.white;
        }
        else
        {
            cardImg.color = ColorCardBg;
        }

        // Top Subhead: NEWTON'S
        GameObject subObj = CreateUIElement("TMP_SubHead", cardRect, new Vector2(300f, 22f), new Vector2(0f, 96f));
        TextMeshProUGUI subTmp = subObj.AddComponent<TextMeshProUGUI>();
        if (sharedFontAsset != null) subTmp.font = sharedFontAsset;
        subTmp.text = subHead;
        subTmp.fontSize = 14f;
        subTmp.fontStyle = FontStyles.Bold;
        subTmp.alignment = TextAlignmentOptions.Center;
        subTmp.color = ColorIceWhite;

        // Top MainHead: FIRST / SECOND / THIRD LAW
        GameObject mainObj = CreateUIElement("TMP_MainHead", cardRect, new Vector2(300f, 26f), new Vector2(0f, 72f));
        TextMeshProUGUI mainTmp = mainObj.AddComponent<TextMeshProUGUI>();
        if (sharedFontAsset != null) mainTmp.font = sharedFontAsset;
        mainTmp.text = mainHead;
        mainTmp.fontSize = 18f;
        mainTmp.fontStyle = FontStyles.Bold;
        mainTmp.alignment = TextAlignmentOptions.Center;
        mainTmp.color = ColorPureWhite;

        // Central Graphic Icon
        GameObject iconObj = CreateUIElement("Icon_Graphic", cardRect, new Vector2(136f, 136f), new Vector2(0f, 5f));
        Image iconImg = iconObj.AddComponent<Image>();
        if (iconSprite != null)
        {
            iconImg.sprite = iconSprite;
            iconImg.color = Color.white;
        }
        else
        {
            iconImg.color = ColorNeonCyan;
        }

        // Footer Label (INERTIA / FORCE & ACCELERATION / ACTION & REACTION)
        GameObject footObj = CreateUIElement("TMP_Footer", cardRect, new Vector2(320f, 44f), new Vector2(0f, -88f));
        TextMeshProUGUI footTmp = footObj.AddComponent<TextMeshProUGUI>();
        if (sharedFontAsset != null) footTmp.font = sharedFontAsset;
        footTmp.text = footerLabel;
        footTmp.fontSize = 15f;
        footTmp.fontStyle = FontStyles.Bold;
        footTmp.alignment = TextAlignmentOptions.Center;
        footTmp.color = ColorNeonCyan;
        footTmp.lineSpacing = -8f;
    }

    private void CreateEnterLaboratoryButton(Transform parent, Sprite pillSprite, Vector2 pos)
    {
        enterButtonObj = CreateUIElement("Btn_EnterLaboratory", parent, new Vector2(460f, 72f), pos);
        enterButtonRect = enterButtonObj.GetComponent<RectTransform>();

        // Frame
        GameObject frameObj = CreateUIElement("Enter_Frame", enterButtonRect, new Vector2(460f, 72f), Vector2.zero);
        enterButtonFrame = frameObj.AddComponent<Image>();
        if (pillSprite != null)
        {
            enterButtonFrame.sprite = pillSprite;
            enterButtonFrame.color = Color.white;
        }
        else
        {
            enterButtonFrame.color = ColorNeonCyan;
        }

        // Fill
        GameObject fillObj = CreateUIElement("Enter_Fill", frameObj.transform, new Vector2(450f, 62f), Vector2.zero);
        enterButtonFill = fillObj.AddComponent<Image>();
        enterButtonFill.color = ColorButtonNormal;

        // Label: ENTER LABORATORY →
        GameObject txtObj = CreateUIElement("TMP_Enter", fillObj.transform, new Vector2(430f, 54f), Vector2.zero);
        enterButtonTmp = txtObj.AddComponent<TextMeshProUGUI>();
        if (sharedFontAsset != null) enterButtonTmp.font = sharedFontAsset;
        enterButtonTmp.text = "ENTER LABORATORY →";
        enterButtonTmp.fontSize = 24f;
        enterButtonTmp.fontStyle = FontStyles.Bold;
        enterButtonTmp.alignment = TextAlignmentOptions.Center;
        enterButtonTmp.color = ColorPureWhite;

        // BoxCollider for precise 3D Raycasting & Physical Direct Touch
        enterButtonCollider = enterButtonObj.AddComponent<BoxCollider>();
        enterButtonCollider.size = new Vector3(480f, 85f, 60f);
        enterButtonCollider.center = Vector3.zero;
    }

    private void CreateTechDecals(Transform parent)
    {
        // Bottom-Left Tech Marks
        GameObject lObj = CreateUIElement("Tech_Decal_Left", parent, new Vector2(160f, 28f), new Vector2(-490f, -325f));
        TextMeshProUGUI lTmp = lObj.AddComponent<TextMeshProUGUI>();
        if (sharedFontAsset != null) lTmp.font = sharedFontAsset;
        lTmp.text = "[ ///// ///// ]";
        lTmp.fontSize = 14f;
        lTmp.fontStyle = FontStyles.Bold;
        lTmp.alignment = TextAlignmentOptions.Center;
        lTmp.color = new Color(0.00f, 0.75f, 0.95f, 0.60f);

        // Bottom-Right Tech Marks
        GameObject rObj = CreateUIElement("Tech_Decal_Right", parent, new Vector2(160f, 28f), new Vector2(490f, -325f));
        TextMeshProUGUI rTmp = rObj.AddComponent<TextMeshProUGUI>();
        if (sharedFontAsset != null) rTmp.font = sharedFontAsset;
        rTmp.text = "[ \\\\\\\\\\ \\\\\\\\\\ ]";
        rTmp.fontSize = 14f;
        rTmp.fontStyle = FontStyles.Bold;
        rTmp.alignment = TextAlignmentOptions.Center;
        rTmp.color = new Color(0.00f, 0.75f, 0.95f, 0.60f);
    }

    private void BindExistingElements()
    {
        Transform b = panelCanvasGo.transform.Find("Panel_SciFiFrame/Btn_EnterLaboratory");
        if (b == null) b = panelCanvasGo.transform.Find("Panel_SciFiFrame/Panel_Background/Btn_EnterLaboratory");
        if (b != null)
        {
            enterButtonObj = b.gameObject;
            enterButtonRect = b.GetComponent<RectTransform>();
            enterButtonCollider = b.GetComponent<BoxCollider>();
            if (enterButtonCollider == null) enterButtonCollider = b.gameObject.AddComponent<BoxCollider>();
            enterButtonCollider.size = new Vector3(480f, 85f, 60f);

            enterButtonFrame = b.Find("Enter_Frame")?.GetComponent<Image>();
            enterButtonFill = b.Find("Enter_Frame/Enter_Fill")?.GetComponent<Image>();
            enterButtonTmp = b.Find("Enter_Frame/Enter_Fill/TMP_Enter")?.GetComponent<TextMeshProUGUI>();
        }
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

    private Sprite LoadSpriteAsset(string path)
    {
#if UNITY_EDITOR
        Sprite sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sp != null) return sp;

        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (tex != null)
        {
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }
#endif
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

    #endregion

    #region Interaction & Raycasting

    void Update()
    {
        if (!isPanelVisible || isDismissing) return;

        UpdateInteraction();
    }

    private void UpdateInteraction()
    {
        Ray ray = default;
        bool hasRay = false;
        bool selectionRequested = false;

        // 0. Automated test injection override
        if (isTestActive)
        {
            ray = testRay;
            hasRay = true;
            if (isTestTrigger) selectionRequested = true;
        }
        // 1. Meta Quest 3S Tracked Hand Ray
        else if (FloatingVRDestinationPanel.Instance != null && FloatingVRDestinationPanel.Instance.IsHandTrackingActive && FloatingVRDestinationPanel.Instance.HasActiveHandRay)
        {
            ray = FloatingVRDestinationPanel.Instance.ActiveHandRay;
            hasRay = true;
            if (FloatingVRDestinationPanel.Instance.IsPinching) selectionRequested = true;
        }
        // 2. Meta Quest 3S Touch Plus Controller Ray
        else if (rightHandController != null)
        {
            ray = new Ray(rightHandController.position + rightHandController.forward * 0.05f, rightHandController.forward);
            hasRay = true;

            XRInputDevice rDev = XRInputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (rDev.isValid)
            {
                if (rDev.TryGetFeatureValue(XRCommonUsages.trigger, out float tVal) && tVal > 0.40f) selectionRequested = true;
                if (rDev.TryGetFeatureValue(XRCommonUsages.triggerButton, out bool tBtn) && tBtn) selectionRequested = true;
                if (rDev.TryGetFeatureValue(XRCommonUsages.primaryButton, out bool pBtn) && pBtn) selectionRequested = true;
            }
        }
        // 3. Desktop / Mouse / Eye Ray Fallback
        else if (playerCamera != null)
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

        if (NewtonInput.GetMouseButton(0) || NewtonInput.GetKey(KeyCode.E) || NewtonInput.GetKey(KeyCode.Space) || NewtonInput.GetKey(KeyCode.Return))
        {
            selectionRequested = true;
        }

        bool selectClickThisFrame = selectionRequested && !prevSelectionState;
        prevSelectionState = selectionRequested;

        if (!hasRay || enterButtonCollider == null) return;

        // Raycast against ENTER LABORATORY button
        bool hitButton = enterButtonCollider.Raycast(ray, out RaycastHit hitInfo, 15.0f);

        // Direct Touch / Poke Check for Hand Tracking (Fingertip physical penetration)
        if (!hitButton && FloatingVRDestinationPanel.Instance != null && FloatingVRDestinationPanel.Instance.IsHandTrackingActive)
        {
            Transform hand = FloatingVRDestinationPanel.Instance.ActiveHandTransform;
            if (hand != null)
            {
                Transform indexTip = hand.Find("IndexTip");
                Vector3 tipPos = (indexTip != null) ? indexTip.position : hand.position;
                if (enterButtonCollider.bounds.Contains(tipPos))
                {
                    hitButton = true;
                    selectClickThisFrame = true; // Direct poke triggers selection!
                }
            }
        }

        // Hover Highlight
        if (hitButton != isButtonHovered)
        {
            isButtonHovered = hitButton;
            SetButtonHover(isButtonHovered);
        }

        // Trigger Click
        if (hitButton && selectClickThisFrame)
        {
            if (Time.unscaledTime - lastClickTime > 0.30f)
            {
                lastClickTime = Time.unscaledTime;
                OnEnterLaboratoryClicked();
            }
        }
    }

    public void SetButtonHover(bool hovered)
    {
        if (enterButtonFrame != null)
        {
            enterButtonFrame.color = hovered ? ColorGlowCyan : ColorNeonCyan;
        }
        if (enterButtonFill != null)
        {
            enterButtonFill.color = hovered ? ColorButtonHover : ColorButtonNormal;
        }
        if (enterButtonRect != null)
        {
            enterButtonRect.localScale = hovered ? Vector3.one * 1.05f : Vector3.one;
        }
    }

    public void OnEnterLaboratoryClicked(bool immediate = false)
    {
        TriggerHapticPulse();
        if (immediate)
        {
            SetPanelVisibility(false);
            Debug.Log("[Newton VR] Grand Welcome Panel dismissed immediately via ENTER LABORATORY button.");
        }
        else
        {
            StartCoroutine(AnimateButtonClickAndDismiss());
        }
    }

    private IEnumerator AnimateButtonClickAndDismiss()
    {
        isDismissing = true;

        // Button confirmation feedback
        if (enterButtonFill != null) enterButtonFill.color = ColorEmeraldGreen;
        if (enterButtonFrame != null) enterButtonFrame.color = ColorPureWhite;
        if (enterButtonRect != null) enterButtonRect.localScale = Vector3.one * 0.94f;

        yield return new WaitForSeconds(0.12f);

        if (enterButtonRect != null) enterButtonRect.localScale = Vector3.one * 1.04f;

        // Smooth holographic dismiss fade out
        float elapsed = 0f;
        float duration = 0.25f;
        Vector3 origScale = panelCanvasGo != null ? panelCanvasGo.transform.localScale : Vector3.one * 0.0015f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            if (panelCanvasGroup != null) panelCanvasGroup.alpha = Mathf.Lerp(1.0f, 0.0f, t);
            if (panelCanvasGo != null) panelCanvasGo.transform.localScale = Vector3.Lerp(origScale, origScale * 0.88f, t);
            yield return null;
        }

        // Hide ONLY the Welcome Panel
        SetPanelVisibility(false);
        isDismissing = false;

        // Reset scale and alpha for next time
        if (panelCanvasGo != null) panelCanvasGo.transform.localScale = origScale;
        if (panelCanvasGroup != null) panelCanvasGroup.alpha = 1.0f;

        Debug.Log("[Newton VR] Grand Welcome Panel smoothly dismissed. Navigation panel and LABS button remain fully active in Main Lobby.");
    }

    private IEnumerator PlayEntranceAnimation()
    {
        if (panelCanvasGroup == null && panelCanvasGo != null)
        {
            panelCanvasGroup = panelCanvasGo.GetComponent<CanvasGroup>();
            if (panelCanvasGroup == null) panelCanvasGroup = panelCanvasGo.AddComponent<CanvasGroup>();
        }

        if (panelCanvasGroup != null && panelCanvasGo != null)
        {
            panelCanvasGroup.alpha = 0f;
            Vector3 targetScale = Vector3.one * 0.0015f;
            panelCanvasGo.transform.localScale = targetScale * 0.88f;

            float elapsed = 0f;
            float duration = 0.35f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = Mathf.Sin(t * Mathf.PI * 0.5f); // Smooth ease out
                panelCanvasGroup.alpha = ease;
                panelCanvasGo.transform.localScale = Vector3.Lerp(targetScale * 0.88f, targetScale, ease);
                yield return null;
            }

            panelCanvasGroup.alpha = 1.0f;
            panelCanvasGo.transform.localScale = targetScale;
        }
    }

    private void TriggerHapticPulse()
    {
        if (!XRSettings.isDeviceActive) return;
        XRInputDevice rDev = XRInputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if (rDev.isValid) rDev.SendHapticImpulse(0, 0.6f, 0.12f);

        XRInputDevice lDev = XRInputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        if (lDev.isValid) lDev.SendHapticImpulse(0, 0.6f, 0.12f);
    }

    #endregion

    #region Positioning & Visibility

    /// <summary>
    /// Snaps the Welcome Panel centrally in front of the player at eye level.
    /// Dominates the initial central view as the MAIN visual attraction.
    /// </summary>
    public void SnapToCenterPosition()
    {
        // Central placement in front of player spawn (0, 0, 5.80) facing (0, 180, 0)
        // Position: (0.00, 1.58, 3.80) facing Euler(8.0, 180.0, 0.0)
        Vector3 targetPos = new Vector3(0.00f, 1.58f, 3.80f);
        Quaternion targetRot = Quaternion.Euler(8.0f, 180.0f, 0.0f);

        // If playerCamera is present and far from origin spawn, orient relative to camera
        if (playerCamera != null)
        {
            Vector3 camPos = playerCamera.transform.position;
            // Check if player is near spawn (0, 0, 5.80)
            if (Vector3.Distance(camPos, new Vector3(0f, 1.70f, 5.80f)) > 3.0f)
            {
                Vector3 headFwd = Vector3.ProjectOnPlane(playerCamera.transform.forward, Vector3.up).normalized;
                if (headFwd.sqrMagnitude < 0.01f) headFwd = Vector3.forward;
                targetPos = camPos + headFwd * viewingDistance;
                targetPos.y = centerHeight;
                targetRot = Quaternion.LookRotation(headFwd) * Quaternion.Euler(upwardTiltAngle, 0f, 0f);
            }
        }

        transform.position = targetPos;
        transform.rotation = targetRot;

        if (panelCanvasGo != null)
        {
            panelCanvasGo.transform.localPosition = Vector3.zero;
            panelCanvasGo.transform.localRotation = Quaternion.identity;
            panelCanvasGo.transform.localScale = Vector3.one * 0.0015f;
        }
    }

    /// <summary>
    /// Backwards compatibility alias
    /// </summary>
    public void SnapToRightPosition()
    {
        SnapToCenterPosition();
    }

    public void SetPanelVisibility(bool visible)
    {
        isPanelVisible = visible;
        if (panelCanvasGo != null)
        {
            panelCanvasGo.SetActive(visible);
        }
        if (!visible)
        {
            isButtonHovered = false;
            SetButtonHover(false);
        }
    }

    #endregion

    #region Automated Test Overrides

    public void SetTestRaycast(bool active, Vector3 origin, Vector3 direction, bool trigger)
    {
        isTestActive = active;
        testRay = new Ray(origin, direction);
        isTestTrigger = trigger;
    }

    #endregion
}
