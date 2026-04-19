using UnityEngine;

public class LaneUnit : MonoBehaviour
{
    private const float IdleHoldDistance = 0.05f;
    private const int IdleTargetPickAttempts = 6;
    private const int SortingPrecision = 100;
    private const float FacingThreshold = 0.001f;

    public UnitDef UnitDef { get; private set; }
    public Vector3 Position => transform.position;
    public LaneUnitState State { get; private set; }
    public bool IsInCombat => State == LaneUnitState.Attacking;
    public int CurrentHp { get; private set; }
    public Vector3 HomeSlotCenter { get; private set; }
    public Vector3 IdleAnchorPosition { get; private set; }
    public LaneEnemy TargetEnemy { get; private set; }
    public bool HasTarget => TargetEnemy != null;
    public bool HasLimitedLifetime => UnitDef != null && UnitDef.LifetimeSeconds > 0f;
    public float LifetimeProgressNormalized =>
        !HasLimitedLifetime
            ? 0f
            : Mathf.Clamp01(1f - (Mathf.Max(0f, remainingLifetime) / Mathf.Max(0.01f, UnitDef.LifetimeSeconds)));

    private float attackCooldown;
    private bool hasHomePosition;
    private Vector3 idleMicroTargetPosition;
    private float idleMicroRadius;
    private float idleMicroMoveSpeed;
    private float idleMicroRetargetDelay;
    private float idleMicroRetargetTimer;
    private int idleMicroRetargetIndex;
    private LaneCombatFeedbackView combatFeedbackView;
    private SpriteRenderer spriteRenderer;
    private float remainingLifetime = -1f;

    private void Update()
    {
        if (UnitDef == null)
        {
            return;
        }

        if (UpdateLifetime(Time.deltaTime))
        {
            return;
        }

        UpdateState(Time.deltaTime);
    }

    private void LateUpdate()
    {
        UpdateSortingOrder();
    }

