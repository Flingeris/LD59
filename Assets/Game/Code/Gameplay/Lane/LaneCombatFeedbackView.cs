using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class LaneCombatFeedbackView : MonoBehaviour
{
    private static readonly Color AllyHitFlashColor = new(1f, 0.45f, 0.45f, 1f);
    private static readonly Color EnemyHitFlashColor = Color.white;

    [Header("Bars")]
    [SerializeField] private WorldBarFillShrink hpBar;

    [SerializeField] private WorldBarFillShrink lifetimeBar;
    [SerializeField] private SpriteRenderer targetRenderer;

    [Header("Feedback")]
    [Min(0f)] [SerializeField] private float popupVerticalPadding = 0.28f;

    [Min(0f)] [SerializeField] private float hitFlashDuration = 0.08f;

    private Coroutine flashCoroutine;
    private Color baseSpriteColor = Color.white;
    private Color hitFlashColor = Color.white;
    private float popupOffsetY;
    private bool isBound;
    private LaneUnit boundLaneUnit;
    private int maxHp = 1;

    private void Awake()
    {
        EnsureReferences();
    }

    private void LateUpdate()
    {
        RefreshLifetimeBar();
    }

    private void OnDisable()
    {
        Cleanup();
    }

    private void OnDestroy()
    {
        Cleanup();
    }

    public void Bind(int maxHpValue, bool isEnemy)
    {
        EnsureReferences();

        maxHp = Mathf.Max(1, maxHpValue);
        baseSpriteColor = targetRenderer != null ? targetRenderer.color : Color.white;
        hitFlashColor = isEnemy ? EnemyHitFlashColor : AllyHitFlashColor;
        boundLaneUnit = isEnemy ? null : GetComponent<LaneUnit>();

        var spriteHeight = targetRenderer != null ? targetRenderer.bounds.size.y : 0.4f;
        popupOffsetY = spriteHeight * 0.6f + popupVerticalPadding;

        hpBar?.SetVisible(true);
        hpBar?.SetNormalized(1f);

        if (lifetimeBar != null)
        {
            if (boundLaneUnit != null && boundLaneUnit.HasLimitedLifetime)
            {
                lifetimeBar.SetVisible(true);
                lifetimeBar.SetNormalized(1f - boundLaneUnit.LifetimeProgressNormalized);
            }
            else
            {
                lifetimeBar.SetVisible(false);
            }
        }

        isBound = true;
    }

    public void PlayDamageFeedback(int damage, int currentHp)
    {
        if (!isBound || damage <= 0)
            return;

        var normalizedHp = Mathf.Clamp01((float)Mathf.Max(0, currentHp) / Mathf.Max(1, maxHp));
        hpBar?.SetNormalized(normalizedHp);

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
            targetRenderer.color = baseSpriteColor;

        hpBar?.SetVisible(false);
        lifetimeBar?.SetVisible(false);

        boundLaneUnit = null;
    }

    private void RefreshLifetimeBar()
    {
        if (!isBound || lifetimeBar == null)
            return;

        if (boundLaneUnit == null || !boundLaneUnit.HasLimitedLifetime)
        {
            lifetimeBar.SetVisible(false);
            return;
        }

        lifetimeBar.SetVisible(true);
        lifetimeBar.SetNormalized(1f - boundLaneUnit.LifetimeProgressNormalized);
    }

    private void PlayHitFlash()
    {
        if (targetRenderer == null)
            return;

        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        flashCoroutine = StartCoroutine(HitFlashRoutine());
    }

    private IEnumerator HitFlashRoutine()
    {
        targetRenderer.color = hitFlashColor;
        yield return new WaitForSeconds(hitFlashDuration);
        targetRenderer.color = baseSpriteColor;
        flashCoroutine = null;
    }

    private void EnsureReferences()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<SpriteRenderer>();

        if (hpBar == null)
        {
            var hp = transform.Find("HpBar");
            if (hp != null)
                hpBar = hp.GetComponent<WorldBarFillShrink>();
        }

        if (lifetimeBar == null)
        {
            var lt = transform.Find("LifetimeBar");
            if (lt != null)
                lifetimeBar = lt.GetComponent<WorldBarFillShrink>();
        }
    }
}