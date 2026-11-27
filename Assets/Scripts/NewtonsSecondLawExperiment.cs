using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Newton's Second Law of Motion (F = m * a) Interactive Lab Experiment.
/// Demonstrates the fundamental relationship between Force, Mass, and Acceleration.
/// Features:
/// - Dedicated 3D Interactive Control Consoles for BOTH Cart 1 (Blue) and Cart 2 (Red) mass selection.
/// - Dedicated 3D Force Control Console (0-20 N) with instant preset and step buttons.
/// - Visual real-time mass block scaling and floating labels above both carts.
/// - Side-by-side acceleration comparison (a1 = F/m1 vs a2 = F/m2).
/// - Full VR controller and Desktop PC raycast/click interaction ([E] or Left Click).
/// </summary>
public class NewtonsSecondLawExperiment : MonoBehaviour
{
    public enum ComparisonMode
    {
        SameForce_DifferentMass, // F_A = F_B, m_A != m_B
        SameMass_DifferentForce  // m_A = m_B, F_A != F_B
    }

    [Header("Experiment Parameters")]
    [Tooltip("Applied primary net force in Newtons (2 to 20 N).")]
    [Range(2f, 20f)]
    public float appliedForce = 10.0f;

    [Tooltip("Secondary force ratio for comparison mode 2.")]
    public float secondaryForceRatio = 0.4f;

    [Tooltip("Mass of Cart 1 (Blue / Cyan) in kg.")]
    public float massCartA = 1.0f;

    [Tooltip("Mass of Cart 2 (Red) in kg.")]
    public float massCartB = 5.0f;

    [Tooltip("Active comparison mode.")]
    public ComparisonMode activeMode = ComparisonMode.SameForce_DifferentMass;

    [Tooltip("Maximum track travel distance in meters.")]
    public float trackLength = 7.5f;

    public enum ExperimentState
    {
        Ready,
        Accelerating,
        Stopped
    }

    [Header("Current Live State")]
    public ExperimentState currentState = ExperimentState.Ready;
    public float currentForceA = 10.0f;
    public float currentForceB = 10.0f;
    public float velocityA = 0.0f;
    public float velocityB = 0.0f;
    public float accelA = 0.0f;
    public float accelB = 0.0f;
    public float distanceA = 0.0f;
    public float distanceB = 0.0f;

    [Header("Sub-Object References (Auto-assigned)")]
    public Transform cartARoot;
    public Transform cartBRoot;
    public Transform launchButton;
    public Transform stopButton;
    public Transform resetButton;

    [Header("Panels")]
    public Transform cartControlPanel;
    public Transform forceSliderPanel;
    public Transform massSelectionPanel;
    public Transform modePanel;
    public Transform liveDataPanel;
    public Transform secondLawBoard;

    [Header("Controls: Force Slider & Arrows")]
    public Transform forceSliderKnob;
    public Transform forceSliderRail;
    public Transform forceArrowAShaft;
    public Transform forceArrowAHead;
    public Transform forceArrowBShaft;
    public Transform forceArrowBHead;

    [Header("Visual Mass Blocks & Labels on Carts")]
    public Transform cartAMassBlock;
    public Transform cartAMassLabel;
    public Vector3 initialMassBlockScaleA = Vector3.one;

    public Transform cartBMassBlock;
    public Transform cartBMassLabel;
    public Vector3 initialMassBlockScaleB = Vector3.one;

    private TextMesh cartAFloatingLabel;
    private TextMesh cartBFloatingLabel;

    public List<Transform> cartAWheels = new List<Transform>();
    public List<Transform> cartBWheels = new List<Transform>();

    [Header("Live Data UI")]
    public TextMesh liveTelemetryText;
    public TextMesh forceReadoutText;
    public TextMesh modeDisplayText;
    public TextMesh liveDataPanelText;

    [Header("3D Interactive Control Console")]
    public Transform interactiveConsoleRoot;
    private TextMesh consoleCart1MassText;
    private TextMesh consoleCart1AccelText;
    private TextMesh consoleForceText;
    private TextMesh consoleForceRatioText;
    private TextMesh consoleCart2MassText;
    private TextMesh consoleCart2AccelText;

    private Dictionary<string, Renderer> consoleButtonRenderers = new Dictionary<string, Renderer>();

    // Multi-part cart synchronization
    private List<Transform> cartAParts = new List<Transform>();
    private List<Vector3> initialPartOffsetsA = new List<Vector3>();

    private List<Transform> cartBParts = new List<Transform>();
    private List<Vector3> initialPartOffsetsB = new List<Vector3>();

    // Initial transforms
    private Vector3 initialPosA;
    private Vector3 initialPosB;
    private Vector3 initialShaftScaleA = Vector3.one;
    private Vector3 initialShaftScaleB = Vector3.one;
    private Vector3 initialHeadLocalPosA = Vector3.zero;
    private Vector3 initialHeadLocalPosB = Vector3.zero;

    private float sliderMinX = -7.95f;
    private float sliderMaxX = -5.65f;

    private AudioSource audioSource;
    private AudioClip clickSound;
    private AudioClip launchSound;

    void Awake()
    {
        InitializeSubObjects();
        SetupAudio();
        BuildOrUpdate3DControlPanels();
    }

    void Start()
    {
        ResetExperiment();
    }

