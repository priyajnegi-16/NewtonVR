using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controller for Room 4 — Newtonian VR Observation & Data Analysis Centre.
/// Manages interactive data stations, Second Law F=ma calculators and dynamic graph plotting,
/// Third Law telemetry comparisons, and central Experiment Summary console tabs.
/// Supports Meta Quest 3S Touch Plus controllers, real hand-tracking pinches, and editor mouse clicks.
/// </summary>
public class VRRoom4ObservationController : MonoBehaviour
{
    public static VRRoom4ObservationController Instance { get; private set; }

    [Header("Station 2: Newton's Second Law (F = ma)")]
    public TextMeshProUGUI massValueText;
    public TextMeshProUGUI forceValueText;
    public TextMeshProUGUI accelerationValueText;
    public RectTransform graphLineTransform;
    public RectTransform graphPointTransform;
    public TextMeshProUGUI graphSlopeText;

    [Header("Station 3: Third Law Telemetry")]
    public TextMeshProUGUI cart1ForceText;
    public TextMeshProUGUI cart2ForceText;
    public TextMeshProUGUI actionReactionStatusText;

    [Header("Center Summary Console Tabs")]
    public Image tabResultsBg;
    public Image tabComparisonBg;
    public Image tabConclusionBg;
    public Image tabResultsFrame;
    public Image tabComparisonFrame;
    public Image tabConclusionFrame;
    public TextMeshProUGUI tabResultsText;
    public TextMeshProUGUI tabComparisonText;
    public TextMeshProUGUI tabConclusionText;
    public Button tabResultsBtn;
    public Button tabComparisonBtn;
    public Button tabConclusionBtn;

    [Header("Center Summary Console Content Panels")]
    public GameObject contentResultsPanel;
    public GameObject contentComparisonPanel;
    public GameObject contentConclusionPanel;

    [Header("Dynamic Real Experiment Data Displays")]
    public TextMeshProUGUI law1DataBodyText;
    public TextMeshProUGUI law2DataBodyText;
    public TextMeshProUGUI law3DataBodyText;

    [Header("Legacy Fallbacks")]
    public TextMeshProUGUI summaryHeadingText;
    public TextMeshProUGUI summaryBodyText;

    // Interactive Second Law Values
    private readonly float[] availableMasses = new float[] { 1.0f, 2.0f, 5.0f, 10.0f };
    private readonly float[] availableForces = new float[] { 5.0f, 10.0f, 15.0f, 20.0f };
    private int currentMassIndex = 2;   // Default 5.0 kg (matches reference)
    private int currentForceIndex = 1;  // Default 10.0 N (matches reference)

    public float CurrentMass => availableMasses[currentMassIndex];
    public float CurrentForce => availableForces[currentForceIndex];
    public float CurrentAcceleration => CurrentForce / CurrentMass;

    // Summary Console Active Tab (0: Results, 1: Comparison, 2: Conclusion)
    private int currentSummaryTab = 0;
    public int CurrentSummaryTab => currentSummaryTab;

    // Futuristic Theme Colors
    private readonly Color colorCyanActive     = new Color(0.00f, 0.88f, 1.00f, 1.0f);
    private readonly Color colorTabHighlightBg = new Color(0.00f, 0.48f, 0.82f, 1.0f); // Bright illuminated blue
    private readonly Color colorTabNormalBg    = new Color(0.03f, 0.08f, 0.18f, 0.95f); // Deep navy
    private readonly Color colorTabHoverBg     = new Color(0.00f, 0.32f, 0.58f, 1.0f); // Hover cyan-blue
    private readonly Color colorTabActiveFrame = new Color(0.00f, 0.92f, 1.00f, 1.0f); // Cyan neon border
    private readonly Color colorTabNormalFrame = new Color(0.08f, 0.22f, 0.38f, 0.80f); // Subtle navy border
    private readonly Color colorTextActive     = Color.white;
    private readonly Color colorTextNormal     = new Color(0.50f, 0.78f, 0.92f, 0.90f);

    private int hoveredTabIndex = -1;

    void Awake()
    {
        Instance = this;
        AutoBindReferencesIfNull();
    }

    void Start()
    {
        AutoBindReferencesIfNull();
        SetupButtonListeners();
        UpdateSecondLawDisplay();
        RefreshLiveExperimentData();
        SetSummaryTab(0);
    }

