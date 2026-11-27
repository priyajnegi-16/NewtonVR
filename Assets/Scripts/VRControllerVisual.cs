using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// Renders a responsive 3D VR controller for Left and Right hands.
/// Automatically handles visual representation, tracking, and button animations in VR.
/// </summary>
public class VRControllerVisual : MonoBehaviour
{
    public enum HandType { Left, Right }
    [Tooltip("Whether this visual represents the Left or Right hand controller.")]
    public HandType hand = HandType.Left;

    [Tooltip("Color of the controller body.")]
    public Color bodyColor = new Color(0.12f, 0.12f, 0.14f);

    [Tooltip("Accent glow color for this controller.")]
    public Color accentColor = new Color(0.1f, 0.75f, 1.0f); // Cyan for Left

    [Tooltip("Show forward pointing ray for aiming and direction reference.")]
    public bool showPointerRay = true;

    private Transform stickTransform;
    private Transform triggerTransform;
    private LineRenderer pointerRay;

    void Awake()
    {
        if (hand == HandType.Right)
        {
            accentColor = new Color(1.0f, 0.45f, 0.1f); // Orange for Right
        }
        BuildProceduralController();
    }

    private void BuildProceduralController()
    {
        // 1. Controller Grip Body (Capsule)
        GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        handle.name = "GripHandle";
        handle.transform.SetParent(transform, false);
        handle.transform.localPosition = new Vector3(0f, -0.05f, -0.04f);
        handle.transform.localRotation = Quaternion.Euler(25f, 0f, 0f);
        handle.transform.localScale = new Vector3(0.035f, 0.065f, 0.035f);
        RemoveCollider(handle);
        SetColor(handle, bodyColor, 0.85f, 0.2f);

        // 2. Tracking Sensor Ring (Forward canted ring)
        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "TrackingRing";
        ring.transform.SetParent(transform, false);
        ring.transform.localPosition = new Vector3(0f, 0.02f, 0.02f);
        ring.transform.localRotation = Quaternion.Euler(-30f, 0f, 0f);
        ring.transform.localScale = new Vector3(0.09f, 0.008f, 0.09f);
        RemoveCollider(ring);
        SetColor(ring, bodyColor * 1.3f, 0.9f, 0.3f);

        // 3. Controller Top Faceplate
        GameObject faceplate = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        faceplate.name = "Faceplate";
        faceplate.transform.SetParent(transform, false);
        faceplate.transform.localPosition = new Vector3(0f, 0.015f, -0.01f);
        faceplate.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);
        faceplate.transform.localScale = new Vector3(0.045f, 0.005f, 0.045f);
        RemoveCollider(faceplate);
        SetColor(faceplate, bodyColor * 0.9f, 0.7f, 0.1f);

        // 4. Thumbstick
        GameObject stick = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        stick.name = "Thumbstick";
        stick.transform.SetParent(faceplate.transform, false);
        stick.transform.localPosition = new Vector3(0f, 1.2f, 0.1f);
        stick.transform.localRotation = Quaternion.identity;
        stick.transform.localScale = new Vector3(0.35f, 0.8f, 0.35f);
        RemoveCollider(stick);
        SetColor(stick, Color.black, 0.95f, 0.1f);
        stickTransform = stick.transform;

        // 5. Glowing LED Ring Accent
        GameObject led = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        led.name = "StatusLED";
        led.transform.SetParent(transform, false);
        led.transform.localPosition = new Vector3(0f, 0.012f, 0.035f);
        led.transform.localScale = new Vector3(0.012f, 0.008f, 0.012f);
        RemoveCollider(led);
        SetGlowColor(led, accentColor);

        // 6. Forward Pointer Ray
        if (showPointerRay)
        {
            GameObject rayObj = new GameObject("PointerRay");
            rayObj.transform.SetParent(transform, false);
            rayObj.transform.localPosition = new Vector3(0f, 0.01f, 0.04f);

            pointerRay = rayObj.AddComponent<LineRenderer>();
            pointerRay.useWorldSpace = false;
            pointerRay.startWidth = 0.005f;
            pointerRay.endWidth = 0.002f;
            pointerRay.positionCount = 2;
            pointerRay.SetPosition(0, Vector3.zero);
            pointerRay.SetPosition(1, new Vector3(0f, 0f, 5.0f));

            Material rayMat = new Material(Shader.Find("Sprites/Default"));
            Color rayCol = new Color(0.0f, 0.88f, 1.0f, 0.90f); // Luminous electric cyan laser (reference image)
            rayMat.color = Color.white;
            pointerRay.material = rayMat;
            pointerRay.startColor = rayCol;
            Color rayEndCol = new Color(0.125f, 0.95f, 0.75f, 0.35f);
            pointerRay.endColor = rayEndCol;
            pointerRay.startWidth = 0.0055f;
            pointerRay.endWidth = 0.0020f;
            pointerRay.enabled = false; // Start hidden to avoid view clutter; only show when aiming
        }
    }

    public void SetPointerRayState(bool active, float length = 3.0f)
    {
        if (pointerRay == null) return;
        pointerRay.enabled = active;
        if (active)
        {
            pointerRay.SetPosition(0, Vector3.zero);
            pointerRay.SetPosition(1, new Vector3(0f, 0f, Mathf.Max(0.1f, length)));
        }
    }

    private void RemoveCollider(GameObject go)
    {
        Collider c = go.GetComponent<Collider>();
        if (c != null)
        {
            DestroyImmediate(c);
        }
    }

    private void SetColor(GameObject go, Color color, float metallic, float smoothness)
    {
        Renderer r = go.GetComponent<Renderer>();
        if (r != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = color;
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Glossiness", smoothness);
            r.material = mat;
        }
    }

    private void SetGlowColor(GameObject go, Color glowColor)
    {
        Renderer r = go.GetComponent<Renderer>();
        if (r != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = glowColor;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", glowColor * 1.8f);
            r.material = mat;
        }
    }

    /// <summary>
    /// Animates the controller visual based on live controller input.
    /// </summary>
    public void UpdateInputVisuals(Vector2 stickAxis, float triggerVal)
    {
        if (stickTransform != null)
        {
            float maxTilt = 20f;
            stickTransform.localRotation = Quaternion.Euler(stickAxis.y * maxTilt, 0f, -stickAxis.x * maxTilt);
        }
    }

    public void SetVisibility(bool visible)
    {
        gameObject.SetActive(visible);
    }
}
