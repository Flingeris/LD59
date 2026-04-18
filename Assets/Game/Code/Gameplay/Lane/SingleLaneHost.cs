using UnityEngine;

public class SingleLaneHost : MonoBehaviour
{
    [SerializeField] private Transform playerSpawnPoint;
    [SerializeField] private Transform[] playerDefenseSlots;
    [SerializeField] private float alliedHomeSpreadRadius = 0.35f;
    [SerializeField] private float alliedIdleMicroRadius = 0.08f;
    [SerializeField] private float alliedIdleMicroMoveSpeed = 0.45f;
    [SerializeField] private float alliedIdleMicroRetargetDelay = 1.1f;
    [SerializeField] private float alliedDefenseLeashDistance = 3f;
    [SerializeField] private Transform enemyAggroThresholdPoint;
    [SerializeField] private Transform enemyBreakthroughPoint;
    [SerializeField] private Transform enemySpawnPoint;
    [SerializeField] private Transform enemyForwardTarget;
    [SerializeField] private float enemySpawnSpreadRadius = 0.3f;

    public Transform PlayerSpawnPoint => playerSpawnPoint;
    public Transform[] PlayerDefenseSlots => playerDefenseSlots;
    public float AlliedHomeSpreadRadius => alliedHomeSpreadRadius;
    public float AlliedIdleMicroRadius => alliedIdleMicroRadius;
    public float AlliedIdleMicroMoveSpeed => alliedIdleMicroMoveSpeed;
    public float AlliedIdleMicroRetargetDelay => alliedIdleMicroRetargetDelay;
    public float AlliedDefenseLeashDistance => alliedDefenseLeashDistance;
    public Transform EnemyAggroThresholdPoint => enemyAggroThresholdPoint;
    public Transform EnemyBreakthroughPoint => enemyBreakthroughPoint;
    public Transform EnemySpawnPoint => enemySpawnPoint;
    public Transform EnemyForwardTarget => enemyForwardTarget;
    public float EnemySpawnSpreadRadius => enemySpawnSpreadRadius;
}
