using UnityEngine;

[CreateAssetMenu(menuName = "Bellgrave/Defs/Enemy Def")]
public class EnemyDef : ContentDef
{
    [SerializeField] private int hp;
    [SerializeField] private int damage;
    [SerializeField] private int goldReward = 1;
    [SerializeField] private float attackInterval;
    [SerializeField] private float moveSpeed;
    [SerializeField] private GameObject viewPrefab;

    public int Hp => hp;
    public int Damage => damage;
    public int GoldReward => goldReward;
    public float AttackInterval => attackInterval;
    public float MoveSpeed => moveSpeed;
    public GameObject ViewPrefab => viewPrefab;
}
