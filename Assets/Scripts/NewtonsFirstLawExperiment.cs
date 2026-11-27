using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Newton's First Law of Motion (Law of Inertia) Interactive Lab Experiment.
/// Demonstrates:
/// 1. Forward motion along track toward obstacle with ball securely seated on cart.
/// 2. Outcome A (Obstacle Raised): Cart and ball reach obstacle and stop together.
/// 3. Outcome B (Obstacle Lowered): Cart and ball continue to end of track and fall together into visible space below.
/// 4. Obstacle control reliably toggles between both outcomes (sinking into track in Z when down).
/// 5. Friction control: Cart velocity and deceleration respond visibly to friction (constant glide at μ = 0, decelerates with friction).
/// 6. Reset restores cart, ball, and obstacle to starting positions.
/// 7. Live Physics Data panel is positioned to the side so track, cart, and ball remain 100% visible throughout run.
/// Fully compatible with Meta Quest 3S VR controllers and Desktop PC controls.
/// </summary>
[ExecuteAlways]
public class NewtonsFirstLawExperiment : MonoBehaviour
{
    [Header("Experiment Parameters")]
    [Tooltip("Target cruise speed after launch in m/s.")]
    public float launchSpeed = 5.5f;

    [Tooltip("Coefficient of kinetic friction (0 = pure frictionless 1st Law demo, >0 = deceleration).")]
    [Range(0f, 0.40f)]
    public float frictionCoefficient = 0.0f;

    [Tooltip("Mass of the cart system in kg.")]
    public float cartMass = 2.0f;

    [Tooltip("Mass of the inertia ball in kg.")]
    public float ballMass = 0.8f;

    public enum ExperimentState
    {
        Ready,       // Cart at start position, ball seated in holder ring
        Grabbed,     // Hand actively holding and moving cart along track
        Launching,   // Spring launch impulse phase
        Cruising,    // Moving forward along track under Newton's First Law
        Collided,    // Struck obstacle; cart and ball stop together cleanly
        Falling,     // Reached track end; cart and ball arcing together into visible space below
        Stopped      // Settled on floor or halted by friction / emergency stop
    }

    [Header("Current Live State")]
    public ExperimentState currentState = ExperimentState.Ready;
    public float currentVelocity = 0.0f;
    public float currentAcceleration = 0.0f;
    public float netForce = 0.0f;
    public bool isObstacleRaised = true;

    [Header("Sub-Object References (Auto-assigned if empty)")]
    public Transform cartRoot;
    public Transform inertiaBall;
    public Transform ballHolderRing;
    public Transform obstacleRoot;
    public Transform resetButton;
    public Transform launchStopButton;
    public Transform obstacleButton;
    public Transform frictionConsole;
    public Transform frictionSliderKnob;
    public Transform sliderTrack;

    [Header("Wheel Transforms for Rolling Animation")]
    public Transform[] wheels;

    [Header("Live Data UI")]
    public TextMesh dataDisplayTextMesh;
    public TextMesh mainStatusTextMesh;
    public TextMesh frictionDisplayTextMesh;

    // Cart multi-part synchronization (keeps wheels, chassis, top plate, lights together)
    private List<Transform> cartParts = new List<Transform>();
    private List<Vector3> initialPartLocalOffsets = new List<Vector3>();
    private List<Quaternion> initialPartLocalRotations = new List<Quaternion>();

    // Obstacle multi-part synchronization (keeps housing, face, base, lights, stripes together)
    private List<Transform> obstacleParts = new List<Transform>();
    private List<Vector3> obstacleBaseLocalPos = new List<Vector3>();

    // Track boundary limits along local X axis in Station1 coordinates:
    // Cart starts at +1.70f; forward toward obstacle decreases X toward -4.60f and -6.80f.
    private float startTrackX = 1.70f;
    private float obstacleTrackX = -4.60f;
    private float fallTrackX = -6.80f;
    private float endTrackX = -7.20f;
    private float floorZ = -0.05f; // Lab floor rest level in local Z

    // Friction slider bounds along local X
    private float sliderMinX = 4.10f;
    private float sliderMaxX = 5.20f;

    // Internal State
    private Vector3 initialCartLocalPos;
    private Vector3 initialBallLocalPos;
    private Quaternion initialBallLocalRot;

    private Rigidbody ballRigidbody;
    private SphereCollider ballCollider;
    private AudioSource audioSource;
    private Coroutine frictionKnobCoroutine = null;
    private Coroutine obstacleCoroutine = null;

    private float cartLocalX = 1.70f;
    private float cartLocalZ = 1.57f;
    private float verticalFallVelocity = 0f;
    private float pitchAngle = 0f;

    // Grabbing state
    private bool isCartGrabbed = false;
    private Transform grabbingHand = null;
    private float lastHandLocalX = 0f;
    private float grabVelocity = 0f;

    void Awake()
    {
        if (launchSpeed < 5.0f) launchSpeed = 5.5f;
        InitializeSubObjects();
        SetupAudio();
        SetupPhysicsComponents();
        AdjustObstructingPanels();
    }

    void Start()
    {
        if (launchSpeed < 5.0f) launchSpeed = 5.5f;
        ResetExperiment();
    }

    /// <summary>
    /// Repositions Live Data Panel to the side console and callouts behind track
    /// so the entire track, cart, ball, and obstacle are 100% visible throughout the run.
    /// </summary>
    public void AdjustObstructingPanels()
    {
        Transform[] allChildren = GetComponentsInChildren<Transform>(true);
        foreach (Transform t in allChildren)
        {
            string n = t.name;

            // Shift Live Data Panel to the side (X = -7.85m on right wing console)
            if (n.Contains("Live Data Panel"))
            {
                if (t.localPosition.x > -4.0f)
                {
                    Vector3 p = t.localPosition;
                    p.x -= 5.45f;
                    t.localPosition = p;
                }
            }

            // Move callouts behind the track in depth
            if (n.Contains("Track Callout") || n.Contains("Obstacle Callout") || n.Contains("Cart Callout"))
            {
                if (t.localPosition.z < 2.0f)
                {
                    Vector3 p = t.localPosition;
                    p.z += 1.8f;
                    t.localPosition = p;
                }
            }
        }
    }

