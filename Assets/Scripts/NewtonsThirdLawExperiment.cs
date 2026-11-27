using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Newton's Third Law of Motion (Action and Reaction: F1 = -F2) Interactive Lab Experiment.
/// Demonstrates that whenever object A exerts a force on object B, object B simultaneously
/// exerts a force equal in magnitude and opposite in direction on object A.
/// Features wide separation between Cart A (Cyan) and Cart B (Violet), dramatic approach run,
/// satisfying center crash impact, and elastic bounceback recoil with live synchronized F1 = -F2 telemetry.
/// Fully compatible with Meta Quest 3S VR controller pointing/triggers & Desktop PC.
/// </summary>
public class NewtonsThirdLawExperiment : MonoBehaviour
{
    [Header("Experiment Parameters")]
    [Tooltip("Starting separation offset from center for each cart.")]
    public float separationDistance = 3.4f;

    [Tooltip("Approach speed in m/s toward the center crash.")]
    public float approachSpeed = 3.6f;

    [Tooltip("Applied push force in Newtons.")]
    public float appliedForce = 12.0f;

    [Tooltip("Peak collision impact force in Newtons.")]
    public float impactForceMagnitude = 24.0f;

    [Tooltip("Elastic restitution bounce factor (0.75 = energetic bounce).")]
    public float restitution = 0.82f;

    [Tooltip("Mass of Cart A in kg.")]
    public float massA = 1.0f;

    [Tooltip("Mass of Cart B in kg.")]
    public float massB = 1.0f;

    public enum ExperimentState
    {
        Ready,
        Approaching,
        Bouncing,
        Stopped
    }

    [Header("Current Live State")]
    public ExperimentState currentState = ExperimentState.Ready;
    public float forceA = 0.0f;
    public float forceB = 0.0f;
    public float velocityA = 0.0f;
    public float velocityB = 0.0f;
    public float displacementA = 0.0f;
    public float displacementB = 0.0f;

    [Header("Sub-Object References (Auto-assigned)")]
    public Transform cartARoot;
    public Transform cartBRoot;
    public Transform springRoot;
    public Transform pushLeftButton;
    public Transform pushRightButton;
    public Transform resetButton;

    [Header("Force Indicators & Visuals")]
    public Transform leftForceIndicator;
    public Transform rightForceIndicator;

    public List<Transform> cartAWheels = new List<Transform>();
    public List<Transform> cartBWheels = new List<Transform>();

    [Header("Digital Readouts")]
    public TextMesh f1ValueText;
    public TextMesh f2ValueText;
    public TextMesh equationDisplayText;

    // Multi-part cart synchronization
    private List<Transform> cartAParts = new List<Transform>();
    private List<Vector3> initialPartOffsetsA = new List<Vector3>();

    private List<Transform> cartBParts = new List<Transform>();
    private List<Vector3> initialPartOffsetsB = new List<Vector3>();

    private Vector3 initialPosA;
    private Vector3 initialPosB;
    private Vector3 initialSpringScale = Vector3.one;
    private float centerContactThreshold = 1.62f;

    private AudioSource audioSource;
    private AudioClip clickSound;
    private AudioClip crashSound;
    private AudioClip springSound;

    void Awake()
    {
        InitializeSubObjects();
        SetupAudio();
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

            // Cart A Cyan parts
            if (n.Contains("Cart A Cyan") || (n.Contains("Cart A") && !n.Contains("Cart B")))
            {
                if (cartARoot == null && (n.Contains("Chassis") || n.Contains("Top")))
                {
                    cartARoot = t;
                }
                if (!cartAParts.Contains(t) && !n.Contains("Caption"))
                {
                    cartAParts.Add(t);
                }
                if (n.Contains("Wheel"))
                {
                    cartAWheels.Add(t);
                }
            }

            // Cart B Violet parts
            if (n.Contains("Cart B Violet") || (n.Contains("Cart B") && !n.Contains("Cart A")))
            {
                if (cartBRoot == null && (n.Contains("Chassis") || n.Contains("Top")))
                {
                    cartBRoot = t;
                }
                if (!cartBParts.Contains(t) && !n.Contains("Caption"))
                {
                    cartBParts.Add(t);
                }
                if (n.Contains("Wheel"))
                {
                    cartBWheels.Add(t);
                }
            }

            // Central Spring
            if (springRoot == null && (n.Contains("Spring") || n.Contains("Coil") || n.Contains("Accordion")))
            {
                springRoot = t;
            }

            // Buttons
            if (pushLeftButton == null && n.Contains("PUSH LEFT") && !n.Contains("Base") && !n.Contains("Text"))
            {
                pushLeftButton = t;
            }
            if (pushRightButton == null && n.Contains("PUSH RIGHT") && !n.Contains("Base") && !n.Contains("Text"))
            {
                pushRightButton = t;
            }
            if (resetButton == null && (n.Contains("RESET Button") || n == "RESET Button") && !n.Contains("Base") && !n.Contains("Text"))
            {
                resetButton = t;
            }

            // Force indicators
            if (leftForceIndicator == null && n.Contains("Left Force Indicator"))
            {
                leftForceIndicator = t;
            }
            if (rightForceIndicator == null && n.Contains("Right Force Indicator"))
            {
                rightForceIndicator = t;
            }
        }

