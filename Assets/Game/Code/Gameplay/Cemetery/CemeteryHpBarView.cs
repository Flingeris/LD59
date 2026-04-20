using System.Collections;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class CemeteryHpBarView : MonoBehaviour
{
    private static readonly Color DamageFlashColor = new(1f, 0.45f, 0.45f, 1f);
    private static readonly Color RepairFlashColor = new(0.55f, 1f, 0.65f, 1f);

    [Header("Bar")]
    [SerializeField] private WorldBarFillShrink hpBar;

    [SerializeField] private TMP_Text hpText;
    [SerializeField] private bool showOnlyAtNight = true;

    [Header("Feedback")]
    [SerializeField] private SpriteRenderer targetRenderer;

    [SerializeField] private Transform popupAnchor;
    [Min(0f)] [SerializeField] private float popupVerticalPadding = 0.32f;
    [Min(0f)] [SerializeField] private float flashDuration = 0.08f;

    private Coroutine flashCoroutine;
    private Color baseSpriteColor = Color.white;
    private float popupOffsetY;
    private int cachedMaxHp = 1;
    private bool subscribed;

    private void Awake()
    {
        EnsureReferences();
        CacheVisualMetrics();
        SetVisible(false);
    }

    private void Start()
    {
        TrySubscribe();
        RefreshImmediate();
    }

    private void OnEnable()
    {
        TrySubscribe();
        RefreshImmediate();
    }

    private void Update()
    {
        UpdateVisibilityOnly();
    }

    private void OnDisable()
    {
        Unsubscribe();
        StopFlash();
        SetVisible(false);
    }

    private void OnDestroy()
    {
        Unsubscribe();
        StopFlash();
    }

    private void HandleCemeteryDamaged(int damageAmount)
    {
        RefreshImmediate();

        if (damageAmount > 0)
        {
            Popups.DamageAbove(GetPopupAnchor(), popupOffsetY, damageAmount);
            PlayFlash(DamageFlashColor);
        }
    }

    private void HandleCemeteryRepaired(int repairedAmount)
    {
        RefreshImmediate();

        if (repairedAmount > 0)
        {
            Popups.HealAbove(GetPopupAnchor(), popupOffsetY, repairedAmount);
            PlayFlash(RepairFlashColor);
        }
    }

    private void RefreshImmediate()
    {
        EnsureReferences();

        if (G.main == null || G.main.RunState == null)
        {
            SetVisible(false);
            return;
        }

        var runState = G.main.RunState;
        cachedMaxHp = Mathf.Max(1, runState.CemeteryMaxState);

        var shouldBeVisible = !showOnlyAtNight || runState.CurrentPhase == GamePhase.Night;
        if (!shouldBeVisible)
        {
            SetVisible(false);
            return;
        }

        var currentHp = Mathf.Clamp(runState.CemeteryState, 0, cachedMaxHp);
        var normalized = (float)currentHp / cachedMaxHp;

        SetVisible(true);
        hpBar?.SetNormalized(normalized);

        if (hpText != null)
        {
            hpText.text = $"{currentHp}/{cachedMaxHp}";
        }
    }

    private void UpdateVisibilityOnly()
    {
        if (G.main == null || G.main.RunState == null)
        {
            SetVisible(false);
            return;
        }

        var runState = G.main.RunState;
        var shouldBeVisible = !showOnlyAtNight || runState.CurrentPhase == GamePhase.Night;

        if (!shouldBeVisible)
        {
            SetVisible(false);
            return;
        }

        if (hpBar != null && !hpBar.gameObject.activeSelf)
        {
            RefreshImmediate();
        }
    }

    private void SetVisible(bool visible)
    {
        hpBar?.SetVisible(visible);

        if (hpText != null)
        {
            hpText.gameObject.SetActive(visible);
        }
    }

    private void PlayFlash(Color flashColor)
    {
        if (targetRenderer == null)
        {
            return;
        }

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }

        flashCoroutine = StartCoroutine(FlashRoutine(flashColor));
    }

    private IEnumerator FlashRoutine(Color flashColor)
    {
        targetRenderer.color = flashColor;
        yield return new WaitForSeconds(flashDuration);

        if (targetRenderer != null)
        {
            targetRenderer.color = baseSpriteColor;
        }

        flashCoroutine = null;
    }

    private void StopFlash()
    {
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }

        if (targetRenderer != null)
        {
            targetRenderer.color = baseSpriteColor;
        }
    }

    private void TrySubscribe()
    {
        if (subscribed || G.main == null)
        {
            return;
        }

        G.main.CemeteryDamaged += HandleCemeteryDamaged;
        G.main.CemeteryRepaired += HandleCemeteryRepaired;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed || G.main == null)
        {
            subscribed = false;
            return;
        }

        G.main.CemeteryDamaged -= HandleCemeteryDamaged;
        G.main.CemeteryRepaired -= HandleCemeteryRepaired;
        subscribed = false;
    }

    private Transform GetPopupAnchor()
    {
        if (popupAnchor != null)
        {
            return popupAnchor;
        }

        if (targetRenderer != null)
        {
            return targetRenderer.transform;
        }

        return transform;
    }

    private void EnsureReferences()
    {
        if (hpBar == null)
        {
            hpBar = GetComponentInChildren<WorldBarFillShrink>(true);
        }

        if (hpText == null)
        {
            hpText = GetComponentInChildren<TMP_Text>(true);
        }

        if (targetRenderer == null)
        {
            targetRenderer = GetComponentInChildren<SpriteRenderer>();
        }
    }

    private void CacheVisualMetrics()
    {
        baseSpriteColor = targetRenderer != null ? targetRenderer.color : Color.white;

        var spriteHeight = targetRenderer != null ? targetRenderer.bounds.size.y : 0.5f;
        popupOffsetY = spriteHeight * 0.6f + popupVerticalPadding;
    }
}