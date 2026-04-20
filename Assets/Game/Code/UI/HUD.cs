using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class HUD : MonoBehaviour
{
    [Serializable]
    private class BellButtonBinding
    {
        public string bellId;
        public Button button;
        public TMP_Text label;
        public UnityAction clickAction;
    }

    [SerializeField] private TMP_Text faithText;
    [SerializeField] private RectTransform faithPickupTarget;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private RectTransform goldPickupTarget;
    [SerializeField] private TMP_Text cemeteryStateText;
    [SerializeField] private TMP_Text phaseText;
    [SerializeField] private WaveProgressView waveProgressView;
    [SerializeField] private DayScreenView dayScreenView;
    [SerializeField] private DefeatScreenView defeatScreenView;
    [SerializeField] private WinScreenView winScreenView;
    [SerializeField] private PhaseTransitionView phaseTransitionView;
    [SerializeField] private BellButtonBinding[] bellButtons;
    [SerializeField] private TMP_Text cemeteryHpText;
    public event Action DayScreenStartNightRequested;
    public event Action<string> DayScreenUpgradePurchaseRequested;
    public event Action DefeatScreenRestartRequested;
    public event Action WinScreenRestartRequested;

    private bool missingMainWarningShown;
    private FaithPickupVfxController faithPickupVfxController;
    private GoldPickupVfxController goldPickupVfxController;

    private void Awake()
    {
        G.HUD = this;
        EnsureFaithPickupVfxController();
        EnsureGoldPickupVfxController();
        BindDayScreenView();
        BindDefeatScreenView();
        BindWinScreenView();
        RefreshBellButtonLabels();
        RefreshBellButtonInteractivity(null);
    }

    private void OnDestroy()
    {
        UnbindDayScreenView();
        UnbindDefeatScreenView();
        UnbindWinScreenView();

        if (G.HUD == this)
        {
            G.HUD = null;
        }
    }

    private void Update()
    {
        RefreshView();
    }

    public void RefreshView()
    {
        if (G.main == null || G.main.RunState == null)
        {
            RefreshBellButtonInteractivity(null);
            waveProgressView?.SetInactive();

            if (!missingMainWarningShown)
            {
                Debug.LogWarning("HUD: Main or RunState not found");
                missingMainWarningShown = true;
            }

            return;
        }

        missingMainWarningShown = false;

        var runState = G.main.RunState;
        var showRuntimeHud = runState.CurrentPhase == GamePhase.Night;
        SetRuntimeHudVisible(showRuntimeHud);
        RefreshBellButtonInteractivity(runState);

        if (faithText != null)
        {
            faithText.text = $"{runState.Faith}";
        }

        if (goldText != null)
        {
            goldText.text = $"{runState.Gold}";
        }

        if (cemeteryStateText != null)
        {
            cemeteryStateText.text = $"Cemetery: {runState.CemeteryState}/{runState.CemeteryMaxState}";
        }

        if (phaseText != null)
        {
            phaseText.text = $"Phase: {runState.CurrentPhase}";
        }

        waveProgressView?.RefreshView();
    }

    public void ShowDayScreen(RunState runState, IReadOnlyList<DayUpgradeItemData> upgradeItems)
    {
        if (dayScreenView == null)
        {
            return;
        }

        dayScreenView.Show(runState, upgradeItems);
    }

    public void HideDayScreen()
    {
        if (dayScreenView == null)
        {
            return;
        }

        dayScreenView.Hide();
    }

    public void RefreshDayScreen(RunState runState, IReadOnlyList<DayUpgradeItemData> upgradeItems)
    {
        if (dayScreenView == null || !dayScreenView.gameObject.activeSelf)
        {
            return;
        }

        dayScreenView.Refresh(runState, upgradeItems);
    }

    public void ShowDefeatScreen(RunState runState)
    {
        if (defeatScreenView == null)
        {
            return;
        }

        defeatScreenView.Show(runState);
    }

    public void HideDefeatScreen()
    {
        if (defeatScreenView == null)
        {
            return;
        }

        defeatScreenView.Hide();
    }

    public void ShowWinScreen(RunState runState)
    {
        if (winScreenView == null)
        {
            return;
        }

        winScreenView.Show(runState);
    }

    public void HideWinScreen()
    {
        if (winScreenView == null)
        {
            return;
        }

        winScreenView.Hide();
    }

    public void ShowPhaseTransition(GamePhase phase)
    {
        if (phaseTransitionView == null)
        {
            return;
        }

        phaseTransitionView.Play(phase);
    }

    public void HidePhaseTransition()
    {
        if (phaseTransitionView == null)
        {
            return;
        }

        phaseTransitionView.Hide();
    }

    public void PlayGoldPickupEffect(Vector3 worldPosition, int goldAmount)
    {
        if (goldAmount <= 0)
        {
            return;
        }

        EnsureGoldPickupVfxController();
        goldPickupVfxController?.PlayFromWorld(worldPosition, goldAmount);
    }

    public void PlayFaithPickupEffect(Vector3 worldPosition, int faithAmount)
    {
        if (faithAmount <= 0)
        {
            return;
        }

        EnsureFaithPickupVfxController();
        faithPickupVfxController?.PlayFromWorld(worldPosition, faithAmount);
    }

    public void PlayFaithSpendEffect(Vector3 worldTargetPosition, int faithAmount)
    {
        if (faithAmount <= 0)
        {
            return;
        }

        EnsureFaithPickupVfxController();
        faithPickupVfxController?.PlayToWorld(worldTargetPosition, faithAmount);
    }

    private void BindBellButtons()
    {
        if (bellButtons == null)
        {
            return;
        }

        for (var i = 0; i < bellButtons.Length; i++)
        {
            var binding = bellButtons[i];
            if (binding == null || binding.button == null)
            {
                continue;
            }

            var bellId = binding.bellId;
            binding.clickAction = () => OnBellButtonPressed(bellId);
            binding.button.onClick.AddListener(binding.clickAction);
        }
    }

    private void UnbindBellButtons()
    {
        if (bellButtons == null)
        {
            return;
        }

        for (var i = 0; i < bellButtons.Length; i++)
        {
            var binding = bellButtons[i];
            if (binding == null || binding.button == null)
            {
                continue;
            }

            if (binding.clickAction != null)
            {
                binding.button.onClick.RemoveListener(binding.clickAction);
            }
        }
    }

    private void BindDayScreenView()
    {
        if (dayScreenView == null)
        {
            return;
        }

        dayScreenView.StartNightRequested -= HandleDayScreenStartNightRequested;
        dayScreenView.StartNightRequested += HandleDayScreenStartNightRequested;
        dayScreenView.UpgradePurchaseRequested -= HandleDayScreenUpgradePurchaseRequested;
        dayScreenView.UpgradePurchaseRequested += HandleDayScreenUpgradePurchaseRequested;
    }

    private void UnbindDayScreenView()
    {
        if (dayScreenView == null)
        {
            return;
        }

        dayScreenView.StartNightRequested -= HandleDayScreenStartNightRequested;
        dayScreenView.UpgradePurchaseRequested -= HandleDayScreenUpgradePurchaseRequested;
    }

    private void BindDefeatScreenView()
    {
        if (defeatScreenView == null)
        {
            return;
        }

        defeatScreenView.RestartRequested -= HandleDefeatScreenRestartRequested;
        defeatScreenView.RestartRequested += HandleDefeatScreenRestartRequested;
    }

    private void UnbindDefeatScreenView()
    {
        if (defeatScreenView == null)
        {
            return;
        }

        defeatScreenView.RestartRequested -= HandleDefeatScreenRestartRequested;
    }

    private void BindWinScreenView()
    {
        if (winScreenView == null)
        {
            return;
        }

        winScreenView.RestartRequested -= HandleWinScreenRestartRequested;
        winScreenView.RestartRequested += HandleWinScreenRestartRequested;
    }

    private void UnbindWinScreenView()
    {
        if (winScreenView == null)
        {
            return;
        }

        winScreenView.RestartRequested -= HandleWinScreenRestartRequested;
    }

    private void RefreshBellButtonLabels()
    {
        if (bellButtons == null)
        {
            return;
        }

        for (var i = 0; i < bellButtons.Length; i++)
        {
            var binding = bellButtons[i];
            if (binding == null || binding.label == null)
            {
                continue;
            }

            var bellDef = CMS.Get<BellDef>(binding.bellId);
            if (bellDef == null)
            {
                binding.label.text = binding.bellId;
                continue;
            }

            binding.label.text = $"{bellDef.DisplayName} ({bellDef.FaithCost})";
        }
    }

    private void RefreshBellButtonInteractivity(RunState runState)
    {
        if (bellButtons == null)
        {
            return;
        }

        for (var i = 0; i < bellButtons.Length; i++)
        {
            var binding = bellButtons[i];
            if (binding == null || binding.button == null)
            {
                continue;
            }

            binding.button.interactable = false;
        }
    }

    private void OnBellButtonPressed(string bellId)
    {
        _ = bellId;
    }

    private void EnsureGoldPickupVfxController()
    {
        if (goldPickupVfxController == null)
        {
            goldPickupVfxController = GetComponent<GoldPickupVfxController>();
            if (goldPickupVfxController == null)
            {
                goldPickupVfxController = gameObject.AddComponent<GoldPickupVfxController>();
            }
        }

        goldPickupVfxController.Initialize(goldText, goldPickupTarget);
    }

    private void EnsureFaithPickupVfxController()
    {
        if (faithPickupVfxController == null)
        {
            faithPickupVfxController = GetComponent<FaithPickupVfxController>();
            if (faithPickupVfxController == null)
            {
                faithPickupVfxController = gameObject.AddComponent<FaithPickupVfxController>();
            }
        }

        faithPickupVfxController.Initialize(faithText, faithPickupTarget);
    }

    private void HandleDayScreenStartNightRequested()
    {
        DayScreenStartNightRequested?.Invoke();
    }

    private void HandleDayScreenUpgradePurchaseRequested(string upgradeId)
    {
        DayScreenUpgradePurchaseRequested?.Invoke(upgradeId);
    }

    private void HandleDefeatScreenRestartRequested()
    {
        DefeatScreenRestartRequested?.Invoke();
    }

    private void HandleWinScreenRestartRequested()
    {
        WinScreenRestartRequested?.Invoke();
    }

    private void SetRuntimeHudVisible(bool visible)
    {
        SetViewVisible(faithText, visible);
        SetViewVisible(goldText, visible);
        SetViewVisible(cemeteryStateText, visible);
        SetViewVisible(phaseText, visible);

        if (waveProgressView != null && waveProgressView.gameObject.activeSelf != visible)
        {
            waveProgressView.gameObject.SetActive(visible);
        }
    }

    private static void SetViewVisible(Component component, bool visible)
    {
        if (component == null)
        {
            return;
        }

        if (component.gameObject.activeSelf != visible)
        {
            component.gameObject.SetActive(visible);
        }
    }
}

