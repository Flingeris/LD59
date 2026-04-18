using UnityEngine;

public class LaneUnit : MonoBehaviour
{
    private const float IdleHoldDistance = 0.05f;

    public UnitDef UnitDef { get; private set; }
    public Vector3 Position => transform.position;
    public LaneUnitState State { get; private set; }
    public bool IsInCombat => State == LaneUnitState.Attacking;
    public int CurrentHp { get; private set; }
    public Vector3 HomeSlotCenter { get; private set; }
    public Vector3 IdleAnchorPosition { get; private set; }
    public LaneEnemy TargetEnemy { get; private set; }
    public bool HasTarget => TargetEnemy != null;

    private float attackCooldown;
    private bool hasHomePosition;
    private Vector3 idleMicroTargetPosition;
    private float idleMicroRadius;
    private float idleMicroMoveSpeed;
    private float idleMicroRetargetDelay;
    private float idleMicroRetargetTimer;
    private int idleMicroRetargetIndex;

    private void Update()
    {
        if (UnitDef == null)
        {
            return;
        }

        UpdateState(Time.deltaTime);
    }

    public void Initialize(UnitDef unitDef)
    {
        UnitDef = unitDef;
        CurrentHp = unitDef.Hp;
        State = LaneUnitState.Waiting;
        TargetEnemy = null;
        attackCooldown = unitDef.AttackInterval;
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
        CurrentHp -= damage;
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
                transform.position = IdleAnchorPosition;
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
            transform.position = IdleAnchorPosition;
            idleMicroTargetPosition = IdleAnchorPosition;
            idleMicroRetargetTimer = 0f;
            State = LaneUnitState.Waiting;
        }
    }

    private bool MoveToPosition(Vector3 targetPosition, float deltaTime)
    {
        var offset = targetPosition - transform.position;
        if (offset.sqrMagnitude <= 0.0001f)
        {
            transform.position = targetPosition;
            return true;
        }

        var direction = offset.normalized;
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
            transform.position = idleMicroTargetPosition;
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

        var angleSeed = Mathf.Abs(GetInstanceID() * 41 + idleMicroRetargetIndex * 59);
        var radiusSeed = Mathf.Abs(GetInstanceID() * 67 + idleMicroRetargetIndex * 83);
        var angle = angleSeed % 360;
        var radiusFactor = Mathf.Sqrt((radiusSeed % 1000) / 1000f);
        var radius = idleMicroRadius * radiusFactor;
        var radians = angle * Mathf.Deg2Rad;
        var offset = new Vector3(Mathf.Cos(radians) * radius, Mathf.Sin(radians) * radius, 0f);
        idleMicroTargetPosition = IdleAnchorPosition + offset;
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
            transform.position = targetPosition;
            return true;
        }

        var direction = offset.normalized;
        var moveStep = moveSpeedOverride * deltaTime;
        if (offset.magnitude <= moveStep)
        {
            transform.position = targetPosition;
            return true;
        }

        transform.position += direction * moveStep;
        return false;
    }
}
