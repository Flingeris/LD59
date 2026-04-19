using UnityEngine;

public class LaneEnemy : MonoBehaviour
{
    public EnemyDef EnemyDef { get; private set; }
    public Vector3 Position => transform.position;
    public int CurrentHp { get; private set; }
    public LaneUnit TargetUnit { get; private set; }
    public bool HasTarget => TargetUnit != null;
    public bool IsInCombat => TargetUnit != null;

    private Vector3 forwardTargetPosition;
    private float attackCooldown;
    private LaneCombatFeedbackView combatFeedbackView;

    private void Update()
    {
        if (EnemyDef == null)
        {
            return;
        }

        UpdateBehavior(Time.deltaTime);
    }

    public void Initialize(EnemyDef enemyDef, Vector3 targetPosition)
    {
        EnemyDef = enemyDef;
        CurrentHp = enemyDef.Hp;
        forwardTargetPosition = targetPosition;
        TargetUnit = null;
        attackCooldown = enemyDef.AttackInterval;
        combatFeedbackView = GetComponent<LaneCombatFeedbackView>();
        if (combatFeedbackView == null)
        {
            combatFeedbackView = gameObject.AddComponent<LaneCombatFeedbackView>();
        }

        combatFeedbackView.Bind(enemyDef.Hp, true);
    }

    public void SetTargetUnit(LaneUnit laneUnit)
    {
        if (laneUnit == null)
        {
            return;
        }

        TargetUnit = laneUnit;
    }

    public void ClearTargetUnit()
    {
        TargetUnit = null;
    }

    public void ApplyDamage(int damage)
    {
        if (damage <= 0)
        {
            return;
        }

        CurrentHp -= damage;
        combatFeedbackView?.PlayDamageFeedback(damage, CurrentHp);
    }

    private void OnDestroy()
    {
        combatFeedbackView?.Cleanup();
    }

    public bool IsDead()
    {
        return CurrentHp <= 0;
    }

    public bool IsWithinAttackRange(LaneUnit laneUnit, float attackDistance)
    {
        if (laneUnit == null)
        {
            return false;
        }

        return Vector3.Distance(Position, laneUnit.Position) <= attackDistance;
    }

    public bool HasReachedBreakthrough(float breakthroughX)
    {
        return Position.x <= breakthroughX;
    }

    private void UpdateBehavior(float deltaTime)
    {
        if (TargetUnit == null || TargetUnit.IsDead())
        {
            TargetUnit = null;
            MoveForward(deltaTime);
            return;
        }

        attackCooldown -= deltaTime;
        if (attackCooldown > 0f)
        {
            return;
        }

        TargetUnit.ApplyDamage(EnemyDef.Damage);
        attackCooldown = EnemyDef.AttackInterval;
    }

    private void MoveForward(float deltaTime)
    {
        var offset = forwardTargetPosition - transform.position;
        if (offset.sqrMagnitude <= 0.0001f)
        {
            transform.position = forwardTargetPosition;
            return;
        }

        var direction = offset.normalized;
        var moveStep = EnemyDef.MoveSpeed * deltaTime;
        if (offset.magnitude <= moveStep)
        {
            transform.position = forwardTargetPosition;
            return;
        }

        transform.position += direction * moveStep;
    }
}