[DisallowMultipleComponent]
public class GoldPickupVfxController : MonoBehaviour
{
    private const float CompactCanvasWidthThreshold = 420f;
    private const float CompactCanvasHeightThreshold = 240f;
    private const float DropDurationMin = 0.12f;
    private const float DropDurationMax = 0.18f;
    private const float FlyDurationMin = 0.34f;
    private const float FlyDurationMax = 0.5f;
    private const int TrailSegmentCount = 6;
    private const float TrailBaseAlpha = 0.7f;
    private const float TrailBaseFollowFactor = 0.13f;
    private const float CurveHeightFactor = 0.5f;

    private Canvas ownerCanvas;
    private RectTransform canvasRect;
    private RectTransform effectRoot;
    private TMP_Text targetText;
    private RectTransform pulseTargetRect;
    private RectTransform pickupTargetRect;

    public void Initialize(TMP_Text goldCounterText, RectTransform goldTargetRect)
    {
        targetText = goldCounterText;
        pulseTargetRect = goldCounterText != null ? goldCounterText.rectTransform : null;
        pickupTargetRect = goldTargetRect != null ? goldTargetRect : pulseTargetRect;
        ownerCanvas ??= GetComponent<Canvas>();
        canvasRect ??= transform as RectTransform;
        EnsureEffectRoot();
    }

