using System.Collections.Generic;
using UnityEngine;

// Explicit aliases to eliminate any ambiguity
using XRInputDevice = UnityEngine.XR.InputDevice;
using XRInputDevices = UnityEngine.XR.InputDevices;
using XRCommonUsages = UnityEngine.XR.CommonUsages;
using XRNode = UnityEngine.XR.XRNode;
using XRDeviceCharacteristics = UnityEngine.XR.InputDeviceCharacteristics;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Natural VR Controller Teleportation & Floating Destination Menu for Meta Quest 3S controllers & Desktop fallback.
/// - Hold Right Controller Thumbstick Up or Grip to aim parabolic curved teleport ray.
/// - Valid floor surfaces display a glowing cyan landing reticle.
/// - Release to teleport the complete XR rig safely without penetrating walls or obstacles.
/// - Floating VR Destination Menu accessible via Left Controller Primary Button (X/Y) or Menu Button (or Tab / V on Desktop).
/// - Four instant VR destinations: Lab 1 (1st Law), Lab 2 (2nd Law), Lab 3 (3rd Law), MainLobby.
/// </summary>
public class VRTeleportController : MonoBehaviour
{
    [Header("Player & XR Rig References")]
    public CharacterController characterController;
    public Camera playerCamera;
    public Transform rightHandController;
    public Transform leftHandController;

    [Header("Teleport Arc Settings")]
    [Tooltip("Enable ray-based parabolic teleportation. Disabled by default in favor of instant Touch Plus B-button room navigation.")]
    public bool enableRayTeleport = false;

    [Tooltip("Number of sample points along the parabolic arc.")]
    public int arcSegments = 32;

    [Tooltip("Initial forward launch velocity of the arc.")]
    public float arcVelocity = 11.0f;

    [Tooltip("Simulated gravity pull on the arc.")]
    public float arcGravity = 18.0f;

    [Tooltip("Maximum teleport range in meters.")]
    public float maxTeleportDistance = 25.0f;

    [Tooltip("Minimum upward normal to consider surface a valid floor.")]
    public float minFloorNormalY = 0.65f;

    [Header("Safe Destination Spawn Points")]
    public Vector3 lab1SpawnPosition = new Vector3(0f, 0f, -41.0f);
    public Vector3 lab1SpawnRotation = new Vector3(0f, 180f, 0f);

    public Vector3 lab2SpawnPosition = new Vector3(-41.8f, 0f, 0f);
    public Vector3 lab2SpawnRotation = new Vector3(0f, -90f, 0f);

    public Vector3 lab3SpawnPosition = new Vector3(41.8f, 0f, 0f);
    public Vector3 lab3SpawnRotation = new Vector3(0f, 90f, 0f);

    public Vector3 mainLobbySpawnPosition = new Vector3(0f, 0f, 5.80f);
    public Vector3 mainLobbySpawnRotation = new Vector3(0f, 180f, 0f);

    public Vector3 room4SpawnPosition = new Vector3(0f, 0.02f, 42.50f);
    public Vector3 room4SpawnRotation = new Vector3(0f, 0f, 0f);

    // Runtime Visuals
    private LineRenderer arcLineRenderer;
    private GameObject landingReticle;
    private Material reticleMaterial;
    private Material arcMaterial;

    // State Tracking
    private bool isAimingTeleport = false;
    public bool IsAimingTeleport => isAimingTeleport;
    private bool hasValidTarget = false;
    private Vector3 targetPosition;
    private Vector3 targetNormal;

    // Floating Menu
    private GameObject floatingMenuRoot;
    private bool isMenuOpen = false;
    private List<MenuButtonData> menuButtons = new List<MenuButtonData>();

    private struct MenuButtonData
    {
        public GameObject buttonObj;
        public System.Action onClick;
        public string label;
        public Renderer renderer;
        public Material defaultMat;
        public Material hoverMat;
    }

