using UnityEngine;

[DisallowMultipleComponent]
public class NightPoiProgressView : MonoBehaviour
{
    [SerializeField] private NightPointOfInterest pointOfInterest;
    [SerializeField] private WorldProgressBarPresenter progressBarPresenter;
    [SerializeField] private SpriteRenderer targetRenderer;

    private void Awake()
    {
        EnsureReferences();
        progressBarPresenter?.Bind(targetRenderer);
        progressBarPresenter?.SetInactive();
    }

    private void OnDisable()
    {
        progressBarPresenter?.SetInactive();
    }

    private void Update()
    {
        EnsureReferences();

        if (progressBarPresenter == null || pointOfInterest == null || G.main == null)
        {
            progressBarPresenter?.SetInactive();
            return;
        }

        if (!G.main.TryGetNightPoiProgress(pointOfInterest.Id, pointOfInterest.Type, out var normalizedProgress))
        {
            progressBarPresenter.SetInactive();
            return;
        }

        progressBarPresenter.Refresh(normalizedProgress, false, true);
    }

    public void Bind(NightPointOfInterest poi)
    {
        pointOfInterest = poi;
        EnsureReferences();
        progressBarPresenter?.Bind(targetRenderer);
    }

    private void EnsureReferences()
    {
        if (pointOfInterest == null)
        {
            pointOfInterest = GetComponentInParent<NightPointOfInterest>();
        }

        if (progressBarPresenter == null)
        {
            progressBarPresenter = GetComponent<WorldProgressBarPresenter>();
        }

        if (targetRenderer == null)
        {
            targetRenderer = pointOfInterest != null
                ? pointOfInterest.GetComponent<SpriteRenderer>()
                : GetComponentInParent<SpriteRenderer>();
        }
    }
}