    public void PlayFromWorld(Vector3 worldPosition, int goldAmount)
    {
        if (goldAmount <= 0 || pickupTargetRect == null || canvasRect == null)
        {
            return;
        }

        EnsureEffectRoot();
        if (effectRoot == null || !TryGetCanvasLocalPoint(worldPosition, out var startLocalPoint))
        {
            return;
        }

        var targetLocalPoint = GetTargetLocalPoint();
        var visualCount = CalculateVisualCount(goldAmount);
        var compactLayout = IsCompactCanvas();

        for (var i = 0; i < visualCount; i++)
        {
            SpawnCoinMote(startLocalPoint, targetLocalPoint, compactLayout, i == visualCount - 1);
        }
    }

    private void EnsureEffectRoot()
    {
        if (effectRoot != null || canvasRect == null)
        {
            return;
        }

        var rootObject = new GameObject("GoldPickupVfxRoot", typeof(RectTransform));
        effectRoot = rootObject.transform as RectTransform;
        effectRoot.SetParent(canvasRect, false);
        effectRoot.anchorMin = new Vector2(0.5f, 0.5f);
        effectRoot.anchorMax = new Vector2(0.5f, 0.5f);
        effectRoot.pivot = new Vector2(0.5f, 0.5f);
        effectRoot.anchoredPosition = Vector2.zero;
        effectRoot.sizeDelta = Vector2.zero;
        effectRoot.SetAsLastSibling();
    }