    private void InitializeSubObjects()
    {
        Transform[] allChildren = GetComponentsInChildren<Transform>(true);
        cartAParts.Clear();
        cartBParts.Clear();
        initialPartOffsetsA.Clear();
        initialPartOffsetsB.Clear();

        foreach (Transform t in allChildren)
        {
            string n = t.gameObject.name;

            // Cart A root & parts
            if (n.Contains("Cart A"))
            {
                if (cartARoot == null && (n.Contains("Chassis") || n.Contains("Top")))
                {
                    cartARoot = t;
                }
                if (!cartAParts.Contains(t) && !n.Contains("Force Arrow") && !n.Contains("Caption"))
                {
                    cartAParts.Add(t);
                }
                if (n.Contains("Wheel"))
                {
                    cartAWheels.Add(t);
                }
                if (cartAMassBlock == null && n.Contains("MassBlock"))
                {
                    cartAMassBlock = t;
                    initialMassBlockScaleA = t.localScale;
                }
                if (cartAMassLabel == null && n.Contains("MassLabel"))
                {
                    cartAMassLabel = t;
                }
            }

            // Cart B root & parts
            if (n.Contains("Cart B"))
            {
                if (cartBRoot == null && (n.Contains("Chassis") || n.Contains("Top")))
                {
                    cartBRoot = t;
                }
                if (!cartBParts.Contains(t) && !n.Contains("Force Arrow") && !n.Contains("Caption"))
                {
                    cartBParts.Add(t);
                }
                if (n.Contains("Wheel"))
                {
                    cartBWheels.Add(t);
                }
                if (cartBMassBlock == null && n.Contains("MassBlock"))
                {
                    cartBMassBlock = t;
                    initialMassBlockScaleB = t.localScale;
                }
                if (cartBMassLabel == null && n.Contains("MassLabel"))
                {
                    cartBMassLabel = t;
                }
            }

            // Panels
            if (cartControlPanel == null && n.Contains("Cart Control Panel")) cartControlPanel = t;
            if (forceSliderPanel == null && n.Contains("Force Slider Panel")) forceSliderPanel = t;
            if (massSelectionPanel == null && n.Contains("Mass Selection Panel")) massSelectionPanel = t;
            if (modePanel == null && n.Contains("Mode Panel") && !n.Contains("Line") && !n.Contains("Rim") && !n.Contains("Glow") && !n.Contains("Heading") && !n.Contains("Underline")) modePanel = t;
            if (liveDataPanel == null && n.Contains("Live Data Panel") && !n.Contains("Line") && !n.Contains("Rim") && !n.Contains("Glow") && !n.Contains("Heading") && !n.Contains("Underline")) liveDataPanel = t;
            if (secondLawBoard == null && n.Contains("Second Law Board") && !n.Contains("Line") && !n.Contains("Rim") && !n.Contains("Glow") && !n.Contains("Heading") && !n.Contains("Underline")) secondLawBoard = t;

            // Force slider & arrows
            if (forceSliderKnob == null && n.Contains("Force Slider Knob")) forceSliderKnob = t;
            if (forceSliderRail == null && n.Contains("Force Slider Rail")) forceSliderRail = t;
            if (forceArrowAShaft == null && n.Contains("Force Arrow A_Shaft")) forceArrowAShaft = t;
            if (forceArrowAHead == null && n.Contains("Force Arrow A_Head")) forceArrowAHead = t;
            if (forceArrowBShaft == null && n.Contains("Force Arrow B_Shaft")) forceArrowBShaft = t;
            if (forceArrowBHead == null && n.Contains("Force Arrow B_Head")) forceArrowBHead = t;
        }

        // Fallbacks for cart roots
        if (cartARoot == null && cartAParts.Count > 0) cartARoot = cartAParts[0];
        if (cartBRoot == null && cartBParts.Count > 0) cartBRoot = cartBParts[0];

        if (cartARoot != null) initialPosA = cartARoot.localPosition;
        if (cartBRoot != null) initialPosB = cartBRoot.localPosition;

        foreach (Transform p in cartAParts) initialPartOffsetsA.Add(p.localPosition);
        foreach (Transform p in cartBParts) initialPartOffsetsB.Add(p.localPosition);

        // Hide legacy FBX panels that were occluded or redundant
        if (cartControlPanel != null) cartControlPanel.gameObject.SetActive(false);
        if (forceSliderPanel != null) forceSliderPanel.gameObject.SetActive(false);
        if (massSelectionPanel != null) massSelectionPanel.gameObject.SetActive(false);
        if (modePanel != null) modePanel.gameObject.SetActive(false);

        // Slider boundaries along X
        if (forceSliderRail != null)
        {
            sliderMinX = forceSliderRail.localPosition.x - 1.15f;
            sliderMaxX = forceSliderRail.localPosition.x + 1.15f;
        }
        else if (forceSliderKnob != null)
        {
            sliderMinX = forceSliderKnob.localPosition.x - 1.15f;
            sliderMaxX = forceSliderKnob.localPosition.x + 1.15f;
        }

        if (forceArrowAShaft != null) initialShaftScaleA = forceArrowAShaft.localScale;
        if (forceArrowBShaft != null) initialShaftScaleB = forceArrowBShaft.localScale;
        if (forceArrowAHead != null) initialHeadLocalPosA = forceArrowAHead.localPosition;
        if (forceArrowBHead != null) initialHeadLocalPosB = forceArrowBHead.localPosition;

        // Disable collider on background Second Law Board so it never intercepts clicks
        if (secondLawBoard != null)
        {
            Collider boardCol = secondLawBoard.GetComponent<Collider>();
            if (boardCol != null) boardCol.enabled = false;
        }

        // Setup text displays and floating cart labels
        SetupDisplayElements();
    }

    #region Procedural 3D Interactive Control Console