    private void InitializeSubObjects()
    {
        Transform[] allChildren = GetComponentsInChildren<Transform>(true);

        List<Transform> wheelList = new List<Transform>();
        cartParts.Clear();
        initialPartLocalOffsets.Clear();
        initialPartLocalRotations.Clear();
        obstacleParts.Clear();
        obstacleBaseLocalPos.Clear();

        foreach (Transform t in allChildren)
        {
            string n = t.gameObject.name;

            if (cartRoot == null && (n.Contains("Cart Chassis") || n.Contains("GrabHandle") || n == "Cart" || n.Contains("FirstLawCart")))
            {
                cartRoot = t;
            }
            else if (inertiaBall == null && (n.Contains("Inertia Ball") || n == "Ball"))
            {
                inertiaBall = t;
            }
            else if (ballHolderRing == null && n.Contains("Ball Holder Ring"))
            {
                ballHolderRing = t;
            }
            else if (obstacleRoot == null && (n.Contains("Obstacle Face") || n.Contains("Obstacle Housing")))
            {
                obstacleRoot = t;
            }
            else if (resetButton == null && n.Contains("RESET Button") && !n.Contains("Base") && !n.Contains("Label"))
            {
                resetButton = t;
            }
            else if (launchStopButton == null && (n.Contains("STOP Button") || n.Contains("StartButton") || n.Contains("CtrlBtn")) && !n.Contains("Base") && !n.Contains("Label"))
            {
                launchStopButton = t;
            }
            else if (obstacleButton == null && n.Contains("OBSTACLE Button") && !n.Contains("Base") && !n.Contains("Label"))
            {
                obstacleButton = t;
            }
            else if (frictionConsole == null && (n == "Friction Console" || n.Contains("Friction Console")) && !n.Contains("Border") && !n.Contains("Title") && !n.Contains("Text"))
            {
                frictionConsole = t;
            }
            else if (frictionSliderKnob == null && (n.Contains("Friction Slider Knob") || n.Contains("FrictionKnob") || n.Contains("Knob")))
            {
                frictionSliderKnob = t;
            }
            else if (sliderTrack == null && (n.Contains("Slider Track") || n.Contains("FrictionRail") || n.Contains("Rail")))
            {
                sliderTrack = t;
            }

            // Identify all parts that visually belong to the cart
            if (n.Contains("Cart Chassis") || n.Contains("Cart Top Plate") || n.Contains("Cart Cyan Side Light") ||
                n.Contains("Cart Wheel") || n.Contains("Wheel Hub") || n.Contains("Ball Holder Ring") ||
                n.Contains("FirstLawCart") || n.Contains("GrabHandle"))
            {
                if (!cartParts.Contains(t))
                {
                    cartParts.Add(t);
                }
            }

            if (n.Contains("Cart Wheel") || n.Contains("Trolley_W"))
            {
                wheelList.Add(t);
            }

            // Gather all obstacle parts (housing, face, base, warning lights, hazard stripes)
            if ((n.Contains("Obstacle") || n.Contains("Stopper") || n.Contains("Hazard") || n.Contains("Warning Light"))
                && !n.Contains("Button") && !n.Contains("Label") && !n.Contains("Text") && !n.Contains("Callout"))
            {
                if (!obstacleParts.Contains(t))
                {
                    obstacleParts.Add(t);
                    obstacleBaseLocalPos.Add(t.localPosition);
                }
            }
        }

        if (obstacleRoot == null && obstacleParts.Count > 0)
        {
            obstacleRoot = obstacleParts[0];
        }

        if (wheels == null || wheels.Length == 0)
        {
            wheels = wheelList.ToArray();
        }

        // Cache initial local positions and rotations
        if (cartRoot != null)
        {
            initialCartLocalPos = cartRoot.localPosition;
            startTrackX = initialCartLocalPos.x; // +1.70f
            cartLocalX = startTrackX;
            cartLocalZ = initialCartLocalPos.z; // +1.57f
            floorZ = -0.05f;
        }
        else
        {
            initialCartLocalPos = new Vector3(1.70f, -0.25f, 1.57f);
            startTrackX = 1.70f;
            cartLocalX = 1.70f;
            cartLocalZ = 1.57f;
            floorZ = -0.05f;
        }

        foreach (Transform part in cartParts)
        {
            initialPartLocalOffsets.Add(part.localPosition);
            initialPartLocalRotations.Add(part.localRotation);
        }

        if (inertiaBall != null)
        {
            initialBallLocalPos = inertiaBall.localPosition;
            initialBallLocalRot = inertiaBall.localRotation;
        }
        else
        {
            initialBallLocalPos = new Vector3(1.70f, -0.25f, 2.13f);
            initialBallLocalRot = Quaternion.identity;
        }

        // Obstacle contact position along X (decreasing X is forward)
        Transform obstacleFace = null;
        foreach (Transform part in obstacleParts)
        {
            if (part.name.Contains("Obstacle Face") || part.name.Contains("Obstacle Housing"))
            {
                obstacleFace = part;
                break;
            }
        }

        if (obstacleFace != null)
        {
            // Face is at -5.35f; bumper contact is at -4.60f
            obstacleTrackX = obstacleFace.localPosition.x + 0.75f;
            endTrackX = obstacleFace.localPosition.x - 1.85f;
        }
        else
        {
            obstacleTrackX = -4.60f;
            endTrackX = -7.20f;
        }
        fallTrackX = -6.80f; // Track deck overflow point

        // Slider boundaries along local X
        if (frictionSliderKnob != null)
        {
            sliderMinX = 4.10f;
            sliderMaxX = 5.20f;
        }

        SetupDisplayPanels();

        // Add interactive colliders to buttons and controls
        AddButtonCollider(resetButton, "Reset", new Vector3(0.65f, 0.65f, 0.65f));
        AddButtonCollider(launchStopButton, "LaunchStop", new Vector3(0.65f, 0.65f, 0.65f));
        AddButtonCollider(obstacleButton, "Obstacle", new Vector3(0.65f, 0.65f, 0.65f));

        // Broad colliders on entire Friction Console unit
        if (frictionConsole != null)
        {
            BoxCollider fcBc = frictionConsole.GetComponent<BoxCollider>();
            if (fcBc == null) fcBc = frictionConsole.gameObject.AddComponent<BoxCollider>();
            fcBc.size = new Vector3(2.6f, 1.8f, 0.8f);
            fcBc.center = Vector3.zero;
            if (!frictionConsole.gameObject.name.Contains("FrictionConsole"))
                frictionConsole.gameObject.name = $"{frictionConsole.gameObject.name}_FrictionConsole";
        }

        if (frictionSliderKnob != null)
        {
            BoxCollider knobCol = frictionSliderKnob.GetComponent<BoxCollider>();
            if (knobCol == null) knobCol = frictionSliderKnob.gameObject.AddComponent<BoxCollider>();
            knobCol.size = new Vector3(0.9f, 0.9f, 0.9f);
            if (!frictionSliderKnob.gameObject.name.Contains("FrictionKnob"))
                frictionSliderKnob.gameObject.name = $"{frictionSliderKnob.gameObject.name}_FrictionKnob";
        }

        foreach (Transform t in allChildren)
        {
            string n = t.gameObject.name;
            if (n.Contains("Friction Console_") || n.Contains("Slider Track") || n.Contains("FrictionRail"))
            {
                BoxCollider partCol = t.GetComponent<BoxCollider>();
                if (partCol == null) partCol = t.gameObject.AddComponent<BoxCollider>();
                partCol.size = new Vector3(1.2f, 0.6f, 0.4f);
                if (!t.gameObject.name.Contains("FrictionPart"))
                    t.gameObject.name = $"{t.gameObject.name}_FrictionPart";
            }
        }

        // Add grab collider to Cart
        SetupCartGrabCollider();
    }

