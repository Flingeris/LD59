using UnityEngine;

[DisallowMultipleComponent]
public class WorldProgressBarPresenter : MonoBehaviour
{
    [SerializeField] private WorldProgressBarView progressBarView;
    [SerializeField] private SpriteRenderer targetRenderer;
    [Min(0f)] [SerializeField] private float barVerticalPadding = 0.08f;
    [Min(0f)] [SerializeField] private float anchorHeightMultiplier = 0.55f;
    [SerializeField] private bool anchorBelowTarget;
    [SerializeField] private Vector3 manualWorldOffset;
    [SerializeField] private bool matchInverseLossyScale = true;

    private bool isBound;

    private void Awake()
    {
        EnsureReferences();
        progressBarView?.SetInactive();
    }

    private void LateUpdate()
    {
        if (!isBound)
        {
            return;
        }

        UpdateAnchorTransform();
    }

    public void Bind(SpriteRenderer ownerRenderer = null)
    {
        EnsureReferences();

        if (ownerRenderer != null)
        {
            targetRenderer = ownerRenderer;
        }

        if (progressBarView == null || targetRenderer == null)
        {
            isBound = false;
            progressBarView?.SetInactive();
            return;
        }

        progressBarView.Bind(targetRenderer);
        isBound = true;
        UpdateAnchorTransform();
    }

    public void Refresh(float normalizedProgress, bool useCountdownFill, bool isVisible)
    {
        if (!isBound)
        {
            Bind(targetRenderer);
        }

        progressBarView?.Refresh(normalizedProgress, useCountdownFill, isVisible);
    }

    public void SetInactive()
    {
        progressBarView?.SetInactive();
    }

    private void EnsureReferences()
    {
        if (progressBarView == null)
        {
            progressBarView = GetComponent<WorldProgressBarView>();
        }

        if (targetRenderer == null)
        {
            targetRenderer = GetComponentInParent<SpriteRenderer>();
        }
    }

    private void UpdateAnchorTransform()
    {
        if (targetRenderer == null)
        {
            return;
        }

        var spriteHeight = targetRenderer.bounds.size.y > 0f ? targetRenderer.bounds.size.y : 0.4f;
        var verticalOffset = spriteHeight * Mathf.Max(0f, anchorHeightMultiplier) + Mathf.Max(0f, barVerticalPadding);
        if (anchorBelowTarget)
        {
            verticalOffset *= -1f;
        }

        transform.position = targetRenderer.transform.position + Vector3.up * verticalOffset + manualWorldOffset;
        transform.rotation = Quaternion.identity;

        if (!matchInverseLossyScale)
        {
            return;
        }

        var lossyScale = targetRenderer.transform.lossyScale;
        transform.localScale = new Vector3(
            1f / Mathf.Max(0.0001f, lossyScale.x),
            1f / Mathf.Max(0.0001f, lossyScale.y),
            1f / Mathf.Max(0.0001f, lossyScale.z));
    }
}