    /// <summary>
    /// Builds a sleek, futuristic 3-module control console right on the front table ledge:
    /// - Left: Cart 1 (Blue) Mass Panel (1, 2, 5, 10 kg presets + [-]/[+])
    /// - Center: Force Controller (5, 10, 15, 20 N presets + [-]/[+]) & Launch / Reset
    /// - Right: Cart 2 (Red) Mass Panel (1, 2, 5, 10 kg presets + [-]/[+])
    /// </summary>
    [ContextMenu("Build 3D Control Panels Now")]
    public void BuildOrUpdate3DControlPanels()
    {
        Transform existing = transform.Find("Lab2_Interactive_Control_Console");
        if (existing != null)
        {
            Destroy(existing.gameObject);
        }

        consoleButtonRenderers.Clear();

        GameObject consoleGo = new GameObject("Lab2_Interactive_Control_Console");
        consoleGo.transform.SetParent(transform, false);
        // Positioned right on the front desk ledge facing the player, tilted back 28 deg for optimal ergonomics
        consoleGo.transform.localPosition = new Vector3(0.0f, 1.05f, -2.05f);
        consoleGo.transform.localRotation = Quaternion.Euler(28.0f, 0f, 0f);
        interactiveConsoleRoot = consoleGo.transform;

        Material navyMat = FindMaterial("Deep Navy", new Color(0.015f, 0.035f, 0.08f), Color.black);
        Material cyanMat = FindMaterial("Neon Cyan", new Color(0.05f, 0.85f, 1.0f), new Color(0.1f, 0.85f, 1.2f) * 1.8f);
        Material redMat = FindMaterial("Red Cart", new Color(0.95f, 0.12f, 0.16f), new Color(0.85f, 0.05f, 0.1f) * 1.5f);
        Material goldMat = FindMaterial("GlowGold", new Color(1.0f, 0.75f, 0.1f), new Color(1.0f, 0.70f, 0.1f) * 1.8f);
        Material greenMat = FindMaterial("Launch Green", new Color(0.1f, 0.95f, 0.25f), new Color(0.1f, 1.2f, 0.3f) * 1.8f);
        Material warningRedMat = FindMaterial("Warning Red", new Color(0.95f, 0.05f, 0.08f), new Color(1.2f, 0.05f, 0.1f) * 1.6f);
        Material darkBtnMat = FindMaterial("Graphite Metal", new Color(0.05f, 0.07f, 0.11f), Color.black);

        // =========================================================================
        // 1. LEFT CONSOLE: CART 1 MASS (CYAN / BLUE)
        // =========================================================================
        GameObject cart1Panel = CreateConsoleBackplate(consoleGo.transform, "Cart1_Mass_Console", new Vector3(-1.85f, 0f, 0f), new Vector3(1.50f, 0.82f, 0.04f), navyMat, cyanMat);

        CreateHeader(cart1Panel.transform, "CART 1 MASS (BLUE)", new Vector3(0f, 0.30f, -0.03f), new Color(0.2f, 0.9f, 1.0f), 24);
        consoleCart1MassText = CreateValueDisplay(cart1Panel.transform, "Cart1_Mass_Display", $"{massCartA:F1} kg", new Vector3(0f, 0.16f, -0.03f), new Color(0.1f, 0.95f, 1.0f), 32);

        // Step buttons: [-] and [+]
        CreateButton(cart1Panel.transform, "Cart1_Mass_Minus", "[- 1kg]", new Vector3(-0.35f, 0.04f, -0.03f), new Vector3(0.32f, 0.13f, 0.05f), darkBtnMat, Color.white, 20);
        CreateButton(cart1Panel.transform, "Cart1_Mass_Plus", "[+ 1kg]", new Vector3(0.35f, 0.04f, -0.03f), new Vector3(0.32f, 0.13f, 0.05f), darkBtnMat, Color.white, 20);

        // Preset buttons: 1kg, 2kg, 5kg, 10kg
        CreateButton(cart1Panel.transform, "Cart1_Mass_1kg", "1 kg", new Vector3(-0.52f, -0.13f, -0.03f), new Vector3(0.29f, 0.14f, 0.05f), darkBtnMat, Color.white, 22);
        CreateButton(cart1Panel.transform, "Cart1_Mass_2kg", "2 kg", new Vector3(-0.17f, -0.13f, -0.03f), new Vector3(0.29f, 0.14f, 0.05f), darkBtnMat, Color.white, 22);
        CreateButton(cart1Panel.transform, "Cart1_Mass_5kg", "5 kg", new Vector3(0.17f, -0.13f, -0.03f), new Vector3(0.29f, 0.14f, 0.05f), darkBtnMat, Color.white, 22);
        CreateButton(cart1Panel.transform, "Cart1_Mass_10kg", "10 kg", new Vector3(0.52f, -0.13f, -0.03f), new Vector3(0.29f, 0.14f, 0.05f), darkBtnMat, Color.white, 22);

        consoleCart1AccelText = CreateValueDisplay(cart1Panel.transform, "Cart1_Accel_Preview", $"Expected: a₁ = {appliedForce / massCartA:F1} m/s²", new Vector3(0f, -0.29f, -0.03f), new Color(0.6f, 0.9f, 1.0f), 20);

        // =========================================================================
        // 2. CENTER CONSOLE: FORCE CONTROLLER & LAUNCH / RESET
        // =========================================================================
        GameObject forcePanel = CreateConsoleBackplate(consoleGo.transform, "Force_Control_Console", new Vector3(0.0f, 0f, 0f), new Vector3(1.75f, 0.82f, 0.04f), navyMat, goldMat);

        CreateHeader(forcePanel.transform, "APPLIED FORCE (F)", new Vector3(0f, 0.30f, -0.03f), new Color(1.0f, 0.85f, 0.2f), 24);
        consoleForceText = CreateValueDisplay(forcePanel.transform, "Force_Display", $"{appliedForce:F1} N", new Vector3(0f, 0.17f, -0.03f), new Color(1.0f, 0.88f, 0.25f), 34);

        // Step buttons: [- 2N] and [+ 2N]
        CreateButton(forcePanel.transform, "Force_Minus", "[- 2N]", new Vector3(-0.42f, 0.05f, -0.03f), new Vector3(0.38f, 0.13f, 0.05f), darkBtnMat, Color.white, 20);
        CreateButton(forcePanel.transform, "Force_Plus", "[+ 2N]", new Vector3(0.42f, 0.05f, -0.03f), new Vector3(0.38f, 0.13f, 0.05f), darkBtnMat, Color.white, 20);

        // Presets: 5N, 10N, 15N, 20N
        CreateButton(forcePanel.transform, "Force_5N", "5 N", new Vector3(-0.57f, -0.11f, -0.03f), new Vector3(0.33f, 0.13f, 0.05f), darkBtnMat, Color.white, 22);
        CreateButton(forcePanel.transform, "Force_10N", "10 N", new Vector3(-0.19f, -0.11f, -0.03f), new Vector3(0.33f, 0.13f, 0.05f), darkBtnMat, Color.white, 22);
        CreateButton(forcePanel.transform, "Force_15N", "15 N", new Vector3(0.19f, -0.11f, -0.03f), new Vector3(0.33f, 0.13f, 0.05f), darkBtnMat, Color.white, 22);
        CreateButton(forcePanel.transform, "Force_20N", "20 N", new Vector3(0.57f, -0.11f, -0.03f), new Vector3(0.33f, 0.13f, 0.05f), darkBtnMat, Color.white, 22);

        // Action Buttons: LAUNCH & RESET
        CreateButton(forcePanel.transform, "Launch_Carts_Button", "► LAUNCH CARTS", new Vector3(-0.42f, -0.27f, -0.03f), new Vector3(0.78f, 0.15f, 0.05f), greenMat, Color.black, 22);
        CreateButton(forcePanel.transform, "Reset_Simulation_Button", "↺ RESET", new Vector3(0.42f, -0.27f, -0.03f), new Vector3(0.78f, 0.15f, 0.05f), warningRedMat, Color.white, 22);

        // =========================================================================
        // 3. RIGHT CONSOLE: CART 2 MASS (RED / CRIMSON)
        // =========================================================================
        GameObject cart2Panel = CreateConsoleBackplate(consoleGo.transform, "Cart2_Mass_Console", new Vector3(1.85f, 0f, 0f), new Vector3(1.50f, 0.82f, 0.04f), navyMat, redMat);

        CreateHeader(cart2Panel.transform, "CART 2 MASS (RED)", new Vector3(0f, 0.30f, -0.03f), new Color(1.0f, 0.45f, 0.45f), 24);
        consoleCart2MassText = CreateValueDisplay(cart2Panel.transform, "Cart2_Mass_Display", $"{massCartB:F1} kg", new Vector3(0f, 0.16f, -0.03f), new Color(1.0f, 0.35f, 0.35f), 32);

        // Step buttons: [-] and [+]
        CreateButton(cart2Panel.transform, "Cart2_Mass_Minus", "[- 1kg]", new Vector3(-0.35f, 0.04f, -0.03f), new Vector3(0.32f, 0.13f, 0.05f), darkBtnMat, Color.white, 20);
        CreateButton(cart2Panel.transform, "Cart2_Mass_Plus", "[+ 1kg]", new Vector3(0.35f, 0.04f, -0.03f), new Vector3(0.32f, 0.13f, 0.05f), darkBtnMat, Color.white, 20);

        // Preset buttons: 1kg, 2kg, 5kg, 10kg
        CreateButton(cart2Panel.transform, "Cart2_Mass_1kg", "1 kg", new Vector3(-0.52f, -0.13f, -0.03f), new Vector3(0.29f, 0.14f, 0.05f), darkBtnMat, Color.white, 22);
        CreateButton(cart2Panel.transform, "Cart2_Mass_2kg", "2 kg", new Vector3(-0.17f, -0.13f, -0.03f), new Vector3(0.29f, 0.14f, 0.05f), darkBtnMat, Color.white, 22);
        CreateButton(cart2Panel.transform, "Cart2_Mass_5kg", "5 kg", new Vector3(0.17f, -0.13f, -0.03f), new Vector3(0.29f, 0.14f, 0.05f), darkBtnMat, Color.white, 22);
        CreateButton(cart2Panel.transform, "Cart2_Mass_10kg", "10 kg", new Vector3(0.52f, -0.13f, -0.03f), new Vector3(0.29f, 0.14f, 0.05f), darkBtnMat, Color.white, 22);

        consoleCart2AccelText = CreateValueDisplay(cart2Panel.transform, "Cart2_Accel_Preview", $"Expected: a₂ = {appliedForce / massCartB:F1} m/s²", new Vector3(0f, -0.29f, -0.03f), new Color(1.0f, 0.7f, 0.7f), 20);

        UpdateConsoleHighlights();
    }

