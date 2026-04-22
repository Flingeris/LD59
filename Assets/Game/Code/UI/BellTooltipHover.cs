using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(BellWorldObject))]
public sealed class BellTooltipHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private BellTooltipView tooltipView;

    private BellWorldObject bellWorldObject;

    private void Awake()
    {
        bellWorldObject = GetComponent<BellWorldObject>();
        TryResolveTooltip();
    }

    private void TryResolveTooltip()
    {
        if (tooltipView == null)
        {
            tooltipView = FindAnyObjectByType<BellTooltipView>(FindObjectsInactive.Include);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        TryResolveTooltip();

        if (tooltipView == null || bellWorldObject == null)
            return;

        var bellId = bellWorldObject.BellId;

        var bellDef = TryGetBellDef(bellId);
        var resolved = ResolveTooltipData(bellId, bellDef);

        tooltipView.Show(resolved.title, resolved.body);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltipView != null)
            tooltipView.Hide();
    }

    private void OnDisable()
    {
        if (tooltipView != null)
            tooltipView.Hide();
    }

    private void OnDestroy()
    {
        if (tooltipView != null)
            tooltipView.Hide();
    }

    private BellDef TryGetBellDef(string bellId)
    {
        if (string.IsNullOrWhiteSpace(bellId))
            return null;

        // CMS.Get<T>() логирует ошибку при отсутствии.
        // Для hover лучше сначала тихо поискать через GetAll.
        foreach (var bell in CMS.GetAll<BellDef>())
        {
            if (bell != null && bell.Id == bellId)
                return bell;
        }

        return null;
    }

    private UnitDef TryGetUnitDef(string unitId)
    {
        if (string.IsNullOrWhiteSpace(unitId))
            return null;

        foreach (var unit in CMS.GetAll<UnitDef>())
        {
            if (unit != null && unit.Id == unitId)
                return unit;
        }

        return null;
    }

    private (string title, string body) ResolveTooltipData(string bellId, BellDef bellDef)
    {
        string title = "Unknown Bell";
        int faithCost = 0;
        UnitDef unitDef = null;

        if (bellDef != null)
        {
            title = BellTooltipView.BuildTitle(bellDef, bellId);

            faithCost = bellDef.FaithCost;
            if (G.main != null && G.main.RunState != null)
            {
                faithCost = Mathf.Max(0, faithCost + G.main.RunState.BellFaithCostModifier);
            }

            unitDef = TryGetUnitDef(bellDef.LinkedUnitId);
        }
        else
        {
            // Fallback для случаев, когда колокол создан/инициализирован кодом нестандартно
            // или bellId ещё не лежит в CMS.
            switch (bellId)
            {
                case "bell_small":
                    title = "Bone Bell";
                    faithCost = 7;
                    unitDef = TryGetUnitDef("skel1");
                    break;

                case "bell_zombie":
                    title = "Grave Bell";
                    faithCost = 14;
                    unitDef = TryGetUnitDef("zombie");
                    break;

                case "bell_vampire":
                    title = "Blood Bell";
                    faithCost = 24;
                    unitDef = TryGetUnitDef("vampire");
                    break;

                default:
                    title = string.IsNullOrWhiteSpace(bellId) ? "Unknown Bell" : bellId;
                    faithCost = 0;
                    unitDef = null;
                    break;
            }

            if (G.main != null && G.main.RunState != null)
            {
                faithCost = Mathf.Max(0, faithCost + G.main.RunState.BellFaithCostModifier);
            }
        }

        var body = BellTooltipView.BuildBody(bellDef, unitDef, faithCost);
        return (title, body);
    }
}
