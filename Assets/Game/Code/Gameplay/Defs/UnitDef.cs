using UnityEngine;

[CreateAssetMenu(menuName = "Bellgrave/Defs/Unit Def")]
public class UnitDef : ContentDef
{
    [SerializeField] private int hp;
    [SerializeField] private int damage;
    [SerializeField] private float attackInterval;
    [SerializeField] private float moveSpeed;
    [Min(0f)] [SerializeField] private float lifetimeSeconds;
    [SerializeField] private GameObject viewPrefab;

    public int Hp => hp;
    public int Damage => damage;
    public float AttackInterval => attackInterval;
    public float MoveSpeed => moveSpeed;
    public float LifetimeSeconds => lifetimeSeconds;
    public GameObject ViewPrefab => viewPrefab;
}