    private void SetupCartGrabCollider()
    {
        if (cartRoot == null) return;
        BoxCollider bc = cartRoot.GetComponent<BoxCollider>();
        if (bc == null)
        {
            bc = cartRoot.gameObject.AddComponent<BoxCollider>();
        }
        bc.size = new Vector3(1.6f, 0.9f, 1.2f);
        bc.center = new Vector3(0f, 0.25f, 0f);
        if (!cartRoot.gameObject.name.Contains("FirstLawCart_GrabHandle"))
            cartRoot.gameObject.name = "FirstLawCart_GrabHandle";
    }

    private void AddButtonCollider(Transform btn, string btnTag, Vector3 size)
    {
        if (btn == null) return;
        BoxCollider bc = btn.GetComponent<BoxCollider>();
        if (bc == null)
        {
            bc = btn.gameObject.AddComponent<BoxCollider>();
        }
        bc.size = size;
        if (!btn.gameObject.name.Contains(btnTag))
            btn.gameObject.name = $"{btn.gameObject.name}_{btnTag}";
    }

    private void SetupPhysicsComponents()
    {
        if (inertiaBall != null)
        {
            ballCollider = inertiaBall.GetComponent<SphereCollider>();
            if (ballCollider == null)
            {
                ballCollider = inertiaBall.gameObject.AddComponent<SphereCollider>();
            }
            ballCollider.radius = 0.28f;
            ballCollider.isTrigger = true; // Trigger prevents physics collision impulses from pushing ball backward

            ballRigidbody = inertiaBall.GetComponent<Rigidbody>();
            if (ballRigidbody == null)
            {
                ballRigidbody = inertiaBall.gameObject.AddComponent<Rigidbody>();
            }

            ballRigidbody.mass = ballMass;
            ballRigidbody.isKinematic = true; // Firmly synchronized with cart
            ballRigidbody.detectCollisions = false;
        }
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
    }

