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

    internal void InitializeRuntime(
        string contentId,
        int contentHp,
        int contentDamage,
        float contentAttackInterval,
        float contentMoveSpeed,
        float contentLifetimeSeconds,
        GameObject contentViewPrefab)
    {
        InitializeContent(contentId);
        hp = contentHp;
        damage = contentDamage;
        attackInterval = contentAttackInterval;
        moveSpeed = contentMoveSpeed;
        lifetimeSeconds = contentLifetimeSeconds;
        viewPrefab = contentViewPrefab;
    }
}
