using UnityEngine;

public readonly struct UnitRuntimeStats
{
    public readonly int MaxHp;
    public readonly int Damage;
    public readonly float AttackIntervalSeconds;
    public readonly float MoveSpeed;
    public readonly float LifetimeSeconds;

    public UnitRuntimeStats(
        int maxHp,
        int damage,
        float attackIntervalSeconds,
        float moveSpeed,
        float lifetimeSeconds)
    {
        MaxHp = Mathf.Max(0, maxHp);
        Damage = Mathf.Max(0, damage);
        AttackIntervalSeconds = Mathf.Max(0f, attackIntervalSeconds);
        MoveSpeed = Mathf.Max(0f, moveSpeed);
        LifetimeSeconds = Mathf.Max(0f, lifetimeSeconds);
    }

    public static UnitRuntimeStats From(UnitDef unitDef, RunState runState)
    {
        if (unitDef == null)
        {
            return new UnitRuntimeStats(0, 0, 0f, 0f, 0f);
        }

        var hpModifier = runState != null ? runState.GetUnitHpModifier(unitDef.Id) : 0;
        var damageModifier = runState != null ? runState.GetUnitDamageModifier(unitDef.Id) : 0;
        var lifetimeModifier = runState != null ? runState.GetUnitLifetimeModifier(unitDef.Id) : 0f;

        return new UnitRuntimeStats(
            unitDef.Hp + hpModifier,
            unitDef.Damage + damageModifier,
            unitDef.AttackInterval,
            unitDef.MoveSpeed,
            unitDef.LifetimeSeconds + lifetimeModifier);
    }
}