    void Awake()
    {
        if (characterController == null) characterController = GetComponent<CharacterController>();
        if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();

        SetupArcVisuals();
        SetupLandingReticle();
        // Legacy basic menu disabled in favor of clean World Space FloatingVRDestinationPanel
    }

    private void SetupArcVisuals()
    {
        GameObject arcObj = new GameObject("VR_TeleportArc");
        arcObj.transform.SetParent(transform, false);

        arcLineRenderer = arcObj.AddComponent<LineRenderer>();
        arcLineRenderer.positionCount = arcSegments;
        arcLineRenderer.startWidth = 0.035f;
        arcLineRenderer.endWidth = 0.012f;
        arcLineRenderer.useWorldSpace = true;
        arcLineRenderer.enabled = false;

        Shader shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        arcMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        arcMaterial.color = new Color(0.05f, 0.88f, 1.0f, 0.9f);
        arcLineRenderer.material = arcMaterial;

        // Gradient: Cyan to transparent
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(new Color(0.1f, 0.9f, 1.0f), 0.0f), new GradientColorKey(new Color(0.0f, 0.6f, 1.0f), 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.85f, 0.0f), new GradientAlphaKey(0.95f, 0.8f), new GradientAlphaKey(0.2f, 1.0f) }
        );
        arcLineRenderer.colorGradient = gradient;
    }

    private void SetupLandingReticle()
    {
        landingReticle = new GameObject("VR_TeleportLandingReticle");
        landingReticle.transform.SetParent(transform, false);

        // Flat cylinder disc for landing
        GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disc.transform.SetParent(landingReticle.transform, false);
        disc.transform.localScale = new Vector3(0.75f, 0.015f, 0.75f);
        disc.transform.localPosition = Vector3.zero;

        Collider col = disc.GetComponent<Collider>();
        if (col != null)
        {
            if (characterController != null) Physics.IgnoreCollision(characterController, col, true);
            DestroyImmediate(col);
        }

        Shader shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Unlit/Color");

        reticleMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        reticleMaterial.color = new Color(0.05f, 0.85f, 1.0f, 0.8f);
        if (reticleMaterial.HasProperty("_EmissionColor"))
        {
            reticleMaterial.EnableKeyword("_EMISSION");
            reticleMaterial.SetColor("_EmissionColor", new Color(0.05f, 0.85f, 1.0f) * 1.5f);
        }

        disc.GetComponent<MeshRenderer>().sharedMaterial = reticleMaterial;

        // Inner glowing ring/indicator
        GameObject innerRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        innerRing.transform.SetParent(landingReticle.transform, false);
        innerRing.transform.localScale = new Vector3(0.35f, 0.02f, 0.35f);
        innerRing.transform.localPosition = new Vector3(0f, 0.005f, 0f);
        Collider innerCol = innerRing.GetComponent<Collider>();
        if (innerCol != null)
        {
            if (characterController != null) Physics.IgnoreCollision(characterController, innerCol, true);
            DestroyImmediate(innerCol);
        }

        Material innerMat = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        innerMat.color = Color.white;
        if (innerMat.HasProperty("_EmissionColor"))
        {
            innerMat.EnableKeyword("_EMISSION");
            innerMat.SetColor("_EmissionColor", Color.white * 1.8f);
        }
        innerRing.GetComponent<MeshRenderer>().sharedMaterial = innerMat;

        landingReticle.SetActive(false);
    }

    private void CreateFloatingDestinationMenu()
    {
        floatingMenuRoot = new GameObject("VR_DestinationMenu");
        floatingMenuRoot.transform.SetParent(transform, false);

        // Futuristic Backing Panel
        GameObject backplate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        backplate.name = "Menu_Backplate";
        backplate.transform.SetParent(floatingMenuRoot.transform, false);
        backplate.transform.localPosition = Vector3.zero;
        backplate.transform.localScale = new Vector3(1.15f, 0.85f, 0.035f);

        Collider bc = backplate.GetComponent<Collider>();
        if (bc) Destroy(bc);

        Shader standardShader = Shader.Find("Standard");
        Material backMat = new Material(standardShader) { hideFlags = HideFlags.HideAndDontSave };
        backMat.color = new Color(0.015f, 0.035f, 0.08f, 0.95f);
        backMat.SetFloat("_Metallic", 0.3f);
        backMat.SetFloat("_Glossiness", 0.8f);
        backplate.GetComponent<MeshRenderer>().sharedMaterial = backMat;

        // Glowing Trim
        GameObject trim = GameObject.CreatePrimitive(PrimitiveType.Cube);
        trim.name = "Menu_Trim";
        trim.transform.SetParent(floatingMenuRoot.transform, false);
        trim.transform.localPosition = new Vector3(0f, 0f, -0.005f);
        trim.transform.localScale = new Vector3(1.18f, 0.88f, 0.02f);
        Collider tc = trim.GetComponent<Collider>();
        if (tc) Destroy(tc);

        Material trimMat = new Material(standardShader) { hideFlags = HideFlags.HideAndDontSave };
        Color cyanTrim = new Color(0.05f, 0.85f, 1.0f);
        trimMat.color = cyanTrim;
        trimMat.EnableKeyword("_EMISSION");
        trimMat.SetColor("_EmissionColor", cyanTrim * 1.4f);
        trim.GetComponent<MeshRenderer>().sharedMaterial = trimMat;

        // Title text
        GameObject titleObj = new GameObject("Menu_Title");
        titleObj.transform.SetParent(floatingMenuRoot.transform, false);
        titleObj.transform.localPosition = new Vector3(0f, 0.32f, -0.025f);
        TextMesh titleTm = titleObj.AddComponent<TextMesh>();
        titleTm.text = "VR TELEPORT NAVIGATION";
        titleTm.fontSize = 28;
        titleTm.characterSize = 0.016f;
        titleTm.alignment = TextAlignment.Center;
        titleTm.anchor = TextAnchor.MiddleCenter;
        titleTm.color = cyanTrim;
        titleTm.fontStyle = FontStyle.Bold;

        // Create 4 Destination Buttons
        CreateMenuButton("LAB 1 — Newton's 1st Law", new Vector3(0f, 0.16f, -0.025f), new Color(0.05f, 0.85f, 1.0f), () => TeleportToDestination(lab1SpawnPosition, Quaternion.Euler(lab1SpawnRotation)));
        CreateMenuButton("LAB 2 — Newton's 2nd Law", new Vector3(0f, 0.03f, -0.025f), new Color(0.6f, 0.2f, 1.0f), () => TeleportToDestination(lab2SpawnPosition, Quaternion.Euler(lab2SpawnRotation)));
        CreateMenuButton("LAB 3 — Newton's 3rd Law", new Vector3(0f, -0.10f, -0.025f), new Color(1.0f, 0.65f, 0.1f), () => TeleportToDestination(lab3SpawnPosition, Quaternion.Euler(lab3SpawnRotation)));
        CreateMenuButton("MAIN LOBBY — Central Hall", new Vector3(0f, -0.23f, -0.025f), new Color(0.2f, 0.95f, 0.6f), () => TeleportToDestination(mainLobbySpawnPosition, Quaternion.Euler(mainLobbySpawnRotation)));

        floatingMenuRoot.SetActive(false);
    }

    private void CreateMenuButton(string label, Vector3 localPos, Color accentColor, System.Action onClickAction)
    {
        GameObject btnObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        btnObj.name = $"Button_{label.Replace(" ", "")}";
        btnObj.transform.SetParent(floatingMenuRoot.transform, false);
        btnObj.transform.localPosition = localPos;
        btnObj.transform.localScale = new Vector3(0.95f, 0.095f, 0.025f);

        Collider bCol = btnObj.GetComponent<Collider>();
        if (bCol != null && characterController != null)
        {
            Physics.IgnoreCollision(characterController, bCol, true);
        }

        Shader standardShader = Shader.Find("Standard");
        Material defMat = new Material(standardShader) { hideFlags = HideFlags.HideAndDontSave };
        defMat.color = new Color(0.04f, 0.08f, 0.15f);
        defMat.EnableKeyword("_EMISSION");
        defMat.SetColor("_EmissionColor", accentColor * 0.4f);

        Material hovMat = new Material(standardShader) { hideFlags = HideFlags.HideAndDontSave };
        hovMat.color = accentColor * 0.9f;
        hovMat.EnableKeyword("_EMISSION");
        hovMat.SetColor("_EmissionColor", accentColor * 1.6f);

        Renderer r = btnObj.GetComponent<Renderer>();
        r.sharedMaterial = defMat;

        // Label Text
        GameObject textObj = new GameObject("Button_Label");
        textObj.transform.SetParent(btnObj.transform, false);
        textObj.transform.localPosition = new Vector3(0f, 0f, -0.6f);
        TextMesh tm = textObj.AddComponent<TextMesh>();
        tm.text = label;
        tm.fontSize = 24;
        tm.characterSize = 0.028f;
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.color = Color.white;
        tm.fontStyle = FontStyle.Bold;

        MenuButtonData data = new MenuButtonData
        {
            buttonObj = btnObj,
            onClick = onClickAction,
            label = label,
            renderer = r,
            defaultMat = defMat,
            hoverMat = hovMat
        };
        menuButtons.Add(data);
    }

    void Update()
    {
        HandleMenuToggleInput();
        HandleMenuInteraction();
        HandleTeleportArcInput();
    }

    private void HandleMenuToggleInput()
    {
        // Suppressed legacy menu in favor of the clean World Space FloatingVRDestinationPanel
    }

    public void ToggleDestinationMenu()
    {
        FloatingVRDestinationPanel panel = FindFirstObjectByType<FloatingVRDestinationPanel>();
        if (panel != null) panel.TogglePanel();
    }

    private void HandleMenuInteraction()
    {
        // Suppressed legacy menu in favor of the clean World Space FloatingVRDestinationPanel
    }

    private bool isAimingFromRight = false;
    private bool externalAimOverride = false;

    public void SetExternalTeleportAim(bool aiming)
    {
        externalAimOverride = aiming;
    }

    private void HandleTeleportArcInput()
    {
        if (!enableRayTeleport)
        {
            if (arcLineRenderer != null && arcLineRenderer.enabled) arcLineRenderer.enabled = false;
            if (landingReticle != null && landingReticle.activeSelf) landingReticle.SetActive(false);
            isAimingTeleport = false;
            hasValidTarget = false;
            return;
        }

        bool aimRequested = false;

        // 1. VR Mode: Hold Left Controller Thumbstick Click to aim teleport arc from Left Hand
        XRInputDevice leftHand = XRInputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        if (leftHand.isValid)
        {
            if (leftHand.TryGetFeatureValue(XRCommonUsages.primary2DAxisClick, out bool lStickClick) && lStickClick)
            {
                aimRequested = true;
                isAimingFromRight = false;
            }
        }

#if ENABLE_INPUT_SYSTEM
        // InputSystem fallback for Meta Quest Link: Left Controller Thumbstick Click
        if (!aimRequested)
        {
            foreach (var dev in InputSystem.devices)
            {
                if (!dev.added) continue;
                bool isLeft = false;
                for (int u = 0; u < dev.usages.Count; u++)
                {
                    if (dev.usages[u] == UnityEngine.InputSystem.CommonUsages.LeftHand) { isLeft = true; break; }
                }
                string devName = dev.name;
                string devLayout = dev.layout;
                if ((devName.IndexOf("left", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                     devLayout.IndexOf("left", System.StringComparison.OrdinalIgnoreCase) >= 0) &&
                    (devName.IndexOf("right", System.StringComparison.OrdinalIgnoreCase) < 0))
                {
                    isLeft = true;
                }

                if (isLeft)
                {
                    var stickClick = dev.GetChildControl<UnityEngine.InputSystem.Controls.ButtonControl>("thumbstickClicked")
                                     ?? dev.GetChildControl<UnityEngine.InputSystem.Controls.ButtonControl>("primary2DAxisClick");
                    if (stickClick != null && stickClick.isPressed)
                    {
                        aimRequested = true;
                        isAimingFromRight = false;
                        break;
                    }
                }
            }
        }
#endif

        // Allow PlayerController to drive aim state directly if detected
        if (externalAimOverride)
        {
            aimRequested = true;
            isAimingFromRight = false;
        }

        // 2. Desktop Fallback: Hold Right Mouse Button or Key R (aims from camera)
        PlayerController pc = GetComponent<PlayerController>();
        bool vrActive = pc != null ? pc.IsVRActive : false;
        if (!vrActive && !aimRequested)
        {
            if (NewtonInput.GetMouseButton(1) || NewtonInput.GetKey(KeyCode.R))
            {
                aimRequested = true;
                isAimingFromRight = false;
            }
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.rKey.isPressed) { aimRequested = true; isAimingFromRight = false; }
            if (Mouse.current != null && Mouse.current.rightButton.isPressed) { aimRequested = true; isAimingFromRight = false; }
#endif
        }

        Ray startRay = GetTeleportOriginRay(isAimingFromRight);

        if (aimRequested)
        {
            isAimingTeleport = true;
            UpdateTeleportArc(startRay);
        }
        else if (isAimingTeleport)
        {
            // Released input -> Execute teleport if target valid
            if (hasValidTarget)
            {
                ExecuteTeleport(targetPosition);
            }

            // Hide arc & reticle
            isAimingTeleport = false;
            hasValidTarget = false;
            if (arcLineRenderer != null) arcLineRenderer.enabled = false;
            if (landingReticle != null) landingReticle.SetActive(false);
        }
    }

    private Ray GetTeleportOriginRay(bool fromRight)
    {
        Transform handCtrl = fromRight ? rightHandController : leftHandController;
        if (handCtrl == null) handCtrl = leftHandController != null ? leftHandController : rightHandController;

        if (handCtrl != null)
        {
            // Offset 8cm along forward from controller
            Vector3 origin = handCtrl.position + handCtrl.forward * 0.08f;
            Vector3 direction = Quaternion.AngleAxis(12f, handCtrl.right) * handCtrl.forward;
            return new Ray(origin, direction);
        }
        else if (playerCamera != null)
        {
            return new Ray(playerCamera.transform.position + playerCamera.transform.forward * 0.2f, playerCamera.transform.forward);
        }
        return new Ray(transform.position + Vector3.up * 1.5f + transform.forward * 0.3f, transform.forward);
    }

    private void UpdateTeleportArc(Ray startRay)
    {
        if (arcLineRenderer == null) return;
        arcLineRenderer.enabled = true;

        Vector3 currentPos = startRay.origin;
        Vector3 currentVelocity = startRay.direction.normalized * arcVelocity;
        float timeStep = 0.035f;

        List<Vector3> points = new List<Vector3>();
        points.Add(currentPos);

        hasValidTarget = false;

        for (int i = 1; i < arcSegments; i++)
        {
            Vector3 nextPos = currentPos + currentVelocity * timeStep;
            currentVelocity += Vector3.down * (arcGravity * timeStep);

            Vector3 stepDir = nextPos - currentPos;
            float stepDist = stepDir.magnitude;

            // Use RaycastAll to filter out self-collisions (player capsule, hands, triggers)
            RaycastHit[] hits = Physics.RaycastAll(currentPos, stepDir.normalized, stepDist);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            RaycastHit hit = default;
            bool hitFound = false;

            for (int h = 0; h < hits.Length; h++)
            {
                Collider c = hits[h].collider;
                if (c == null) continue;
                if (c == characterController) continue;
                if (c.transform.IsChildOf(transform)) continue;
                if (c.isTrigger) continue;

                hit = hits[h];
                hitFound = true;
                break;
            }

            if (hitFound)
            {
                points.Add(hit.point);

                // Check if hit surface is a valid walkable floor (normal facing upwards)
                if (hit.normal.y >= minFloorNormalY && hit.distance <= maxTeleportDistance)
                {
                    hasValidTarget = true;
                    targetPosition = hit.point;
                    targetNormal = hit.normal;

                    if (landingReticle != null)
                    {
                        landingReticle.SetActive(true);
                        landingReticle.transform.position = hit.point + hit.normal * 0.015f;
                        landingReticle.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);

                        // Glowing cyan for valid floor
                        if (reticleMaterial != null)
                        {
                            reticleMaterial.color = new Color(0.05f, 0.85f, 1.0f, 0.85f);
                            if (reticleMaterial.HasProperty("_EmissionColor"))
                                reticleMaterial.SetColor("_EmissionColor", new Color(0.05f, 0.85f, 1.0f) * 1.5f);
                        }
                    }
                }
                else
                {
                    hasValidTarget = false;
                    if (landingReticle != null)
                    {
                        landingReticle.SetActive(true);
                        landingReticle.transform.position = hit.point + hit.normal * 0.015f;
                        landingReticle.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);

                        // Red tint if hitting walls/obstacles
                        if (reticleMaterial != null)
                        {
                            reticleMaterial.color = new Color(1.0f, 0.1f, 0.1f, 0.8f);
                            if (reticleMaterial.HasProperty("_EmissionColor"))
                                reticleMaterial.SetColor("_EmissionColor", new Color(1.0f, 0.1f, 0.1f) * 1.5f);
                        }
                    }
                }
                break;
            }

            points.Add(nextPos);
            currentPos = nextPos;
        }

        arcLineRenderer.positionCount = points.Count;
        arcLineRenderer.SetPositions(points.ToArray());

        if (!hasValidTarget && landingReticle != null)
        {
            landingReticle.SetActive(false);
        }
    }

    public void ExecuteTeleport(Vector3 destination)
    {
        if (characterController != null) characterController.enabled = false;

        // Maintain horizontal head offset so the user's head lands exactly where they aimed
        Vector3 headOffset = Vector3.zero;
        if (playerCamera != null)
        {
            headOffset = playerCamera.transform.position - transform.position;
            headOffset.y = 0f;
        }

        // Set player root cleanly at destination floor level (no mid-air drop or camera bouncing)
        Vector3 newPos = destination - headOffset;
        newPos.y = destination.y;
        transform.position = newPos;

        if (characterController != null) characterController.enabled = true;

        PlayerController pc = GetComponent<PlayerController>();
        if (pc != null) pc.ResetVerticalVelocity();

        Debug.Log($"[Newton VR] Teleported XR Rig cleanly to: {destination} (Root at {newPos})");
    }

    public void TeleportToDestination(Vector3 destination, Quaternion orientation)
    {
        if (characterController != null) characterController.enabled = false;

        Vector3 headOffset = Vector3.zero;
        if (playerCamera != null)
        {
            headOffset = playerCamera.transform.position - transform.position;
            headOffset.y = 0f;
        }

        transform.position = destination - headOffset;
        transform.rotation = orientation;

        if (characterController != null) characterController.enabled = true;

        PlayerController pc = GetComponent<PlayerController>();
        if (pc != null) pc.ResetVerticalVelocity();
        Debug.Log($"[Newton VR] Teleported XR Rig to destination {destination} with orientation {orientation.eulerAngles}");
    }
}

