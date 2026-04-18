using UnityEngine;

[CreateAssetMenu(menuName = "Bellgrave/Defs/Bell Def")]
public class BellDef : ContentDef
{
    [SerializeField] private string displayName;
    [SerializeField] private int faithCost;
    [SerializeField] private string linkedUnitId;
    [SerializeField] private Sprite icon;

    public string DisplayName => displayName;
    public int FaithCost => faithCost;
    public string LinkedUnitId => linkedUnitId;
    public Sprite Icon => icon;
}
