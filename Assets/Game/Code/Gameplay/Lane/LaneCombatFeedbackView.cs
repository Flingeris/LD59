using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class LaneCombatFeedbackView : MonoBehaviour
{
    private static readonly Color AllyHitFlashColor = new(1f, 0.45f, 0.45f, 1f);
    private static readonly Color EnemyHitFlashColor = Color.white;

    [SerializeField] private CombatHpBarView hpBarPrefab;
    [SerializeField] private WorldProgressBarPresenter lifetimeProgressPresenter;
    [SerializeField] private SpriteRenderer targetRenderer;
    [Min(0f)] [SerializeField] private float barVerticalPadding = 0.08f;
    [Min(0f)] [SerializeField] private float popupVerticalPadding = 0.28f;
    [Min(0f)] [SerializeField] private float hitFlashDuration = 0.08f;

    private CombatHpBarView hpBarInstance;
    private Coroutine flashCoroutine;
    private Color baseSpriteColor = Color.white;
    private Color hitFlashColor = Color.white;
    private float popupOffsetY;
    private bool isBound;
    private LaneUnit boundLaneUnit;

    private void Awake()
    {
        EnsureTargetRenderer();
    }

    private void LateUpdate()
    {
        RefreshLifetimeProgressPresentation();
    }

    private void OnDisable()
    {
        Cleanup();
    }

    private void OnDestroy()
    {
        Cleanup();
    }

    public void Bind(int maxHp, bool isEnemy)
    {
        EnsureTargetRenderer();

        baseSpriteColor = targetRenderer != null ? targetRenderer.color : Color.white;
        hitFlashColor = isEnemy ? EnemyHitFlashColor : AllyHitFlashColor;
        boundLaneUnit = isEnemy ? null : GetComponent<LaneUnit>();

        var spriteHeight = targetRenderer != null ? targetRenderer.bounds.size.y : 0.4f;
        popupOffsetY = spriteHeight * 0.6f + popupVerticalPadding;
        var barOffsetY = spriteHeight * 0.55f + barVerticalPadding;

        EnsureHpBarInstance(barOffsetY);
        hpBarInstance?.Bind(maxHp, isEnemy, targetRenderer);
        hpBarInstance?.Refresh(maxHp);
        lifetimeProgressPresenter?.Bind(targetRenderer);
        isBound = true;
        RefreshLifetimeProgressPresentation();
    }

    public void PlayDamageFeedback(int damage, int currentHp)
    {
        if (!isBound || damage <= 0)
        {
            return;
        }

        hpBarInstance?.Refresh(currentHp);
        Popups.DamageAbove(transform, popupOffsetY, damage);
        PlayHitFlash();
    }

    public void Cleanup()
    {
        isBound = false;

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }

        if (targetRenderer != null)
        {
            targetRenderer.color = baseSpriteColor;
        }

        if (hpBarInstance != null)
        {
            Destroy(hpBarInstance.gameObject);
            hpBarInstance = null;
        }

        lifetimeProgressPresenter?.SetInactive();
        boundLaneUnit = null;
    }

    private void PlayHitFlash()
    {
        if (targetRenderer == null)
        {
            return;
        }

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }

        flashCoroutine = StartCoroutine(HitFlashRoutine());
    }

    private IEnumerator HitFlashRoutine()
    {
        targetRenderer.color = hitFlashColor;
        yield return new WaitForSeconds(hitFlashDuration);
        targetRenderer.color = baseSpriteColor;
        flashCoroutine = null;
    }

    private void EnsureHpBarInstance(float barOffsetY)
    {
        if (hpBarInstance == null)
        {
            if (hpBarPrefab == null)
            {
                Debug.LogWarning("LaneCombatFeedbackView: hpBarPrefab is not assigned");
                return;
            }

            hpBarInstance = Instantiate(hpBarPrefab, transform);
        }

        var hpBarTransform = hpBarInstance.transform;
        hpBarTransform.localRotation = Quaternion.identity;
        hpBarTransform.localScale = GetInverseLossyScale();
        hpBarTransform.localPosition = transform.InverseTransformPoint(transform.position + Vector3.up * barOffsetY);
    }

    private void EnsureTargetRenderer()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponentInChildren<SpriteRenderer>();
        }
    }

    private Vector3 GetInverseLossyScale()
    {
        var lossyScale = transform.lossyScale;
        return new Vector3(
            1f / Mathf.Max(0.0001f, lossyScale.x),
            1f / Mathf.Max(0.0001f, lossyScale.y),
            1f / Mathf.Max(0.0001f, lossyScale.z));
    }

    private void RefreshLifetimeProgressPresentation()
    {
        if (lifetimeProgressPresenter == null)
        {
            return;
        }

        if (!isBound || boundLaneUnit == null || !boundLaneUnit.HasLimitedLifetime)
        {
            lifetimeProgressPresenter.SetInactive();
            return;
        }

        lifetimeProgressPresenter.Refresh(boundLaneUnit.LifetimeProgressNormalized, true, true);
    }
}
