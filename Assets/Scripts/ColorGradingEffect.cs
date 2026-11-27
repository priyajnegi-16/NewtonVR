using UnityEngine;

/// <summary>
/// Studio-quality Color Grading & Tone Mapping component for Camera rendering.
/// Configured to never interfere with Unity Scene View or Edit Mode.
[RequireComponent(typeof(Camera))]
[AddComponentMenu("Rendering/Newton Color Grading")]
public class ColorGradingEffect : MonoBehaviour
{
    [Header("Exposure & Tone")]
    [Tooltip("Adjusts overall exposure in stops (EV). Negative = darker, Positive = brighter.")]
    [Range(-3.0f, 3.0f)]
    public float exposure = 0.0f;

    [Tooltip("Expands or compresses the range of tonal values.")]
    [Range(0.2f, 2.5f)]
    public float contrast = 1.05f;

    [Tooltip("Purity or intensity of colors. 0 = Black & White, 1 = Normal, >1 = Vibrance.")]
    [Range(0.0f, 2.5f)]
    public float saturation = 1.05f;

    [Header("White Balance")]
    [Tooltip("Shifts color temperature: Negative = Cool/Blue, Positive = Warm/Orange.")]
    [Range(-1.0f, 1.0f)]
    public float temperature = 0.05f;

    [Tooltip("Fine-tunes color tint: Negative = Green, Positive = Magenta.")]
    [Range(-1.0f, 1.0f)]
    public float tint = 0.0f;

    [Tooltip("Multiplies the render output by a custom color tint.")]
    public Color colorFilter = Color.white;

    [Header("Cinematic Vignette")]
    [Tooltip("Darkens edges of the screen to draw focus to the center.")]
    [Range(0.0f, 1.0f)]
    public float vignetteIntensity = 0.25f;

    [Tooltip("Smoothness of the vignette falloff.")]
    [Range(0.05f, 1.0f)]
    public float vignetteSmoothness = 0.45f;

    private Material material;
    private Shader shader;

    private static readonly int ExposureID = Shader.PropertyToID("_Exposure");
    private static readonly int ContrastID = Shader.PropertyToID("_Contrast");
    private static readonly int SaturationID = Shader.PropertyToID("_Saturation");
    private static readonly int TemperatureID = Shader.PropertyToID("_Temperature");
    private static readonly int TintID = Shader.PropertyToID("_Tint");
    private static readonly int ColorFilterID = Shader.PropertyToID("_ColorFilter");
    private static readonly int VignetteIntensityID = Shader.PropertyToID("_VignetteIntensity");
    private static readonly int VignetteSmoothnessID = Shader.PropertyToID("_VignetteSmoothness");

    void Awake()
    {
        // On VR headsets / Meta Quest Link / Android XR, disable fullscreen post-processing blit passes to eliminate GPU buffering and judder
        if (Application.isMobilePlatform || UnityEngine.XR.XRSettings.isDeviceActive)
        {
            enabled = false;
        }
    }

    void OnEnable()
    {
        if (Application.isMobilePlatform || UnityEngine.XR.XRSettings.isDeviceActive)
        {
            enabled = false;
            return;
        }
        InitMaterial();
    }

    void OnValidate()
    {
        InitMaterial();
    }

    void OnDisable()
    {
        if (material != null)
        {
            DestroyImmediate(material);
            material = null;
        }
    }

    private void InitMaterial()
    {
        if (shader == null)
        {
            shader = Shader.Find("Hidden/NewtonVR/ColorGrading");
        }

        if (shader != null && material == null)
        {
            material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        }
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (Application.isMobilePlatform || UnityEngine.XR.XRSettings.isDeviceActive)
        {
            Graphics.Blit(source, destination);
            enabled = false;
            return;
        }

        if (!Application.isPlaying)
        {
            Graphics.Blit(source, destination);
            return;
        }

        if (material == null)
        {
            InitMaterial();
        }

        if (material == null || shader == null || !shader.isSupported)
        {
            Graphics.Blit(source, destination);
            return;
        }

        material.SetFloat(ExposureID, exposure);
        material.SetFloat(ContrastID, contrast);
        material.SetFloat(SaturationID, saturation);
        material.SetFloat(TemperatureID, temperature);
        material.SetFloat(TintID, tint);
        material.SetColor(ColorFilterID, colorFilter);
        material.SetFloat(VignetteIntensityID, vignetteIntensity);
        material.SetFloat(VignetteSmoothnessID, vignetteSmoothness);

        Graphics.Blit(source, destination, material);
    }
}
