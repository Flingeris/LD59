using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[ExecuteAlways]
[DisallowMultipleComponent]
public class NightAtmosphereVolumeController : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;

    [Header("Bloom")]
    [SerializeField] private bool bloomEnabled = true;
    [Min(0f)] [SerializeField] private float bloomThreshold = 1.1f;
    [Min(0f)] [SerializeField] private float bloomIntensity = 0.12f;
    [Range(0f, 1f)] [SerializeField] private float bloomScatter = 0.35f;

    [Header("Color Adjustments")]
    [SerializeField] private float postExposure = -0.05f;
    [Range(-100f, 100f)] [SerializeField] private float contrast = 6f;
    [ColorUsage(false, true)] [SerializeField] private Color colorFilter = new(0.93f, 0.96f, 1f, 1f);

    [Header("Vignette")]
    [SerializeField] private bool vignetteEnabled = true;
    [Range(0f, 1f)] [SerializeField] private float vignetteIntensity = 0.18f;
    [Range(0.01f, 1f)] [SerializeField] private float vignetteSmoothness = 0.35f;

    private Volume volume;

    private void Reset()
    {
        RefreshAtmosphere();
    }

    private void OnEnable()
    {
        RefreshAtmosphere();
    }

    private void OnValidate()
    {
        RefreshAtmosphere();
    }

    private void RefreshAtmosphere()
    {
        EnsureCameraPostProcessing();

        var targetVolume = GetOrCreateVolume();
        if (targetVolume == null)
        {
            return;
        }

        targetVolume.isGlobal = true;
        targetVolume.priority = 0f;
        targetVolume.blendDistance = 0f;
        targetVolume.weight = 1f;

        var profile = targetVolume.profile;
        if (profile == null)
        {
            return;
        }

        ConfigureBloom(profile);
        ConfigureColorAdjustments(profile);
        ConfigureVignette(profile);
    }

    private void EnsureCameraPostProcessing()
    {
        var cameraToConfigure = ResolveTargetCamera();
        if (cameraToConfigure == null)
        {
            return;
        }

        var cameraData = cameraToConfigure.GetUniversalAdditionalCameraData();
        if (!cameraData.renderPostProcessing)
        {
            cameraData.renderPostProcessing = true;
        }
    }

    private Camera ResolveTargetCamera()
    {
        if (targetCamera != null)
        {
            return targetCamera;
        }

        targetCamera = Camera.main;
        if (targetCamera == null)
        {
            targetCamera = FindFirstObjectByType<Camera>();
        }

        return targetCamera;
    }

    private Volume GetOrCreateVolume()
    {
        if (volume == null && !TryGetComponent(out volume))
        {
            volume = gameObject.AddComponent<Volume>();
        }

        return volume;
    }

    private void ConfigureBloom(VolumeProfile profile)
    {
        var bloom = GetOrAddComponent<Bloom>(profile);
        bloom.active = bloomEnabled;
        bloom.threshold.overrideState = true;
        bloom.threshold.value = Mathf.Max(0f, bloomThreshold);
        bloom.intensity.overrideState = true;
        bloom.intensity.value = Mathf.Max(0f, bloomIntensity);
        bloom.scatter.overrideState = true;
        bloom.scatter.value = Mathf.Clamp01(bloomScatter);
        bloom.highQualityFiltering.overrideState = true;
        bloom.highQualityFiltering.value = false;
        bloom.filter.overrideState = true;
        bloom.filter.value = BloomFilterMode.Gaussian;
        bloom.maxIterations.overrideState = true;
        bloom.maxIterations.value = 4;
        bloom.tint.overrideState = true;
        bloom.tint.value = Color.white;
        bloom.dirtIntensity.overrideState = true;
        bloom.dirtIntensity.value = 0f;
    }

    private void ConfigureColorAdjustments(VolumeProfile profile)
    {
        var adjustments = GetOrAddComponent<ColorAdjustments>(profile);
        adjustments.active = true;
        adjustments.postExposure.overrideState = true;
        adjustments.postExposure.value = postExposure;
        adjustments.contrast.overrideState = true;
        adjustments.contrast.value = Mathf.Clamp(contrast, -100f, 100f);
        adjustments.colorFilter.overrideState = true;
        adjustments.colorFilter.value = colorFilter;
        adjustments.hueShift.overrideState = true;
        adjustments.hueShift.value = 0f;
        adjustments.saturation.overrideState = true;
        adjustments.saturation.value = 0f;
    }

    private void ConfigureVignette(VolumeProfile profile)
    {
        var vignette = GetOrAddComponent<Vignette>(profile);
        vignette.active = vignetteEnabled;
        vignette.color.overrideState = true;
        vignette.color.value = Color.black;
        vignette.center.overrideState = true;
        vignette.center.value = new Vector2(0.5f, 0.5f);
        vignette.intensity.overrideState = true;
        vignette.intensity.value = Mathf.Clamp01(vignetteIntensity);
        vignette.smoothness.overrideState = true;
        vignette.smoothness.value = Mathf.Clamp(vignetteSmoothness, 0.01f, 1f);
        vignette.rounded.overrideState = true;
        vignette.rounded.value = false;
    }

    private static T GetOrAddComponent<T>(VolumeProfile profile) where T : VolumeComponent
    {
        if (profile.TryGet<T>(out var component) && component != null)
        {
            return component;
        }

        return profile.Add<T>(true);
    }
}