        // Fallbacks for cart roots
        if (cartARoot == null && cartAParts.Count > 0) cartARoot = cartAParts[0];
        if (cartBRoot == null && cartBParts.Count > 0) cartBRoot = cartBParts[0];

        if (cartARoot != null) initialPosA = cartARoot.localPosition;
        if (cartBRoot != null) initialPosB = cartBRoot.localPosition;

        foreach (Transform p in cartAParts) initialPartOffsetsA.Add(p.localPosition);
        foreach (Transform p in cartBParts) initialPartOffsetsB.Add(p.localPosition);

        if (springRoot != null) initialSpringScale = springRoot.localScale;

        // Add colliders for VR ray / hand interaction
        AddButtonCollider(pushLeftButton, "PushLeft");
        AddButtonCollider(pushRightButton, "PushRight");
        AddButtonCollider(resetButton, "Reset");
        if (cartARoot != null) AddButtonCollider(cartARoot, "DirectPushLeft");
        if (cartBRoot != null) AddButtonCollider(cartBRoot, "DirectPushRight");

        SetupDigitalDisplays();

        Debug.Log($"[Newton VR Lab 3] Sub-objects initialized: CartA={cartAParts.Count}, CartB={cartBParts.Count}, Spring={springRoot != null}");
    }

    private void AddButtonCollider(Transform btn, string actionName)
    {
        if (btn == null) return;
        BoxCollider bc = btn.GetComponent<BoxCollider>();
        if (bc == null)
        {
            bc = btn.gameObject.AddComponent<BoxCollider>();
            bc.size = new Vector3(0.55f, 0.55f, 0.55f);
        }
        btn.gameObject.name = $"{btn.gameObject.name}_{actionName}";
    }

    private void SetupDigitalDisplays()
    {
        Transform f1Housing = null;
        Transform f2Housing = null;
        Transform equationObj = null;

        foreach (Transform t in GetComponentsInChildren<Transform>(true))
        {
            string n = t.name;
            if (f1Housing == null && (n.Contains("FORCE F1 Display Housing") || n.Contains("FORCE F1 Screen") || n.Contains("F1")))
            {
                f1Housing = t;
            }
            if (f2Housing == null && (n.Contains("FORCE F2 Display Housing") || n.Contains("FORCE F2 Screen") || n.Contains("F2")))
            {
                f2Housing = t;
            }
            if (equationObj == null && n.Contains("ThirdLaw_Equation"))
            {
                equationObj = t;
            }
        }

        if (f1ValueText == null && f1Housing != null)
        {
            Transform textT = f1Housing.Find("Live_F1_Text");
            if (textT == null)
            {
                GameObject newTextObj = new GameObject("Live_F1_Text");
                newTextObj.transform.SetParent(f1Housing, false);
                newTextObj.transform.localPosition = new Vector3(0f, -0.05f, -0.06f);
                newTextObj.transform.localRotation = Quaternion.identity;

                f1ValueText = newTextObj.AddComponent<TextMesh>();
                f1ValueText.fontSize = 32;
                f1ValueText.characterSize = 0.045f;
                f1ValueText.alignment = TextAlignment.Center;
                f1ValueText.anchor = TextAnchor.MiddleCenter;
                f1ValueText.color = new Color(0.1f, 0.95f, 1.0f); // Vivid Cyan
                f1ValueText.fontStyle = FontStyle.Bold;
            }
            else
            {
                f1ValueText = textT.GetComponent<TextMesh>();
            }
        }

        if (f2ValueText == null && f2Housing != null)
        {
            Transform textT = f2Housing.Find("Live_F2_Text");
            if (textT == null)
            {
                GameObject newTextObj = new GameObject("Live_F2_Text");
                newTextObj.transform.SetParent(f2Housing, false);
                newTextObj.transform.localPosition = new Vector3(0f, -0.05f, -0.06f);
                newTextObj.transform.localRotation = Quaternion.identity;

                f2ValueText = newTextObj.AddComponent<TextMesh>();
                f2ValueText.fontSize = 32;
                f2ValueText.characterSize = 0.045f;
                f2ValueText.alignment = TextAlignment.Center;
                f2ValueText.anchor = TextAnchor.MiddleCenter;
                f2ValueText.color = new Color(0.85f, 0.35f, 1.0f); // Vivid Purple
                f2ValueText.fontStyle = FontStyle.Bold;
            }
            else
            {
                f2ValueText = textT.GetComponent<TextMesh>();
            }
        }

        if (equationDisplayText == null && equationObj != null)
        {
            equationDisplayText = equationObj.GetComponent<TextMesh>();
        }
    }

    private void SetupAudio()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0.7f;
            audioSource.maxDistance = 25f;
            audioSource.playOnAwake = false;
        }

        clickSound = CreateTone(720f, 0.06f);
        crashSound = CreateCrashTone(180f, 0.25f);
        springSound = CreateTone(440f, 0.12f);
    }

    private AudioClip CreateTone(float frequency, float duration)
    {
        int sampleRate = 22050;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = 1f - (t / duration);
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * 0.5f;
        }
        AudioClip clip = AudioClip.Create("Tone", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateCrashTone(float frequency, float duration)
    {
        int sampleRate = 22050;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = Mathf.Exp(-t * 9f);
            float noise = (Random.value * 2f - 1f) * 0.4f;
            float tone = Mathf.Sin(2f * Mathf.PI * frequency * t);
            samples[i] = (tone + noise) * envelope * 0.7f;
        }
        AudioClip clip = AudioClip.Create("Crash", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    void Update()
    {
        HandleKeyboardShortcuts();
        UpdatePhysicsSimulation();
        UpdateDisplays();
    }

    private void HandleKeyboardShortcuts()
    {
        Camera cam = Camera.main;
        if (cam != null && Vector3.Distance(transform.position, cam.transform.position) > 22f) return;

        // 1 or LeftArrow: Push Left
        if (NewtonInput.GetKeyDown(KeyCode.Alpha1) || NewtonInput.GetKeyDown(KeyCode.LeftArrow))
        {
            PushLeft();
        }

        // 2 or RightArrow: Push Right
        if (NewtonInput.GetKeyDown(KeyCode.Alpha2) || NewtonInput.GetKeyDown(KeyCode.RightArrow))
        {
            PushRight();
        }

        // Space: Toggle Push or Stop
        if (NewtonInput.GetKeyDown(KeyCode.Space))
        {
            if (currentState == ExperimentState.Ready || currentState == ExperimentState.Stopped)
            {
                PushLeft();
            }
            else
            {
                StopExperiment();
            }
        }

        // R or 3: Reset Experiment
        if (NewtonInput.GetKeyDown(KeyCode.R) || NewtonInput.GetKeyDown(KeyCode.Alpha3))
        {
            ResetExperiment();
        }
    }

    private void UpdatePhysicsSimulation()
    {
        if (currentState == ExperimentState.Ready || currentState == ExperimentState.Stopped) return;

        float dt = Time.deltaTime;

        if (currentState == ExperimentState.Approaching)
        {
            // Both carts accelerate toward each other across the open track
            displacementA += velocityA * dt;
            displacementB += velocityB * dt;

            // Check for collision in center
            float currentCartGap = (initialPosB.x + displacementB) - (initialPosA.x + displacementA);

            if (currentCartGap <= centerContactThreshold)
            {
                // CRASH IMPACT!
                TriggerCrashAndBounce();
            }
        }
        else if (currentState == ExperimentState.Bouncing)
        {
            // Both carts rebound outward in opposite directions
            displacementA += velocityA * dt;
            displacementB += velocityB * dt;

            // Decelerate recoil with track friction
            float decelRate = 2.4f;
            if (velocityA < 0f)
            {
                velocityA += decelRate * dt;
                if (velocityA > 0f) velocityA = 0f;
            }
            else if (velocityA > 0f)
            {
                velocityA -= decelRate * dt;
                if (velocityA < 0f) velocityA = 0f;
            }

            if (velocityB > 0f)
            {
                velocityB -= decelRate * dt;
                if (velocityB < 0f) velocityB = 0f;
            }
            else if (velocityB < 0f)
            {
                velocityB += decelRate * dt;
                if (velocityB > 0f) velocityB = 0f;
            }

            // Stop when recoil motion settles
            if (Mathf.Abs(velocityA) < 0.05f && Mathf.Abs(velocityB) < 0.05f)
            {
                velocityA = 0f;
                velocityB = 0f;
                currentState = ExperimentState.Stopped;
                NarrationManager.Instance?.NotifyExperimentCompleted(3);
            }
        }

        // Bound track constraints so carts stay on track
        displacementA = Mathf.Clamp(displacementA, -separationDistance - 0.5f, 0.0f);
        displacementB = Mathf.Clamp(displacementB, 0.0f, separationDistance + 0.5f);

        // Move all parts of Cart A together
        for (int i = 0; i < cartAParts.Count; i++)
        {
            if (cartAParts[i] != null && i < initialPartOffsetsA.Count)
            {
                cartAParts[i].localPosition = initialPartOffsetsA[i] + new Vector3(displacementA, 0f, 0f);
            }
        }

        // Move all parts of Cart B together
        for (int i = 0; i < cartBParts.Count; i++)
        {
            if (cartBParts[i] != null && i < initialPartOffsetsB.Count)
            {
                cartBParts[i].localPosition = initialPartOffsetsB[i] + new Vector3(displacementB, 0f, 0f);
            }
        }

        // Rotate wheels
        float wheelRotA = -velocityA * dt * 360f;
        foreach (var w in cartAWheels)
        {
            if (w != null) w.Rotate(Vector3.forward, wheelRotA, Space.Self);
        }
        float wheelRotB = -velocityB * dt * 360f;
        foreach (var w in cartBWheels)
        {
            if (w != null) w.Rotate(Vector3.forward, wheelRotB, Space.Self);
        }

        // Scale connecting spring/coil between carts
        if (springRoot != null)
        {
            float currentCartGap = (initialPosB.x + displacementB) - (initialPosA.x + displacementA);
            float scaleFactor = Mathf.Clamp(currentCartGap / 4.0f, 0.25f, 3.5f);
            springRoot.localScale = new Vector3(initialSpringScale.x * scaleFactor, initialSpringScale.y, initialSpringScale.z);
        }
    }

    private void TriggerCrashAndBounce()
    {
        currentState = ExperimentState.Bouncing;

        // Play dramatic crash sound
        if (audioSource != null && crashSound != null)
        {
            audioSource.PlayOneShot(crashSound, 0.85f);
        }

        // Equal and opposite collision impact force: F1 = -F2
        forceA = +impactForceMagnitude;
        forceB = -impactForceMagnitude;

        // Elastic rebound: both carts bounce off in opposite directions
        velocityA = -Mathf.Abs(velocityA) * restitution;
        velocityB = +Mathf.Abs(velocityB) * restitution;

        // Flash force indicators
        FlashForceIndicator(leftForceIndicator, new Color(0.1f, 0.95f, 1.0f));
        FlashForceIndicator(rightForceIndicator, new Color(0.85f, 0.35f, 1.0f));

        Debug.Log($"[Newton VR Lab 3] CRASH IMPACT & BOUNCE! Equal & Opposite Collision Force: F1 = {forceA:F1} N, F2 = {forceB:F1} N (F1 = -F2)");
    }

    private void UpdateDisplays()
    {
        // Equal magnitudes and opposite signs
        if (f1ValueText != null)
        {
            string sign = forceA > 0.05f ? "+" : (forceA < -0.05f ? "" : " ");
            f1ValueText.text = $"F1 = {sign}{forceA:F1} N";
        }

        if (f2ValueText != null)
        {
            string sign = forceB > 0.05f ? "+" : (forceB < -0.05f ? "" : " ");
            f2ValueText.text = $"F2 = {sign}{forceB:F1} N";
        }
    }

    public void PushLeft()
    {
        if (audioSource != null && springSound != null) audioSource.PlayOneShot(springSound, 0.65f);

        // Cart A initiates push toward center, Cart B reacts with equal and opposite propulsion
        forceA = +appliedForce;
        forceB = -appliedForce;

        velocityA = +approachSpeed;
        velocityB = -approachSpeed;

        currentState = ExperimentState.Approaching;
        FlashForceIndicator(leftForceIndicator, new Color(0.1f, 0.95f, 1.0f));

        StartCoroutine(ButtonPressAnim(pushLeftButton));
        Debug.Log($"[Newton VR Lab 3] PUSH LEFT: Carts approaching center across {separationDistance * 2:F1}m track gap!");
    }

    public void PushRight()
    {
        if (audioSource != null && springSound != null) audioSource.PlayOneShot(springSound, 0.65f);

        // Cart B initiates push toward center, Cart A reacts with equal and opposite propulsion
        forceA = +appliedForce;
        forceB = -appliedForce;

        velocityA = +approachSpeed;
        velocityB = -approachSpeed;

        currentState = ExperimentState.Approaching;
        FlashForceIndicator(rightForceIndicator, new Color(0.85f, 0.35f, 1.0f));

        StartCoroutine(ButtonPressAnim(pushRightButton));
        Debug.Log($"[Newton VR Lab 3] PUSH RIGHT: Carts approaching center across {separationDistance * 2:F1}m track gap!");
    }

    public void StopExperiment()
    {
        currentState = ExperimentState.Stopped;
        velocityA = 0f;
        velocityB = 0f;
        forceA = 0f;
        forceB = 0f;
        if (audioSource != null && clickSound != null) audioSource.PlayOneShot(clickSound, 0.5f);
        Debug.Log("[Newton VR Lab 3] Stopped motion safely.");
    }

    public void ResetExperiment()
    {
        currentState = ExperimentState.Ready;
        velocityA = 0f;
        velocityB = 0f;
        forceA = 0f;
        forceB = 0f;

        // Position carts separated at opposite sides of the track
        displacementA = -separationDistance;
        displacementB = +separationDistance;

        // Move all parts of Cart A to left separated start
        for (int i = 0; i < cartAParts.Count; i++)
        {
            if (cartAParts[i] != null && i < initialPartOffsetsA.Count)
            {
                cartAParts[i].localPosition = initialPartOffsetsA[i] + new Vector3(displacementA, 0f, 0f);
            }
        }

        // Move all parts of Cart B to right separated start
        for (int i = 0; i < cartBParts.Count; i++)
        {
            if (cartBParts[i] != null && i < initialPartOffsetsB.Count)
            {
                cartBParts[i].localPosition = initialPartOffsetsB[i] + new Vector3(displacementB, 0f, 0f);
            }
        }

        if (springRoot != null)
        {
            springRoot.localScale = initialSpringScale;
        }

        UpdateDisplays();
        StartCoroutine(ButtonPressAnim(resetButton));
        if (audioSource != null && clickSound != null) audioSource.PlayOneShot(clickSound, 0.6f);
        Debug.Log($"[Newton VR Lab 3] Reset: Carts positioned separated at ±{separationDistance:F1}m, ready for crash!");
    }

    private void FlashForceIndicator(Transform indicator, Color col)
    {
        if (indicator == null) return;
        StartCoroutine(FlashIndicatorRoutine(indicator, col));
    }

    private IEnumerator FlashIndicatorRoutine(Transform ind, Color col)
    {
        Renderer r = ind.GetComponent<Renderer>();
        if (r == null || r.material == null) yield break;

        Color orig = r.material.HasProperty("_EmissionColor") ? r.material.GetColor("_EmissionColor") : Color.black;
        r.material.EnableKeyword("_EMISSION");
        r.material.SetColor("_EmissionColor", col * 2.5f);
        yield return new WaitForSeconds(0.35f);
        r.material.SetColor("_EmissionColor", orig);
    }

    private IEnumerator ButtonPressAnim(Transform btn)
    {
        if (btn == null) yield break;
        Vector3 orig = btn.localPosition;
        btn.localPosition = orig + Vector3.down * 0.05f;
        yield return new WaitForSeconds(0.12f);
        btn.localPosition = orig;
    }

    public void ReceiveInteraction(GameObject target)
    {
        if (target == null) return;
        string n = target.name.ToLower();

        if (n.Contains("reset"))
        {
            ResetExperiment();
        }
        else if (n.Contains("pushleft") || n.Contains("push left"))
        {
            PushLeft();
        }
        else if (n.Contains("pushright") || n.Contains("push right"))
        {
            PushRight();
        }
        else if (n.Contains("stop"))
        {
            StopExperiment();
        }
    }
}
