using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Random = UnityEngine.Random;

public class HUD : MonoBehaviour
{
    [System.Serializable]
    private class BellButtonBinding
    {
        public string bellId;
        public Button button;
        public TMP_Text label;
        [HideInInspector] public UnityAction clickAction;
    }

    [SerializeField] private TMP_Text faithText;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text cemeteryStateText;
    [SerializeField] private TMP_Text phaseText;
    [SerializeField] private WaveProgressView waveProgressView;
    [SerializeField] private DayScreenView dayScreenView;
    [SerializeField] private DefeatScreenView defeatScreenView;
    [SerializeField] private WinScreenView winScreenView;
    [SerializeField] private PhaseTransitionView phaseTransitionView;
    [SerializeField] private TMP_Text bellFeedbackText;
    [SerializeField] private BellButtonBinding[] bellButtons;

    public event Action DayScreenStartNightRequested;
    public event Action<string> DayScreenUpgradePurchaseRequested;
    public event Action DefeatScreenRestartRequested;
    public event Action WinScreenRestartRequested;

    private bool missingMainWarningShown;
    private GoldPickupVfxController goldPickupVfxController;

    private void Awake()
    {
        G.HUD = this;
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
            faithText.text = $"Faith: {runState.Faith}";
        }

        if (goldText != null)
        {
            goldText.text = $"Gold: {runState.Gold}";
        }

        if (cemeteryStateText != null)
        {
            cemeteryStateText.text = $"Cemetery: {runState.CemeteryState}";
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

    public void ShowBellFeedback(string message)
    {
        SetBellFeedback(message);
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
        SetBellFeedback("Use bells in the world");
    }

    private void SetBellFeedback(string message)
    {
        if (bellFeedbackText != null)
        {
            bellFeedbackText.text = message;
        }
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

        goldPickupVfxController.Initialize(goldText);
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
        SetViewVisible(bellFeedbackText, visible);

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

    private Canvas ownerCanvas;
    private RectTransform canvasRect;
    private RectTransform effectRoot;
    private TMP_Text targetText;
    private RectTransform targetRect;

    public void Initialize(TMP_Text goldCounterText)
    {
        targetText = goldCounterText;
        targetRect = goldCounterText != null ? goldCounterText.rectTransform : null;
        ownerCanvas ??= GetComponent<Canvas>();
        canvasRect ??= transform as RectTransform;
        EnsureEffectRoot();
    }

    public void PlayFromWorld(Vector3 worldPosition, int goldAmount)
    {
        if (goldAmount <= 0 || targetRect == null || canvasRect == null)
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
        targetRect.GetWorldCorners(worldCorners);
        var targetWorldPoint = Vector3.Lerp(worldCorners[0], worldCorners[1], 0.5f);
        targetWorldPoint.x -= 2f;

        var screenPoint = RectTransformUtility.WorldToScreenPoint(GetUiCamera(), targetWorldPoint);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPoint,
            GetUiCamera(),
            out var localPoint);
        return localPoint;
    }

    private void SpawnCoinMote(Vector2 startLocalPoint, Vector2 targetLocalPoint, bool compactLayout, bool triggerTargetPulse)
    {
        var moteObject = new GameObject("GoldPickupMote", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        var moteRect = moteObject.transform as RectTransform;
        moteRect.SetParent(effectRoot, false);
        moteRect.anchorMin = new Vector2(0.5f, 0.5f);
        moteRect.anchorMax = new Vector2(0.5f, 0.5f);
        moteRect.pivot = new Vector2(0.5f, 0.5f);

        var size = compactLayout ? Random.Range(2.5f, 4f) : Random.Range(4f, 6f);
        moteRect.sizeDelta = new Vector2(size, size);
        moteRect.anchoredPosition = startLocalPoint;
        moteRect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        moteRect.localScale = Vector3.one * Random.Range(0.8f, 1.05f);

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
        var scatterPoint = startLocalPoint + scatterOffset;
        var approachOffset = new Vector2(
            Random.Range(compactLayout ? -3f : -7f, compactLayout ? 3f : 7f),
            Random.Range(compactLayout ? -3f : -7f, compactLayout ? 3f : 7f));
        var finalPoint = targetLocalPoint + approachOffset;
        var dropDuration = Random.Range(DropDurationMin, DropDurationMax);
        var flyDuration = Random.Range(FlyDurationMin, FlyDurationMax);
        var startDelay = Random.Range(0f, 0.08f);

        var sequence = DOTween.Sequence();
        sequence.SetLink(moteObject, LinkBehaviour.KillOnDestroy);
        sequence.AppendInterval(startDelay);
        sequence.Append(moteRect.DOAnchorPos(scatterPoint, dropDuration).SetEase(Ease.OutQuad));
        sequence.Join(moteRect.DORotate(
            new Vector3(0f, 0f, moteRect.localEulerAngles.z + Random.Range(-70f, 70f)),
            dropDuration,
            RotateMode.Fast));
        sequence.Join(moteRect.DOScale(Random.Range(0.9f, 1.15f), dropDuration).SetEase(Ease.OutQuad));
        sequence.Append(moteRect.DOAnchorPos(finalPoint, flyDuration).SetEase(Ease.InQuad));
        sequence.Join(moteRect.DOScale(0.25f, flyDuration).SetEase(Ease.InQuad));
        sequence.Join(moteImage.DOFade(0.2f, flyDuration).SetEase(Ease.InQuad));
        sequence.OnComplete(() =>
        {
            if (triggerTargetPulse)
            {
                PulseTarget();
            }

            Destroy(moteObject);
        });
    }

    private void PulseTarget()
    {
        if (targetRect == null)
        {
            return;
        }

        targetRect.DOKill();
        targetRect.localScale = Vector3.one;
        targetRect.DOPunchScale(new Vector3(0.18f, 0.18f, 0f), 0.24f, 5, 0.7f);

        if (targetText == null)
        {
            return;
        }

        targetText.DOKill();
        var baseColor = targetText.color;
        targetText.color = new Color(1f, 0.96f, 0.76f, baseColor.a);
        targetText.DOColor(baseColor, 0.2f).SetEase(Ease.OutQuad);
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
