using System.Collections.Generic;
using UnityEngine;

public class RunState
{
    public int Faith;
    public int Gold;
    public int StartingNightFaith;
    public int BellFaithCostModifier;
    public int FaithCollectionPayoutAmount;
    public float FaithCollectionIntervalSeconds;
    public float FaithCollectionTimerProgress;
    public int CemeteryState;
    public int CemeteryMaxState;
    public int NightCemeteryRepairAmount;
    public float NightCemeteryRepairIntervalSeconds;
    public float NightCemeteryRepairTimerProgress;
    public int CurrentDay;
    public int CurrentNight;
    public GamePhase CurrentPhase;
    public DayRewardData LastDayReward;
    public HashSet<string> PurchasedUpgradeIds;
    public Dictionary<string, int> UnitDamageModifiersById;
    public Dictionary<string, int> UnitHpModifiersById;
    public Dictionary<string, float> UnitLifetimeModifiersById;
    public int InstantNightRepairChargesPerNight;
    public int RemainingInstantNightRepairCharges;
    public int InstantNightRepairAmount;
    public KeeperState Keeper;

    public static RunState CreateInitial(
        int initialCemeteryState,
        float initialKeeperMoveSpeed,
        int startingNightFaith,
        int faithCollectionPayoutAmount,
        float faithCollectionIntervalSeconds,
        int nightCemeteryRepairAmount,
        float nightCemeteryRepairIntervalSeconds)
    {
        var clampedInitialCemeteryState = Mathf.Max(0, initialCemeteryState);
        var clampedStartingNightFaith = Mathf.Max(0, startingNightFaith);
        var clampedFaithCollectionPayoutAmount = Mathf.Max(0, faithCollectionPayoutAmount);
        var clampedFaithCollectionIntervalSeconds = Mathf.Max(0f, faithCollectionIntervalSeconds);
        var clampedNightCemeteryRepairAmount = Mathf.Max(0, nightCemeteryRepairAmount);
        var clampedNightCemeteryRepairIntervalSeconds = Mathf.Max(0f, nightCemeteryRepairIntervalSeconds);

        return new RunState
        {
            Faith = 0,
            Gold = 0,
            StartingNightFaith = clampedStartingNightFaith,
            BellFaithCostModifier = 0,
            FaithCollectionPayoutAmount = clampedFaithCollectionPayoutAmount,
            FaithCollectionIntervalSeconds = clampedFaithCollectionIntervalSeconds,
            FaithCollectionTimerProgress = 0f,
            CemeteryState = clampedInitialCemeteryState,
            CemeteryMaxState = clampedInitialCemeteryState,
            NightCemeteryRepairAmount = clampedNightCemeteryRepairAmount,
            NightCemeteryRepairIntervalSeconds = clampedNightCemeteryRepairIntervalSeconds,
            NightCemeteryRepairTimerProgress = 0f,
            CurrentDay = 0,
            CurrentNight = 0,
            CurrentPhase = GamePhase.Transition,
            LastDayReward = new DayRewardData(),
            PurchasedUpgradeIds = new HashSet<string>(),
            UnitDamageModifiersById = new Dictionary<string, int>(),
            UnitHpModifiersById = new Dictionary<string, int>(),
            UnitLifetimeModifiersById = new Dictionary<string, float>(),
            InstantNightRepairChargesPerNight = 0,
            RemainingInstantNightRepairCharges = 0,
            InstantNightRepairAmount = 0,
            Keeper = KeeperState.CreateInitial(initialKeeperMoveSpeed)
        };
    }

    public int GetUnitDamageModifier(string unitId)
    {
        return GetUnitIntModifier(UnitDamageModifiersById, unitId);
    }

    public int GetUnitHpModifier(string unitId)
    {
        return GetUnitIntModifier(UnitHpModifiersById, unitId);
    }

    public float GetUnitLifetimeModifier(string unitId)
    {
        return GetUnitFloatModifier(UnitLifetimeModifiersById, unitId);
    }

    public void AddUnitDamageModifier(string unitId, int amount)
    {
        AddUnitIntModifier(UnitDamageModifiersById, unitId, amount);
    }

    public void AddUnitHpModifier(string unitId, int amount)
    {
        AddUnitIntModifier(UnitHpModifiersById, unitId, amount);
    }

    public void AddUnitLifetimeModifier(string unitId, float amount)
    {
        AddUnitFloatModifier(UnitLifetimeModifiersById, unitId, amount);
    }

    private static int GetUnitIntModifier(Dictionary<string, int> modifiers, string unitId)
    {
        if (modifiers == null || string.IsNullOrWhiteSpace(unitId))
        {
            return 0;
        }

        return modifiers.TryGetValue(unitId, out var value) ? value : 0;
    }

    private static float GetUnitFloatModifier(Dictionary<string, float> modifiers, string unitId)
    {
        if (modifiers == null || string.IsNullOrWhiteSpace(unitId))
        {
            return 0f;
        }

        return modifiers.TryGetValue(unitId, out var value) ? value : 0f;
    }

    private static void AddUnitIntModifier(Dictionary<string, int> modifiers, string unitId, int amount)
    {
        if (modifiers == null || string.IsNullOrWhiteSpace(unitId) || amount == 0)
        {
            return;
        }

        modifiers.TryGetValue(unitId, out var currentValue);
        modifiers[unitId] = currentValue + amount;
    }

    private static void AddUnitFloatModifier(Dictionary<string, float> modifiers, string unitId, float amount)
    {
        if (modifiers == null || string.IsNullOrWhiteSpace(unitId) || Mathf.Approximately(amount, 0f))
        {
            return;
        }

        modifiers.TryGetValue(unitId, out var currentValue);
        modifiers[unitId] = currentValue + amount;
    }
}
