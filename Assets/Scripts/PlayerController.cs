using System.Collections.Generic;
using UnityEngine;

// Explicit aliases to eliminate any ambiguity between UnityEngine.XR and UnityEngine.InputSystem
using XRInputDevice = UnityEngine.XR.InputDevice;
using XRInputDevices = UnityEngine.XR.InputDevices;
using XRCommonUsages = UnityEngine.XR.CommonUsages;
using XRNode = UnityEngine.XR.XRNode;
using XRSettings = UnityEngine.XR.XRSettings;
using XRDeviceCharacteristics = UnityEngine.XR.InputDeviceCharacteristics;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Universal Walkthrough Controller with Full Meta Quest 3S Headset & Controller Support + Desktop PC Fallback.
/// Fully resolves camera wobbling, ensures rock-solid VR Origin/Camera tracking,
/// reliable thumbstick locomotion, smooth snap turning, teleportation, and complete controller interaction across all experiments.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public enum GameDrawMode
    {
        Shaded, // Standard lit mode (Default)
        Unlit   // Pure Unlit draw mode
    }

    [Header("Draw Mode (Shading Mode)")]
    [Tooltip("Choose between Shaded mode (standard lit mode) or Unlit mode.")]
    public GameDrawMode drawMode = GameDrawMode.Shaded;

    [Header("VR Rig References")]
    [Tooltip("Parent transform for camera and hands to apply floor height offset.")]
    public Transform cameraOffset;

    [Tooltip("Reference to the player's main camera (HMD Center Eye).")]
    public Camera playerCamera;

    [Tooltip("Left Hand VR Controller object.")]
    public Transform leftHandController;

    [Tooltip("Right Hand VR Controller object.")]
    public Transform rightHandController;

    [Header("Locomotion Settings")]
    [Tooltip("Normal walking speed in m/s.")]
    public float walkSpeed = 4.0f;

    [Tooltip("Sprint speed in m/s.")]
    public float sprintSpeed = 7.0f;

    [Tooltip("Jump height in meters.")]
    public float jumpHeight = 1.0f;

    [Tooltip("Gravity acceleration.")]
    public float gravity = 20.0f;

    [Header("VR Turning")]
    [Tooltip("Enable snap turning with Right Thumbstick in VR (recommended to avoid motion sickness).")]
    public bool enableSnapTurn = true;

    [Tooltip("Angle in degrees for each snap turn flick.")]
    public float snapTurnAngle = 45.0f;

    [Tooltip("Speed for continuous smooth turning if snap turn is disabled.")]
    public float smoothTurnSpeed = 75.0f;

    [Header("Desktop Look Settings")]
    [Tooltip("Mouse look sensitivity on desktop.")]
    public float mouseSensitivity = 2.0f;

    [Tooltip("Maximum vertical look angle.")]
    public float maxPitchAngle = 85.0f;

    [Tooltip("Minimum vertical look angle.")]
    public float minPitchAngle = -85.0f;

    [Header("UI & HUD")]
    [Tooltip("Show on-screen control hints.")]
    public bool showControlsHUD = true;

    // Internal State
    private CharacterController characterController;
    private float verticalVelocity = 0.0f;
    private float cameraPitch = 0.0f;
    private bool cursorLocked = true;
    private bool isVRActive = false;
    public bool IsVRActive => isVRActive;
    private bool snapTurnReset = true;
    private GameDrawMode activeDrawMode = (GameDrawMode)(-1);

    private Shader unlitShader;
    private VRControllerVisual leftVisual;
    private VRControllerVisual rightVisual;
    private bool prevRightAction = false;
    private bool prevLeftAction = false;
    private bool prevRightBState = false;
    private bool prevLeftXState = false;
    private bool prevLeftAltState = false;
    private int currentStationIndex = 0; // 0 = MainLobby, 1 = Lab 1, 2 = Lab 2, 3 = Lab 3
    private int roomSwitchCount = 0;
    private string lastNavEventText = "Simulation Started in MainLobby";
    private float lastNavEventTime = 0f;
    private bool lastRightBInputDetected = false;
    private bool lastLeftXInputDetected = false;
    [Header("Diagnostic Display")]
    public bool showDiagnosticOverlay = false;
    private GameObject vrDiagnosticHudObj = null;
    private TextMesh vrDiagnosticTextMesh = null;
    private GameObject vrPointerReticle = null;

    private NewtonsFirstLawExperiment exp1Cached;
    private NewtonsSecondLawExperiment exp2Cached;
    private NewtonsThirdLawExperiment exp3Cached;
    private static readonly List<XRInputDevice> s_CachedDevices = new List<XRInputDevice>();

    // Unified controller state struct
    public struct ControllerState
    {
        public bool isTracked;
        public Vector3 localPosition;
        public Quaternion localRotation;
        public Vector2 thumbstick;
        public bool thumbstickClick;
        public float trigger;
        public bool triggerDown;
        public float grip;
        public bool gripDown;
        public bool primaryButton;   // Button A on Right Hand, Button X on Left Hand
        public bool secondaryButton; // Button B on Right Hand, Button Y on Left Hand
        public bool menuButton;
    }

    void Awake()
    {
        // Meta Quest 3S VR Performance: decouple from desktop monitor VSync & render at full 90 FPS
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 90;

        characterController = GetComponent<CharacterController>();
        if (characterController != null)
        {
            characterController.height = 1.8f;
            characterController.radius = 0.35f;
            characterController.center = new Vector3(0, 0.9f, 0);
            characterController.stepOffset = 0.35f;
            characterController.slopeLimit = 45f;
            characterController.skinWidth = 0.02f;
            characterController.minMoveDistance = 0f;

            // Ensure CharacterController ignores collision with all colliders inside Player hierarchy
            Collider[] childCols = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < childCols.Length; i++)
            {
                if (childCols[i] != characterController)
                {
                    Physics.IgnoreCollision(characterController, childCols[i], true);
                }
            }
        }

        if (cameraOffset == null)
        {
            cameraOffset = transform.Find("CameraOffset");
        }

        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera == null && Camera.main != null)
            {
                playerCamera = Camera.main;
            }
        }

        if (leftHandController == null && cameraOffset != null)
        {
            leftHandController = cameraOffset.Find("LeftHandController");
        }
        if (rightHandController == null && cameraOffset != null)
        {
            rightHandController = cameraOffset.Find("RightHandController");
        }

        unlitShader = Shader.Find("Hidden/NewtonVR/UnlitReplacement");
        if (unlitShader == null)
        {
            unlitShader = Shader.Find("Unlit/Texture");
        }

        if (leftHandController != null)
        {
            leftVisual = leftHandController.GetComponentInChildren<VRControllerVisual>(true);
        }
        if (rightHandController != null)
        {
            rightVisual = rightHandController.GetComponentInChildren<VRControllerVisual>(true);
        }

        // Procedural VR pointer reticle for 3D depth cue in headset
        vrPointerReticle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        vrPointerReticle.name = "VR_PointerReticle";
        vrPointerReticle.transform.localScale = Vector3.one * 0.035f;
        Collider reticleCol = vrPointerReticle.GetComponent<Collider>();
        if (reticleCol != null)
        {
            if (characterController != null) Physics.IgnoreCollision(characterController, reticleCol, true);
            DestroyImmediate(reticleCol);
        }
        Renderer reticleRen = vrPointerReticle.GetComponent<Renderer>();
        if (reticleRen != null)
        {
            Material rMat = new Material(Shader.Find("Standard"));
            rMat.color = new Color(0.1f, 0.9f, 1.0f);
            rMat.EnableKeyword("_EMISSION");
            rMat.SetColor("_EmissionColor", new Color(0.1f, 0.9f, 1.0f) * 2.0f);
            reticleRen.sharedMaterial = rMat;
        }
        vrPointerReticle.SetActive(false);
    }

    void Start()
    {
        // Initial spawn: Main Lobby directly in front of the blue START LAB sign facing central HoloGlobe
        if (characterController != null) characterController.enabled = false;
        transform.position = new Vector3(0f, 0.0f, 5.80f);
        transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        if (characterController != null) characterController.enabled = true;

        if (cameraOffset != null)
        {
            cameraOffset.localPosition = Vector3.zero;
            cameraOffset.localRotation = Quaternion.identity;
        }

        cameraPitch = 0f;
        if (playerCamera != null)
        {
            playerCamera.transform.localPosition = new Vector3(0f, 1.70f, 0f);
            playerCamera.transform.localRotation = Quaternion.identity;
        }

        ApplyDrawMode(drawMode);
        exp1Cached = Object.FindFirstObjectByType<NewtonsFirstLawExperiment>();
        exp2Cached = Object.FindFirstObjectByType<NewtonsSecondLawExperiment>();
        exp3Cached = Object.FindFirstObjectByType<NewtonsThirdLawExperiment>();

        CheckVRHardware();
        if (!isVRActive)
        {
            SetCursorLock(true);
        }
        else
        {
            SetCursorLock(false);
        }

        SetupVRDiagnosticHUD();
    }

    void OnValidate()
    {
        if (Application.isPlaying && playerCamera != null)
        {
            ApplyDrawMode(drawMode);
        }
    }

    public void ApplyDrawMode(GameDrawMode mode)
    {
        if (playerCamera == null) return;

        drawMode = mode;
        activeDrawMode = mode;

        if (mode == GameDrawMode.Unlit)
        {
            if (unlitShader == null)
            {
                unlitShader = Shader.Find("Hidden/NewtonVR/UnlitReplacement");
                if (unlitShader == null) unlitShader = Shader.Find("Unlit/Texture");
            }

            if (unlitShader != null)
            {
                // Only replace Opaque RenderType so TextMeshPro, UI, and Transparent shaders remain completely intact!
                playerCamera.SetReplacementShader(unlitShader, "RenderType");
                Debug.Log("[Newton VR] Game Draw Mode: UNLIT");
            }
        }
        else
        {
            playerCamera.ResetReplacementShader();
            Debug.Log("[Newton VR] Game Draw Mode: SHADED");
        }
    }

    public void ToggleDrawMode()
    {
        GameDrawMode nextMode = (drawMode == GameDrawMode.Unlit) ? GameDrawMode.Shaded : GameDrawMode.Unlit;
        ApplyDrawMode(nextMode);
    }

    void Update()
    {
        if (drawMode != activeDrawMode)
        {
            ApplyDrawMode(drawMode);
        }

        CheckVRHardware();

        // 1. Controller Tracking, B-Button Instant Room Switch & X-Button Panel Toggle (Always active!)
        UpdateVRTracking();

        // 2. Unified Locomotion & Turning (Left Stick + WASD + Right Stick Snap Turn)
        HandleUnifiedMovement();

        // 3. Interactions with Experiments (Controller Pointer/Gaze Ray + Trigger + Click)
        HandleInteractions();

        // 4. Hotkeys for testing & accessibility
        if (IsKeyJustPressed(KeyCode.T)) TeleportToStationIndex(1); // Lab 1
        if (IsKeyJustPressed(KeyCode.K)) TeleportToStationIndex(2); // Lab 2
        if (IsKeyJustPressed(KeyCode.L)) TeleportToStationIndex(3); // Lab 3
        if (IsKeyJustPressed(KeyCode.M)) TeleportToStationIndex(0); // Main Lobby
        if (IsKeyJustPressed(KeyCode.O) || IsKeyJustPressed(KeyCode.Alpha4)) TeleportToStationIndex(4); // Room 4 Observation
        if (IsKeyJustPressed(KeyCode.B)) CycleNextStationTeleport();
        if (IsKeyJustPressed(KeyCode.N))
        {
            FloatingVRDestinationPanel panel = FindFirstObjectByType<FloatingVRDestinationPanel>();
            if (panel != null) panel.TogglePanel();
        }
        if (IsKeyJustPressed(KeyCode.P)) { if (NarrationManager.Instance != null) NarrationManager.Instance.ReplayCurrentStation(); }
        if (IsKeyJustPressed(KeyCode.U)) ToggleDrawMode();
        if (IsKeyJustPressed(KeyCode.H)) showControlsHUD = !showControlsHUD;

        // Failsafe respawn if fallen below ground
        if (transform.position.y < -20.0f)
        {
            verticalVelocity = 0f;
            if (characterController != null) characterController.enabled = false;
            transform.position = new Vector3(0f, 0.0f, -10.0f);
            if (characterController != null) characterController.enabled = true;
        }
    }

    private void CheckVRHardware()
    {
        if (isVRActive) return; // Keep VR mode latched once detected to prevent mode flip-flopping

        bool vrPresent = XRSettings.isDeviceActive;

        if (!vrPresent && Application.isMobilePlatform)
        {
            vrPresent = true;
        }

        // Modern Unity XR Subsystem detection
        if (!vrPresent)
        {
            var displaySubsystems = new List<UnityEngine.XR.XRDisplaySubsystem>();
            SubsystemManager.GetSubsystems(displaySubsystems);
            for (int i = 0; i < displaySubsystems.Count; i++)
            {
                if (displaySubsystems[i].running)
                {
                    vrPresent = true;
                    break;
                }
            }
        }

        if (!vrPresent)
        {
            s_CachedDevices.Clear();
            XRInputDevices.GetDevicesWithCharacteristics(XRDeviceCharacteristics.HeadMounted, s_CachedDevices);
            if (s_CachedDevices.Count > 0 && s_CachedDevices[0].isValid)
            {
                vrPresent = true;
            }
        }

        if (!vrPresent)
        {
            s_CachedDevices.Clear();
            XRInputDevices.GetDevicesWithCharacteristics(XRDeviceCharacteristics.Controller | XRDeviceCharacteristics.HeldInHand, s_CachedDevices);
            if (s_CachedDevices.Count > 0 && s_CachedDevices[0].isValid)
            {
                vrPresent = true;
            }
        }

#if ENABLE_INPUT_SYSTEM
        if (!vrPresent)
        {
            foreach (var device in InputSystem.devices)
            {
                if (!device.added) continue;
                if (device is UnityEngine.InputSystem.XR.XRHMD || device is UnityEngine.InputSystem.XR.XRController)
                {
                    vrPresent = true;
                    break;
                }
                string devName = device.name.ToLower();
                string devLayout = device.layout.ToLower();
                string devProduct = (device.description.product ?? "").ToLower();
                if (devName.Contains("oculus") || devName.Contains("openxr") || devName.Contains("quest") ||
                    devLayout.Contains("oculus") || devLayout.Contains("openxr") || devLayout.Contains("quest") ||
                    devProduct.Contains("oculus") || devProduct.Contains("quest") || devProduct.Contains("meta"))
                {
                    vrPresent = true;
                    break;
                }
            }
        }
#endif

        if (vrPresent)
        {
            isVRActive = true;
            Debug.Log("[Newton VR] VR Headset & Meta Quest 3S Controllers Active. Desktop mouse rotation strictly disabled.");
            if (leftVisual != null) leftVisual.SetVisibility(true);
            if (rightVisual != null) rightVisual.SetVisibility(true);
            SetCursorLock(false);
        }
    }

    #region Unified Dual-Pipeline Controller Input Polling

    /// <summary>
    /// Reads controller tracking and button/stick inputs from both UnityEngine.XR and UnityEngine.InputSystem.
    /// Guarantees 100% responsiveness on Meta Quest 3S whether using OpenXR, Meta Quest Link, or Android Standalone.
    /// </summary>
    public ControllerState PollController(bool isRightHand)
    {
        ControllerState state = new ControllerState();
        XRNode node = isRightHand ? XRNode.RightHand : XRNode.LeftHand;

        // Pipeline 1: UnityEngine.XR.InputDevices
        XRInputDevice xrDev = XRInputDevices.GetDeviceAtXRNode(node);
        if (xrDev.isValid)
        {
            state.isTracked = true;
            if (xrDev.TryGetFeatureValue(XRCommonUsages.devicePosition, out Vector3 p) && p != Vector3.zero)
                state.localPosition = p;
            if (xrDev.TryGetFeatureValue(XRCommonUsages.deviceRotation, out Quaternion r) && r != Quaternion.identity)
                state.localRotation = r;

            xrDev.TryGetFeatureValue(XRCommonUsages.primary2DAxis, out state.thumbstick);
            xrDev.TryGetFeatureValue(XRCommonUsages.primary2DAxisClick, out state.thumbstickClick);

            xrDev.TryGetFeatureValue(XRCommonUsages.trigger, out state.trigger);
            if (xrDev.TryGetFeatureValue(XRCommonUsages.triggerButton, out bool tb) && tb) state.triggerDown = true;
            if (state.trigger > 0.35f) state.triggerDown = true;

            xrDev.TryGetFeatureValue(XRCommonUsages.grip, out state.grip);
            if (xrDev.TryGetFeatureValue(XRCommonUsages.gripButton, out bool gb) && gb) state.gripDown = true;
            if (state.grip > 0.35f) state.gripDown = true;

            xrDev.TryGetFeatureValue(XRCommonUsages.primaryButton, out state.primaryButton);
            xrDev.TryGetFeatureValue(XRCommonUsages.secondaryButton, out state.secondaryButton);
            xrDev.TryGetFeatureValue(XRCommonUsages.menuButton, out state.menuButton);
        }

#if ENABLE_INPUT_SYSTEM
        // Pipeline 2: UnityEngine.InputSystem.devices
        foreach (var dev in InputSystem.devices)
        {
            if (!dev.added) continue;

            bool matches = false;
            for (int u = 0; u < dev.usages.Count; u++)
            {
                if (isRightHand && dev.usages[u] == UnityEngine.InputSystem.CommonUsages.RightHand) { matches = true; break; }
                if (!isRightHand && dev.usages[u] == UnityEngine.InputSystem.CommonUsages.LeftHand) { matches = true; break; }
            }

            if (!matches)
            {
                string devName = dev.name;
                string devLayout = dev.layout;
                string devProduct = dev.description.product ?? "";
                string target = isRightHand ? "right" : "left";
                string antiTarget = isRightHand ? "left" : "right";

                if ((devName.IndexOf(target, System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                     devLayout.IndexOf(target, System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                     devProduct.IndexOf(target, System.StringComparison.OrdinalIgnoreCase) >= 0) &&
                    (devName.IndexOf(antiTarget, System.StringComparison.OrdinalIgnoreCase) < 0))
                {
                    matches = true;
                }
            }

            if (dev is UnityEngine.InputSystem.XR.XRHMD || dev.layout.IndexOf("HMD", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                matches = false;
            }

            if (matches)
            {
                state.isTracked = true;

                // Position
                var posCtrl = dev.GetChildControl<UnityEngine.InputSystem.Controls.Vector3Control>("devicePosition")
                              ?? dev.GetChildControl<UnityEngine.InputSystem.Controls.Vector3Control>("pointerPosition");
                if (posCtrl != null)
                {
                    Vector3 pv = posCtrl.ReadValue();
                    if (pv != Vector3.zero) state.localPosition = pv;
                }

                // Rotation
                var rotCtrl = dev.GetChildControl<UnityEngine.InputSystem.Controls.QuaternionControl>("deviceRotation")
                              ?? dev.GetChildControl<UnityEngine.InputSystem.Controls.QuaternionControl>("pointerRotation");
                if (rotCtrl != null)
                {
                    Quaternion rv = rotCtrl.ReadValue();
                    if (rv != Quaternion.identity) state.localRotation = rv;
                }

                // Thumbstick
                var stickCtrl = dev.GetChildControl<UnityEngine.InputSystem.Controls.Vector2Control>("thumbstick")
                                ?? dev.GetChildControl<UnityEngine.InputSystem.Controls.Vector2Control>("primary2DAxis")
                                ?? dev.GetChildControl<UnityEngine.InputSystem.Controls.Vector2Control>("joystick");
                if (stickCtrl != null)
                {
                    Vector2 sv = stickCtrl.ReadValue();
                    if (sv.sqrMagnitude > state.thumbstick.sqrMagnitude) state.thumbstick = sv;
                }

                var stickClickCtrl = dev.GetChildControl<UnityEngine.InputSystem.Controls.ButtonControl>("thumbstickClicked")
                                     ?? dev.GetChildControl<UnityEngine.InputSystem.Controls.ButtonControl>("primary2DAxisClick");
                if (stickClickCtrl != null && stickClickCtrl.isPressed) state.thumbstickClick = true;

                // Trigger
                var trigAxis = dev.GetChildControl<UnityEngine.InputSystem.Controls.AxisControl>("trigger");
                if (trigAxis != null)
                {
                    float tv = trigAxis.ReadValue();
                    if (tv > state.trigger) state.trigger = tv;
                }
                var trigBtn = dev.GetChildControl<UnityEngine.InputSystem.Controls.ButtonControl>("triggerPressed")
                              ?? dev.GetChildControl<UnityEngine.InputSystem.Controls.ButtonControl>("triggerButton");
                if (trigBtn != null && trigBtn.isPressed) { state.triggerDown = true; state.trigger = Mathf.Max(state.trigger, 1.0f); }
                if (state.trigger > 0.35f) state.triggerDown = true;

                // Grip
                var gripAxis = dev.GetChildControl<UnityEngine.InputSystem.Controls.AxisControl>("grip");
                if (gripAxis != null)
                {
                    float gv = gripAxis.ReadValue();
                    if (gv > state.grip) state.grip = gv;
                }
                var gripBtn = dev.GetChildControl<UnityEngine.InputSystem.Controls.ButtonControl>("gripPressed")
                              ?? dev.GetChildControl<UnityEngine.InputSystem.Controls.ButtonControl>("gripButton");
                if (gripBtn != null && gripBtn.isPressed) { state.gripDown = true; state.grip = Mathf.Max(state.grip, 1.0f); }
                if (state.grip > 0.35f) state.gripDown = true;

                // Primary Button (A on Right / X on Left)
                var primBtn = dev.GetChildControl<UnityEngine.InputSystem.Controls.ButtonControl>("primaryButton")
                              ?? dev.GetChildControl<UnityEngine.InputSystem.Controls.ButtonControl>("buttonSouth")
                              ?? dev.GetChildControl<UnityEngine.InputSystem.Controls.ButtonControl>(isRightHand ? "aButton" : "xButton");
                if (primBtn != null && primBtn.isPressed) state.primaryButton = true;

                // Secondary Button (B on Right / Y on Left)
                var secBtn = dev.GetChildControl<UnityEngine.InputSystem.Controls.ButtonControl>("secondaryButton")
                             ?? dev.GetChildControl<UnityEngine.InputSystem.Controls.ButtonControl>("buttonEast")
                             ?? dev.GetChildControl<UnityEngine.InputSystem.Controls.ButtonControl>(isRightHand ? "bButton" : "yButton");
                if (secBtn != null && secBtn.isPressed) state.secondaryButton = true;

                // Menu Button
                var menuBtn = dev.GetChildControl<UnityEngine.InputSystem.Controls.ButtonControl>("menuButton")
                              ?? dev.GetChildControl<UnityEngine.InputSystem.Controls.ButtonControl>("menu");
                if (menuBtn != null && menuBtn.isPressed) state.menuButton = true;
            }
        }

        // Pipeline 3: Gamepad.current
        if (Gamepad.current != null)
        {
            if (isRightHand)
            {
                Vector2 rs = Gamepad.current.rightStick.ReadValue();
                if (rs.sqrMagnitude > state.thumbstick.sqrMagnitude) state.thumbstick = rs;

                float rt = Gamepad.current.rightTrigger.ReadValue();
                if (rt > state.trigger) state.trigger = rt;
                if (state.trigger > 0.35f || Gamepad.current.rightShoulder.isPressed) state.triggerDown = true;

                if (Gamepad.current.aButton.isPressed || Gamepad.current.buttonSouth.isPressed) state.primaryButton = true;
                if (Gamepad.current.bButton.isPressed || Gamepad.current.buttonEast.isPressed) state.secondaryButton = true;
            }
            else
            {
                Vector2 ls = Gamepad.current.leftStick.ReadValue();
                if (ls.sqrMagnitude > state.thumbstick.sqrMagnitude) state.thumbstick = ls;

                float lt = Gamepad.current.leftTrigger.ReadValue();
                if (lt > state.trigger) state.trigger = lt;
                if (state.trigger > 0.35f || Gamepad.current.leftShoulder.isPressed) state.triggerDown = true;

                if (Gamepad.current.xButton.isPressed || Gamepad.current.buttonWest.isPressed) state.primaryButton = true;
                if (Gamepad.current.yButton.isPressed || Gamepad.current.buttonNorth.isPressed) state.secondaryButton = true;
                if (Gamepad.current.startButton.isPressed || Gamepad.current.selectButton.isPressed) state.menuButton = true;
            }
        }
#endif

        // Pipeline 4: Legacy Joysticks (UnityEngine.Input)
        try
        {
            if (isRightHand)
            {
                if (Input.GetKey(KeyCode.JoystickButton1) || Input.GetKey(KeyCode.Joystick1Button1) || Input.GetKey(KeyCode.Joystick2Button1))
                    state.secondaryButton = true; // Button B
                if (Input.GetKey(KeyCode.JoystickButton0) || Input.GetKey(KeyCode.Joystick1Button0) || Input.GetKey(KeyCode.Joystick2Button0))
                    state.primaryButton = true;   // Button A
                if (Input.GetKey(KeyCode.JoystickButton5))
                    state.thumbstickClick = true;
            }
            else
            {
                if (Input.GetKey(KeyCode.JoystickButton2) || Input.GetKey(KeyCode.Joystick1Button2) || Input.GetKey(KeyCode.Joystick2Button2))
                    state.primaryButton = true;   // Button X
                if (Input.GetKey(KeyCode.JoystickButton3) || Input.GetKey(KeyCode.Joystick1Button3) || Input.GetKey(KeyCode.Joystick2Button3))
                    state.secondaryButton = true; // Button Y
                if (Input.GetKey(KeyCode.JoystickButton7))
                    state.menuButton = true;
                if (Input.GetKey(KeyCode.JoystickButton4))
                    state.thumbstickClick = true;
            }
        }
        catch { }

        return state;
    }

    #endregion

    #region VR Tracking & Input

    private void UpdateVRTracking()
    {
        // Poll both controllers across all pipelines
        ControllerState leftState = PollController(false);
        ControllerState rightState = PollController(true);

        // Auto-latch VR mode if any controller activity or tracking is active
        if (leftState.isTracked || rightState.isTracked || rightState.triggerDown || leftState.triggerDown ||
            rightState.secondaryButton || leftState.primaryButton || rightState.thumbstick.sqrMagnitude > 0.05f ||
            leftState.thumbstick.sqrMagnitude > 0.05f)
        {
            if (!isVRActive)
            {
                isVRActive = true;
                SetCursorLock(false);
                if (leftVisual != null) leftVisual.SetVisibility(true);
                if (rightVisual != null) rightVisual.SetVisibility(true);
                Debug.Log("[Newton VR] Controller activity detected -> VR Active Mode Engaged!");
            }
        }

        // Update left controller local transform
        if (leftHandController != null)
        {
            if (leftState.isTracked && leftState.localPosition != Vector3.zero)
            {
                leftHandController.localPosition = leftState.localPosition;
                leftHandController.localRotation = leftState.localRotation;
            }
            else if (playerCamera != null)
            {
                leftHandController.position = playerCamera.transform.position - playerCamera.transform.right * 0.22f + playerCamera.transform.forward * 0.40f - playerCamera.transform.up * 0.15f;
                leftHandController.rotation = playerCamera.transform.rotation;
            }
        }

        // Update right controller local transform
        if (rightHandController != null)
        {
            if (rightState.isTracked && rightState.localPosition != Vector3.zero)
            {
                rightHandController.localPosition = rightState.localPosition;
                rightHandController.localRotation = rightState.localRotation;
            }
            else if (playerCamera != null)
            {
                rightHandController.position = playerCamera.transform.position + playerCamera.transform.right * 0.22f + playerCamera.transform.forward * 0.40f - playerCamera.transform.up * 0.15f;
                rightHandController.rotation = playerCamera.transform.rotation;
            }
        }

        // Animate 3D controller visuals
        if (leftVisual != null) leftVisual.UpdateInputVisuals(leftState.thumbstick, leftState.trigger);
        if (rightVisual != null) rightVisual.UpdateInputVisuals(rightState.thumbstick, rightState.trigger);

        // Reparent wrist HUD if left hand controller is available
        if (leftHandController != null && vrDiagnosticHudObj != null && vrDiagnosticHudObj.transform.parent != leftHandController)
        {
            vrDiagnosticHudObj.transform.SetParent(leftHandController, false);
            vrDiagnosticHudObj.transform.localPosition = new Vector3(0.04f, 0.08f, -0.04f);
            vrDiagnosticHudObj.transform.localRotation = Quaternion.Euler(55f, 15f, 0f);
        }

        // =========================================================================
        // 1. Right Controller Button B: INSTANT ROOM SWITCHING (Debounced)
        // Sequence: Main Lobby (0) -> Lab 1 (1) -> Lab 2 (2) -> Lab 3 (3) -> Main Lobby (0)...
        // "one press of b to newtons first law, 2nd press to 2nd law experiment so on"
        // =========================================================================
        bool rightBPressed = rightState.secondaryButton;
        if (!rightBPressed)
        {
            try
            {
                if (Input.GetKey(KeyCode.JoystickButton1) || Input.GetKey(KeyCode.Joystick1Button1) || Input.GetKey(KeyCode.Joystick2Button1) || Input.GetKey(KeyCode.B))
                    rightBPressed = true;
            }
            catch { }
        }

        if (rightBPressed && !prevRightBState)
        {
            CycleNextStationTeleport();
            TriggerHapticFeedback(rightHandController);
        }
        prevRightBState = rightBPressed;
        lastRightBInputDetected = rightBPressed;

        // =========================================================================
        // 2. Left Controller Button X: Toggle Luxury Floating Room Panel (Debounced)
        // =========================================================================
        bool leftXPressed = leftState.primaryButton;
        if (!leftXPressed)
        {
            try
            {
                if (Input.GetKey(KeyCode.JoystickButton2) || Input.GetKey(KeyCode.Joystick1Button2) || Input.GetKey(KeyCode.Joystick2Button2))
                    leftXPressed = true;
            }
            catch { }
        }
        bool leftAltPressed = leftState.secondaryButton || leftState.menuButton;
        if (!leftAltPressed)
        {
            try
            {
                if (Input.GetKey(KeyCode.JoystickButton3) || Input.GetKey(KeyCode.JoystickButton7))
                    leftAltPressed = true;
            }
            catch { }
        }

        if ((leftXPressed && !prevLeftXState) || (leftAltPressed && !prevLeftAltState))
        {
            FloatingVRDestinationPanel panel = FindFirstObjectByType<FloatingVRDestinationPanel>();
            if (panel != null)
            {
                panel.TogglePanel();
                TriggerHapticFeedback(leftHandController);
            }
        }
        prevLeftXState = leftXPressed;
        prevLeftAltState = leftAltPressed;
        lastLeftXInputDetected = leftXPressed || leftAltPressed;

        // Update live in-headset VR Diagnostic HUD
        if (NewtonInput.GetKeyDown(KeyCode.F1))
        {
            showDiagnosticOverlay = !showDiagnosticOverlay;
            if (vrDiagnosticHudObj != null)
                vrDiagnosticHudObj.SetActive(showDiagnosticOverlay);
        }
        UpdateDiagnosticDisplay();
    }

    private void HandleUnifiedMovement()
    {
        if (characterController == null || !characterController.enabled) return;

        ControllerState leftState = PollController(false);
        ControllerState rightState = PollController(true);

        // 1. Locomotion Movement Vector (Left Thumbstick / WASD / Gamepad Left Stick)
        Vector2 moveAxis = leftState.thumbstick;

        // Deadzone check
        if (moveAxis.magnitude < 0.15f) moveAxis = Vector2.zero;

        // Keyboard WASD / Gamepad fallback
        Vector2 kAxis = NewtonInput.GetMoveAxis();
        if (kAxis.sqrMagnitude > moveAxis.sqrMagnitude) moveAxis = kAxis;

        if (moveAxis.sqrMagnitude > 1.0f) moveAxis.Normalize();

        // Movement direction relative to Headset (HMD) / Camera orientation, flattened on horizontal plane (XZ)
        Vector3 forward = playerCamera != null ? playerCamera.transform.forward : transform.forward;
        Vector3 right = playerCamera != null ? playerCamera.transform.right : transform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 moveDir = right * moveAxis.x + forward * moveAxis.y;
        if (moveDir.sqrMagnitude > 1.0f) moveDir.Normalize();

        // Ray-based teleportation is completely disabled per design (No rays, no landing markers, no thumbstick click)
        VRTeleportController teleportCtrl = GetComponent<VRTeleportController>();
        if (teleportCtrl != null)
        {
            teleportCtrl.SetExternalTeleportAim(false);
        }

        float speed = walkSpeed;
        Vector3 displacement = moveDir * (speed * Time.deltaTime);

        // 2. Right Thumbstick Snap Turn (pivoting around player head to prevent wobbling)
        Vector3 turnPivot = playerCamera != null ? playerCamera.transform.position : transform.position;
        turnPivot.y = transform.position.y;

        Vector2 turnAxis = rightState.thumbstick;
        if (enableSnapTurn)
        {
            if (Mathf.Abs(turnAxis.x) > 0.50f)
            {
                if (snapTurnReset)
                {
                    float angle = Mathf.Sign(turnAxis.x) * snapTurnAngle;
                    transform.RotateAround(turnPivot, Vector3.up, angle);
                    snapTurnReset = false;
                }
            }
            else if (Mathf.Abs(turnAxis.x) < 0.20f)
            {
                snapTurnReset = true;
            }
        }
        else
        {
            float turnAngle = turnAxis.x * smoothTurnSpeed * Time.deltaTime;
            transform.RotateAround(turnPivot, Vector3.up, turnAngle);
        }

        // Desktop mouse look if not in VR and cursor is locked
        if (!isVRActive)
        {
            HandleCursorState();
            if (cursorLocked)
            {
                HandleDesktopRotation();
            }
        }

        // Gravity
        if (characterController.isGrounded && verticalVelocity < 0.0f)
        {
            verticalVelocity = -1.5f;
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }
        displacement.y = verticalVelocity * Time.deltaTime;

        characterController.Move(displacement);

        // Boundary containment to keep player safely inside playable lab area (Extended to Z=62.0 to support Room 4 South Rotunda)
        Vector3 curPos = transform.position;
        float clampedX = Mathf.Clamp(curPos.x, -52.0f, 52.0f);
        float clampedZ = Mathf.Clamp(curPos.z, -52.0f, 62.0f);
        if (curPos.x != clampedX || curPos.z != clampedZ)
        {
            curPos.x = clampedX;
            curPos.z = clampedZ;
            characterController.enabled = false;
            transform.position = curPos;
            characterController.enabled = true;
        }
    }

    #endregion

    #region Desktop Input & Movement (Fallback Only)

    private void HandleCursorState()
    {
        bool escPressed = NewtonInput.GetKeyDown(KeyCode.Escape);
        bool clickPressed = NewtonInput.GetMouseButtonDown(0);

        if (escPressed)
        {
            SetCursorLock(false);
        }
        else if (clickPressed && !cursorLocked)
        {
            SetCursorLock(true);
        }
    }

    private void SetCursorLock(bool locked)
    {
        cursorLocked = locked;
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    private void HandleDesktopRotation()
    {
        // When VR is active, desktop rotation is strictly disabled to prevent camera wobbling
        if (isVRActive) return;

        Vector2 mDelta = NewtonInput.GetMouseDelta();
        float mouseX = mDelta.x * mouseSensitivity;
        float mouseY = mDelta.y * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        if (playerCamera != null)
        {
            cameraPitch -= mouseY;
            cameraPitch = Mathf.Clamp(cameraPitch, minPitchAngle, maxPitchAngle);
            playerCamera.transform.localEulerAngles = new Vector3(cameraPitch, 0f, 0f);
        }
    }

    #endregion

    #region Interaction & Raycasting System

    private GameObject hoveredInteractable = null;
    private Renderer lastHoveredRenderer = null;
    private Color originalEmissionColor = Color.black;
    private bool hasOriginalEmission = false;

    // Cart grabbing state
    private NewtonsFirstLawExperiment activeExp1Grab = null;
    private Transform activeGrabHand = null;

    private void HandleInteractions()
    {
        // 0. Check if Parabolic Teleport Arc is being aimed
        VRTeleportController teleportCtrl = GetComponent<VRTeleportController>();
        bool isAimingTeleport = teleportCtrl != null && teleportCtrl.IsAimingTeleport;

        if (isAimingTeleport)
        {
            // When aiming parabolic teleport arc, hide straight pointer rays and suppress UI/experiment clicks
            if (rightVisual != null) rightVisual.SetPointerRayState(false);
            if (leftVisual != null) leftVisual.SetPointerRayState(false);
            if (vrPointerReticle != null) vrPointerReticle.SetActive(false);
            UpdateHoverVisual(null);
            return;
        }

        // 1. Process active cart grabbing if in progress
        if (activeExp1Grab != null)
        {
            bool stillHolding = false;
            ControllerState rStateGrab = PollController(true);
            ControllerState lStateGrab = PollController(false);
            if (rStateGrab.gripDown || lStateGrab.gripDown) stillHolding = true;

            try
            {
                if (Input.GetKey(KeyCode.G) || Input.GetKey(KeyCode.JoystickButton4) || Input.GetKey(KeyCode.JoystickButton5))
                    stillHolding = true;
            }
            catch { }

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.gKey.isPressed) stillHolding = true;
            if (Gamepad.current != null && (Gamepad.current.rightShoulder.isPressed || Gamepad.current.leftShoulder.isPressed)) stillHolding = true;
#endif

            if (stillHolding && activeGrabHand != null)
            {
                activeExp1Grab.UpdateGrabCart(activeGrabHand);
                return;
            }
            else
            {
                activeExp1Grab.ReleaseCart();
                activeExp1Grab = null;
                activeGrabHand = null;
            }
        }

        // 2. Determine Ray and Input Actions across all pipelines
        Ray activeRay = new Ray();
        bool hasRay = false;
        RaycastHit hit = default;
        bool interactJustPressed = false;
        bool interactHeld = false;
        bool grabRequested = false;
        Transform pointingSource = null;

        ControllerState rState = PollController(true);
        ControllerState lState = PollController(false);

        // Actions from VR Controller
        bool rAction = rState.triggerDown || rState.primaryButton;
        bool rActionJustPressed = rAction && !prevRightAction;
        prevRightAction = rAction;

        if (rActionJustPressed) interactJustPressed = true;
        if (rAction) interactHeld = true;
        if (rState.gripDown || lState.gripDown) grabRequested = true;

        // Desktop / Keyboard / Mouse / Gamepad fallbacks
        if (NewtonInput.GetMouseButtonDown(0) || NewtonInput.GetKeyDown(KeyCode.E)) interactJustPressed = true;
        if (NewtonInput.GetMouseButton(0) || NewtonInput.GetKey(KeyCode.E)) interactHeld = true;
        if (NewtonInput.GetKey(KeyCode.G)) grabRequested = true;

#if ENABLE_INPUT_SYSTEM
        if (Gamepad.current != null)
        {
            if (Gamepad.current.rightTrigger.wasPressedThisFrame || Gamepad.current.aButton.wasPressedThisFrame) interactJustPressed = true;
            if (Gamepad.current.rightTrigger.isPressed || Gamepad.current.aButton.isPressed) interactHeld = true;
            if (Gamepad.current.rightShoulder.isPressed) grabRequested = true;
        }
#endif

        // RAYCAST TARGETING:
        // Priority 1: Meta Quest 3S Tracked Hand Ray (Natural Hand Tracking)
        if (FloatingVRDestinationPanel.Instance != null && FloatingVRDestinationPanel.Instance.IsHandTrackingActive && FloatingVRDestinationPanel.Instance.HasActiveHandRay)
        {
            activeRay = FloatingVRDestinationPanel.Instance.ActiveHandRay;
            hasRay = SafeRaycast(activeRay, 25.0f, out hit);
            pointingSource = FloatingVRDestinationPanel.Instance.ActiveHandTransform != null ? FloatingVRDestinationPanel.Instance.ActiveHandTransform : transform;

            if (FloatingVRDestinationPanel.Instance.IsPinchJustPressed)
            {
                interactJustPressed = true;
                interactHeld = true;
            }
            if (FloatingVRDestinationPanel.Instance.IsPinching)
            {
                interactHeld = true;
            }
        }
        // Priority 2: Meta Quest 3S Touch Plus Controller Ray
        else if (rightHandController != null)
        {
            activeRay = new Ray(rightHandController.position + rightHandController.forward * 0.05f, rightHandController.forward);
            hasRay = SafeRaycast(activeRay, 25.0f, out hit);
            pointingSource = rightHandController;

            // If right controller ray hits an interactable, use it.
            // Otherwise, check camera gaze ray as seamless fallback so looking at a control always works!
            if ((!hasRay || !IsInteractableObject(hit.collider.gameObject)) && playerCamera != null)
            {
                Ray cameraRay = new Ray(playerCamera.transform.position + playerCamera.transform.forward * 0.1f, playerCamera.transform.forward);
                if (SafeRaycast(cameraRay, 25.0f, out RaycastHit camHit))
                {
                    if (IsInteractableObject(camHit.collider.gameObject))
                    {
                        activeRay = cameraRay;
                        hit = camHit;
                        hasRay = true;
                        pointingSource = playerCamera.transform;
                    }
                }
            }
        }
        else if (playerCamera != null)
        {
            activeRay = new Ray(playerCamera.transform.position + playerCamera.transform.forward * 0.1f, playerCamera.transform.forward);
            hasRay = SafeRaycast(activeRay, 25.0f, out hit);
            pointingSource = playerCamera.transform;
        }

        GameObject currentHitObj = null;
        Vector3 currentHitPoint = Vector3.zero;
        bool isWalkableFloor = false;

        if (hasRay && hit.collider != null)
        {
            currentHitPoint = hit.point;
            bool isNearLab1 = Vector3.Distance(transform.position, new Vector3(0f, 0f, -46f)) < 15f;
            bool isNearLab2 = Vector3.Distance(transform.position, new Vector3(-41.8f, 0f, 0f)) < 18f;
            bool isNearLab3 = Vector3.Distance(transform.position, new Vector3(41.8f, 0f, 0f)) < 18f;

            // 3. First Law Experiment Interactions (Lab 1)
            NewtonsFirstLawExperiment exp1 = hit.collider.GetComponentInParent<NewtonsFirstLawExperiment>();
            if (exp1 == null && isNearLab1)
            {
                string hitLower = hit.collider.gameObject.name.ToLower();
                if (hitLower.Contains("firstlaw") || hitLower.Contains("friction") || hitLower.Contains("obstacle") ||
                    hitLower.Contains("cart") || hitLower.Contains("ball") || hitLower.Contains("inertia") ||
                    hitLower.Contains("button") || hitLower.Contains("launch") || hitLower.Contains("reset"))
                {
                    exp1 = exp1Cached != null ? exp1Cached : Object.FindFirstObjectByType<NewtonsFirstLawExperiment>();
                }
            }

            if (exp1 != null)
            {
                currentHitObj = hit.collider.gameObject;
                string hitName = hit.collider.gameObject.name.ToLower();

                if (hitName.Contains("cart") || hitName.Contains("chassis") || hitName.Contains("trolley") ||
                    hitName.Contains("grab") || hitName.Contains("ball") || hitName.Contains("sphere") ||
                    hitName.Contains("wheel") || hitName.Contains("hub"))
                {
                    if (grabRequested && activeExp1Grab == null)
                    {
                        activeExp1Grab = exp1;
                        activeGrabHand = pointingSource;
                        exp1.StartGrabCart(pointingSource);
                    }
                }
                else if (interactJustPressed)
                {
                    exp1.ReceiveInteraction(hit.collider.gameObject);
                    TriggerHapticFeedback(pointingSource);
                }
            }

            // 4. Second Law Experiment Interactions (Lab 2 — F = m × a)
            NewtonsSecondLawExperiment exp2 = hit.collider.GetComponentInParent<NewtonsSecondLawExperiment>();
            if (exp2 == null && isNearLab2)
            {
                string hitLower = hit.collider.gameObject.name.ToLower();
                if (hitLower.Contains("secondlaw") || hitLower.Contains("station2") || hitLower.Contains("force") ||
                    hitLower.Contains("mass") || hitLower.Contains("slider") || hitLower.Contains("cart") ||
                    hitLower.Contains("launch") || hitLower.Contains("stop") || hitLower.Contains("reset") ||
                    hitLower.Contains("button") || hitLower.Contains("knob"))
                {
                    exp2 = exp2Cached != null ? exp2Cached : Object.FindFirstObjectByType<NewtonsSecondLawExperiment>();
                }
            }

            if (exp2 != null)
            {
                currentHitObj = hit.collider.gameObject;
                string hitName = hit.collider.gameObject.name.ToLower();
                bool isSliderHit = hitName.Contains("force") || hitName.Contains("slider") || hitName.Contains("knob") || hitName.Contains("rail");

                if (interactJustPressed)
                {
                    exp2.ReceiveInteraction(hit.collider.gameObject, hit.point);
                    TriggerHapticFeedback(pointingSource);
                }
                else if (interactHeld && isSliderHit)
                {
                    exp2.ReceiveInteraction(hit.collider.gameObject, hit.point);
                }
            }

            // 5. Third Law Experiment Interactions (Lab 3 — Action & Reaction)
            NewtonsThirdLawExperiment exp3 = hit.collider.GetComponentInParent<NewtonsThirdLawExperiment>();
            if (exp3 == null && isNearLab3)
            {
                string hitLower = hit.collider.gameObject.name.ToLower();
                if (hitLower.Contains("thirdlaw") || hitLower.Contains("station3") || hitLower.Contains("action") ||
                    hitLower.Contains("reaction") || hitLower.Contains("push") || hitLower.Contains("left") ||
                    hitLower.Contains("right") || hitLower.Contains("reset") || hitLower.Contains("stop") ||
                    hitLower.Contains("button") || hitLower.Contains("cart"))
                {
                    exp3 = exp3Cached != null ? exp3Cached : Object.FindFirstObjectByType<NewtonsThirdLawExperiment>();
                }
            }
            if (exp3 != null)
            {
                currentHitObj = hit.collider.gameObject;
                if (interactJustPressed)
                {
                    exp3.ReceiveInteraction(hit.collider.gameObject);
                    TriggerHapticFeedback(pointingSource);
                }
            }

            // 6. START LAB Hub Button Interaction
            bool isNearLobby = Vector3.Distance(transform.position, Vector3.zero) < 25f;
            string rawHitName = hit.collider.gameObject.name.ToLower();
            if (rawHitName.Contains("startbutton") || rawHitName.Contains("starttext") || rawHitName.Contains("start lab") || (rawHitName.Contains("start") && isNearLobby))
            {
                currentHitObj = hit.collider.gameObject;
                if (interactJustPressed)
                {
                    EnterLabFromStartButton();
                    TriggerHapticFeedback(pointingSource);
                }
            }

            // 6.5. LABS Navigation Button Interaction
            if (rawHitName.Contains("navtab") || rawHitName.Contains("vr_worldspacenavtab") || rawHitName.Contains("labs") || (hit.collider.transform.parent != null && hit.collider.transform.parent.name.ToLower().Contains("navtab")))
            {
                currentHitObj = hit.collider.gameObject;
                if (interactJustPressed)
                {
                    FloatingVRDestinationPanel fv = FloatingVRDestinationPanel.Instance;
                    if (fv != null)
                    {
                        fv.SetPanelVisibility(true);
                        fv.SnapToLeftPosition();
                    }
                    TriggerHapticFeedback(pointingSource);
                }
            }

            // 6.6. Welcome Panel ENTER LABORATORY Button Interaction
            if (rawHitName.Contains("enterlaboratory") || rawHitName.Contains("btn_enter") || rawHitName.Contains("welcome") ||
                (hit.collider.transform.parent != null && hit.collider.transform.parent.name.ToLower().Contains("enterlaboratory")))
            {
                currentHitObj = hit.collider.gameObject;
                if (interactJustPressed)
                {
                    VRWelcomePanel wp = VRWelcomePanel.Instance;
                    if (wp != null)
                    {
                        wp.OnEnterLaboratoryClicked();
                    }
                    TriggerHapticFeedback(pointingSource);
                }
            }

            // 6.7. Room 4 Observation Centre Interactive Buttons (Mass, Force, Summary Tabs)
            if (rawHitName.Contains("massselector") || rawHitName.Contains("btn_mass"))
            {
                currentHitObj = hit.collider.gameObject;
                if (interactJustPressed)
                {
                    if (VRRoom4ObservationController.Instance != null)
                        VRRoom4ObservationController.Instance.CycleMass();
                    TriggerHapticFeedback(pointingSource);
                }
            }
            else if (rawHitName.Contains("forceselector") || rawHitName.Contains("btn_force"))
            {
                currentHitObj = hit.collider.gameObject;
                if (interactJustPressed)
                {
                    if (VRRoom4ObservationController.Instance != null)
                        VRRoom4ObservationController.Instance.CycleForce();
                    TriggerHapticFeedback(pointingSource);
                }
            }
            VRSummaryTabButton summaryTabBtn = hit.collider.GetComponentInParent<VRSummaryTabButton>();
            if (summaryTabBtn != null)
            {
                currentHitObj = summaryTabBtn.gameObject;
                if (interactJustPressed)
                {
                    Debug.Log($"[Newton VR Raycast] Controller Ray / Hand Triggered VRSummaryTabButton: '{summaryTabBtn.tabName}' (Index {summaryTabBtn.tabIndex})");
                    summaryTabBtn.OnSelectTab();
                    TriggerHapticFeedback(pointingSource);
                }
            }
            else if (rawHitName.Contains("tab_results") || rawHitName.Contains("results"))
            {
                currentHitObj = hit.collider.gameObject;
                if (interactJustPressed)
                {
                    Debug.Log("[Newton VR Raycast] Controller Ray / Hand Triggered: RESULTS TAB (Index 0)");
                    if (VRRoom4ObservationController.Instance != null)
                        VRRoom4ObservationController.Instance.SetSummaryTab(0);
                    TriggerHapticFeedback(pointingSource);
                }
            }
            else if (rawHitName.Contains("tab_comparison") || rawHitName.Contains("comparison"))
            {
                currentHitObj = hit.collider.gameObject;
                if (interactJustPressed)
                {
                    Debug.Log("[Newton VR Raycast] Controller Ray / Hand Triggered: COMPARISON TAB (Index 1)");
                    if (VRRoom4ObservationController.Instance != null)
                        VRRoom4ObservationController.Instance.SetSummaryTab(1);
                    TriggerHapticFeedback(pointingSource);
                }
            }
            else if (rawHitName.Contains("tab_conclusion") || rawHitName.Contains("conclusion"))
            {
                currentHitObj = hit.collider.gameObject;
                if (interactJustPressed)
                {
                    Debug.Log("[Newton VR Raycast] Controller Ray / Hand Triggered: CONCLUSION TAB (Index 2)");
                    if (VRRoom4ObservationController.Instance != null)
                        VRRoom4ObservationController.Instance.SetSummaryTab(2);
                    TriggerHapticFeedback(pointingSource);
                }
            }

            // 6.8. Navigation Panel Destination Buttons Direct Interaction (Controller Ray, Hand Pinch & Mouse Click)
            if (rawHitName.Contains("room4") || rawHitName.Contains("observation"))
            {
                currentHitObj = hit.collider.gameObject;
                if (interactJustPressed)
                {
                    FloatingVRDestinationPanel fv = FloatingVRDestinationPanel.Instance != null ? FloatingVRDestinationPanel.Instance : Object.FindFirstObjectByType<FloatingVRDestinationPanel>();
                    if (fv != null)
                    {
                        fv.SelectDestination(FloatingVRDestinationPanel.DestinationRoom.Room4_Observation);
                    }
                    else
                    {
                        TeleportToStationIndex(4);
                    }
                    TriggerHapticFeedback(pointingSource);
                }
            }
            else if (rawHitName.Contains("room1") || rawHitName.Contains("firstlaw"))
            {
                currentHitObj = hit.collider.gameObject;
                if (interactJustPressed)
                {
                    FloatingVRDestinationPanel fv = FloatingVRDestinationPanel.Instance != null ? FloatingVRDestinationPanel.Instance : Object.FindFirstObjectByType<FloatingVRDestinationPanel>();
                    if (fv != null) fv.SelectDestination(FloatingVRDestinationPanel.DestinationRoom.Lab1_FirstLaw);
                    else TeleportToStationIndex(1);
                    TriggerHapticFeedback(pointingSource);
                }
            }
            else if (rawHitName.Contains("room2") || rawHitName.Contains("secondlaw"))
            {
                currentHitObj = hit.collider.gameObject;
                if (interactJustPressed)
                {
                    FloatingVRDestinationPanel fv = FloatingVRDestinationPanel.Instance != null ? FloatingVRDestinationPanel.Instance : Object.FindFirstObjectByType<FloatingVRDestinationPanel>();
                    if (fv != null) fv.SelectDestination(FloatingVRDestinationPanel.DestinationRoom.Lab2_SecondLaw);
                    else TeleportToStationIndex(2);
                    TriggerHapticFeedback(pointingSource);
                }
            }
            else if (rawHitName.Contains("room3") || rawHitName.Contains("thirdlaw"))
            {
                currentHitObj = hit.collider.gameObject;
                if (interactJustPressed)
                {
                    FloatingVRDestinationPanel fv = FloatingVRDestinationPanel.Instance != null ? FloatingVRDestinationPanel.Instance : Object.FindFirstObjectByType<FloatingVRDestinationPanel>();
                    if (fv != null) fv.SelectDestination(FloatingVRDestinationPanel.DestinationRoom.Lab3_ThirdLaw);
                    else TeleportToStationIndex(3);
                    TriggerHapticFeedback(pointingSource);
                }
            }
            else if (rawHitName.Contains("mainlobby") || (rawHitName.Contains("lobby") && !rawHitName.Contains("start")))
            {
                currentHitObj = hit.collider.gameObject;
                if (interactJustPressed)
                {
                    FloatingVRDestinationPanel fv = FloatingVRDestinationPanel.Instance != null ? FloatingVRDestinationPanel.Instance : Object.FindFirstObjectByType<FloatingVRDestinationPanel>();
                    if (fv != null) fv.SelectDestination(FloatingVRDestinationPanel.DestinationRoom.MainLobby);
                    else TeleportToStationIndex(0);
                    TriggerHapticFeedback(pointingSource);
                }
            }

            // 7. Desktop Fallback: Point-and-Click Floor Teleportation (VR uses Left Stick Click Teleport Arc)
            if (!isVRActive && currentHitObj == null && hit.normal.y >= 0.55f)
            {
                isWalkableFloor = true;
                if (interactJustPressed)
                {
                    ExecuteFloorTeleport(currentHitPoint);
                }
            }
        }

        // 8. Visual Hover Highlight Feedback
        UpdateHoverVisual(currentHitObj);

        // 9. Controller Ray Cleanliness & VR Pointer Reticle Dot Update
        // Ray and reticle are visible ONLY when pointing at usable controls, keeping the player's view uncluttered
        bool isAimingAtTarget = hasRay && (currentHitObj != null);

        if (isAimingAtTarget)
        {
            float rayDist = Vector3.Distance(pointingSource != null ? pointingSource.position : transform.position, currentHitPoint);
            if (rightVisual != null) rightVisual.SetPointerRayState(true, Mathf.Min(rayDist, 10.0f));
            if (leftVisual != null) leftVisual.SetPointerRayState(false);

            if (vrPointerReticle != null)
            {
                vrPointerReticle.SetActive(true);
                vrPointerReticle.transform.position = currentHitPoint;
                vrPointerReticle.transform.localScale = Vector3.one * 0.04f;
            }
        }
        else
        {
            if (rightVisual != null) rightVisual.SetPointerRayState(false);
            if (leftVisual != null) leftVisual.SetPointerRayState(false);
            if (vrPointerReticle != null) vrPointerReticle.SetActive(false);
        }
    }

    private void TriggerHapticFeedback(Transform sourceHand)
    {
        if (!isVRActive) return;
        XRNode node = (sourceHand == leftHandController) ? XRNode.LeftHand : XRNode.RightHand;
        XRInputDevice dev = XRInputDevices.GetDeviceAtXRNode(node);
        if (dev.isValid)
        {
            dev.SendHapticImpulse(0, 0.45f, 0.08f);
        }
    }

    private bool IsInteractableObject(GameObject go)
    {
        if (go == null) return false;
        string n = go.name.ToLower();
        string pn = go.transform.parent != null ? go.transform.parent.name.ToLower() : "";
        if (go.GetComponent<VRSummaryTabButton>() != null || (go.transform.parent != null && go.transform.parent.GetComponent<VRSummaryTabButton>() != null))
            return true;

        return n.Contains("button") || n.Contains("cart") || n.Contains("launch") || n.Contains("stop") ||
               n.Contains("reset") || n.Contains("start") || n.Contains("mass") || n.Contains("force") ||
               n.Contains("slider") || n.Contains("knob") || n.Contains("rail") || n.Contains("obstacle") ||
               n.Contains("push") || n.Contains("friction") || n.Contains("ball") ||
               n.Contains("navtab") || n.Contains("labs") || n.Contains("vr_worldspacenav") ||
               n.Contains("welcome") || n.Contains("enterlaboratory") || n.Contains("btn_enter") ||
               n.Contains("btn_") || n.Contains("room") || n.Contains("observation") || n.Contains("mainlobby") ||
               n.Contains("tab") || n.Contains("comparison") || n.Contains("conclusion") || n.Contains("results") || n.Contains("summary") ||
               pn.Contains("button") || pn.Contains("cart") || pn.Contains("slider") || pn.Contains("navtab") || pn.Contains("welcome") || pn.Contains("enter") || pn.Contains("navcanvas") || pn.Contains("panel_") || pn.Contains("tab");
    }

    private bool SafeRaycast(Ray ray, float maxDistance, out RaycastHit hit)
    {
        RaycastHit[] hits = Physics.RaycastAll(ray, maxDistance);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        for (int i = 0; i < hits.Length; i++)
        {
            Collider c = hits[i].collider;
            if (c == null) continue;
            if (c == characterController) continue;
            if (c.transform.IsChildOf(transform)) continue;
            if (c.name.Contains("Pedestal") || c.name.Contains("Globe")) continue; // Decorative pedestal must never block controls
            if (c.isTrigger)
            {
                bool isInteractable = IsInteractableObject(c.gameObject);
                if (!isInteractable) continue;
            }

            hit = hits[i];
            return true;
        }
        hit = default;
        return false;
    }

    public void ResetVerticalVelocity()
    {
        verticalVelocity = 0f;
    }

    public void CycleNextStationTeleport()
    {
        currentStationIndex = (currentStationIndex + 1) % 4;
        TeleportToStationIndex(currentStationIndex);
    }

    public void SetCurrentStationIndex(int index)
    {
        currentStationIndex = Mathf.Clamp(index, 0, 4);
        lastNavEventText = $"Panel Jump -> {GetStationName(currentStationIndex)}";
        lastNavEventTime = Time.time;
        UpdateDiagnosticDisplay();
    }

    public string GetStationName(int index)
    {
        switch (index)
        {
            case 0: return "Main Lobby";
            case 1: return "Lab 1 (Newton's 1st Law)";
            case 2: return "Lab 2 (Newton's 2nd Law)";
            case 3: return "Lab 3 (Newton's 3rd Law)";
            case 4: return "Room 4 (Observation & Analysis)";
            default: return "Main Lobby";
        }
    }

    public void TeleportToStationIndex(int index)
    {
        currentStationIndex = Mathf.Clamp(index, 0, 4);
        roomSwitchCount++;
        lastNavEventText = $"[B] Jump #{roomSwitchCount} -> {GetStationName(currentStationIndex)}";
        lastNavEventTime = Time.time;

        if (characterController != null) characterController.enabled = false;

        Vector3 targetPos;
        Quaternion targetRot;
        string stationName;

        switch (currentStationIndex)
        {
            case 1:
                targetPos = new Vector3(0f, 0f, -41.0f);
                targetRot = Quaternion.Euler(0f, 180f, 0f);
                stationName = "Lab 1 (Newton's 1st Law)";
                if (NarrationManager.Instance != null) NarrationManager.Instance.OnEnterLab1();
                break;
            case 2:
                targetPos = new Vector3(-41.8f, 0f, 0f);
                targetRot = Quaternion.Euler(0f, -90f, 0f);
                stationName = "Lab 2 (Newton's 2nd Law)";
                if (NarrationManager.Instance != null) NarrationManager.Instance.OnEnterLab2();
                break;
            case 3:
                targetPos = new Vector3(41.8f, 0f, 0f);
                targetRot = Quaternion.Euler(0f, 90f, 0f);
                stationName = "Lab 3 (Newton's 3rd Law)";
                if (NarrationManager.Instance != null) NarrationManager.Instance.OnEnterLab3();
                break;
            case 4:
                GameObject sp = GameObject.Find("Room4SpawnPoint");
                if (sp == null) sp = GameObject.Find("Room4_SpawnPoint");
                if (sp != null)
                {
                    targetPos = sp.transform.position;
                    targetRot = sp.transform.rotation;
                }
                else
                {
                    targetPos = new Vector3(0f, 0.02f, 42.50f);
                    targetRot = Quaternion.Euler(0f, 0f, 0f);
                }
                stationName = "Room 4 (Observation & Analysis)";
                break;
            default:
                targetPos = new Vector3(0f, 0.0f, 5.80f);
                targetRot = Quaternion.Euler(0f, 180f, 0f);
                stationName = "Main Lobby";
                if (NarrationManager.Instance != null) NarrationManager.Instance.OnEnterMainLobby();
                break;
        }

        Vector3 headOffset = Vector3.zero;
        if (playerCamera != null)
        {
            headOffset = playerCamera.transform.position - transform.position;
            headOffset.y = 0f;
        }

        transform.position = targetPos - headOffset;
        transform.rotation = targetRot;
        verticalVelocity = 0f;
        cameraPitch = 0f;
        if (playerCamera != null) playerCamera.transform.localEulerAngles = Vector3.zero;

        if (characterController != null) characterController.enabled = true;

        FloatingVRDestinationPanel panel = FindFirstObjectByType<FloatingVRDestinationPanel>();
        if (panel != null)
        {
            panel.SetCurrentActiveRoom(currentStationIndex);
            panel.SnapToLeftPosition();
            panel.SetPanelVisibility(false);
        }

        UpdateDiagnosticDisplay();
        Debug.Log($"[Newton VR] Button B Station Jump #{roomSwitchCount}: Teleported to {stationName} at {targetPos}");
    }

    private void SetupVRDiagnosticHUD()
    {
        if (vrDiagnosticHudObj != null) return;

        vrDiagnosticHudObj = new GameObject("VR_Diagnostic_WristHUD");
        if (leftHandController != null)
        {
            vrDiagnosticHudObj.transform.SetParent(leftHandController, false);
            vrDiagnosticHudObj.transform.localPosition = new Vector3(0.04f, 0.08f, -0.04f);
            vrDiagnosticHudObj.transform.localRotation = Quaternion.Euler(55f, 15f, 0f);
        }
        else
        {
            vrDiagnosticHudObj.transform.SetParent(transform, false);
            vrDiagnosticHudObj.transform.localPosition = new Vector3(-0.35f, 1.15f, 0.65f);
            vrDiagnosticHudObj.transform.localRotation = Quaternion.identity;
        }
        vrDiagnosticHudObj.transform.localScale = Vector3.one * 0.007f;

        // Background Slate
        GameObject slate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        slate.name = "Slate";
        slate.transform.SetParent(vrDiagnosticHudObj.transform, false);
        slate.transform.localPosition = new Vector3(0f, 0f, 0.02f);
        slate.transform.localScale = new Vector3(28f, 16f, 0.2f);
        Collider slateCol = slate.GetComponent<Collider>();
        if (slateCol != null) Destroy(slateCol);

        Renderer slateRend = slate.GetComponent<Renderer>();
        if (slateRend != null)
        {
            Material slateMat = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Standard"));
            slateMat.color = new Color(0.08f, 0.09f, 0.12f, 0.92f);
            slateRend.material = slateMat;
        }

        // TextMesh
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(vrDiagnosticHudObj.transform, false);
        textObj.transform.localPosition = new Vector3(-13f, 6.8f, 0f);

        vrDiagnosticTextMesh = textObj.AddComponent<TextMesh>();
        vrDiagnosticTextMesh.characterSize = 0.45f;
        vrDiagnosticTextMesh.fontSize = 24;
        vrDiagnosticTextMesh.alignment = TextAlignment.Left;
        vrDiagnosticTextMesh.anchor = TextAnchor.UpperLeft;
        vrDiagnosticTextMesh.color = new Color(0.95f, 0.85f, 0.55f);

        UpdateDiagnosticDisplay();
        vrDiagnosticHudObj.SetActive(showDiagnosticOverlay);
    }

    private void UpdateDiagnosticDisplay()
    {
        if (vrDiagnosticTextMesh == null) return;
        if (vrDiagnosticHudObj != null && !vrDiagnosticHudObj.activeInHierarchy) return;

        int nextIdx = (currentStationIndex + 1) % 4;
        string bStateStr = lastRightBInputDetected ? "[PRESSED - JUMP]" : "[READY]";
        string xStateStr = lastLeftXInputDetected ? "[PRESSED - MENU]" : "[READY]";

        if (lastRightBInputDetected)
        {
            vrDiagnosticTextMesh.color = Color.cyan;
        }
        else if (lastLeftXInputDetected)
        {
            vrDiagnosticTextMesh.color = Color.green;
        }
        else
        {
            vrDiagnosticTextMesh.color = new Color(0.95f, 0.85f, 0.55f);
        }

        FloatingVRDestinationPanel panel = FindFirstObjectByType<FloatingVRDestinationPanel>();
        string panelState = (panel != null && panel.IsPanelVisible) ? "OPEN (LEFT)" : "CLOSED (TAB)";

        vrDiagnosticTextMesh.text =
            "QUEST 3S DIAGNOSTIC\n" +
            $"Room: {GetStationName(currentStationIndex)}\n" +
            $"Right [B]: {bStateStr} -> Next: {GetStationName(nextIdx)}\n" +
            $"Left [X]: {xStateStr} | Nav Panel: {panelState}\n" +
            $"Total Jumps: {roomSwitchCount}";
    }

    public void ExecuteFloorTeleport(Vector3 destination)
    {
        VRTeleportController tc = GetComponent<VRTeleportController>();
        if (tc != null)
        {
            tc.ExecuteTeleport(destination);
        }
        else
        {
            if (characterController != null) characterController.enabled = false;
            Vector3 headOffset = Vector3.zero;
            if (playerCamera != null)
            {
                headOffset = playerCamera.transform.position - transform.position;
                headOffset.y = 0f;
            }
            Vector3 newPos = destination - headOffset;
            newPos.y = destination.y;
            transform.position = newPos;
            verticalVelocity = 0f;
            if (characterController != null) characterController.enabled = true;
        }
        Debug.Log($"[Newton VR] Floor Teleport to: {destination}");
    }

    public void EnterLabFromStartButton()
    {
        Debug.Log("[Newton VR] START LAB clicked: Opening VR Destination Panel on left side (no teleport)...");
        // DO NOT teleport the player. Keep the player standing comfortably at the spawn position in front of START LAB and globe.
        FloatingVRDestinationPanel panel = FloatingVRDestinationPanel.Instance != null ? FloatingVRDestinationPanel.Instance : FindFirstObjectByType<FloatingVRDestinationPanel>();
        if (panel != null)
        {
            panel.SetPanelVisibility(true);
            panel.SnapToLeftPosition();
        }

        if (NarrationManager.Instance != null)
        {
            NarrationManager.Instance.OnEnterFromStartLab();
        }
    }

    private void UpdateHoverVisual(GameObject newHoverObj)
    {
        if (newHoverObj == hoveredInteractable) return;

        // Restore previous hovered object
        if (lastHoveredRenderer != null && hasOriginalEmission)
        {
            if (lastHoveredRenderer.material.HasProperty("_EmissionColor"))
            {
                lastHoveredRenderer.material.SetColor("_EmissionColor", originalEmissionColor);
            }
            lastHoveredRenderer = null;
            hasOriginalEmission = false;
        }

        hoveredInteractable = newHoverObj;

        // Apply hover highlight to new object
        if (hoveredInteractable != null)
        {
            Renderer r = hoveredInteractable.GetComponent<Renderer>();
            if (r != null && r.material != null && r.material.HasProperty("_EmissionColor"))
            {
                lastHoveredRenderer = r;
                originalEmissionColor = r.material.GetColor("_EmissionColor");
                hasOriginalEmission = true;

                // Emissive highlight glow (soft cyan-white)
                Color highlightColor = originalEmissionColor + new Color(0.15f, 0.45f, 0.65f);
                r.material.EnableKeyword("_EMISSION");
                r.material.SetColor("_EmissionColor", highlightColor);
            }
        }
    }

    #endregion

    private bool IsKeyJustPressed(KeyCode key)
    {
        return NewtonInput.GetKeyDown(key);
    }

    void OnGUI()
    {
        // 1. Live Controller & Navigation Diagnostic Overlay (Toggleable via F1, default off)
        if (showDiagnosticOverlay)
        {
            RenderDiagnosticGUI();
        }

        if (isVRActive) return;

        if (cursorLocked)
        {
            float crosshairSize = 6f;
            float centerX = Screen.width / 2f;
            float centerY = Screen.height / 2f;
            GUI.color = (hoveredInteractable != null) ? new Color(0.2f, 1f, 0.4f, 0.95f) : new Color(1f, 1f, 1f, 0.6f);
            GUI.Box(new Rect(centerX - 1, centerY - crosshairSize / 2, 2, crosshairSize), GUIContent.none);
            GUI.Box(new Rect(centerX - crosshairSize / 2, centerY - 1, crosshairSize, 2), GUIContent.none);

            if (hoveredInteractable != null)
            {
                GUIStyle interactStyle = new GUIStyle(GUI.skin.box);
                interactStyle.fontSize = 13;
                interactStyle.alignment = TextAnchor.MiddleCenter;
                interactStyle.fontStyle = FontStyle.Bold;
                interactStyle.normal.textColor = Color.white;
                GUI.color = new Color(0.1f, 0.9f, 0.5f, 0.95f);
                string cleanName = hoveredInteractable.name.Replace("Button", "").Replace("_", " ").Trim();
                string low = cleanName.ToLower();

                if (low.Contains("cart1") || low.Contains("cart 1"))
                {
                    if (low.Contains("10kg") || low.Contains("10")) cleanName = "Cart 1 Mass: 10 kg";
                    else if (low.Contains("1kg") || low.Contains(" 1")) cleanName = "Cart 1 Mass: 1 kg";
                    else if (low.Contains("2kg") || low.Contains(" 2")) cleanName = "Cart 1 Mass: 2 kg";
                    else if (low.Contains("5kg") || low.Contains(" 5")) cleanName = "Cart 1 Mass: 5 kg";
                    else if (low.Contains("minus") || low.Contains("-")) cleanName = "Cart 1 Mass: -1 kg";
                    else if (low.Contains("plus") || low.Contains("+")) cleanName = "Cart 1 Mass: +1 kg";
                    else cleanName = "Cycle Cart 1 Mass";
                }
                else if (low.Contains("cart2") || low.Contains("cart 2"))
                {
                    if (low.Contains("10kg") || low.Contains("10")) cleanName = "Cart 2 Mass: 10 kg";
                    else if (low.Contains("1kg") || low.Contains(" 1")) cleanName = "Cart 2 Mass: 1 kg";
                    else if (low.Contains("2kg") || low.Contains(" 2")) cleanName = "Cart 2 Mass: 2 kg";
                    else if (low.Contains("5kg") || low.Contains(" 5")) cleanName = "Cart 2 Mass: 5 kg";
                    else if (low.Contains("minus") || low.Contains("-")) cleanName = "Cart 2 Mass: -1 kg";
                    else if (low.Contains("plus") || low.Contains("+")) cleanName = "Cart 2 Mass: +1 kg";
                    else cleanName = "Cycle Cart 2 Mass";
                }
                else if (low.Contains("force"))
                {
                    if (low.Contains("20n") || low.Contains("20")) cleanName = "Set Force: 20 N";
                    else if (low.Contains("15n") || low.Contains("15")) cleanName = "Set Force: 15 N";
                    else if (low.Contains("10n") || low.Contains("10")) cleanName = "Set Force: 10 N";
                    else if (low.Contains("5n") || low.Contains(" 5")) cleanName = "Set Force: 5 N";
                    else if (low.Contains("minus") || low.Contains("-")) cleanName = "Decrease Force: -2 N";
                    else if (low.Contains("plus") || low.Contains("+")) cleanName = "Increase Force: +2 N";
                    else cleanName = "Adjust Force";
                }
                else if (low.Contains("startbutton") || low.Contains("starttext") || low.Contains("start")) cleanName = "START LAB";
                else if (low.Contains("launch")) cleanName = "Launch Carts (F = ma)";
                else if (low.Contains("reset")) cleanName = "Reset Experiment";
                else if (low.Contains("stop")) cleanName = "Stop Motion";
                else if (low.Contains("mode")) cleanName = "Toggle Comparison Mode";
                else if (low.Contains("mass") && low.Contains("10")) cleanName = "Select Mass: 10 kg";
                else if (low.Contains("mass") && low.Contains("1")) cleanName = "Select Mass: 1 kg";
                else if (low.Contains("mass") && low.Contains("2")) cleanName = "Select Mass: 2 kg";
                else if (low.Contains("mass") && low.Contains("5")) cleanName = "Select Mass: 5 kg";

                GUI.Box(new Rect(centerX - 140, centerY + 18, 280, 28), $"[E] or Click : {cleanName}", interactStyle);
            }
        }
        else
        {
            GUI.color = new Color(1f, 0.95f, 0.4f, 0.95f);
            GUIStyle promptStyle = new GUIStyle(GUI.skin.box);
            promptStyle.fontSize = 14;
            promptStyle.alignment = TextAnchor.MiddleCenter;
            promptStyle.fontStyle = FontStyle.Bold;
            GUI.Box(new Rect(Screen.width / 2f - 200, Screen.height - 65, 400, 38), "Click Game View to lock mouse & walk", promptStyle);
        }

        if (showControlsHUD)
        {
            GUI.color = Color.white;
            GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.fontSize = 11;
            boxStyle.alignment = TextAnchor.MiddleLeft;
            boxStyle.normal.textColor = Color.white;

            string modeLabel = drawMode == GameDrawMode.Unlit
                ? "<color=#55ffff><b>UNLIT</b></color>"
                : "<color=#ffaa55><b>SHADED</b></color>";

            Vector3 p = transform.position;
            string stationHint = "";
            if (Vector3.Distance(p, new Vector3(0f, 0f, -46f)) < 15f)
            {
                stationHint = "\n<color=#55ffff><b>LAB 1 (INERTIA) KEYS:</b></color>\n" +
                              "• <b>[Click & Drag]</b> Cart : Push along track\n" +
                              "• <b>[Space]</b> or <b>[1]</b> : Launch/Stop | <b>[O]</b> or <b>[2]</b> : Obstacle\n" +
                              "• <b>[F]</b> or <b>[4]</b> : Friction | <b>[R]</b> : Reset";
            }
            else if (Vector3.Distance(p, new Vector3(-46f, 0f, 0f)) < 15f)
            {
                stationHint = "\n<color=#55ffff><b>LAB 2 (F = m × a) CONTROLS:</b></color>\n" +
                              "• <b>[Click 3D Panels]</b> : Select Cart 1 Mass, Cart 2 Mass, & Force\n" +
                              "• <b>[1][2][5][0]</b> : Cart 1 Mass | <b>[6][7][8][9]</b> : Cart 2 Mass\n" +
                              "• <b>[F]</b> or <b>[+/-]</b> : Step Force (2-20 N) | <b>[Space]</b> : Launch / Stop | <b>[R]</b> : Reset";
            }
            else if (Vector3.Distance(p, new Vector3(46.6f, 0f, 0f)) < 15f)
            {
                stationHint = "\n<color=#55ffff><b>LAB 3 (ACTION-REACTION) KEYS:</b></color>\n" +
                              "• <b>[1]</b> or <b>[Left Arrow]</b> : Push Left\n" +
                              "• <b>[2]</b> or <b>[Right Arrow]</b> : Push Right\n" +
                              "• <b>[Space]</b> : Push/Stop | <b>[R]</b> : Reset";
            }

            string hudText = "<b>Desktop Controls:</b>\n" +
                             "• Move: <b>[W][A][S][D]</b> | Sprint: <b>[Shift]</b> | Jump: <b>[Space]</b>\n" +
                             "• Look: <b>Mouse</b> | Interact/Click: <b>[E]</b> or <b>Left Click</b>\n" +
                             "• Teleport: <b>[T]</b> Lab 1 | <b>[K]</b> Lab 2 | <b>[L]</b> Lab 3 | <b>[M]</b> Lobby\n" +
                             "• Narration: <b>[P]</b> Replay Voice | Draw Mode: " + modeLabel + " [<b>U</b>] | Free Cursor: <b>[Esc]</b> | HUD: <b>[H]</b>" +
                             stationHint;

            float boxHeight = string.IsNullOrEmpty(stationHint) ? 120f : 195f;
            GUI.Box(new Rect(15, 15, 480, boxHeight), hudText, boxStyle);
        }

        // Active Narration Subtitle Banner
        if (NarrationManager.Instance != null && NarrationManager.Instance.IsPlaying)
        {
            GUI.color = new Color(0.1f, 0.85f, 1f, 0.95f);
            GUIStyle narrStyle = new GUIStyle(GUI.skin.box);
            narrStyle.fontSize = 12;
            narrStyle.alignment = TextAnchor.MiddleCenter;
            narrStyle.fontStyle = FontStyle.Bold;
            narrStyle.normal.textColor = Color.white;
            GUI.Box(new Rect(Screen.width / 2f - 250, 18, 500, 32), $"🔊 Instructor: {NarrationManager.Instance.currentNarrationTitle}", narrStyle);
        }
    }

    private void RenderDiagnosticGUI()
    {
        int boxW = 420;
        int boxH = 150;
        int x = Screen.width - boxW - 20;
        int y = 20;

        GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.normal.background = Texture2D.whiteTexture;

        Color prevColor = GUI.color;
        GUI.color = new Color(0.08f, 0.09f, 0.11f, 0.90f);
        GUI.Box(new Rect(x, y, boxW, boxH), GUIContent.none, boxStyle);

        // Border: Champagne Gold
        GUI.color = new Color(0.84f, 0.71f, 0.43f, 0.95f);
        GUI.Box(new Rect(x, y, boxW, 2), GUIContent.none);
        GUI.Box(new Rect(x, y + boxH - 2, boxW, 2), GUIContent.none);
        GUI.Box(new Rect(x, y, 2, boxH), GUIContent.none);
        GUI.Box(new Rect(x + boxW - 2, y, 2, boxH), GUIContent.none);

        GUIStyle headerStyle = new GUIStyle(GUI.skin.label);
        headerStyle.fontSize = 12;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.normal.textColor = new Color(0.84f, 0.71f, 0.43f);

        GUIStyle textStyle = new GUIStyle(GUI.skin.label);
        textStyle.fontSize = 11;
        textStyle.normal.textColor = Color.white;
        textStyle.richText = true;

        GUI.Label(new Rect(x + 12, y + 8, boxW - 24, 20), "META QUEST 3S NAVIGATION DIAGNOSTICS", headerStyle);

        int nextIdx = (currentStationIndex + 1) % 4;
        string bStatus = lastRightBInputDetected ? "<color=#00FFAA><b>[PRESSED - TELEPORTING]</b></color>" : "<color=#D0D0D0>[RELEASED - READY]</color>";
        string xStatus = lastLeftXInputDetected ? "<color=#00FFAA><b>[PRESSED - TOGGLING]</b></color>" : "<color=#D0D0D0>[RELEASED]</color>";

        GUI.Label(new Rect(x + 12, y + 30, boxW - 24, 18), $"Current Room: <b>{GetStationName(currentStationIndex)}</b>", textStyle);
        GUI.Label(new Rect(x + 12, y + 48, boxW - 24, 18), $"Right Controller [B] Button: {bStatus}", textStyle);
        GUI.Label(new Rect(x + 12, y + 66, boxW - 24, 18), $"Next Destination on [B]: <b>{GetStationName(nextIdx)}</b>", textStyle);
        GUI.Label(new Rect(x + 12, y + 84, boxW - 24, 18), $"Left Controller [X] Button: {xStatus} (Panel)", textStyle);
        GUI.Label(new Rect(x + 12, y + 102, boxW - 24, 18), $"Ray Teleport: <color=#FF8866>DISABLED</color> | Jumps Executed: <b>{roomSwitchCount}</b>", textStyle);
        GUI.Label(new Rect(x + 12, y + 120, boxW - 24, 18), "Keys: [B] Next | [M] Lobby | [T] Lab1 | [K] Lab2 | [L] Lab3 | [N] Menu", textStyle);

        GUI.color = prevColor;
    }
}
