using System;
using UnityEngine;

public class LaneEnemy : MonoBehaviour
{
    private const int SortingPrecision = 100;

    public event Action<LaneEnemy> CemeteryAttackTriggered;

    public EnemyDef EnemyDef { get; private set; }
    public Vector3 Position => transform.position;
    public int CurrentHp { get; private set; }
    public LaneUnit TargetUnit { get; private set; }
    public bool HasTarget => TargetUnit != null;
    public bool IsInCombat => TargetUnit != null;
    public bool IsAttackingCemetery { get; private set; }

    private Vector3 forwardTargetPosition;
    private Vector3 cemeteryAttackPosition;
    private float attackCooldown;
    private LaneCombatFeedbackView combatFeedbackView;
    private SpriteRenderer spriteRenderer;

    private void Update()
    {
        if (EnemyDef == null)
        {
            return;
        }

        UpdateBehavior(Time.deltaTime);
    }

    private void LateUpdate()
    {
        UpdateSortingOrder();
    }

    public void Initialize(EnemyDef enemyDef, Vector3 targetPosition)
    {
        EnemyDef = enemyDef;
        CurrentHp = enemyDef.Hp;
        forwardTargetPosition = targetPosition;
        cemeteryAttackPosition = targetPosition;
        TargetUnit = null;
        attackCooldown = enemyDef.AttackInterval;
        IsAttackingCemetery = false;
        combatFeedbackView = GetComponent<LaneCombatFeedbackView>();
        if (combatFeedbackView == null)
        {
            combatFeedbackView = gameObject.AddComponent<LaneCombatFeedbackView>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        combatFeedbackView.Bind(enemyDef.Hp, true);
        UpdateSortingOrder();
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

    public void EnterCemeteryAttackState(Vector3 attackPosition)
    {
        IsAttackingCemetery = true;
        cemeteryAttackPosition = attackPosition;
        forwardTargetPosition = attackPosition;
        TargetUnit = null;
        attackCooldown = EnemyDef != null ? EnemyDef.AttackInterval : 0f;
        transform.position = attackPosition;
    }

    private void UpdateBehavior(float deltaTime)
    {
        if (TargetUnit == null || TargetUnit.IsDead())
        {
            TargetUnit = null;

            if (IsAttackingCemetery)
            {
                UpdateCemeteryAttack(deltaTime);
                return;
            }

            MoveForward(deltaTime);
            return;
        }

        attackCooldown -= deltaTime;
        if (attackCooldown > 0f)
        {
            return;
        }

        TargetUnit.ApplyDamage(EnemyDef.Damage);
        G.audioSystem.PlayRandomPitched(SoundId.SFX_CombatHit);
        attackCooldown = EnemyDef.AttackInterval;
    }

    private void UpdateCemeteryAttack(float deltaTime)
    {
        attackCooldown -= deltaTime;
        if (attackCooldown > 0f)
        {
            return;
        }

        CemeteryAttackTriggered?.Invoke(this);
        G.audioSystem.PlayRandomPitched(SoundId.SFX_CombatHit);
        attackCooldown = EnemyDef.AttackInterval;
        transform.position = cemeteryAttackPosition;
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

    private void UpdateSortingOrder()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.sortingOrder = -Mathf.RoundToInt(transform.position.y * SortingPrecision);
    }
}