    private bool TryGetCanvasLocalPoint(Vector3 worldPosition, out Vector2 localPoint)
    {
        localPoint = Vector2.zero;

        var renderCamera = GetRenderCamera();
        if (renderCamera == null)
        {
            return false;
        }

        var screenPoint = renderCamera.WorldToScreenPoint(worldPosition);
        if (screenPoint.z < 0f)
        {
            return false;
        }

        screenPoint.x = Mathf.Clamp(screenPoint.x, 0f, Screen.width);
        screenPoint.y = Mathf.Clamp(screenPoint.y, 0f, Screen.height);
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPoint,
            GetUiCamera(),
            out localPoint);
    }

    private Vector2 GetTargetLocalPoint()
    {
        var worldCorners = new Vector3[4];
        pickupTargetRect.GetWorldCorners(worldCorners);
        var usesFallbackTarget = pickupTargetRect == pulseTargetRect;
        var targetWorldPoint = usesFallbackTarget
            ? Vector3.Lerp(worldCorners[0], worldCorners[1], 0.5f)
            : Vector3.Lerp(worldCorners[0], worldCorners[2], 0.5f);

        if (usesFallbackTarget)
        {
            targetWorldPoint.x -= 2f;
        }

        var screenPoint = RectTransformUtility.WorldToScreenPoint(GetUiCamera(), targetWorldPoint);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPoint,
            GetUiCamera(),
            out var localPoint);
        return localPoint;
    }

    private void SpawnCoinMote(Vector2 startLocalPoint, Vector2 targetLocalPoint, bool compactLayout,
        bool triggerTargetPulse)
    {
        var moteObject = new GameObject("GoldPickupMote", typeof(RectTransform), typeof(CanvasRenderer),
            typeof(RawImage));
        var moteRect = moteObject.transform as RectTransform;
        moteRect.SetParent(effectRoot, false);
        moteRect.anchorMin = new Vector2(0.5f, 0.5f);
        moteRect.anchorMax = new Vector2(0.5f, 0.5f);
        moteRect.pivot = new Vector2(0.5f, 0.5f);

        var size = compactLayout ? Random.Range(1.75f, 2.75f) : Random.Range(2.75f, 4f);
        moteRect.sizeDelta = new Vector2(size, size);
        moteRect.anchoredPosition = startLocalPoint;
        moteRect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        moteRect.localScale = Vector3.one * Random.Range(0.72f, 0.92f);

        var moteImage = moteObject.GetComponent<RawImage>();
        moteImage.texture = Texture2D.whiteTexture;
        moteImage.raycastTarget = false;
        moteImage.color = Color.Lerp(
            new Color(1f, 0.86f, 0.3f, 1f),
            new Color(1f, 0.95f, 0.65f, 1f),
            Random.value);

        var scatterOffset = new Vector2(
            Random.Range(compactLayout ? -10f : -18f, compactLayout ? 10f : 18f),
            Random.Range(compactLayout ? -14f : -20f, compactLayout ? 4f : 8f));

        var trailRects = new RectTransform[TrailSegmentCount];
        var trailImages = new RawImage[TrailSegmentCount];
        InitializeTrailMotes(startLocalPoint, size, moteRect.localScale.x, trailRects, trailImages);

        var scatterPoint = startLocalPoint + scatterOffset;
        var approachOffset = new Vector2(
            Random.Range(compactLayout ? -3f : -7f, compactLayout ? 3f : 7f),
            Random.Range(compactLayout ? -3f : -7f, compactLayout ? 3f : 7f));
        var finalPoint = targetLocalPoint + approachOffset;
        var dropDuration = Random.Range(DropDurationMin, DropDurationMax);
        var flyDuration = Random.Range(FlyDurationMin, FlyDurationMax);
        var totalDuration = dropDuration + flyDuration;
        var startDelay = Random.Range(0f, 0.08f);
        var curveControlPoint = BuildCurveControlPoint(scatterPoint, finalPoint, compactLayout);
        var didComplete = false;

        var sequence = DOTween.Sequence();
        sequence.SetLink(moteObject, LinkBehaviour.KillOnDestroy);
        sequence.AppendInterval(startDelay);
        sequence.Append(moteRect.DOAnchorPos(scatterPoint, dropDuration).SetEase(Ease.OutQuad));
        sequence.Join(moteRect.DORotate(
            new Vector3(0f, 0f, moteRect.localEulerAngles.z + Random.Range(-70f, 70f)),
            dropDuration));
        sequence.Join(moteRect.DOScale(Random.Range(0.8f, 1f), dropDuration).SetEase(Ease.OutQuad));
        sequence.Append(DOVirtual.Float(0f, 1f, flyDuration, value =>
        {
            var curvedPosition = EvaluateQuadraticBezier(scatterPoint, curveControlPoint, finalPoint, value);
            moteRect.anchoredPosition = curvedPosition;

            var tangent = EvaluateQuadraticBezierTangent(scatterPoint, curveControlPoint, finalPoint, value);
            if (tangent.sqrMagnitude > 0.0001f)
            {
                var angle = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg - 90f;
                moteRect.localRotation = Quaternion.Euler(0f, 0f, angle);
            }
        }).SetEase(Ease.InOutSine));
        sequence.Join(moteRect.DOScale(0.18f, flyDuration).SetEase(Ease.InQuad));
        sequence.Join(moteImage.DOFade(0.2f, flyDuration).SetEase(Ease.InQuad));
        for (var i = 0; i < trailImages.Length; i++)
        {
            if (trailImages[i] == null)
            {
                continue;
            }

            sequence.Insert(startDelay, trailImages[i].DOFade(0f, totalDuration).SetEase(Ease.OutQuad));
        }

        sequence.OnUpdate(() => UpdateTrailMotes(moteRect, trailRects, trailImages));
        sequence.OnComplete(() =>
        {
            didComplete = true;
            if (triggerTargetPulse)
            {
                PulseTarget();
            }

            CleanupMoteObjects(moteObject, trailRects);
        });
        sequence.OnKill(() =>
        {
            if (!didComplete)
            {
                CleanupMoteObjects(moteObject, trailRects);
            }
        });
    }

    private void InitializeTrailMotes(
        Vector2 startLocalPoint,
        float baseSize,
        float baseScale,
        RectTransform[] trailRects,
        RawImage[] trailImages)
    {
        for (var i = 0; i < TrailSegmentCount; i++)
        {
            var trailObject = new GameObject("GoldPickupTrail", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(RawImage));
            var trailRect = trailObject.transform as RectTransform;
            trailRect.SetParent(effectRoot, false);
            trailRect.anchorMin = new Vector2(0.5f, 0.5f);
            trailRect.anchorMax = new Vector2(0.5f, 0.5f);
            trailRect.pivot = new Vector2(0.5f, 0.5f);
            trailRect.anchoredPosition = startLocalPoint;
            trailRect.sizeDelta = Vector2.one * Mathf.Max(1.3f, baseSize * (1.08f - 0.16f * i));
            trailRect.localScale = Vector3.one * Mathf.Max(0.35f, baseScale * (1.02f - 0.1f * i));

            var trailImage = trailObject.GetComponent<RawImage>();
            trailImage.texture = Texture2D.whiteTexture;
            trailImage.raycastTarget = false;
            trailImage.color = new Color(
                1f,
                Mathf.Min(1f, 0.82f + 0.03f * i),
                Mathf.Clamp01(0.28f + 0.02f * i),
                Mathf.Max(0.1f, TrailBaseAlpha - 0.08f * i));

            trailRects[i] = trailRect;
            trailImages[i] = trailImage;
        }
    }

    private static void UpdateTrailMotes(RectTransform moteRect, RectTransform[] trailRects, RawImage[] trailImages)
    {
        if (moteRect == null || trailRects == null)
        {
            return;
        }

        var sourcePosition = moteRect.anchoredPosition;
        var sourceScale = moteRect.localScale.x;
        var sourceRotation = moteRect.localEulerAngles.z;

        for (var i = 0; i < trailRects.Length; i++)
        {
            var trailRect = trailRects[i];
            if (trailRect == null)
            {
                continue;
            }

            var followFactor = Mathf.Max(0.08f, TrailBaseFollowFactor - i * 0.04f);
            trailRect.anchoredPosition = Vector2.Lerp(trailRect.anchoredPosition, sourcePosition, followFactor);
            trailRect.localScale = Vector3.Lerp(
                trailRect.localScale,
                Vector3.one * Mathf.Max(0.26f, sourceScale * (1.02f - 0.12f * i)),
                0.2f);
            trailRect.localRotation = Quaternion.Lerp(
                trailRect.localRotation,
                Quaternion.Euler(0f, 0f, sourceRotation),
                0.18f);

            sourcePosition = trailRect.anchoredPosition;
            sourceScale = trailRect.localScale.x;
            sourceRotation = trailRect.localEulerAngles.z;

            if (trailImages != null && i < trailImages.Length && trailImages[i] != null)
            {
                trailImages[i].transform.SetAsFirstSibling();
            }
        }
    }

    private Vector2 BuildCurveControlPoint(Vector2 startPoint, Vector2 endPoint, bool compactLayout)
    {
        var midpoint = (startPoint + endPoint) * 0.5f;
        var direction = endPoint - startPoint;
        var distance = direction.magnitude;
        if (distance <= 0.001f)
        {
            return midpoint;
        }

        var normalizedDirection = direction / distance;
        var arcNormal = new Vector2(-normalizedDirection.y, normalizedDirection.x);
        if (arcNormal.y < 0f)
        {
            arcNormal = -arcNormal;
        }

        var arcHeight = Mathf.Clamp(
            distance * CurveHeightFactor,
            compactLayout ? 10f : 14f,
            compactLayout ? 18f : 28f);
        var upwardLift = compactLayout ? 4f : 6f;
        return midpoint + arcNormal * arcHeight + Vector2.up * upwardLift;
    }

    private static Vector2 EvaluateQuadraticBezier(Vector2 startPoint, Vector2 controlPoint, Vector2 endPoint, float t)
    {
        var inverseT = 1f - t;
        return inverseT * inverseT * startPoint
               + 2f * inverseT * t * controlPoint
               + t * t * endPoint;
    }

    private static Vector2 EvaluateQuadraticBezierTangent(Vector2 startPoint, Vector2 controlPoint, Vector2 endPoint,
        float t)
    {
        return 2f * (1f - t) * (controlPoint - startPoint)
               + 2f * t * (endPoint - controlPoint);
    }

    private void PulseTarget()
    {
        if (pulseTargetRect == null)
        {
            return;
        }

        PunchScaleTarget(pulseTargetRect);

        if (pickupTargetRect != null && pickupTargetRect != pulseTargetRect)
        {
            PunchScaleTarget(pickupTargetRect);
        }

        if (targetText == null)
        {
            return;
        }

        targetText.DOKill();
        var baseColor = targetText.color;
        targetText.color = new Color(1f, 0.96f, 0.76f, baseColor.a);
        targetText.DOColor(baseColor, 0.2f).SetEase(Ease.OutQuad);
    }

    private static void PunchScaleTarget(RectTransform targetRect)
    {
        if (targetRect == null)
        {
            return;
        }

        targetRect.DOKill();
        targetRect.localScale = Vector3.one;
        targetRect.DOPunchScale(new Vector3(0.18f, 0.18f, 0f), 0.24f, 5, 0.7f);
    }

    private static void CleanupMoteObjects(GameObject moteObject, RectTransform[] trailRects)
    {
        if (moteObject != null)
        {
            Destroy(moteObject);
        }

        if (trailRects == null)
        {
            return;
        }

        for (var i = 0; i < trailRects.Length; i++)
        {
            if (trailRects[i] != null)
            {
                Destroy(trailRects[i].gameObject);
            }
        }
    }

    private int CalculateVisualCount(int goldAmount)
    {
        if (goldAmount <= 1)
        {
            return 3;
        }

        return Mathf.Clamp(goldAmount + 2, 3, 6);
    }

    private bool IsCompactCanvas()
    {
        if (canvasRect == null)
        {
            return false;
        }

        return canvasRect.rect.width <= CompactCanvasWidthThreshold
               || canvasRect.rect.height <= CompactCanvasHeightThreshold;
    }

    private Camera GetRenderCamera()
    {
        return ownerCanvas != null && ownerCanvas.worldCamera != null
            ? ownerCanvas.worldCamera
            : Camera.main;
    }

    private Camera GetUiCamera()
    {
        if (ownerCanvas == null || ownerCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        return ownerCanvas.worldCamera != null ? ownerCanvas.worldCamera : Camera.main;
    }
}