    public void Initialize(UnitDef unitDef)
    {
        UnitDef = unitDef;
        CurrentHp = unitDef.Hp;
        State = LaneUnitState.Waiting;
        TargetEnemy = null;
        attackCooldown = unitDef.AttackInterval;
        remainingLifetime = unitDef.LifetimeSeconds > 0f ? unitDef.LifetimeSeconds : -1f;
        combatFeedbackView = GetComponent<LaneCombatFeedbackView>();
        if (combatFeedbackView == null)
        {
            combatFeedbackView = gameObject.AddComponent<LaneCombatFeedbackView>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        combatFeedbackView.Bind(unitDef.Hp, false);
        UpdateSortingOrder();
    }

    public void AssignHomePosition(
        Vector3 homeSlotCenter,
        Vector3 idleAnchorPosition,
        float idleMicroRadius,
        float idleMicroMoveSpeed,
        float idleMicroRetargetDelay)
    {
        HomeSlotCenter = homeSlotCenter;
        IdleAnchorPosition = idleAnchorPosition;
        hasHomePosition = true;
        this.idleMicroRadius = Mathf.Max(0f, idleMicroRadius);
        this.idleMicroMoveSpeed = Mathf.Max(0.01f, idleMicroMoveSpeed);
        this.idleMicroRetargetDelay = Mathf.Max(0.1f, idleMicroRetargetDelay);
        idleMicroRetargetTimer = 0f;
        idleMicroRetargetIndex = 0;
        idleMicroTargetPosition = idleAnchorPosition;
        ReturnToHome();
    }

    public void SetTargetEnemy(LaneEnemy laneEnemy)
    {
        if (laneEnemy == null)
        {
            return;
        }

        TargetEnemy = laneEnemy;
        State = LaneUnitState.Chasing;
    }

    public void EnterCombat()
    {
        if (TargetEnemy == null || TargetEnemy.IsDead())
        {
            ClearTargetEnemy();
            return;
        }

        State = LaneUnitState.Attacking;
    }

    public void ClearTargetEnemy()
    {
        TargetEnemy = null;

        if (hasHomePosition)
        {
            ReturnToHome();
        }
        else
        {
            State = LaneUnitState.Waiting;
        }
    }

    public void ReturnToHome()
    {
        TargetEnemy = null;
        idleMicroTargetPosition = IdleAnchorPosition;
        idleMicroRetargetTimer = 0f;
        State = hasHomePosition ? LaneUnitState.Returning : LaneUnitState.Waiting;
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

    public bool IsAvailableForAssignment()
    {
        return !IsDead() && (State == LaneUnitState.Waiting || State == LaneUnitState.Returning) && TargetEnemy == null;
    }

    public bool IsWithinAttackRange(LaneEnemy laneEnemy, float attackDistance)
    {
        if (laneEnemy == null)
        {
            return false;
        }

        return Vector3.Distance(Position, laneEnemy.Position) <= attackDistance;
    }

    private void UpdateState(float deltaTime)
    {
        switch (State)
        {
            case LaneUnitState.Waiting:
                UpdateWaiting(deltaTime);
                break;
            case LaneUnitState.Chasing:
                UpdateChasing(deltaTime);
                break;
            case LaneUnitState.Attacking:
                UpdateAttacking(deltaTime);
                break;
            case LaneUnitState.Returning:
                UpdateReturning(deltaTime);
                break;
        }
    }

    private void UpdateWaiting(float deltaTime)
    {
        if (!hasHomePosition)
        {
            return;
        }

        if (idleMicroRadius <= 0f)
        {
            if (IsCloseEnoughToIdleAnchor())
            {
                return;
            }

            MoveToPosition(IdleAnchorPosition, deltaTime);
            return;
        }

        UpdateIdleMicroMovement(deltaTime);
    }

    private void UpdateChasing(float deltaTime)
    {
        if (TargetEnemy == null || TargetEnemy.IsDead())
        {
            ClearTargetEnemy();
            return;
        }

        MoveToPosition(TargetEnemy.Position, deltaTime);
    }

    private void UpdateAttacking(float deltaTime)
    {
        if (TargetEnemy == null || TargetEnemy.IsDead())
        {
            ClearTargetEnemy();
            return;
        }

        attackCooldown -= deltaTime;
        if (attackCooldown > 0f)
        {
            return;
        }

        TargetEnemy.ApplyDamage(UnitDef.Damage);
        G.audioSystem.PlayRandomPitched(SoundId.SFX_CombatHit, 0.95f, 1.05f);
        attackCooldown = UnitDef.AttackInterval;
    }

    private void UpdateReturning(float deltaTime)
    {
        if (!hasHomePosition)
        {
            State = LaneUnitState.Waiting;
            return;
        }

        if (MoveToPosition(IdleAnchorPosition, deltaTime) || IsCloseEnoughToIdleAnchor())
        {
            idleMicroTargetPosition = transform.position;
            idleMicroRetargetTimer = 0f;
            State = LaneUnitState.Waiting;
        }
    }

    private bool MoveToPosition(Vector3 targetPosition, float deltaTime)
    {
        var offset = targetPosition - transform.position;
        if (offset.sqrMagnitude <= 0.0001f)
        {
            return true;
        }

        var direction = offset.normalized;
        UpdateFacing(direction);
        var moveStep = UnitDef.MoveSpeed * deltaTime;
        if (offset.magnitude <= moveStep)
        {
            transform.position = targetPosition;
            return true;
        }

        transform.position += direction * moveStep;
        return false;
    }

    private bool IsCloseEnoughToIdleAnchor()
    {
        return (IdleAnchorPosition - transform.position).sqrMagnitude <= IdleHoldDistance * IdleHoldDistance;
    }

    private void UpdateIdleMicroMovement(float deltaTime)
    {
        if (IsCloseEnoughToPosition(idleMicroTargetPosition))
        {
            idleMicroRetargetTimer += deltaTime;

            if (idleMicroRetargetTimer >= idleMicroRetargetDelay)
            {
                PickNextIdleMicroTarget();
            }

            return;
        }

        idleMicroRetargetTimer = 0f;
        MoveToPosition(idleMicroTargetPosition, deltaTime, idleMicroMoveSpeed);
    }

    private void PickNextIdleMicroTarget()
    {
        idleMicroRetargetIndex++;
        idleMicroRetargetTimer = 0f;

        var minTravelDistance = Mathf.Max(IdleHoldDistance * 2f, idleMicroRadius * 0.35f);

        for (var i = 0; i < IdleTargetPickAttempts; i++)
        {
            var offset2D = Random.insideUnitCircle * idleMicroRadius;
            var candidate = IdleAnchorPosition + new Vector3(offset2D.x, offset2D.y, 0f);

            if (Vector3.Distance(transform.position, candidate) >= minTravelDistance)
            {
                idleMicroTargetPosition = candidate;
                return;
            }
        }

        var fallbackOffset = Random.insideUnitCircle * idleMicroRadius;
        idleMicroTargetPosition = IdleAnchorPosition + new Vector3(fallbackOffset.x, fallbackOffset.y, 0f);
    }

    private bool IsCloseEnoughToPosition(Vector3 targetPosition)
    {
        return (targetPosition - transform.position).sqrMagnitude <= IdleHoldDistance * IdleHoldDistance;
    }

    private bool MoveToPosition(Vector3 targetPosition, float deltaTime, float moveSpeedOverride)
    {
        var offset = targetPosition - transform.position;
        if (offset.sqrMagnitude <= 0.0001f)
        {
            return true;
        }

        var direction = offset.normalized;
        UpdateFacing(direction);
        var moveStep = moveSpeedOverride * deltaTime;
        if (offset.magnitude <= moveStep)
        {
            transform.position = targetPosition;
            return true;
        }

        transform.position += direction * moveStep;
        return false;
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

    private void UpdateFacing(Vector3 direction)
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (spriteRenderer == null || Mathf.Abs(direction.x) <= FacingThreshold)
        {
            return;
        }

        spriteRenderer.flipX = direction.x < 0f;
    }

    private bool UpdateLifetime(float deltaTime)
    {
        if (remainingLifetime <= 0f)
        {
            return false;
        }

        remainingLifetime -= deltaTime;
        if (remainingLifetime > 0f)
        {
            return false;
        }

        Destroy(gameObject);
        return true;
    }
}
