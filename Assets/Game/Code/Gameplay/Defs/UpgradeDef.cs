using UnityEngine;

[CreateAssetMenu(menuName = "Bellgrave/Defs/Upgrade Def")]
public class UpgradeDef : ContentDef
{
    [SerializeField] private string displayName;
    [SerializeField] private int price;
    [SerializeField] private UpgradeEffectType effectType;
    [SerializeField] private int effectValue;

    public string DisplayName => displayName;
    public int Price => price;
    public UpgradeEffectType EffectType => effectType;
    public int EffectValue => effectValue;
}