[DisallowMultipleComponent]
public class FaithPickupVfxController : MonoBehaviour
{
    private const float CompactCanvasWidthThreshold = 420f;
    private const float CompactCanvasHeightThreshold = 240f;
    private const float DropDurationMin = 0.12f;
    private const float DropDurationMax = 0.18f;
    private const float FlyDurationMin = 0.34f;
    private const float FlyDurationMax = 0.5f;
    private const int TrailSegmentCount = 6;
    private const float TrailBaseAlpha = 0.7f;
    private const float TrailBaseFollowFactor = 0.13f;
    private const float CurveHeightFactor = 0.5f;

    private Canvas ownerCanvas;
    private RectTransform canvasRect;
    private RectTransform effectRoot;
    private TMP_Text targetText;
    private RectTransform pulseTargetRect;
    private RectTransform pickupTargetRect;

    public void Initialize(TMP_Text faithCounterText, RectTransform faithTargetRect)
    {
        targetText = faithCounterText;
        pulseTargetRect = faithCounterText != null ? faithCounterText.rectTransform : null;
        pickupTargetRect = faithTargetRect != null ? faithTargetRect : pulseTargetRect;
        ownerCanvas ??= GetComponent<Canvas>();
        canvasRect ??= transform as RectTransform;
        EnsureEffectRoot();
    }

