using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

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
    [SerializeField] private TMP_Text bellFeedbackText;
    [SerializeField] private BellButtonBinding[] bellButtons;

    private Main main;
    private bool missingMainWarningShown;

    private void Awake()
    {
        G.HUD = this;
        BindBellButtons();
        RefreshBellButtonLabels();
    }

    private void OnDestroy()
    {
        UnbindBellButtons();
    }

    private void Update()
    {
        RefreshView();
    }

    public void RefreshView()
    {
        if (!TryGetMain(out var currentMain) || currentMain.RunState == null)
        {
            if (!missingMainWarningShown)
            {
                Debug.LogWarning("HUD: Main or RunState not found");
                missingMainWarningShown = true;
            }

            return;
        }

        missingMainWarningShown = false;

        var runState = currentMain.RunState;

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

    private void OnBellButtonPressed(string bellId)
    {
        if (!TryGetMain(out var currentMain))
        {
            SetBellFeedback("Main not found");
            return;
        }

        var result = currentMain.TryRingBell(bellId);
        if (result.IsSuccess && result.SpawnResult != null && result.SpawnResult.IsSuccess)
        {
            SetBellFeedback($"Bell: {result.BellDef.DisplayName}");
            return;
        }

        if (!result.IsSuccess)
        {
            SetBellFeedback($"Bell failed: {result.FailureReason}");
            return;
        }

        if (result.SpawnResult != null && !result.SpawnResult.IsSuccess)
        {
            SetBellFeedback($"Spawn failed: {result.SpawnResult.FailureReason}");
        }
    }

    private void SetBellFeedback(string message)
    {
        if (bellFeedbackText != null)
        {
            bellFeedbackText.text = message;
        }
    }

    private bool TryGetMain(out Main currentMain)
    {
        if (main != null)
        {
            currentMain = main;
            return true;
        }

        if (G.main != null)
        {
            main = G.main;
            currentMain = main;
            return true;
        }

        main = FindFirstObjectByType<Main>();
        if (main != null)
        {
            G.main = main;
            currentMain = main;
            return true;
        }

        currentMain = null;
        return false;
    }
}