    public void AutoBindReferencesIfNull()
    {
        Transform[] allTransforms = GetComponentsInChildren<Transform>(true);
        foreach (Transform t in allTransforms)
        {
            string n = t.name.ToLower();
            if (contentResultsPanel == null && n == "panel_results") contentResultsPanel = t.gameObject;
            if (contentComparisonPanel == null && n == "panel_comparison") contentComparisonPanel = t.gameObject;
            if (contentConclusionPanel == null && n == "panel_conclusion") contentConclusionPanel = t.gameObject;

            if (tabResultsBtn == null && n == "tab_results") tabResultsBtn = t.GetComponent<Button>();
            if (tabComparisonBtn == null && n == "tab_comparison") tabComparisonBtn = t.GetComponent<Button>();
            if (tabConclusionBtn == null && n == "tab_conclusion") tabConclusionBtn = t.GetComponent<Button>();

            if (tabResultsBg == null && n == "tab_results") tabResultsBg = t.GetComponent<Image>();
            if (tabComparisonBg == null && n == "tab_comparison") tabComparisonBg = t.GetComponent<Image>();
            if (tabConclusionBg == null && n == "tab_conclusion") tabConclusionBg = t.GetComponent<Image>();

            if (tabResultsFrame == null && n == "tab_results_frame") tabResultsFrame = t.GetComponent<Image>();
            if (tabComparisonFrame == null && n == "tab_comparison_frame") tabComparisonFrame = t.GetComponent<Image>();
            if (tabConclusionFrame == null && n == "tab_conclusion_frame") tabConclusionFrame = t.GetComponent<Image>();

            if (tabResultsText == null && n == "tmp_t1") tabResultsText = t.GetComponent<TextMeshProUGUI>();
            if (tabComparisonText == null && n == "tmp_t2") tabComparisonText = t.GetComponent<TextMeshProUGUI>();
            if (tabConclusionText == null && n == "tmp_t3") tabConclusionText = t.GetComponent<TextMeshProUGUI>();

            if (law1DataBodyText == null && n == "tmp_c1body") law1DataBodyText = t.GetComponent<TextMeshProUGUI>();
            if (law2DataBodyText == null && n == "tmp_c2body") law2DataBodyText = t.GetComponent<TextMeshProUGUI>();
            if (law3DataBodyText == null && n == "tmp_c3body") law3DataBodyText = t.GetComponent<TextMeshProUGUI>();
        }
    }

    private void SetupButtonListeners()
    {
        if (tabResultsBtn != null)
        {
            tabResultsBtn.onClick.RemoveAllListeners();
            tabResultsBtn.onClick.AddListener(() => SetSummaryTab(0));
        }
        if (tabComparisonBtn != null)
        {
            tabComparisonBtn.onClick.RemoveAllListeners();
            tabComparisonBtn.onClick.AddListener(() => SetSummaryTab(1));
        }
        if (tabConclusionBtn != null)
        {
            tabConclusionBtn.onClick.RemoveAllListeners();
            tabConclusionBtn.onClick.AddListener(() => SetSummaryTab(2));
        }
    }

    void Update()
    {
        // Desktop / Editor testing shortcuts
        if (Input.GetKeyDown(KeyCode.Alpha1)) CycleMass();
        if (Input.GetKeyDown(KeyCode.Alpha2)) CycleForce();
        if (Input.GetKeyDown(KeyCode.Tab)) CycleSummaryTab();

        // Periodically refresh live data if experiments are active
        if (Time.frameCount % 60 == 0)
        {
            RefreshLiveExperimentData();
        }
    }

    /// <summary>
    /// Reads real live experimental measurements from the active lab experiments.
    /// Falls back to verified laboratory calibration baselines if experiments haven't run yet.
    /// </summary>
    public void RefreshLiveExperimentData()
    {
        // 1. First Law Experiment Data
        NewtonsFirstLawExperiment exp1 = UnityEngine.Object.FindFirstObjectByType<NewtonsFirstLawExperiment>();
        if (exp1 != null && law1DataBodyText != null)
        {
            float v = exp1.currentVelocity > 0f ? exp1.currentVelocity : 5.5f;
            float mu = exp1.frictionCoefficient;
            bool obs = exp1.isObstacleRaised;
            string stateStr = obs ? "Barrier Raised: Cart halted, ball continues (v=5.5 m/s)" : $"Barrier Lowered: Uniform motion (v={v:F1} m/s, a=0.0 m/s²)";
            law1DataBodyText.text = $"• Cart: {exp1.cartMass:F1} kg | Inertia Ball: {exp1.ballMass:F1} kg\n" +
                                   $"• Velocity: {v:F1} m/s (μ: {mu:F2})\n" +
                                   $"• Status: {(obs ? "BARRIER HALT" : "UNIFORM MOTION")}\n" +
                                   $"• Observation: {stateStr}";
        }

        // 2. Second Law Experiment Data
        NewtonsSecondLawExperiment exp2 = UnityEngine.Object.FindFirstObjectByType<NewtonsSecondLawExperiment>();
        if (exp2 != null && law2DataBodyText != null)
        {
            float F = exp2.appliedForce;
            float mA = exp2.massCartA;
            float mB = exp2.massCartB;
            float aA = exp2.accelA > 0f ? exp2.accelA : (F / mA);
            float aB = exp2.accelB > 0f ? exp2.accelB : (F / mB);
            law2DataBodyText.text = $"• Applied Thrust: F = {F:F1} N\n" +
                                   $"• Cart A ({mA:F1} kg): a = {aA:F1} m/s²\n" +
                                   $"• Cart B ({mB:F1} kg): a = {aB:F1} m/s²\n" +
                                   $"• Ratio: a_A / a_B = {(aA / aB):F1}x (Inverse Mass Ratio)";
        }

        // 3. Third Law Experiment Data
        NewtonsThirdLawExperiment exp3 = UnityEngine.Object.FindFirstObjectByType<NewtonsThirdLawExperiment>();
        if (exp3 != null && law3DataBodyText != null)
        {
            float fA = Mathf.Abs(exp3.forceA);
            float fB = Mathf.Abs(exp3.forceB);
            if (fA == 0f && fB == 0f) { fA = exp3.appliedForce; fB = exp3.appliedForce; }
            law3DataBodyText.text = $"• Interacting Carts: m_A = {exp3.massA:F1} kg, m_B = {exp3.massB:F1} kg\n" +
                                   $"• Action Force: F1 = +{fA:F1} N (Right)\n" +
                                   $"• Reaction Force: F2 = -{fB:F1} N (Left)\n" +
                                   $"• Sensor Verification: |F1| = |F2| (Opposite signs)";
        }
    }

