using UnityEngine;

[CreateAssetMenu(menuName = "Bellgrave/Defs/Upgrade Def")]
public class UpgradeDef : ContentDef
{
    [SerializeField] private string displayName;
    [SerializeField] private int price;
    [SerializeField] private UpgradeEffectType effectType;
    [SerializeField] private float effectValue;
    [SerializeField] private string targetUnitId;

    public string DisplayName => displayName;
    public int Price => price;
    public UpgradeEffectType EffectType => effectType;
    public float EffectValue => effectValue;
    public string TargetUnitId => targetUnitId;

    internal void InitializeRuntime(
        string contentId,
        string contentDisplayName,
        int contentPrice,
        UpgradeEffectType contentEffectType,
        float contentEffectValue,
        string contentTargetUnitId = null)
    {
        InitializeContent(contentId);
        displayName = contentDisplayName;
        price = contentPrice;
        effectType = contentEffectType;
        effectValue = contentEffectValue;
        targetUnitId = contentTargetUnitId;
    }
}