    public void PlayFromWorld(Vector3 worldPosition, int faithAmount)
    {
        if (faithAmount <= 0 || pickupTargetRect == null || canvasRect == null)
        {
            return;
        }

        EnsureEffectRoot();
        if (effectRoot == null || !TryGetCanvasLocalPoint(worldPosition, out var startLocalPoint))
        {
            return;
        }

        var targetLocalPoint = GetTargetLocalPoint();
        var visualCount = CalculateVisualCount(faithAmount);
        var compactLayout = IsCompactCanvas();

        for (var i = 0; i < visualCount; i++)
        {
            SpawnFaithMote(startLocalPoint, targetLocalPoint, compactLayout, i == visualCount - 1);
        }
    }

    public void PlayToWorld(Vector3 worldTargetPosition, int faithAmount)
    {
        if (faithAmount <= 0 || pickupTargetRect == null || canvasRect == null)
        {
            return;
        }

        EnsureEffectRoot();
        if (effectRoot == null || !TryGetCanvasLocalPoint(worldTargetPosition, out var targetLocalPoint))
        {
            return;
        }

        var startLocalPoint = GetTargetLocalPoint();
        var visualCount = CalculateVisualCount(faithAmount);
        var compactLayout = IsCompactCanvas();

        PulseTarget();

        for (var i = 0; i < visualCount; i++)
        {
            SpawnFaithMote(startLocalPoint, targetLocalPoint, compactLayout, false);
        }
    }

    private void EnsureEffectRoot()
    {
        if (effectRoot != null || canvasRect == null)
        {
            return;
        }

        var rootObject = new GameObject("FaithPickupVfxRoot", typeof(RectTransform));
        effectRoot = rootObject.transform as RectTransform;
        effectRoot.SetParent(canvasRect, false);
        effectRoot.anchorMin = new Vector2(0.5f, 0.5f);
        effectRoot.anchorMax = new Vector2(0.5f, 0.5f);
        effectRoot.pivot = new Vector2(0.5f, 0.5f);
        effectRoot.anchoredPosition = Vector2.zero;
        effectRoot.sizeDelta = Vector2.zero;
        effectRoot.SetAsLastSibling();
    }

