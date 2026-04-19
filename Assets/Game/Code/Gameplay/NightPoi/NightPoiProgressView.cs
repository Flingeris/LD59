using UnityEngine;

[DisallowMultipleComponent]
public class NightPoiProgressView : MonoBehaviour
{
    [SerializeField] private NightPointOfInterest pointOfInterest;
    [SerializeField] private WorldBarFillGrow progressBar;

    private void Awake()
    {
        EnsureReferences();
        progressBar?.SetVisible(false);
    }

    private void OnDisable()
    {
        progressBar?.SetVisible(false);
    }

    private void Update()
    {
        EnsureReferences();

        if (progressBar == null || pointOfInterest == null || G.main == null)
        {
            progressBar?.SetVisible(false);
            return;
        }

        if (!G.main.TryGetNightPoiProgress(pointOfInterest.Id, pointOfInterest.Type, out var normalizedProgress))
        {
            progressBar.SetVisible(false);
            return;
        }

        progressBar.SetVisible(true);
        progressBar.SetNormalized(normalizedProgress);
    }

    public void Bind(NightPointOfInterest poi)
    {
        pointOfInterest = poi;
        EnsureReferences();
    }

    private void EnsureReferences()
    {
        if (pointOfInterest == null)
            pointOfInterest = GetComponentInParent<NightPointOfInterest>();

        if (progressBar == null)
            progressBar = GetComponentInChildren<WorldBarFillGrow>();
    }
}