using System.Collections.Generic;
using System;
using UnityEngine;

public class SingleLaneEncounterCoordinator : MonoBehaviour
{
    [SerializeField] private float attackDistance = 0.75f;
    [SerializeField] private SingleLaneHost laneHost;

    public Action<LaneEnemy> OnEnemyBreakthrough;
    public Action<LaneEnemy> OnEnemyCemeteryAttack;
    public Action<LaneEnemy> OnEnemyKilled;
    public bool HasActiveEnemyPressure => enemies.Count > 0;

    private readonly List<LaneUnit> playerUnits = new();
    private readonly List<LaneEnemy> enemies = new();
    private readonly Dictionary<LaneUnit, int> assignedHomeSlots = new();
    private readonly Dictionary<LaneUnit, Vector3> assignedHomePositions = new();

    private void Update()
    {
        CleanupDestroyed();
        UpdateEnemyTargets();
        UpdatePlayerAssignments();
        UpdateCombatStates();
        CheckEnemyBreakthroughs();
    }

    public void RegisterPlayerUnit(LaneUnit laneUnit)
    {
        if (laneUnit == null || playerUnits.Contains(laneUnit))
        {
            return;
        }

        playerUnits.Add(laneUnit);
        laneUnit.AssignHomePosition(
            GetHomeSlotCenter(laneUnit),
            GetIdleAnchorPosition(laneUnit),
            GetIdleMicroRadius(),
            GetIdleMicroMoveSpeed(),
            GetIdleMicroRetargetDelay());
    }

    public void RegisterEnemy(LaneEnemy laneEnemy)
    {
        if (laneEnemy == null || enemies.Contains(laneEnemy))
        {
            return;
        }

        enemies.Add(laneEnemy);
        laneEnemy.CemeteryAttackTriggered -= HandleEnemyCemeteryAttackTriggered;
        laneEnemy.CemeteryAttackTriggered += HandleEnemyCemeteryAttackTriggered;
    }

    public void ClearCombatants()
    {
        for (var i = enemies.Count - 1; i >= 0; i--)
        {
            RemoveEnemyAt(i);
        }

        for (var i = playerUnits.Count - 1; i >= 0; i--)
        {
            RemovePlayerUnitAt(i);
        }

        assignedHomeSlots.Clear();
        assignedHomePositions.Clear();
    }

    private void CleanupDestroyed()
    {
        for (var i = enemies.Count - 1; i >= 0; i--)
        {
            if (enemies[i] != null)
            {
                continue;
            }

            enemies.RemoveAt(i);
        }

        playerUnits.RemoveAll(unit => unit == null);
        CleanupAssignedSlots();
    }

    private void UpdateEnemyTargets()
    {
        for (var i = 0; i < enemies.Count; i++)
        {
            var enemy = enemies[i];
            if (enemy == null)
            {
                continue;
            }

            if (enemy.TargetUnit != null && enemy.TargetUnit.IsDead())
            {
                enemy.ClearTargetUnit();
            }

            if (enemy.TargetUnit != null && enemy.IsWithinAttackRange(enemy.TargetUnit, attackDistance))
            {
                continue;
            }

            if (enemy.TargetUnit != null)
            {
                enemy.ClearTargetUnit();
            }

            var targetUnit = FindClosestUnitInAttackRange(enemy);
            if (targetUnit != null)
            {
                enemy.SetTargetUnit(targetUnit);
            }
            else if (!enemy.IsAttackingCemetery)
            {
                enemy.ClearTargetUnit();
            }
        }
    }

    private void UpdatePlayerAssignments()
    {
        if (laneHost == null || laneHost.EnemyAggroThresholdPoint == null)
        {
            ReturnIdleUnitsHome();
            return;
        }

        var aggroX = laneHost.EnemyAggroThresholdPoint.position.x;
        var activeEnemies = GetAggroEnemies(aggroX);

        if (activeEnemies.Count == 0)
        {
            ReturnIdleUnitsHome();
            return;
        }

        for (var i = 0; i < playerUnits.Count; i++)
        {
            var unit = playerUnits[i];
            if (unit == null || unit.IsDead())
            {
                continue;
            }

            if (unit.TargetEnemy != null && unit.TargetEnemy.IsDead())
            {
                TryRetargetOrReturn(unit, activeEnemies);
                continue;
            }

            if (unit.HasTarget && !IsEnemyWithinDefenseLeash(unit, unit.TargetEnemy))
            {
                unit.ClearTargetEnemy();
            }

            if (!unit.HasTarget && unit.IsAvailableForAssignment())
            {
                var targetEnemy = FindPriorityEnemy(unit, activeEnemies);
                if (targetEnemy != null)
                {
                    unit.SetTargetEnemy(targetEnemy);
                }
            }
        }
    }