    private bool TryGetCanvasLocalPoint(Vector3 worldPosition, out Vector2 localPoint)
    {
        localPoint = Vector2.zero;

        var renderCamera = GetRenderCamera();
        if (renderCamera == null)
        {
            return false;
        }

        var screenPoint = renderCamera.WorldToScreenPoint(worldPosition);
        if (screenPoint.z < 0f)
        {
            return false;
        }

        screenPoint.x = Mathf.Clamp(screenPoint.x, 0f, Screen.width);
        screenPoint.y = Mathf.Clamp(screenPoint.y, 0f, Screen.height);
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPoint,
            GetUiCamera(),
            out localPoint);
    }

    private Vector2 GetTargetLocalPoint()
    {
        var worldCorners = new Vector3[4];
        pickupTargetRect.GetWorldCorners(worldCorners);
        var usesFallbackTarget = pickupTargetRect == pulseTargetRect;
        var targetWorldPoint = usesFallbackTarget
            ? Vector3.Lerp(worldCorners[0], worldCorners[1], 0.5f)
            : Vector3.Lerp(worldCorners[0], worldCorners[2], 0.5f);

        if (usesFallbackTarget)
        {
            targetWorldPoint.x -= 2f;
        }

        var screenPoint = RectTransformUtility.WorldToScreenPoint(GetUiCamera(), targetWorldPoint);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPoint,
            GetUiCamera(),
            out var localPoint);
        return localPoint;
    }

    private void SpawnFaithMote(Vector2 startLocalPoint, Vector2 targetLocalPoint, bool compactLayout,
        bool triggerTargetPulse)
    {
        var moteObject = new GameObject("FaithPickupMote", typeof(RectTransform), typeof(CanvasRenderer),
            typeof(RawImage));
        var moteRect = moteObject.transform as RectTransform;
        moteRect.SetParent(effectRoot, false);
        moteRect.anchorMin = new Vector2(0.5f, 0.5f);
        moteRect.anchorMax = new Vector2(0.5f, 0.5f);
        moteRect.pivot = new Vector2(0.5f, 0.5f);

        var size = compactLayout ? Random.Range(1.75f, 2.75f) : Random.Range(2.75f, 4f);
        moteRect.sizeDelta = new Vector2(size, size);
        moteRect.anchoredPosition = startLocalPoint;
        moteRect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        moteRect.localScale = Vector3.one * Random.Range(0.72f, 0.92f);

        var moteImage = moteObject.GetComponent<RawImage>();
        moteImage.texture = Texture2D.whiteTexture;
        moteImage.raycastTarget = false;
        moteImage.color = Color.Lerp(
            new Color(0.34f, 0.8f, 1f, 1f),
            new Color(0.72f, 0.95f, 1f, 1f),
            Random.value);

        var trailRects = new RectTransform[TrailSegmentCount];
        var trailImages = new RawImage[TrailSegmentCount];
        InitializeTrailMotes(startLocalPoint, size, moteRect.localScale.x, trailRects, trailImages);

        var scatterOffset = new Vector2(
            Random.Range(compactLayout ? -10f : -18f, compactLayout ? 10f : 18f),
            Random.Range(compactLayout ? -14f : -20f, compactLayout ? 4f : 8f));
        var scatterPoint = startLocalPoint + scatterOffset;
        var approachOffset = new Vector2(
            Random.Range(compactLayout ? -3f : -7f, compactLayout ? 3f : 7f),
            Random.Range(compactLayout ? -3f : -7f, compactLayout ? 3f : 7f));
        var finalPoint = targetLocalPoint + approachOffset;
        var dropDuration = Random.Range(DropDurationMin, DropDurationMax);
        var flyDuration = Random.Range(FlyDurationMin, FlyDurationMax);
        var totalDuration = dropDuration + flyDuration;
        var startDelay = Random.Range(0f, 0.08f);
        var curveControlPoint = BuildCurveControlPoint(scatterPoint, finalPoint, compactLayout);
        var didComplete = false;

        var sequence = DOTween.Sequence();
        sequence.SetLink(moteObject, LinkBehaviour.KillOnDestroy);
        sequence.AppendInterval(startDelay);
        sequence.Append(moteRect.DOAnchorPos(scatterPoint, dropDuration).SetEase(Ease.OutQuad));
        sequence.Join(moteRect.DORotate(
            new Vector3(0f, 0f, moteRect.localEulerAngles.z + Random.Range(-70f, 70f)),
            dropDuration));
        sequence.Join(moteRect.DOScale(Random.Range(0.8f, 1f), dropDuration).SetEase(Ease.OutQuad));
        sequence.Append(DOVirtual.Float(0f, 1f, flyDuration, value =>
        {
            var curvedPosition = EvaluateQuadraticBezier(scatterPoint, curveControlPoint, finalPoint, value);
            moteRect.anchoredPosition = curvedPosition;

            var tangent = EvaluateQuadraticBezierTangent(scatterPoint, curveControlPoint, finalPoint, value);
            if (tangent.sqrMagnitude > 0.0001f)
            {
                var angle = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg - 90f;
                moteRect.localRotation = Quaternion.Euler(0f, 0f, angle);
            }
        }).SetEase(Ease.InOutSine));
        sequence.Join(moteRect.DOScale(0.18f, flyDuration).SetEase(Ease.InQuad));
        sequence.Join(moteImage.DOFade(0.2f, flyDuration).SetEase(Ease.InQuad));

        for (var i = 0; i < trailImages.Length; i++)
        {
            if (trailImages[i] == null)
            {
                continue;
            }

            sequence.Insert(startDelay, trailImages[i].DOFade(0f, totalDuration).SetEase(Ease.OutQuad));
        }

        sequence.OnUpdate(() => UpdateTrailMotes(moteRect, trailRects, trailImages));
        sequence.OnComplete(() =>
        {
            didComplete = true;
            if (triggerTargetPulse)
            {
                PulseTarget();
            }

            CleanupMoteObjects(moteObject, trailRects);
        });
        sequence.OnKill(() =>
        {
            if (!didComplete)
            {
                CleanupMoteObjects(moteObject, trailRects);
            }
        });
    }

    private void InitializeTrailMotes(
        Vector2 startLocalPoint,
        float baseSize,
        float baseScale,
        RectTransform[] trailRects,
        RawImage[] trailImages)
    {
        for (var i = 0; i < TrailSegmentCount; i++)
        {
            var trailObject = new GameObject("FaithPickupTrail", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(RawImage));
            var trailRect = trailObject.transform as RectTransform;
            trailRect.SetParent(effectRoot, false);
            trailRect.anchorMin = new Vector2(0.5f, 0.5f);
            trailRect.anchorMax = new Vector2(0.5f, 0.5f);
            trailRect.pivot = new Vector2(0.5f, 0.5f);
            trailRect.anchoredPosition = startLocalPoint;
            trailRect.sizeDelta = Vector2.one * Mathf.Max(1.3f, baseSize * (1.08f - 0.16f * i));
            trailRect.localScale = Vector3.one * Mathf.Max(0.35f, baseScale * (1.02f - 0.1f * i));

            var trailImage = trailObject.GetComponent<RawImage>();
            trailImage.texture = Texture2D.whiteTexture;
            trailImage.raycastTarget = false;
            trailImage.color = new Color(
                0.62f + 0.05f * i,
                0.9f + 0.02f * i,
                1f,
                Mathf.Max(0.1f, TrailBaseAlpha - 0.08f * i));

            trailRects[i] = trailRect;
            trailImages[i] = trailImage;
        }
    }

    private static void UpdateTrailMotes(RectTransform moteRect, RectTransform[] trailRects, RawImage[] trailImages)
    {
        if (moteRect == null || trailRects == null)
        {
            return;
        }

        var sourcePosition = moteRect.anchoredPosition;
        var sourceScale = moteRect.localScale.x;
        var sourceRotation = moteRect.localEulerAngles.z;

        for (var i = 0; i < trailRects.Length; i++)
        {
            var trailRect = trailRects[i];
            if (trailRect == null)
            {
                continue;
            }

            var followFactor = Mathf.Max(0.08f, TrailBaseFollowFactor - i * 0.04f);
            trailRect.anchoredPosition = Vector2.Lerp(trailRect.anchoredPosition, sourcePosition, followFactor);
            trailRect.localScale = Vector3.Lerp(
                trailRect.localScale,
                Vector3.one * Mathf.Max(0.26f, sourceScale * (1.02f - 0.12f * i)),
                0.2f);
            trailRect.localRotation = Quaternion.Lerp(
                trailRect.localRotation,
                Quaternion.Euler(0f, 0f, sourceRotation),
                0.18f);

            sourcePosition = trailRect.anchoredPosition;
            sourceScale = trailRect.localScale.x;
            sourceRotation = trailRect.localEulerAngles.z;

            if (trailImages != null && i < trailImages.Length && trailImages[i] != null)
            {
                trailImages[i].transform.SetAsFirstSibling();
            }
        }
    }

    private Vector2 BuildCurveControlPoint(Vector2 startPoint, Vector2 endPoint, bool compactLayout)
    {
        var midpoint = (startPoint + endPoint) * 0.5f;
        var direction = endPoint - startPoint;
        var distance = direction.magnitude;
        if (distance <= 0.001f)
        {
            return midpoint;
        }

        var normalizedDirection = direction / distance;
        var arcNormal = new Vector2(-normalizedDirection.y, normalizedDirection.x);
        if (arcNormal.y < 0f)
        {
            arcNormal = -arcNormal;
        }

        var arcHeight = Mathf.Clamp(
            distance * CurveHeightFactor,
            compactLayout ? 10f : 14f,
            compactLayout ? 18f : 28f);
        var upwardLift = compactLayout ? 4f : 6f;
        return midpoint + arcNormal * arcHeight + Vector2.up * upwardLift;
    }

    private static Vector2 EvaluateQuadraticBezier(Vector2 startPoint, Vector2 controlPoint, Vector2 endPoint, float t)
    {
        var inverseT = 1f - t;
        return inverseT * inverseT * startPoint
               + 2f * inverseT * t * controlPoint
               + t * t * endPoint;
    }

    private static Vector2 EvaluateQuadraticBezierTangent(Vector2 startPoint, Vector2 controlPoint, Vector2 endPoint,
        float t)
    {
        return 2f * (1f - t) * (controlPoint - startPoint)
               + 2f * t * (endPoint - controlPoint);
    }

    private void PulseTarget()
    {
        if (pulseTargetRect == null)
        {
            return;
        }

        PunchScaleTarget(pulseTargetRect);

        if (pickupTargetRect != null && pickupTargetRect != pulseTargetRect)
        {
            PunchScaleTarget(pickupTargetRect);
        }

        if (targetText == null)
        {
            return;
        }

        targetText.DOKill();
        var baseColor = targetText.color;
        targetText.color = new Color(0.74f, 0.94f, 1f, baseColor.a);
        targetText.DOColor(baseColor, 0.2f).SetEase(Ease.OutQuad);
    }

    private static void PunchScaleTarget(RectTransform targetRect)
    {
        if (targetRect == null)
        {
            return;
        }

        targetRect.DOKill();
        targetRect.localScale = Vector3.one;
        targetRect.DOPunchScale(new Vector3(0.18f, 0.18f, 0f), 0.24f, 5, 0.7f);
    }

    private static void CleanupMoteObjects(GameObject moteObject, RectTransform[] trailRects)
    {
        if (moteObject != null)
        {
            Destroy(moteObject);
        }

        if (trailRects == null)
        {
            return;
        }

        for (var i = 0; i < trailRects.Length; i++)
        {
            if (trailRects[i] != null)
            {
                Destroy(trailRects[i].gameObject);
            }
        }
    }

    private int CalculateVisualCount(int faithAmount)
    {
        if (faithAmount <= 1)
        {
            return 3;
        }

        return Mathf.Clamp(faithAmount + 2, 3, 6);
    }

    private bool IsCompactCanvas()
    {
        if (canvasRect == null)
        {
            return false;
        }

        return canvasRect.rect.width <= CompactCanvasWidthThreshold
               || canvasRect.rect.height <= CompactCanvasHeightThreshold;
    }

    private Camera GetRenderCamera()
    {
        return ownerCanvas != null && ownerCanvas.worldCamera != null
            ? ownerCanvas.worldCamera
            : Camera.main;
    }

    private Camera GetUiCamera()
    {
        if (ownerCanvas == null || ownerCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        return ownerCanvas.worldCamera != null ? ownerCanvas.worldCamera : Camera.main;
    }
}