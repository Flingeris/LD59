using UnityEngine;

[CreateAssetMenu(menuName = "Bellgrave/Defs/Enemy Def")]
public class EnemyDef : ContentDef
{
    [SerializeField] private int hp;
    [SerializeField] private int damage;
    [SerializeField] private float attackInterval;
    [SerializeField] private float moveSpeed;
    [SerializeField] private GameObject viewPrefab;

    public int Hp => hp;
    public int Damage => damage;
    public float AttackInterval => attackInterval;
    public float MoveSpeed => moveSpeed;
    public GameObject ViewPrefab => viewPrefab;
}
