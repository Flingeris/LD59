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

        if (tooltipView == null)
            tooltipView = FindFirstObjectByType<BellTooltipView>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tooltipView == null || bellWorldObject == null)
            return;

        var bellDef = CMS.Get<BellDef>(bellWorldObject.BellId);
        if (bellDef == null)
            return;

        var unitDef = CMS.Get<UnitDef>(bellDef.LinkedUnitId);

        int faithCost = bellDef.FaithCost;
        if (G.main != null && G.main.RunState != null)
            faithCost = Mathf.Max(0, bellDef.FaithCost + G.main.RunState.BellFaithCostModifier);

        var title = BellTooltipView.BuildTitle(bellDef, bellWorldObject.BellId);
        var body = BellTooltipView.BuildBody(bellDef, unitDef, faithCost);

        tooltipView.Show(title, body);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltipView == null)
            return;

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
}