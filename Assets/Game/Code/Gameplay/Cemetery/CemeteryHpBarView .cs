using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class CemeteryHpBarView : MonoBehaviour
{
    [SerializeField] private WorldBarFillGrow hpBar;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private bool showOnlyAtNight = true;

    private void Awake()
    {
        EnsureReferences();
        SetVisible(false);
    }

    private void OnDisable()
    {
        SetVisible(false);
    }

    private void Update()
    {
        EnsureReferences();

        if (G.main == null || G.main.RunState == null)
        {
            SetVisible(false);
            return;
        }

        var runState = G.main.RunState;

        if (showOnlyAtNight && runState.CurrentPhase != GamePhase.Night)
        {
            SetVisible(false);
            return;
        }

        var maxHp = Mathf.Max(1, runState.CemeteryMaxState);
        var currentHp = Mathf.Clamp(runState.CemeteryState, 0, maxHp);
        var normalized = (float)currentHp / maxHp;

        SetVisible(true);
        hpBar?.SetNormalized(normalized);

        if (hpText != null)
        {
            hpText.text = $"{currentHp}/{maxHp}";
        }
    }

    private void SetVisible(bool visible)
    {
        hpBar?.SetVisible(visible);

        if (hpText != null)
            hpText.gameObject.SetActive(visible);
    }

    private void EnsureReferences()
    {
        if (hpBar == null)
            hpBar = GetComponentInChildren<WorldBarFillGrow>(true);

        if (hpText == null)
            hpText = GetComponentInChildren<TMP_Text>(true);
    }
}