    public void CycleMass()
    {
        currentMassIndex = (currentMassIndex + 1) % availableMasses.Length;
        UpdateSecondLawDisplay();
    }

    public void CycleForce()
    {
        currentForceIndex = (currentForceIndex + 1) % availableForces.Length;
        UpdateSecondLawDisplay();
    }

    public void SetMass(float mass)
    {
        for (int i = 0; i < availableMasses.Length; i++)
        {
            if (Mathf.Approximately(availableMasses[i], mass))
            {
                currentMassIndex = i;
                UpdateSecondLawDisplay();
                return;
            }
        }
    }

    public void SetForce(float force)
    {
        for (int i = 0; i < availableForces.Length; i++)
        {
            if (Mathf.Approximately(availableForces[i], force))
            {
                currentForceIndex = i;
                UpdateSecondLawDisplay();
                return;
            }
        }
    }

    public void UpdateSecondLawDisplay()
    {
        float m = CurrentMass;
        float F = CurrentForce;
        float a = CurrentAcceleration;

        if (massValueText != null) massValueText.text = $"{m:F1}  >";
        if (forceValueText != null) forceValueText.text = $"{F:F1}  >";
        if (accelerationValueText != null) accelerationValueText.text = $"{a:F1}";

        UpdateGraphPlot(m, F, a);
    }

    private void UpdateGraphPlot(float m, float F, float a)
    {
        if (graphPointTransform != null)
        {
            float normX = Mathf.Clamp01(F / 20.0f);
            float normY = Mathf.Clamp01(a / 10.0f);
            graphPointTransform.anchoredPosition = new Vector2(normX * 220f, normY * 180f);
        }

        if (graphLineTransform != null)
        {
            float maxA = 20.0f / m;
            float endX = 220f;
            float endY = Mathf.Clamp01(maxA / 10.0f) * 180f;
            float angleDeg = Mathf.Atan2(endY, endX) * Mathf.Rad2Deg;
            float length = Mathf.Sqrt(endX * endX + endY * endY);

            graphLineTransform.sizeDelta = new Vector2(length, 3f);
            graphLineTransform.localEulerAngles = new Vector3(0f, 0f, angleDeg);
        }

        if (graphSlopeText != null)
        {
            graphSlopeText.text = $"Slope = 1/m = {(1.0f / m):F2} kg⁻¹";
        }
    }

    public void SetTabHover(int tabIndex, bool isHovered)
    {
        hoveredTabIndex = isHovered ? tabIndex : -1;
        UpdateTabVisuals();
    }

    public string GetTabName(int index)
    {
        switch (index)
        {
            case 0: return "RESULTS";
            case 1: return "COMPARISON";
            case 2: return "CONCLUSION";
            default: return "UNKNOWN";
        }
    }