    private GameObject CreateConsoleBackplate(Transform parent, string name, Vector3 localPos, Vector3 size, Material baseMat, Material borderMat)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.localPosition = localPos;
        root.transform.localRotation = Quaternion.identity;

        // Base plate
        GameObject plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        plate.name = $"{name}_Base";
        plate.transform.SetParent(root.transform, false);
        plate.transform.localPosition = Vector3.zero;
        plate.transform.localScale = size;
        if (plate.GetComponent<Renderer>() != null && baseMat != null)
        {
            plate.GetComponent<Renderer>().material = baseMat;
        }

        // Glowing border trim
        GameObject rim = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rim.name = $"{name}_Rim";
        rim.transform.SetParent(root.transform, false);
        rim.transform.localPosition = new Vector3(0f, 0f, size.z * 0.4f);
        rim.transform.localScale = new Vector3(size.x + 0.03f, size.y + 0.03f, 0.015f);
        if (rim.GetComponent<Renderer>() != null && borderMat != null)
        {
            rim.GetComponent<Renderer>().material = borderMat;
        }

        // Remove rim collider so it doesn't intercept button clicks
        Collider rimCol = rim.GetComponent<Collider>();
        if (rimCol != null) Destroy(rimCol);

        return root;
    }

    private TextMesh CreateHeader(Transform parent, string text, Vector3 localPos, Color color, int fontSize)
    {
        GameObject go = new GameObject("Header_Text");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.identity;

        TextMesh tm = go.AddComponent<TextMesh>();
        tm.text = text;
        tm.fontSize = fontSize;
        tm.characterSize = 0.03f;
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.color = color;
        tm.fontStyle = FontStyle.Bold;
        return tm;
    }

    private TextMesh CreateValueDisplay(Transform parent, string name, string text, Vector3 localPos, Color color, int fontSize)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.identity;

        TextMesh tm = go.AddComponent<TextMesh>();
        tm.text = text;
        tm.fontSize = fontSize;
        tm.characterSize = 0.03f;
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.color = color;
        tm.fontStyle = FontStyle.Bold;
        return tm;
    }

    private GameObject CreateButton(Transform parent, string name, string label, Vector3 localPos, Vector3 size, Material mat, Color textColor, int fontSize)
    {
        GameObject btn = GameObject.CreatePrimitive(PrimitiveType.Cube);
        btn.name = name;
        btn.transform.SetParent(parent, false);
        btn.transform.localPosition = localPos;
        btn.transform.localRotation = Quaternion.identity;
        btn.transform.localScale = size;

        Renderer r = btn.GetComponent<Renderer>();
        if (r != null && mat != null) r.material = mat;
        if (r != null) consoleButtonRenderers[name] = r;

        // Button Text label
        GameObject textObj = new GameObject("Label");
        textObj.transform.SetParent(btn.transform, false);
        textObj.transform.localPosition = new Vector3(0f, 0f, -0.55f);
        textObj.transform.localRotation = Quaternion.identity;

        TextMesh tm = textObj.AddComponent<TextMesh>();
        tm.text = label;
        tm.fontSize = fontSize;
        tm.characterSize = 0.032f;
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.color = textColor;
        tm.fontStyle = FontStyle.Bold;

        return btn;
    }

    private void UpdateConsoleHighlights()
    {
        Material cyanMat = FindMaterial("Neon Cyan", new Color(0.05f, 0.85f, 1.0f), new Color(0.1f, 0.85f, 1.2f) * 1.8f);
        Material redMat = FindMaterial("Red Cart", new Color(0.95f, 0.12f, 0.16f), new Color(0.85f, 0.05f, 0.1f) * 1.5f);
        Material goldMat = FindMaterial("GlowGold", new Color(1.0f, 0.75f, 0.1f), new Color(1.0f, 0.70f, 0.1f) * 1.8f);
        Material darkBtnMat = FindMaterial("Graphite Metal", new Color(0.05f, 0.07f, 0.11f), Color.black);

        // Highlight Cart 1 presets
        HighlightButton("Cart1_Mass_1kg", Mathf.Approximately(massCartA, 1f), cyanMat, darkBtnMat);
        HighlightButton("Cart1_Mass_2kg", Mathf.Approximately(massCartA, 2f), cyanMat, darkBtnMat);
        HighlightButton("Cart1_Mass_5kg", Mathf.Approximately(massCartA, 5f), cyanMat, darkBtnMat);
        HighlightButton("Cart1_Mass_10kg", Mathf.Approximately(massCartA, 10f), cyanMat, darkBtnMat);

        // Highlight Cart 2 presets
        HighlightButton("Cart2_Mass_1kg", Mathf.Approximately(massCartB, 1f), redMat, darkBtnMat);
        HighlightButton("Cart2_Mass_2kg", Mathf.Approximately(massCartB, 2f), redMat, darkBtnMat);
        HighlightButton("Cart2_Mass_5kg", Mathf.Approximately(massCartB, 5f), redMat, darkBtnMat);
        HighlightButton("Cart2_Mass_10kg", Mathf.Approximately(massCartB, 10f), redMat, darkBtnMat);

        // Highlight Force presets
        HighlightButton("Force_5N", Mathf.Approximately(appliedForce, 5f), goldMat, darkBtnMat);
        HighlightButton("Force_10N", Mathf.Approximately(appliedForce, 10f), goldMat, darkBtnMat);
        HighlightButton("Force_15N", Mathf.Approximately(appliedForce, 15f), goldMat, darkBtnMat);
        HighlightButton("Force_20N", Mathf.Approximately(appliedForce, 20f), goldMat, darkBtnMat);
    }

    private void HighlightButton(string btnName, bool isSelected, Material activeMat, Material idleMat)
    {
        if (consoleButtonRenderers.TryGetValue(btnName, out Renderer r) && r != null)
        {
            r.material = isSelected ? activeMat : idleMat;
            TextMesh tm = r.GetComponentInChildren<TextMesh>();
            if (tm != null)
            {
                tm.color = isSelected ? Color.black : Color.white;
            }
        }
    }

    private Material FindMaterial(string matName, Color fallbackColor, Color fallbackEmission)
    {
        foreach (Material m in Resources.FindObjectsOfTypeAll<Material>())
        {
            if (m != null && m.name == matName) return m;
        }

        Shader s = Shader.Find("Standard");
        if (s == null) s = Shader.Find("Mobile/Diffuse");
        Material mat = new Material(s);
        mat.name = matName;
        mat.color = fallbackColor;
        if (fallbackEmission.maxColorComponent > 0.05f)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", fallbackEmission);
        }
        return mat;
    }

    #endregion

    private void SetupDisplayElements()
    {
        // 1. Telemetry Text on Second Law Board
        if (secondLawBoard != null)
        {
            Transform tObj = secondLawBoard.Find("Live_Telemetry_Text");
            if (tObj == null)
            {
                GameObject textObj = new GameObject("Live_Telemetry_Text");
                textObj.transform.SetParent(secondLawBoard, false);
                textObj.transform.localPosition = new Vector3(0f, 0.15f, 0.08f);
                textObj.transform.localRotation = Quaternion.identity;

                liveTelemetryText = textObj.AddComponent<TextMesh>();
                liveTelemetryText.fontSize = 28;
                liveTelemetryText.characterSize = 0.032f;
                liveTelemetryText.alignment = TextAlignment.Center;
                liveTelemetryText.anchor = TextAnchor.MiddleCenter;
                liveTelemetryText.color = new Color(0.1f, 0.95f, 1.0f);
                liveTelemetryText.fontStyle = FontStyle.Bold;
            }
            else
            {
                liveTelemetryText = tObj.GetComponent<TextMesh>();
            }
        }

        // 2. Floating Labels above Cart A & Cart B
        if (cartARoot != null)
        {
            Transform aLabel = cartARoot.Find("CartA_Floating_Label");
            if (aLabel == null)
            {
                GameObject lblGo = new GameObject("CartA_Floating_Label");
                lblGo.transform.SetParent(cartARoot, false);
                lblGo.transform.localPosition = new Vector3(0f, 0.8f, 0f);
                cartAFloatingLabel = lblGo.AddComponent<TextMesh>();
                cartAFloatingLabel.fontSize = 26;
                cartAFloatingLabel.characterSize = 0.028f;
                cartAFloatingLabel.alignment = TextAlignment.Center;
                cartAFloatingLabel.anchor = TextAnchor.MiddleCenter;
                cartAFloatingLabel.color = new Color(0.2f, 0.9f, 1.0f);
                cartAFloatingLabel.fontStyle = FontStyle.Bold;
            }
            else
            {
                cartAFloatingLabel = aLabel.GetComponent<TextMesh>();
            }
        }

        if (cartBRoot != null)
        {
            Transform bLabel = cartBRoot.Find("CartB_Floating_Label");
            if (bLabel == null)
            {
                GameObject lblGo = new GameObject("CartB_Floating_Label");
                lblGo.transform.SetParent(cartBRoot, false);
                lblGo.transform.localPosition = new Vector3(0f, 0.8f, 0f);
                cartBFloatingLabel = lblGo.AddComponent<TextMesh>();
                cartBFloatingLabel.fontSize = 26;
                cartBFloatingLabel.characterSize = 0.028f;
                cartBFloatingLabel.alignment = TextAlignment.Center;
                cartBFloatingLabel.anchor = TextAnchor.MiddleCenter;
                cartBFloatingLabel.color = new Color(1.0f, 0.35f, 0.35f);
                cartBFloatingLabel.fontStyle = FontStyle.Bold;
            }
            else
            {
                cartBFloatingLabel = bLabel.GetComponent<TextMesh>();
            }
        }

        UpdateCartVisualMassAndLabels();
    }

    private void SetupAudio()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0.7f;
            audioSource.maxDistance = 20f;
            audioSource.playOnAwake = false;
        }

        clickSound = CreateProceduralTone(880f, 0.08f);
        launchSound = CreateProceduralTone(440f, 0.25f);
    }

    private AudioClip CreateProceduralTone(float frequency, float duration)
    {
        int sampleRate = 44100;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = 1f - (t / duration);
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * 0.4f;
        }
        AudioClip clip = AudioClip.Create("Tone", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    void Update()
    {
        HandleKeyboardShortcuts();

        // Calculate active applied forces according to comparison mode
        if (activeMode == ComparisonMode.SameForce_DifferentMass)
        {
            currentForceA = appliedForce;
            currentForceB = appliedForce;
        }
        else // SameMass_DifferentForce
        {
            currentForceA = appliedForce;
            currentForceB = appliedForce * secondaryForceRatio;
        }

        if (currentState == ExperimentState.Accelerating)
        {
            float dt = Time.deltaTime;

            // NEWTON'S 2ND LAW: a = F / m
            accelA = currentForceA / Mathf.Max(massCartA, 0.1f);
            accelB = currentForceB / Mathf.Max(massCartB, 0.1f);

            velocityA += accelA * dt;
            velocityB += accelB * dt;

            float stepA = velocityA * dt;
            float stepB = velocityB * dt;

            distanceA += stepA;
            distanceB += stepB;

            // Move Cart A forward along track (+X direction)
            if (distanceA < trackLength)
            {
                MoveCartAParts(stepA);
                foreach (Transform w in cartAWheels)
                {
                    if (w != null) w.Rotate(Vector3.forward, -(stepA * 360f / 0.4f), Space.Self);
                }
            }

            // Move Cart B forward along track (+X direction)
            if (distanceB < trackLength)
            {
                MoveCartBParts(stepB);
                foreach (Transform w in cartBWheels)
                {
                    if (w != null) w.Rotate(Vector3.forward, -(stepB * 360f / 0.4f), Space.Self);
                }
            }

            if (distanceA >= trackLength && distanceB >= trackLength)
            {
                currentState = ExperimentState.Stopped;
                NarrationManager.Instance?.NotifyExperimentCompleted(2);
            }
        }

        UpdateVisuals();
    }

    private void MoveCartAParts(float delta)
    {
        for (int i = 0; i < cartAParts.Count; i++)
        {
            if (cartAParts[i] != null)
            {
                cartAParts[i].localPosition += new Vector3(delta, 0f, 0f);
            }
        }
    }

    private void MoveCartBParts(float delta)
    {
        for (int i = 0; i < cartBParts.Count; i++)
        {
            if (cartBParts[i] != null)
            {
                cartBParts[i].localPosition += new Vector3(delta, 0f, 0f);
            }
        }
    }

    private void HandleKeyboardShortcuts()
    {
        Camera cam = Camera.main;
        if (cam != null && Vector3.Distance(transform.position, cam.transform.position) > 25f) return;

        // Space or Return: Launch Experiment / Stop
        if (NewtonInput.GetKeyDown(KeyCode.Space) || NewtonInput.GetKeyDown(KeyCode.Return))
        {
            if (currentState == ExperimentState.Ready || currentState == ExperimentState.Stopped)
            {
                LaunchExperiment();
            }
            else
            {
                StopExperiment();
            }
        }

        // R: Reset Experiment
        if (NewtonInput.GetKeyDown(KeyCode.R))
        {
            ResetExperiment();
        }

        // Cart 1 Mass shortcuts: 1, 2, 5, 0 (or Alpha keys)
        if (NewtonInput.GetKeyDown(KeyCode.Alpha1) || NewtonInput.GetKeyDown(KeyCode.Keypad1)) SelectCart1Mass(1.0f);
        if (NewtonInput.GetKeyDown(KeyCode.Alpha2) || NewtonInput.GetKeyDown(KeyCode.Keypad2)) SelectCart1Mass(2.0f);
        if (NewtonInput.GetKeyDown(KeyCode.Alpha5) || NewtonInput.GetKeyDown(KeyCode.Keypad5)) SelectCart1Mass(5.0f);
        if (NewtonInput.GetKeyDown(KeyCode.Alpha0) || NewtonInput.GetKeyDown(KeyCode.Keypad0)) SelectCart1Mass(10.0f);
        if (NewtonInput.GetKeyDown(KeyCode.M)) CycleCart1Mass();

        // Cart 2 Mass shortcuts: 6, 7, 8, 9
        if (NewtonInput.GetKeyDown(KeyCode.Alpha6) || NewtonInput.GetKeyDown(KeyCode.Keypad6)) SelectCart2Mass(1.0f);
        if (NewtonInput.GetKeyDown(KeyCode.Alpha7) || NewtonInput.GetKeyDown(KeyCode.Keypad7)) SelectCart2Mass(2.0f);
        if (NewtonInput.GetKeyDown(KeyCode.Alpha8) || NewtonInput.GetKeyDown(KeyCode.Keypad8)) SelectCart2Mass(5.0f);
        if (NewtonInput.GetKeyDown(KeyCode.Alpha9) || NewtonInput.GetKeyDown(KeyCode.Keypad9)) SelectCart2Mass(10.0f);
        if (NewtonInput.GetKeyDown(KeyCode.N)) CycleCart2Mass();

        // Force shortcuts: F or + / Up arrow (increase), - / Down arrow (decrease)
        if (NewtonInput.GetKeyDown(KeyCode.F) || NewtonInput.GetKeyDown(KeyCode.UpArrow) || NewtonInput.GetKeyDown(KeyCode.Plus) || NewtonInput.GetKeyDown(KeyCode.KeypadPlus))
        {
            StepForce(2.0f);
        }
        if (NewtonInput.GetKeyDown(KeyCode.DownArrow) || NewtonInput.GetKeyDown(KeyCode.Minus) || NewtonInput.GetKeyDown(KeyCode.KeypadMinus))
        {
            StepForce(-2.0f);
        }

        // C or Tab: Toggle Comparison Mode
        if (NewtonInput.GetKeyDown(KeyCode.C) || NewtonInput.GetKeyDown(KeyCode.Tab))
        {
            ToggleComparisonMode();
        }
    }

    #region Force Controls

    public void SetForce(float forceVal)
    {
        appliedForce = Mathf.Clamp(forceVal, 2.0f, 20.0f);
        UpdateForceSliderAndArrows();
        UpdateConsoleHighlights();
        UpdateVisuals();
        if (audioSource != null && clickSound != null) audioSource.PlayOneShot(clickSound, 0.4f);
        Debug.Log($"[Newton VR Lab 2] Applied Force: {appliedForce:F1} N");
    }

    public void StepForce(float delta = 2.0f)
    {
        appliedForce += delta;
        if (appliedForce > 20.05f) appliedForce = 2.0f;
        if (appliedForce < 1.95f) appliedForce = 20.0f;
        UpdateForceSliderAndArrows();
        UpdateConsoleHighlights();
        UpdateVisuals();
        if (audioSource != null && clickSound != null) audioSource.PlayOneShot(clickSound, 0.5f);
        Debug.Log($"[Newton VR Lab 2] Stepped Force to: {appliedForce:F1} N");
    }

    private void UpdateForceSliderAndArrows()
    {
        float ratio = Mathf.Clamp01(appliedForce / 20.0f);

        // Update Slider Knob Physical Position along Rail
        if (forceSliderKnob != null)
        {
            Vector3 pos = forceSliderKnob.localPosition;
            pos.x = Mathf.Lerp(sliderMinX, sliderMaxX, ratio);
            forceSliderKnob.localPosition = pos;
        }

        // Update Force Arrow A
        float scaleA = Mathf.Max(0.15f, ratio * 1.35f);
        if (forceArrowAShaft != null)
        {
            Vector3 s = initialShaftScaleA;
            s.x *= scaleA;
            forceArrowAShaft.localScale = s;
        }
        if (forceArrowAHead != null)
        {
            Vector3 hp = initialHeadLocalPosA;
            hp.x += (scaleA - 1.0f) * 0.45f;
            forceArrowAHead.localPosition = hp;
        }

        // Update Force Arrow B
        float ratioB = (activeMode == ComparisonMode.SameForce_DifferentMass) ? ratio : (ratio * secondaryForceRatio);
        float scaleB = Mathf.Max(0.15f, ratioB * 1.35f);
        if (forceArrowBShaft != null)
        {
            Vector3 s = initialShaftScaleB;
            s.x *= scaleB;
            forceArrowBShaft.localScale = s;
        }
        if (forceArrowBHead != null)
        {
            Vector3 hp = initialHeadLocalPosB;
            hp.x += (scaleB - 1.0f) * 0.45f;
            forceArrowBHead.localPosition = hp;
        }
    }

    #endregion

    #region Independent Mass Selection for BOTH Carts

    // --- CART 1 MASS ---
    public void SelectCart1Mass(float massKg)
    {
        massCartA = Mathf.Clamp(massKg, 0.5f, 20f);
        UpdateCartVisualMassAndLabels();
        UpdateConsoleHighlights();
        UpdateVisuals();
        if (audioSource != null && clickSound != null) audioSource.PlayOneShot(clickSound, 0.6f);
        Debug.Log($"[Newton VR Lab 2] Selected Cart 1 Mass: {massCartA:F1} kg");
    }

    public void StepCart1Mass(float delta)
    {
        float[] presets = new float[] { 1f, 2f, 5f, 10f };
        float current = massCartA;
        if (delta > 0)
        {
            for (int i = 0; i < presets.Length; i++)
            {
                if (presets[i] > current + 0.05f) { SelectCart1Mass(presets[i]); return; }
            }
            SelectCart1Mass(presets[0]);
        }
        else
        {
            for (int i = presets.Length - 1; i >= 0; i--)
            {
                if (presets[i] < current - 0.05f) { SelectCart1Mass(presets[i]); return; }
            }
            SelectCart1Mass(presets[presets.Length - 1]);
        }
    }

    public void CycleCart1Mass()
    {
        StepCart1Mass(1f);
    }

    // --- CART 2 MASS ---
    public void SelectCart2Mass(float massKg)
    {
        massCartB = Mathf.Clamp(massKg, 0.5f, 20f);
        UpdateCartVisualMassAndLabels();
        UpdateConsoleHighlights();
        UpdateVisuals();
        if (audioSource != null && clickSound != null) audioSource.PlayOneShot(clickSound, 0.6f);
        Debug.Log($"[Newton VR Lab 2] Selected Cart 2 Mass: {massCartB:F1} kg");
    }

    public void StepCart2Mass(float delta)
    {
        float[] presets = new float[] { 1f, 2f, 5f, 10f };
        float current = massCartB;
        if (delta > 0)
        {
            for (int i = 0; i < presets.Length; i++)
            {
                if (presets[i] > current + 0.05f) { SelectCart2Mass(presets[i]); return; }
            }
            SelectCart2Mass(presets[0]);
        }
        else
        {
            for (int i = presets.Length - 1; i >= 0; i--)
            {
                if (presets[i] < current - 0.05f) { SelectCart2Mass(presets[i]); return; }
            }
            SelectCart2Mass(presets[presets.Length - 1]);
        }
    }

    public void CycleCart2Mass()
    {
        StepCart2Mass(1f);
    }

    // Backwards compatibility method
    public void SelectMass(float massKg)
    {
        SelectCart2Mass(massKg);
    }

    private void UpdateCartVisualMassAndLabels()
    {
        // 1. Scale Cart A mass block proportionally
        if (cartAMassBlock != null)
        {
            float scaleFactor = Mathf.Clamp(massCartA / 5.0f, 0.55f, 1.55f);
            cartAMassBlock.localScale = new Vector3(initialMassBlockScaleA.x * scaleFactor, initialMassBlockScaleA.y, initialMassBlockScaleA.z * scaleFactor);
        }

        // 2. Scale Cart B mass block proportionally
        if (cartBMassBlock != null)
        {
            float scaleFactor = Mathf.Clamp(massCartB / 5.0f, 0.55f, 1.55f);
            cartBMassBlock.localScale = new Vector3(initialMassBlockScaleB.x * scaleFactor, initialMassBlockScaleB.y, initialMassBlockScaleB.z * scaleFactor);
        }

        // 3. Floating Labels above Carts
        if (cartAFloatingLabel != null)
        {
            cartAFloatingLabel.text = $"<color=#55ffff><b>CART 1\n{massCartA:F0} kg</b></color>";
        }

        if (cartBFloatingLabel != null)
        {
            cartBFloatingLabel.text = $"<color=#ff5555><b>CART 2\n{massCartB:F0} kg</b></color>";
        }
    }

    public void ToggleComparisonMode()
    {
        activeMode = (activeMode == ComparisonMode.SameForce_DifferentMass)
            ? ComparisonMode.SameMass_DifferentForce
            : ComparisonMode.SameForce_DifferentMass;

        UpdateCartVisualMassAndLabels();
        UpdateForceSliderAndArrows();
        UpdateConsoleHighlights();
        UpdateVisuals();
        if (audioSource != null && clickSound != null) audioSource.PlayOneShot(clickSound, 0.7f);
        Debug.Log($"[Newton VR Lab 2] Switched Comparison Mode: {activeMode}");
    }

    #endregion

    private void UpdateVisuals()
    {
        // Calculate display accelerations (shows theoretical a = F/m when ready, or live accel when moving)
        float fA = currentForceA;
        float fB = currentForceB;
        float displayAccelA = (currentState == ExperimentState.Accelerating) ? accelA : (fA / Mathf.Max(massCartA, 0.1f));
        float displayAccelB = (currentState == ExperimentState.Accelerating) ? accelB : (fB / Mathf.Max(massCartB, 0.1f));

        // 1. Update 3D Console Text Displays
        if (consoleCart1MassText != null) consoleCart1MassText.text = $"{massCartA:F1} kg";
        if (consoleCart2MassText != null) consoleCart2MassText.text = $"{massCartB:F1} kg";
        if (consoleForceText != null) consoleForceText.text = $"{appliedForce:F1} N";

        if (consoleCart1AccelText != null) consoleCart1AccelText.text = $"Expected: a₁ = {displayAccelA:F2} m/s²";
        if (consoleCart2AccelText != null) consoleCart2AccelText.text = $"Expected: a₂ = {displayAccelB:F2} m/s²";

        // 2. Mode Display Text on Mode Panel
        if (modeDisplayText != null)
        {
            if (activeMode == ComparisonMode.SameForce_DifferentMass)
            {
                modeDisplayText.text = "MODE 1: SAME FORCE, DIFF MASS\nF_A = F_B  =>  Lighter Cart Accelerates Faster";
            }
            else
            {
                modeDisplayText.text = "MODE 2: SAME MASS, DIFF FORCE\nm_A = m_B  =>  Higher Force Gives Higher Acceleration";
            }
        }

        // 3. Telemetry on Second Law Board
        if (liveTelemetryText != null)
        {
            float ratio = displayAccelB > 0.001f ? (displayAccelA / displayAccelB) : 0f;
            string compHeader = (activeMode == ComparisonMode.SameForce_DifferentMass)
                ? $"<color=#55ffff><b>COMPARISON 1: SAME FORCE ({appliedForce:F1} N), DIFFERENT MASSES</b></color>"
                : $"<color=#ffaa55><b>COMPARISON 2: SAME MASS, DIFFERENT FORCES ({currentForceA:F1} N vs {currentForceB:F1} N)</b></color>";

            liveTelemetryText.text =
                $"{compHeader}\n" +
                "----------------------------------------------------------------------\n" +
                $"• CART 1 (BLUE): m = {massCartA:F0} kg | Force F_A = {currentForceA:F1} N  =>  <b>a_A = {displayAccelA:F2} m/s²</b> | v = {velocityA:F2} m/s\n" +
                $"• CART 2 (RED) : m = {massCartB:F0} kg | Force F_B = {currentForceB:F1} N  =>  <b>a_B = {displayAccelB:F2} m/s²</b> | v = {velocityB:F2} m/s\n" +
                "----------------------------------------------------------------------\n" +
                $"<b>ACCELERATION RATIO (a_A / a_B) = {ratio:F1}x</b>   [F = m × a  =>  a = F / m]";
        }
    }

    public void LaunchExperiment()
    {
        if (currentState == ExperimentState.Ready)
        {
            currentState = ExperimentState.Accelerating;
            if (audioSource != null && launchSound != null) audioSource.PlayOneShot(launchSound, 0.7f);
            AnimateButtonClick(launchButton);
            Debug.Log($"[Newton VR Lab 2] Launched Second Law Demo: aA={accelA:F2} m/s², aB={accelB:F2} m/s²");
        }
    }

    public void StopExperiment()
    {
        currentState = ExperimentState.Stopped;
        velocityA = 0f;
        velocityB = 0f;
        accelA = 0f;
        accelB = 0f;
        if (audioSource != null && clickSound != null) audioSource.PlayOneShot(clickSound, 0.5f);
        AnimateButtonClick(stopButton);
        Debug.Log("[Newton VR Lab 2] Stopped motion safely.");
    }

    public void ResetExperiment()
    {
        currentState = ExperimentState.Ready;
        velocityA = 0f;
        velocityB = 0f;
        accelA = 0f;
        accelB = 0f;
        distanceA = 0f;
        distanceB = 0f;

        // Restore Cart A parts
        for (int i = 0; i < cartAParts.Count; i++)
        {
            if (cartAParts[i] != null && i < initialPartOffsetsA.Count)
            {
                cartAParts[i].localPosition = initialPartOffsetsA[i];
            }
        }
        if (cartARoot != null) cartARoot.localPosition = initialPosA;

        // Restore Cart B parts
        for (int i = 0; i < cartBParts.Count; i++)
        {
            if (cartBParts[i] != null && i < initialPartOffsetsB.Count)
            {
                cartBParts[i].localPosition = initialPartOffsetsB[i];
            }
        }
        if (cartBRoot != null) cartBRoot.localPosition = initialPosB;

        UpdateCartVisualMassAndLabels();
        UpdateForceSliderAndArrows();
        UpdateConsoleHighlights();

        if (audioSource != null && clickSound != null) audioSource.PlayOneShot(clickSound, 0.5f);
        AnimateButtonClick(resetButton);
        UpdateVisuals();
        Debug.Log("[Newton VR Lab 2] Reset Second Law Demo to Start.");
    }

    private void AnimateButtonClick(Transform btn)
    {
        if (btn == null) return;
        StartCoroutine(ButtonClickRoutine(btn));
    }

    private IEnumerator ButtonClickRoutine(Transform btn)
    {
        Vector3 orig = btn.localPosition;
        Vector3 pressed = orig + Vector3.down * 0.05f;
        btn.localPosition = pressed;
        yield return new WaitForSeconds(0.12f);
        btn.localPosition = orig;
    }

    public void ReceiveInteraction(GameObject hitObject, Vector3 worldHitPoint)
    {
        ReceiveInteractionInternal(hitObject, worldHitPoint, hasWorldHitPoint: true);
    }

    public void ReceiveInteraction(GameObject hitObject)
    {
        ReceiveInteractionInternal(hitObject, Vector3.zero, hasWorldHitPoint: false);
    }

    private void ReceiveInteractionInternal(GameObject hitObject, Vector3 worldHitPoint, bool hasWorldHitPoint)
    {
        if (hitObject == null) return;
        string n = hitObject.name.ToLower();
        string pn = hitObject.transform.parent != null ? hitObject.transform.parent.name.ToLower() : "";

        // 1. LAUNCH BUTTON
        if (n.Contains("launch") || pn.Contains("launch"))
        {
            LaunchExperiment();
            return;
        }

        // 2. STOP BUTTON
        if (n.Contains("stop") || pn.Contains("stop"))
        {
            StopExperiment();
            return;
        }

        // 3. RESET BUTTON
        if (n.Contains("reset") || pn.Contains("reset"))
        {
            ResetExperiment();
            return;
        }

        // 4. CART 1 (CART A / BLUE) MASS CONTROLS
        if (n.Contains("cart1") || n.Contains("cart 1") || n.Contains("carta") || (n.Contains("cart a") && !n.Contains("chassis")))
        {
            if (n.Contains("10kg") || n.Contains("10")) { SelectCart1Mass(10.0f); return; }
            if (n.Contains("1kg") || n.Contains("_1")) { SelectCart1Mass(1.0f); return; }
            if (n.Contains("2kg") || n.Contains("_2")) { SelectCart1Mass(2.0f); return; }
            if (n.Contains("5kg") || n.Contains("_5")) { SelectCart1Mass(5.0f); return; }
            if (n.Contains("minus") || n.Contains("dec") || n.Contains("-")) { StepCart1Mass(-1f); return; }
            if (n.Contains("plus") || n.Contains("inc") || n.Contains("+")) { StepCart1Mass(1f); return; }
            CycleCart1Mass();
            return;
        }

        // 5. CART 2 (CART B / RED) MASS CONTROLS
        if (n.Contains("cart2") || n.Contains("cart 2") || n.Contains("cartb") || (n.Contains("cart b") && !n.Contains("chassis")))
        {
            if (n.Contains("10kg") || n.Contains("10")) { SelectCart2Mass(10.0f); return; }
            if (n.Contains("1kg") || n.Contains("_1")) { SelectCart2Mass(1.0f); return; }
            if (n.Contains("2kg") || n.Contains("_2")) { SelectCart2Mass(2.0f); return; }
            if (n.Contains("5kg") || n.Contains("_5")) { SelectCart2Mass(5.0f); return; }
            if (n.Contains("minus") || n.Contains("dec") || n.Contains("-")) { StepCart2Mass(-1f); return; }
            if (n.Contains("plus") || n.Contains("inc") || n.Contains("+")) { StepCart2Mass(1f); return; }
            CycleCart2Mass();
            return;
        }

        // 6. FORCE CONTROLS (Continuous Slider Drag or Preset Buttons)
        if (hasWorldHitPoint && (n.Contains("rail") || n.Contains("slider") || n.Contains("knob")))
        {
            Vector3 localHit = transform.InverseTransformPoint(worldHitPoint);
            float t = Mathf.InverseLerp(sliderMinX, sliderMaxX, localHit.x);
            float mappedForce = Mathf.Clamp(Mathf.Lerp(2.0f, 20.0f, t), 2.0f, 20.0f);
            SetForce(Mathf.Round(mappedForce * 2f) / 2f); // Round to 0.5 N steps
            return;
        }

        if (n.Contains("force"))
        {
            if (n.Contains("20n") || n.Contains("20")) { SetForce(20.0f); return; }
            if (n.Contains("15n") || n.Contains("15")) { SetForce(15.0f); return; }
            if (n.Contains("10n") || n.Contains("10")) { SetForce(10.0f); return; }
            if (n.Contains("5n") || n.Contains(" 5")) { SetForce(5.0f); return; }
            if (n.Contains("minus") || n.Contains("dec") || n.Contains("-")) { StepForce(-2.0f); return; }
            if (n.Contains("plus") || n.Contains("inc") || n.Contains("+")) { StepForce(2.0f); return; }
            StepForce(2.0f);
            return;
        }

        // 7. DIRECT CLICKS ON CARTS CYCLE THEIR MASS
        if (n.Contains("cart a"))
        {
            CycleCart1Mass();
            return;
        }
        if (n.Contains("cart b"))
        {
            CycleCart2Mass();
            return;
        }

        // 8. MODE TOGGLE
        if (n.Contains("mode"))
        {
            ToggleComparisonMode();
            return;
        }
    }
}