    private void SetupDisplayPanels()
    {
        // 1. Live Data Panel Text
        Transform liveDataPanel = null;
        Transform[] allChildren = GetComponentsInChildren<Transform>(true);
        foreach (Transform t in allChildren)
        {
            if (t.name.Contains("Live Data Panel") && !t.name.Contains("Border") && !t.name.Contains("Text") && !t.name.Contains("Underline"))
            {
                liveDataPanel = t;
                break;
            }
        }

        if (liveDataPanel != null)
        {
            Transform textGo = liveDataPanel.Find("LiveSensorDataText");
            if (textGo == null)
            {
                GameObject newTextObj = new GameObject("LiveSensorDataText");
                newTextObj.transform.SetParent(liveDataPanel, false);
                newTextObj.transform.localPosition = new Vector3(0f, 0.05f, -0.06f);
                newTextObj.transform.localRotation = Quaternion.identity;
                newTextObj.transform.localScale = Vector3.one * 0.038f;

                dataDisplayTextMesh = newTextObj.AddComponent<TextMesh>();
                dataDisplayTextMesh.fontSize = 24;
                dataDisplayTextMesh.characterSize = 0.25f;
                dataDisplayTextMesh.alignment = TextAlignment.Left;
                dataDisplayTextMesh.anchor = TextAnchor.MiddleCenter;
                dataDisplayTextMesh.color = new Color(0.1f, 0.85f, 1.0f);
            }
            else
            {
                dataDisplayTextMesh = textGo.GetComponent<TextMesh>();
            }
        }

        // 2. Floating Status Text above Track
        Transform statusGo = transform.Find("FloatingStatusText");
        if (statusGo == null)
        {
            GameObject statusObj = new GameObject("FloatingStatusText");
            statusObj.transform.SetParent(transform, false);
            statusObj.transform.localPosition = new Vector3(0f, 3.2f, -1.8f);
            statusObj.transform.localScale = Vector3.one * 0.04f;

            mainStatusTextMesh = statusObj.AddComponent<TextMesh>();
            mainStatusTextMesh.fontSize = 28;
            mainStatusTextMesh.characterSize = 0.25f;
            mainStatusTextMesh.fontStyle = FontStyle.Bold;
            mainStatusTextMesh.alignment = TextAlignment.Center;
            mainStatusTextMesh.anchor = TextAnchor.MiddleCenter;
            mainStatusTextMesh.color = Color.white;
        }
        else
        {
            mainStatusTextMesh = statusGo.GetComponent<TextMesh>();
        }

        // 3. Friction Console Readout Text
        if (frictionConsole != null)
        {
            Transform fTextGo = frictionConsole.Find("LiveFrictionText_FrictionConsole");
            if (fTextGo == null)
            {
                fTextGo = frictionConsole.Find("LiveFrictionText");
            }

            if (fTextGo == null)
            {
                GameObject fTextObj = new GameObject("LiveFrictionText_FrictionConsole");
                fTextObj.transform.SetParent(frictionConsole, false);
                fTextObj.transform.localPosition = new Vector3(0f, 0.38f, -0.12f);
                fTextObj.transform.localRotation = Quaternion.identity;
                fTextObj.transform.localScale = Vector3.one * 0.045f;

                frictionDisplayTextMesh = fTextObj.AddComponent<TextMesh>();
                frictionDisplayTextMesh.fontSize = 26;
                frictionDisplayTextMesh.characterSize = 0.22f;
                frictionDisplayTextMesh.alignment = TextAlignment.Center;
                frictionDisplayTextMesh.anchor = TextAnchor.MiddleCenter;
                frictionDisplayTextMesh.color = new Color(0.95f, 0.85f, 0.35f);
                frictionDisplayTextMesh.fontStyle = FontStyle.Bold;

                BoxCollider tc = fTextObj.AddComponent<BoxCollider>();
                tc.size = new Vector3(2.4f, 1.2f, 0.4f);
            }
            else
            {
                frictionDisplayTextMesh = fTextGo.GetComponent<TextMesh>();
            }
        }
    }

    void Update()
    {
        HandleKeyboardShortcuts();

        if (isCartGrabbed && grabbingHand != null)
        {
            UpdateGrabMovement();
        }
        else
        {
            UpdateExperimentPhysics();
        }

        UpdateDisplayReadout();
        OrientDisplaysToPlayer();
    }

    private void OrientDisplaysToPlayer()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 camPos = cam.transform.position;

        if (frictionDisplayTextMesh != null)
        {
            Vector3 toCam = frictionDisplayTextMesh.transform.position - camPos;
            if (toCam.sqrMagnitude > 0.01f)
            {
                frictionDisplayTextMesh.transform.rotation = Quaternion.LookRotation(toCam);
            }
        }

        if (mainStatusTextMesh != null)
        {
            Vector3 toCam = mainStatusTextMesh.transform.position - camPos;
            if (toCam.sqrMagnitude > 0.01f)
            {
                mainStatusTextMesh.transform.rotation = Quaternion.LookRotation(toCam);
            }
        }