    private void UpdateCombatStates()
    {
        for (var i = 0; i < playerUnits.Count; i++)
        {
            var unit = playerUnits[i];
            if (unit == null || unit.IsDead())
            {
                continue;
            }

            if (unit.TargetEnemy != null && unit.TargetEnemy.IsDead())
            {
                TryRetargetOrReturn(unit, GetCurrentThreatEnemies());
                continue;
            }

            if (!unit.HasTarget)
            {
                continue;
            }

            if (!IsEnemyWithinDefenseLeash(unit, unit.TargetEnemy))
            {
                unit.ClearTargetEnemy();
                continue;
            }

            if (unit.IsWithinAttackRange(unit.TargetEnemy, attackDistance))
            {
                if (unit.State != LaneUnitState.Attacking)
                {
                    unit.EnterCombat();
                }

                unit.TargetEnemy.SetTargetUnit(unit);
            }
            else if (unit.State == LaneUnitState.Attacking)
            {
                unit.SetTargetEnemy(unit.TargetEnemy);
            }
        }

        for (var i = enemies.Count - 1; i >= 0; i--)
        {
            var enemy = enemies[i];
            if (enemy == null)
            {
                continue;
            }

            if (enemy.IsDead())
            {
                OnEnemyKilled?.Invoke(enemy);
                RemoveEnemyAt(i);
            }
        }

        for (var i = playerUnits.Count - 1; i >= 0; i--)
        {
            var unit = playerUnits[i];
            if (unit == null)
            {
                continue;
            }

            if (unit.IsDead())
            {
                RemovePlayerUnitAt(i);
            }
        }
    }

    private void CheckEnemyBreakthroughs()
    {
        if (laneHost == null || laneHost.EnemyBreakthroughPoint == null)
        {
            return;
        }

        var breakthroughX = laneHost.EnemyBreakthroughPoint.position.x;

        for (var i = enemies.Count - 1; i >= 0; i--)
        {
            var enemy = enemies[i];
            if (enemy == null || enemy.IsInCombat || enemy.IsAttackingCemetery)
            {
                continue;
            }

            if (!enemy.HasReachedBreakthrough(breakthroughX))
            {
                continue;
            }

            enemy.EnterCemeteryAttackState(laneHost.EnemyBreakthroughPoint.position);
            OnEnemyBreakthrough?.Invoke(enemy);
        }
    }

    private void ReturnIdleUnitsHome()
    {
        for (var i = 0; i < playerUnits.Count; i++)
        {
            var unit = playerUnits[i];
            if (unit == null || unit.IsDead())
            {
                continue;
            }

            if (!unit.HasTarget)
            {
                if (unit.State != LaneUnitState.Waiting && unit.State != LaneUnitState.Returning)
                {
                    unit.ReturnToHome();
                }
            }
        }
    }

    private Vector3 GetHomeSlotCenter(LaneUnit laneUnit)
    {
        if (assignedHomePositions.TryGetValue(laneUnit, out var assignedPosition))
        {
            return assignedPosition;
        }

        if (laneHost == null || laneHost.PlayerDefenseSlots == null || laneHost.PlayerDefenseSlots.Length == 0)
        {
            var fallbackPosition = laneHost != null && laneHost.PlayerSpawnPoint != null
                ? laneHost.PlayerSpawnPoint.position
                : laneUnit.Position;
            assignedHomePositions[laneUnit] = fallbackPosition;
            return fallbackPosition;
        }

        var slotIndex = GetLeastPopulatedSlotIndex();
        assignedHomeSlots[laneUnit] = slotIndex;
        assignedHomePositions[laneUnit] = laneHost.PlayerDefenseSlots[slotIndex].position;
        return assignedHomePositions[laneUnit];
    }