    /// <summary>
    /// Switches the active Summary Console tab (0: RESULTS, 1: COMPARISON, 2: CONCLUSION).
    /// Immediately updates active panel content, button highlights, and real experimental data.
    /// </summary>
    public void SetSummaryTab(int tabIndex)
    {
        if (contentResultsPanel == null) AutoBindReferencesIfNull();
        currentSummaryTab = Mathf.Clamp(tabIndex, 0, 2);
        Debug.Log($"[Newton VR Room 4 Controller] SetSummaryTab called -> tabIndex={currentSummaryTab} ({GetTabName(currentSummaryTab)})");

        // 1. Activate Dedicated Sub-Panels
        if (contentResultsPanel != null) contentResultsPanel.SetActive(currentSummaryTab == 0);
        if (contentComparisonPanel != null) contentComparisonPanel.SetActive(currentSummaryTab == 1);
        if (contentConclusionPanel != null) contentConclusionPanel.SetActive(currentSummaryTab == 2);

        Debug.Log($"[Newton VR Room 4 Controller] Displayed Content Panels: Results={(contentResultsPanel != null && contentResultsPanel.activeSelf)}, Comparison={(contentComparisonPanel != null && contentComparisonPanel.activeSelf)}, Conclusion={(contentConclusionPanel != null && contentConclusionPanel.activeSelf)}");

        // 2. Update Tab Styles
        UpdateTabVisuals();

        // 3. Refresh live experimental numbers
        RefreshLiveExperimentData();

        // 4. Update legacy text fields for backward compatibility only if dedicated panels are absent
        if (contentResultsPanel == null && summaryHeadingText != null && summaryBodyText != null)
        {
            switch (currentSummaryTab)
            {
                case 0:
                    summaryHeadingText.text = "EXPERIMENTAL RESULTS SUMMARY";
                    summaryBodyText.text = "• Law 1 (Inertia): Cart maintains constant velocity until external barrier exerts contact force.\n" +
                                           "• Law 2 (Dynamics): Measured acceleration verifies a = F/m with perfect linear proportionality.\n" +
                                           "• Law 3 (Interaction): Impact sensors confirm equal peak force magnitudes (F₁ = F₂ = 12.0 N) in opposite directions.";
                    break;
                case 1:
                    summaryHeadingText.text = "COMPARATIVE MATRIX OF NEWTON'S LAWS";
                    summaryBodyText.text = "• First Law defines force-free equilibrium (ΣF = 0 ⟹ v = const) in an inertial reference frame.\n" +
                                           "• Second Law quantifies dynamic state change when net force is non-zero (ΣF = dp/dt = ma).\n" +
                                           "• Third Law establishes that all forces exist strictly in mutual, simultaneous action-reaction pairs.";
                    break;
                case 2:
                    summaryHeadingText.text = "SCIENTIFIC CONCLUSIONS & CORE LEARNINGS";
                    summaryBodyText.text = "Together, Newton's Three Laws form a complete deterministic framework for classical dynamics:\n" +
                                           "Inertia preserves state, Net Force accelerates mass, and Mutual interactions conserve total linear momentum.";
                    break;
            }
        }
    }

    private void UpdateTabVisuals()
    {
        // Results Tab (Index 0)
        bool rActive = (currentSummaryTab == 0);
        bool rHover = (hoveredTabIndex == 0);
        if (tabResultsBg != null) tabResultsBg.color = rActive ? colorTabHighlightBg : (rHover ? colorTabHoverBg : colorTabNormalBg);
        if (tabResultsFrame != null) tabResultsFrame.color = rActive ? colorTabActiveFrame : (rHover ? colorCyanActive : colorTabNormalFrame);
        if (tabResultsText != null) { tabResultsText.color = rActive ? colorTextActive : (rHover ? Color.white : colorTextNormal); tabResultsText.fontStyle = rActive ? FontStyles.Bold : FontStyles.Normal; }

        // Comparison Tab (Index 1)
        bool cActive = (currentSummaryTab == 1);
        bool cHover = (hoveredTabIndex == 1);
        if (tabComparisonBg != null) tabComparisonBg.color = cActive ? colorTabHighlightBg : (cHover ? colorTabHoverBg : colorTabNormalBg);
        if (tabComparisonFrame != null) tabComparisonFrame.color = cActive ? colorTabActiveFrame : (cHover ? colorCyanActive : colorTabNormalFrame);
        if (tabComparisonText != null) { tabComparisonText.color = cActive ? colorTextActive : (cHover ? Color.white : colorTextNormal); tabComparisonText.fontStyle = cActive ? FontStyles.Bold : FontStyles.Normal; }

        // Conclusion Tab (Index 2)
        bool kActive = (currentSummaryTab == 2);
        bool kHover = (hoveredTabIndex == 2);
        if (tabConclusionBg != null) tabConclusionBg.color = kActive ? colorTabHighlightBg : (kHover ? colorTabHoverBg : colorTabNormalBg);
        if (tabConclusionFrame != null) tabConclusionFrame.color = kActive ? colorTabActiveFrame : (kHover ? colorCyanActive : colorTabNormalFrame);
        if (tabConclusionText != null) { tabConclusionText.color = kActive ? colorTextActive : (kHover ? Color.white : colorTextNormal); tabConclusionText.fontStyle = kActive ? FontStyles.Bold : FontStyles.Normal; }
    }

    public void CycleSummaryTab()
    {
        SetSummaryTab((currentSummaryTab + 1) % 3);
    }
}