        if (dataDisplayTextMesh != null)
        {
            Vector3 toCam = dataDisplayTextMesh.transform.position - camPos;
            if (toCam.sqrMagnitude > 0.01f)
            {
                dataDisplayTextMesh.transform.rotation = Quaternion.LookRotation(toCam);
            }
        }
    }

    private void HandleKeyboardShortcuts()
    {
        Camera cam = Camera.main;
        if (cam != null && Vector3.Distance(transform.position, cam.transform.position) > 28f) return;

        if (NewtonInput.GetKeyDown(KeyCode.Alpha1) || NewtonInput.GetKeyDown(KeyCode.Space))
        {
            ToggleLaunchStop();
        }
        if (NewtonInput.GetKeyDown(KeyCode.Alpha2) || NewtonInput.GetKeyDown(KeyCode.O))
        {
            ToggleObstacle();
        }
        if (NewtonInput.GetKeyDown(KeyCode.Alpha3) || NewtonInput.GetKeyDown(KeyCode.R))
        {
            ResetExperiment();
        }
        if (NewtonInput.GetKeyDown(KeyCode.Alpha4) || NewtonInput.GetKeyDown(KeyCode.F))
        {
            StepFriction();
        }
        if (NewtonInput.GetKeyDown(KeyCode.UpArrow) || NewtonInput.GetKeyDown(KeyCode.Equals) || NewtonInput.GetKeyDown(KeyCode.Plus))
        {
            IncreaseFriction();
        }
        if (NewtonInput.GetKeyDown(KeyCode.DownArrow) || NewtonInput.GetKeyDown(KeyCode.Minus) || NewtonInput.GetKeyDown(KeyCode.KeypadMinus))
        {
            DecreaseFriction();
        }
    }

    #region Cart Grabbing & Push Mechanics

    public void StartGrabCart(Transform hand)
    {
        if (currentState == ExperimentState.Collided || currentState == ExperimentState.Stopped || currentState == ExperimentState.Falling)
        {
            ResetExperiment();
        }

        isCartGrabbed = true;
        grabbingHand = hand;
        currentState = ExperimentState.Grabbed;
        currentVelocity = 0f;
        currentAcceleration = 0f;
        netForce = 0f;

        Vector3 handLocal = transform.InverseTransformPoint(hand.position);
        lastHandLocalX = handLocal.x;
        grabVelocity = 0f;

        PlayBeep(520f, 0.05f);
    }

    public void UpdateGrabCart(Transform hand)
    {
        if (hand != null) grabbingHand = hand;
        UpdateGrabMovement();
    }

    private void UpdateGrabMovement()
    {
        if (grabbingHand == null) return;

        Vector3 handLocal = transform.InverseTransformPoint(grabbingHand.position);
        float deltaX = handLocal.x - lastHandLocalX;
        float dt = Mathf.Max(Time.deltaTime > 0.0001f ? Time.deltaTime : 0.02f, 0.001f);

        // Forward motion toward obstacle is in the -X direction
        float forwardVelocity = -deltaX / dt;
        grabVelocity = Mathf.Lerp(grabVelocity, forwardVelocity, 0.35f);
        lastHandLocalX = handLocal.x;

        // Ensure movement is strictly forward along the track toward the obstacle (-X)
        float minAllowedX = isObstacleRaised ? obstacleTrackX : fallTrackX;
        float forwardDelta = Mathf.Max(0f, -deltaX);
        float targetX = Mathf.Clamp(cartLocalX - forwardDelta, minAllowedX, startTrackX);
        float actualDisplacement = cartLocalX - targetX;

        cartLocalX = targetX;
        ApplyCartTransform(cartLocalX, initialCartLocalPos.z, 0f);

        // Animate wheels while dragging
        if (wheels != null && actualDisplacement > 0.0001f)
        {
            float wheelCircumference = 2f * Mathf.PI * 0.15f;
            float rotDeg = (actualDisplacement / wheelCircumference) * 360f;
            foreach (Transform w in wheels)
            {
                if (w != null) w.Rotate(Vector3.right, rotDeg, Space.Self);
            }
        }
    }

    public void ReleaseCart()
    {
        if (!isCartGrabbed) return;
        isCartGrabbed = false;
        grabbingHand = null;

        if (Mathf.Abs(cartLocalX - startTrackX) < 0.35f || grabVelocity <= 0.35f)
        {
            cartLocalX = startTrackX;
            ApplyCartTransform(cartLocalX, initialCartLocalPos.z, 0f);
            currentVelocity = 0f;
            currentAcceleration = 0f;
            netForce = 0f;
            currentState = ExperimentState.Ready;
            return;
        }

        if (grabVelocity > 0.35f)
        {
            currentVelocity = Mathf.Clamp(grabVelocity, 1.5f, 6.0f);
            currentState = ExperimentState.Cruising;
            PlayBeep(660f, 0.08f);
        }
        else
        {
            currentVelocity = 0f;
            currentState = ExperimentState.Ready;
        }
    }

    #endregion

    private void UpdateExperimentPhysics()
    {
        float dt = Time.deltaTime > 0.0001f ? Time.deltaTime : 0.02f;

        switch (currentState)
        {
            case ExperimentState.Ready:
            case ExperimentState.Grabbed:
                currentVelocity = 0.0f;
                currentAcceleration = 0.0f;
                netForce = 0.0f;
                break;

            case ExperimentState.Launching:
                // Accelerate strictly forward along track toward obstacle (-X direction)
                currentAcceleration = 18.0f;
                currentVelocity = Mathf.Max(currentVelocity + currentAcceleration * dt, 0.2f);
                netForce = (cartMass + ballMass) * currentAcceleration;

                if (currentVelocity >= launchSpeed)
                {
                    currentVelocity = launchSpeed;
                    currentState = ExperimentState.Cruising;
                }

                // Forward motion is in the -X direction
                cartLocalX -= currentVelocity * dt;
                ApplyCartTransform(cartLocalX, initialCartLocalPos.z, 0f);
                break;

            case ExperimentState.Cruising:
                // Newton's 1st Law physics
                if (frictionCoefficient <= 0.0001f)
                {
                    currentAcceleration = 0.0f;
                    netForce = 0.0f;
                    // Velocity remains completely constant (uniform rectilinear motion)
                }
                else
                {
                    float g = 9.81f;
                    currentAcceleration = -frictionCoefficient * g;
                    netForce = (cartMass + ballMass) * currentAcceleration;
                    currentVelocity += currentAcceleration * dt;

                    // If friction decelerates cart to a halt
                    if (currentVelocity <= 0.05f)
                    {
                        currentVelocity = 0.0f;
                        currentAcceleration = 0.0f;
                        netForce = 0.0f;
                        currentState = ExperimentState.Stopped;
                        PlayBeep(330f, 0.15f);
                        NarrationManager.Instance?.NotifyExperimentCompleted(1);
                        Debug.Log($"[Newton VR Lab 1] Cart stopped along track due to friction (μ = {frictionCoefficient:F2}, X = {cartLocalX:F2}m).");
                        break;
                    }
                }

                // Forward motion along track toward obstacle (-X direction)
                cartLocalX -= currentVelocity * dt;
                ApplyCartTransform(cartLocalX, cartLocalZ, 0f);

                // Outcome A: Obstacle is raised -> cart and ball reach it and stop together
                if (isObstacleRaised && cartLocalX <= obstacleTrackX)
                {
                    TriggerObstacleStop();
                }
                // Outcome B: Obstacle is down -> continue to end of track and fall together
                else if (!isObstacleRaised && cartLocalX <= fallTrackX)
                {
                    StartFallingOffTrack();
                }
                break;

            case ExperimentState.Falling:
                // Cart and ball continue forward (-X) while accelerating downward in Z under gravity
                verticalFallVelocity -= 9.81f * dt;
                cartLocalZ += verticalFallVelocity * dt;
                cartLocalX -= currentVelocity * dt;

                // Subtle forward pitch roll tilting down off track edge
                pitchAngle = Mathf.Clamp(pitchAngle + 75f * dt, 0f, 25f);
                ApplyCartTransform(cartLocalX, cartLocalZ, pitchAngle);

                // Land on visible floor below track end
                if (cartLocalZ <= floorZ)
                {
                    cartLocalZ = floorZ;
                    verticalFallVelocity = 0f;
                    currentVelocity = 0f;
                    currentAcceleration = 0f;
                    netForce = 0f;
                    pitchAngle = 8f; // Rest slightly tilted on floor
                    currentState = ExperimentState.Stopped;
                    ApplyCartTransform(cartLocalX, cartLocalZ, pitchAngle);
                    PlayBeep(330f, 0.20f);
                    Debug.Log($"[Newton VR Lab 1] Cart and ball settled visibly on floor below track (X={cartLocalX:F2}, Z={cartLocalZ:F2}).");
                }
                break;

            case ExperimentState.Collided:
            case ExperimentState.Stopped:
                currentVelocity = 0.0f;
                currentAcceleration = 0.0f;
                netForce = 0.0f;
                break;
        }

        // Animate wheels rolling during simulation
        if (wheels != null && wheels.Length > 0 && currentVelocity > 0.01f && currentState != ExperimentState.Falling)
        {
            float wheelCircumference = 2f * Mathf.PI * 0.15f;
            float rotationDegrees = (currentVelocity * dt / wheelCircumference) * 360f;
            foreach (Transform w in wheels)
            {
                if (w != null)
                {
                    w.Rotate(Vector3.right, rotationDegrees, Space.Self);
                }
            }
        }
    }

    /// <summary>
    /// Synchronizes all cart parts, wheels, chassis, and the ball in lockstep.
    /// The ball remains securely on the cart and shares its exact position and pitch.
    /// It is mathematically and physically impossible for the ball to ever move backward relative to the cart.
    /// </summary>
    private void ApplyCartTransform(float x, float z, float pitch)
    {
        cartLocalX = x;
        cartLocalZ = z;
        pitchAngle = pitch;

        Quaternion pitchRot = Quaternion.Euler(0f, -pitch, 0f);
        Vector3 currentCenter = new Vector3(cartLocalX, initialCartLocalPos.y, cartLocalZ);

        for (int i = 0; i < cartParts.Count; i++)
        {
            if (cartParts[i] != null && i < initialPartLocalOffsets.Count && i < initialPartLocalRotations.Count)
            {
                Vector3 relativeOffset = initialPartLocalOffsets[i] - initialCartLocalPos;
                Vector3 rotatedOffset = pitchRot * relativeOffset;
                cartParts[i].localPosition = currentCenter + rotatedOffset;
                cartParts[i].localRotation = pitchRot * initialPartLocalRotations[i];
            }
        }

        if (cartRoot != null)
        {
            cartRoot.localPosition = currentCenter;
            cartRoot.localRotation = pitchRot;
        }

        // Keep ball visibly on the cart at all times (never detaches, never moves backward relative to cart)
        if (inertiaBall != null)
        {
            Vector3 ballOffset = initialBallLocalPos - initialCartLocalPos;
            Vector3 rotatedBallOffset = pitchRot * ballOffset;
            inertiaBall.localPosition = currentCenter + rotatedBallOffset;
            inertiaBall.localRotation = pitchRot * initialBallLocalRot;
        }
    }

    /// <summary>
    /// Outcome A: Cart and ball reach obstacle and stop there together.
    /// </summary>
    private void TriggerObstacleStop()
    {
        currentState = ExperimentState.Collided;
        currentVelocity = 0.0f;
        currentAcceleration = 0.0f;
        netForce = 0.0f;

        cartLocalX = obstacleTrackX;
        ApplyCartTransform(cartLocalX, initialCartLocalPos.z, 0f);

        PlayBeep(440f, 0.18f);
        NarrationManager.Instance?.NotifyExperimentCompleted(1);
        Debug.Log("[Newton VR Lab 1] Cart and ball reached raised obstacle and stopped together.");
    }

    /// <summary>
    /// Outcome B: Cart and ball continue past lowered obstacle and fall off track together into space below.
    /// </summary>
    private void StartFallingOffTrack()
    {
        currentState = ExperimentState.Falling;
        verticalFallVelocity = 0f;
        pitchAngle = 0f;
        PlayBeep(580f, 0.12f);
        NarrationManager.Instance?.NotifyExperimentCompleted(1);
        Debug.Log("[Newton VR Lab 1] Cart and ball rolling off track end together into visible space below.");
    }

    public void ToggleLaunchStop()
    {
        if (currentState == ExperimentState.Ready || currentState == ExperimentState.Stopped || currentState == ExperimentState.Collided)
        {
            if (currentState == ExperimentState.Collided || currentState == ExperimentState.Stopped)
            {
                ResetExperiment();
            }
            currentState = ExperimentState.Launching;
            PlayBeep(660f, 0.1f);
            AnimateButtonClick(launchStopButton);
        }
        else if (currentState == ExperimentState.Cruising || currentState == ExperimentState.Launching || currentState == ExperimentState.Grabbed)
        {
            StopExperiment();
            AnimateButtonClick(launchStopButton);
        }
    }

    public void StopExperiment()
    {
        isCartGrabbed = false;
        grabbingHand = null;
        currentState = ExperimentState.Stopped;
        currentVelocity = 0f;
        currentAcceleration = 0f;
        netForce = 0f;
        verticalFallVelocity = 0f;
        PlayBeep(330f, 0.15f);
        Debug.Log("[Newton VR Lab 1] Stopped motion safely.");
    }

    public void ToggleObstacle()
    {
        isObstacleRaised = !isObstacleRaised;
        if (obstacleCoroutine != null) StopCoroutine(obstacleCoroutine);
        obstacleCoroutine = StartCoroutine(AnimateObstacle(isObstacleRaised));
        AnimateButtonClick(obstacleButton);
        PlayBeep(isObstacleRaised ? 880f : 550f, 0.08f);
        UpdateDisplayReadout();
        Debug.Log($"[Newton VR Lab 1] Obstacle toggled: {(isObstacleRaised ? "RAISED (Cart & ball will stop at obstacle)" : "DOWN (Cart & ball will fall together off track end)")}");
    }

    private IEnumerator AnimateObstacle(bool raise)
    {
        if (obstacleParts.Count == 0) yield break;

        float dur = 0.35f;
        float elapsed = 0f;

        List<Vector3> startPositions = new List<Vector3>();
        List<Vector3> targetPositions = new List<Vector3>();

        for (int i = 0; i < obstacleParts.Count; i++)
        {
            if (obstacleParts[i] != null && i < obstacleBaseLocalPos.Count)
            {
                startPositions.Add(obstacleParts[i].localPosition);
                Vector3 basePos = obstacleBaseLocalPos[i];
                // In Station1 local coordinates, Z is the vertical axis.
                // Lowering by 0.85m in Z retracts the obstacle below track surface (track surface Z = 1.22m)
                Vector3 target = raise ? basePos : new Vector3(basePos.x, basePos.y, basePos.z - 0.85f);
                targetPositions.Add(target);
            }
        }

        while (elapsed < dur)
        {
            elapsed += Time.deltaTime > 0.0001f ? Time.deltaTime : 0.02f;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / dur);
            for (int i = 0; i < obstacleParts.Count; i++)
            {
                if (obstacleParts[i] != null && i < startPositions.Count && i < targetPositions.Count)
                {
                    obstacleParts[i].localPosition = Vector3.Lerp(startPositions[i], targetPositions[i], t);
                }
            }
            yield return null;
        }

        for (int i = 0; i < obstacleParts.Count; i++)
        {
            if (obstacleParts[i] != null && i < targetPositions.Count)
            {
                obstacleParts[i].localPosition = targetPositions[i];
            }
        }
    }

    public void IncreaseFriction()
    {
        SetFriction(Mathf.Clamp(frictionCoefficient + 0.06f, 0.0f, 0.40f));
    }

    public void DecreaseFriction()
    {
        SetFriction(Mathf.Clamp(frictionCoefficient - 0.06f, 0.0f, 0.40f));
    }

    public void SetFriction(float mu)
    {
        frictionCoefficient = Mathf.Clamp(mu, 0.0f, 0.40f);
        UpdateFrictionKnobVisual(true);
        UpdateDisplayReadout();
        PlayBeep(520f + frictionCoefficient * 1200f, 0.10f);
        Debug.Log($"[Newton VR Lab 1] Friction set to μ = {frictionCoefficient:F2}");
    }

    public void StepFriction()
    {
        // Cycle clearly: 0.00 -> 0.06 -> 0.15 -> 0.28 -> 0.40 -> 0.00
        if (frictionCoefficient < 0.03f) SetFriction(0.06f);
        else if (frictionCoefficient < 0.10f) SetFriction(0.15f);
        else if (frictionCoefficient < 0.20f) SetFriction(0.28f);
        else if (frictionCoefficient < 0.35f) SetFriction(0.40f);
        else SetFriction(0.0f);
    }

    private void UpdateFrictionKnobVisual(bool animate = false)
    {
        if (frictionSliderKnob != null)
        {
            float targetX = Mathf.Lerp(sliderMinX, sliderMaxX, Mathf.Clamp01(frictionCoefficient / 0.40f));
            if (animate && gameObject.activeInHierarchy)
            {
                if (frictionKnobCoroutine != null) StopCoroutine(frictionKnobCoroutine);
                frictionKnobCoroutine = StartCoroutine(AnimateKnobRoutine(targetX));
            }
            else
            {
                Vector3 pos = frictionSliderKnob.localPosition;
                pos.x = targetX;
                frictionSliderKnob.localPosition = pos;
            }
        }
    }

    private IEnumerator AnimateKnobRoutine(float targetX)
    {
        if (frictionSliderKnob == null) yield break;
        Vector3 startPos = frictionSliderKnob.localPosition;
        Vector3 endPos = startPos;
        endPos.x = targetX;
        float elapsed = 0f;
        float dur = 0.20f;

        while (elapsed < dur)
        {
            elapsed += Time.deltaTime > 0.0001f ? Time.deltaTime : 0.02f;
            frictionSliderKnob.localPosition = Vector3.Lerp(startPos, endPos, Mathf.SmoothStep(0f, 1f, elapsed / dur));
            yield return null;
        }
        frictionSliderKnob.localPosition = endPos;
    }

    public void ResetExperiment()
    {
        isCartGrabbed = false;
        grabbingHand = null;
        currentState = ExperimentState.Ready;
        currentVelocity = 0.0f;
        currentAcceleration = 0.0f;
        netForce = 0.0f;
        verticalFallVelocity = 0.0f;
        pitchAngle = 0.0f;
        cartLocalX = startTrackX;
        cartLocalZ = initialCartLocalPos.z;

        if (launchSpeed < 5.0f) launchSpeed = 5.5f;

        // Restore cart and ball to starting position
        ApplyCartTransform(startTrackX, initialCartLocalPos.z, 0f);

        // Restore obstacle to raised starting position if retracted
        if (!isObstacleRaised)
        {
            isObstacleRaised = true;
            if (obstacleCoroutine != null) StopCoroutine(obstacleCoroutine);
            for (int i = 0; i < obstacleParts.Count; i++)
            {
                if (obstacleParts[i] != null && i < obstacleBaseLocalPos.Count)
                {
                    obstacleParts[i].localPosition = obstacleBaseLocalPos[i];
                }
            }
        }

        UpdateFrictionKnobVisual(false);
        UpdateDisplayReadout();
        AnimateButtonClick(resetButton);
        PlayBeep(520f, 0.1f);
        Debug.Log("[Newton VR Lab 1] Experiment reset: Cart, ball, and obstacle restored to starting positions.");
    }

    private void AnimateButtonClick(Transform btn)
    {
        if (btn == null) return;
        StartCoroutine(ButtonClickRoutine(btn));
    }

    private IEnumerator ButtonClickRoutine(Transform btn)
    {
        Vector3 orig = btn.localPosition;
        Vector3 pressed = orig + Vector3.down * 0.06f;
        btn.localPosition = pressed;
        yield return new WaitForSeconds(0.12f);
        btn.localPosition = orig;
    }

    private void PlayBeep(float freq, float duration)
    {
        if (audioSource == null) return;
        AudioClip clip = CreateToneClip(freq, duration);
        audioSource.PlayOneShot(clip, 0.6f);
    }

    private AudioClip CreateToneClip(float frequency, float duration)
    {
        int sampleRate = 22050;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = 1f - (float)i / sampleCount;
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope;
        }
        AudioClip clip = AudioClip.Create("Tone", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void UpdateDisplayReadout()
    {
        string statusText = "";

        switch (currentState)
        {
            case ExperimentState.Ready:
                statusText = "STATUS: READY\nPress LAUNCH (or Space/1) to start. Obstacle: " + (isObstacleRaised ? "RAISED" : "DOWN");
                break;
            case ExperimentState.Grabbed:
                statusText = "<color=#ffff55><b>HAND GRABBED: PUSHING CART</b></color>\nRelease hand to let cart cruise along track";
                break;
            case ExperimentState.Launching:
                statusText = "STATUS: LAUNCHING FORWARD\nAccelerating along track toward obstacle...";
                break;
            case ExperimentState.Cruising:
                if (frictionCoefficient <= 0.001f)
                {
                    statusText = "<color=#55ff88><b>NEWTON'S 1ST LAW: UNIFORM MOTION</b></color>\nNet Force = 0.00 N  =>  Velocity = CONSTANT (" + currentVelocity.ToString("F2") + " m/s)";
                }
                else
                {
                    statusText = "<color=#ffaa33><b>DECELERATING UNDER TRACK FRICTION</b></color>\nExternal friction force: F_f = " + netForce.ToString("F2") + " N (a = " + currentAcceleration.ToString("F2") + " m/s²)";
                }
                break;
            case ExperimentState.Collided:
                statusText = "<color=#ff5555><b>OBSTACLE RAISED: STOPPED TOGETHER</b></color>\nObstacle blocked the track. Cart and ball reached it and stopped together!";
                break;
            case ExperimentState.Falling:
                statusText = "<color=#ffff55><b>OBSTACLE DOWN: FALLING OFF TRACK END</b></color>\nObstacle lowered! Cart and ball reached track end and are falling together into visible space below...";
                break;
            case ExperimentState.Stopped:
                if (cartLocalX <= fallTrackX + 0.2f)
                {
                    statusText = "<color=#55ff88><b>FALL COMPLETE: RESTING ON FLOOR</b></color>\nCart and ball fell together into visible space below. Press RESET to restore.";
                }
                else if (frictionCoefficient > 0.001f && currentVelocity <= 0.05f && cartLocalX > obstacleTrackX)
                {
                    statusText = "<color=#ffaa33><b>STOPPED BY TRACK FRICTION</b></color>\nKinetic friction (μ = " + frictionCoefficient.ToString("F2") + ") brought cart and ball to a halt along track.";
                }
                else
                {
                    statusText = "STATUS: STOPPED\nPress RESET (or R/3) to return cart and ball to start";
                }
                break;
        }

        if (mainStatusTextMesh != null)
        {
            mainStatusTextMesh.text = statusText;
        }

        if (dataDisplayTextMesh != null)
        {
            string modeStr = frictionCoefficient <= 0.001f
                ? "<color=#55ff88>IDEAL (Frictionless: μ = 0.00)</color>"
                : "<color=#ffaa55>TRACK FRICTION (μ = " + frictionCoefficient.ToString("F2") + ")</color>";

            string text =
                "<b>=== REAL-TIME TELEMETRY ===</b>\n" +
                $"• Mode: {modeStr}\n" +
                $"• Velocity: <b>{currentVelocity:F2} m/s</b> " + (currentState == ExperimentState.Cruising && frictionCoefficient <= 0.001f ? "(CONSTANT)" : "") + "\n" +
                $"• Acceleration: <b>{currentAcceleration:F2} m/s²</b>\n" +
                $"• Net Force: <b>{netForce:F2} N</b>\n" +
                $"• Cart & Ball Mass: {(cartMass + ballMass):F1} kg (Moving Together)\n" +
                $"• Obstacle: {(isObstacleRaised ? "<color=#ff5555>RAISED (Cart will stop)</color>" : "<color=#55ff88>DOWN (Cart will fall off track)</color>")}\n" +
                "-----------------------------------------\n" +
                "<b>Controls:</b> Space/1 Launch | O/2 Obstacle | F/4/↑/↓ Friction | R/3 Reset";

            dataDisplayTextMesh.text = text;
        }

        if (frictionDisplayTextMesh != null)
        {
            string fLabel = frictionCoefficient <= 0.001f
                ? "<color=#55ff88><b>μ = 0.00 (Frictionless Track)</b></color>\n<size=17><color=#aaffcc>Net Force = 0 N | Infinite Inertial Glide</color></size>"
                : $"<color=#ffaa33><b>μ = {frictionCoefficient:F2} (Kinetic Friction)</b></color>\n<size=17><color=#ffeebb>Braking a = {-frictionCoefficient * 9.81f:F2} m/s²</color></size>";
            frictionDisplayTextMesh.text = fLabel;
        }
    }

    /// <summary>
    /// Handles physical or pointer clicks from VR Controllers or Desktop Mouse Raycasts.
    /// </summary>
    public void ReceiveInteraction(GameObject targetObj)
    {
        if (targetObj == null) return;
        string n = targetObj.name.ToLower();

        // Clicking on cart, ball, chassis, wheels, or handle NEVER causes movement or triggers actions
        if (n.Contains("cart") || n.Contains("chassis") || n.Contains("ball") || 
            n.Contains("sphere") || n.Contains("wheel") || n.Contains("holder") || 
            n.Contains("trolley") || n.Contains("handle"))
        {
            return;
        }

        Debug.Log($"[Newton VR Lab 1] ReceiveInteraction on: {targetObj.name}");

        if (n.Contains("reset"))
        {
            ResetExperiment();
        }
        else if (n.Contains("stop") || n.Contains("launchstop") || n.Contains("startbutton") || n.Contains("ctrlbtn") || n.Contains("launch"))
        {
            ToggleLaunchStop();
        }
        else if (n.Contains("obstacle") || n.Contains("barricade") || n.Contains("hazard"))
        {
            ToggleObstacle();
        }
        else if (n.Contains("friction") || n.Contains("slider") || n.Contains("knob") || n.Contains("rail") || n.Contains("console") || n.Contains("drag"))
        {
            StepFriction();
        }
    }
}
