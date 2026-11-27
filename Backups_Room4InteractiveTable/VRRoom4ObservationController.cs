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

    [Header("Center Summary Console")]
    public TextMeshProUGUI summaryHeadingText;
    public TextMeshProUGUI summaryBodyText;
    public Image tabResultsBg;
    public Image tabComparisonBg;
    public Image tabConclusionBg;

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

    // Colors
    private readonly Color colorCyanActive = new Color(0.00f, 0.88f, 1.00f, 1.0f);
    private readonly Color colorNavyNormal = new Color(0.04f, 0.12f, 0.24f, 0.95f);
    private readonly Color colorTabHighlight = new Color(0.00f, 0.45f, 0.75f, 1.0f);

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        UpdateSecondLawDisplay();
        SetSummaryTab(0);
    }

    void Update()
    {
        // Desktop / Editor testing shortcuts
        if (Input.GetKeyDown(KeyCode.Alpha1)) CycleMass();
        if (Input.GetKeyDown(KeyCode.Alpha2)) CycleForce();
        if (Input.GetKeyDown(KeyCode.Tab)) CycleSummaryTab();
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

        if (massValueText != null)
        {
            massValueText.text = $"{m:F1}  >";
        }

        if (forceValueText != null)
        {
            forceValueText.text = $"{F:F1}  >";
        }

        if (accelerationValueText != null)
        {
            accelerationValueText.text = $"{a:F1}";
        }

        // Update Dynamic Graph
        UpdateGraphPlot(m, F, a);
    }

    private void UpdateGraphPlot(float m, float F, float a)
    {
        if (graphPointTransform != null)
        {
            // Graph area: 0..20 N -> X: 0..220 units; 0..10 m/s^2 -> Y: 0..180 units
            float normX = Mathf.Clamp01(F / 20.0f);
            float normY = Mathf.Clamp01(a / 10.0f);
            graphPointTransform.anchoredPosition = new Vector2(normX * 220f, normY * 180f);
        }

        if (graphLineTransform != null)
        {
            // Angle of the slope: a = (1/m)*F. Max range: (20N, (20/m) m/s^2)
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

    public void SetSummaryTab(int tabIndex)
    {
        currentSummaryTab = Mathf.Clamp(tabIndex, 0, 2);

        if (tabResultsBg != null) tabResultsBg.color = (currentSummaryTab == 0) ? colorTabHighlight : colorNavyNormal;
        if (tabComparisonBg != null) tabComparisonBg.color = (currentSummaryTab == 1) ? colorTabHighlight : colorNavyNormal;
        if (tabConclusionBg != null) tabConclusionBg.color = (currentSummaryTab == 2) ? colorTabHighlight : colorNavyNormal;

        if (summaryHeadingText != null && summaryBodyText != null)
        {
            switch (currentSummaryTab)
            {
                case 0:
                    summaryHeadingText.text = "EXPERIMENTAL RESULTS SUMMARY";
                    summaryBodyText.text = "• Law 1 (Inertia): Cart maintains constant velocity until external barrier exerts normal contact force.\n" +
                                           "• Law 2 (Dynamics): Measured acceleration verifies a = F/m with perfect linear proportionality.\n" +
                                           "• Law 3 (Interaction): Impact sensors confirm equal peak force magnitudes (F₁ = F₂ = 10.0 N) in opposite directions.";
                    break;
                case 1:
                    summaryHeadingText.text = "COMPARATIVE LAW ANALYSIS";
                    summaryBodyText.text = "• First Law defines inertial reference frames where equilibrium implies zero net force (ΣF = 0).\n" +
                                           "• Second Law quantifies non-equilibrium motion when net force is non-zero (ΣF = dp/dt = ma).\n" +
                                           "• Third Law establishes that isolated individual forces cannot exist; all forces are pairwise interactions.";
                    break;
                case 2:
                    summaryHeadingText.text = "NEWTONIAN DYNAMICS CONCLUSION";
                    summaryBodyText.text = "Together, Newton's Three Laws of Motion provide a complete, deterministic framework for classical dynamics:\n" +
                                           "Inertia preserves state, Force accelerates mass, and Mutual interactions conserve total linear momentum.";
                    break;
            }
        }
    }

    public void CycleSummaryTab()
    {
        SetSummaryTab((currentSummaryTab + 1) % 3);
    }
}