    private void CleanupAssignedSlots()
    {
        var releasedUnits = new List<LaneUnit>();

        foreach (var pair in assignedHomeSlots)
        {
            if (pair.Key == null)
            {
                releasedUnits.Add(pair.Key);
            }
        }

        for (var i = 0; i < releasedUnits.Count; i++)
        {
            assignedHomeSlots.Remove(releasedUnits[i]);
            assignedHomePositions.Remove(releasedUnits[i]);
        }
    }

    private void ReleaseAssignedSlot(LaneUnit laneUnit)
    {
        assignedHomeSlots.Remove(laneUnit);
        assignedHomePositions.Remove(laneUnit);
    }

    private List<LaneEnemy> GetAggroEnemies(float aggroX)
    {
        var activeEnemies = new List<LaneEnemy>();

        for (var i = 0; i < enemies.Count; i++)
        {
            var enemy = enemies[i];
            if (enemy == null || enemy.IsDead())
            {
                continue;
            }

            if (enemy.Position.x <= aggroX)
            {
                activeEnemies.Add(enemy);
            }
        }

        return activeEnemies;
    }

    private List<LaneEnemy> GetCurrentThreatEnemies()
    {
        if (laneHost == null || laneHost.EnemyAggroThresholdPoint == null)
        {
            return new List<LaneEnemy>();
        }

        return GetAggroEnemies(laneHost.EnemyAggroThresholdPoint.position.x);
    }

    private LaneEnemy FindPriorityEnemy(LaneUnit laneUnit, List<LaneEnemy> candidateEnemies)
    {
        LaneEnemy bestEnemy = null;
        var bestThreatDistance = float.MaxValue;
        var bestUnitDistance = float.MaxValue;

        if (laneHost == null || laneHost.EnemyBreakthroughPoint == null)
        {
            return FindClosestEnemyByDistance(laneUnit.Position, candidateEnemies);
        }

        var breakthroughX = laneHost.EnemyBreakthroughPoint.position.x;

        for (var i = 0; i < candidateEnemies.Count; i++)
        {
            var enemy = candidateEnemies[i];
            if (enemy == null || enemy.IsDead())
            {
                continue;
            }

            if (!IsEnemyWithinDefenseLeash(laneUnit, enemy))
            {
                continue;
            }

            var threatDistance = Mathf.Abs(enemy.Position.x - breakthroughX);
            var unitDistance = Vector3.Distance(laneUnit.Position, enemy.Position);

            if (threatDistance < bestThreatDistance ||
                (Mathf.Approximately(threatDistance, bestThreatDistance) && unitDistance < bestUnitDistance))
            {
                bestThreatDistance = threatDistance;
                bestUnitDistance = unitDistance;
                bestEnemy = enemy;
            }
        }

        return bestEnemy;
    }

    private LaneUnit FindClosestUnitInAttackRange(LaneEnemy enemy)
    {
        LaneUnit closestUnit = null;
        var closestDistance = float.MaxValue;

        for (var i = 0; i < playerUnits.Count; i++)
        {
            var unit = playerUnits[i];
            if (unit == null || unit.IsDead())
            {
                continue;
            }

            if (!enemy.IsWithinAttackRange(unit, attackDistance))
            {
                continue;
            }

            var distance = Vector3.Distance(enemy.Position, unit.Position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestUnit = unit;
            }
        }

        return closestUnit;
    }

    private void RemoveEnemyAt(int index)
    {
        var enemy = enemies[index];
        enemies.RemoveAt(index);
        if (enemy != null)
        {
            enemy.CemeteryAttackTriggered -= HandleEnemyCemeteryAttackTriggered;
            enemy.ClearTargetUnit();
            Destroy(enemy.gameObject);
        }
    }

    private void RemovePlayerUnitAt(int index)
    {
        var unit = playerUnits[index];
        playerUnits.RemoveAt(index);

        if (unit == null)
        {
            return;
        }

        ReleaseAssignedSlot(unit);
        Destroy(unit.gameObject);
    }

    private void HandleEnemyCemeteryAttackTriggered(LaneEnemy laneEnemy)
    {
        if (laneEnemy == null)
        {
            return;
        }

        OnEnemyCemeteryAttack?.Invoke(laneEnemy);
    }

    private int GetLeastPopulatedSlotIndex()
    {
        if (laneHost == null || laneHost.PlayerDefenseSlots == null || laneHost.PlayerDefenseSlots.Length == 0)
        {
            return 0;
        }

        var bestSlotIndex = 0;
        var lowestPopulation = int.MaxValue;

        for (var i = 0; i < laneHost.PlayerDefenseSlots.Length; i++)
        {
            var population = CountUnitsInSlot(i);
            if (population < lowestPopulation)
            {
                lowestPopulation = population;
                bestSlotIndex = i;
            }
        }

        return bestSlotIndex;
    }

    private int CountUnitsInSlot(int slotIndex)
    {
        var population = 0;

        foreach (var assignedSlot in assignedHomeSlots.Values)
        {
            if (assignedSlot == slotIndex)
            {
                population++;
            }
        }

        return population;
    }

    private LaneEnemy FindClosestEnemyByDistance(Vector3 fromPosition, List<LaneEnemy> candidateEnemies)
    {
        LaneEnemy closestEnemy = null;
        var closestDistance = float.MaxValue;

        for (var i = 0; i < candidateEnemies.Count; i++)
        {
            var enemy = candidateEnemies[i];
            if (enemy == null || enemy.IsDead())
            {
                continue;
            }

            var distance = Vector3.Distance(fromPosition, enemy.Position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = enemy;
            }
        }

        return closestEnemy;
    }

    private bool IsEnemyWithinDefenseLeash(LaneUnit laneUnit, LaneEnemy laneEnemy)
    {
        if (laneEnemy == null)
        {
            return false;
        }

        if (laneHost == null || laneHost.AlliedDefenseLeashDistance <= 0f)
        {
            return true;
        }

        return Vector3.Distance(laneUnit.HomeSlotCenter, laneEnemy.Position) <= laneHost.AlliedDefenseLeashDistance;
    }

    private void TryRetargetOrReturn(LaneUnit laneUnit, List<LaneEnemy> candidateEnemies)
    {
        laneUnit.ClearTargetEnemy();

        if (candidateEnemies == null || candidateEnemies.Count == 0)
        {
            return;
        }

        var nextEnemy = FindPriorityEnemy(laneUnit, candidateEnemies);
        if (nextEnemy != null)
        {
            laneUnit.SetTargetEnemy(nextEnemy);
        }
    }

    private Vector3 GetIdleAnchorPosition(LaneUnit laneUnit)
    {
        if (assignedHomePositions.TryGetValue(laneUnit, out var assignedPosition))
        {
            return GetSpreadSlotPosition(assignedPosition, laneUnit);
        }

        return GetSpreadSlotPosition(GetHomeSlotCenter(laneUnit), laneUnit);
    }

    private Vector3 GetSpreadSlotPosition(Vector3 basePosition, LaneUnit laneUnit)
    {
        if (laneHost == null || laneHost.AlliedHomeSpreadRadius <= 0f)
        {
            return basePosition;
        }

        var stableSeed = Mathf.Abs(laneUnit.GetInstanceID() * 31 + 97);
        var angle = stableSeed % 360;
        var radiusFactor = 0.3f + ((stableSeed / 360) % 100) / 100f * 0.5f;
        var radius = laneHost.AlliedHomeSpreadRadius * radiusFactor;
        var radians = angle * Mathf.Deg2Rad;
        var offset = new Vector3(Mathf.Cos(radians) * radius, Mathf.Sin(radians) * radius, 0f);
        return basePosition + offset;
    }

    private float GetIdleMicroRadius()
    {
        return laneHost != null ? laneHost.AlliedIdleMicroRadius : 0f;
    }

    private float GetIdleMicroMoveSpeed()
    {
        return laneHost != null ? laneHost.AlliedIdleMicroMoveSpeed : 0.25f;
    }

    private float GetIdleMicroRetargetDelay()
    {
        return laneHost != null ? laneHost.AlliedIdleMicroRetargetDelay : 1f;
    }
}
