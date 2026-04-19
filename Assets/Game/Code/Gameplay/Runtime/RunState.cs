using System.Collections.Generic;
using UnityEngine;

public class RunState
{
    public int Faith;
    public int Gold;
    public int StartingNightFaith;
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
            FaithCollectionPayoutAmount = clampedFaithCollectionPayoutAmount,
            FaithCollectionIntervalSeconds = clampedFaithCollectionIntervalSeconds,
            FaithCollectionTimerProgress = 0f,
            CemeteryState = clampedInitialCemeteryState,
            CemeteryMaxState = clampedInitialCemeteryState,
            NightCemeteryRepairAmount = clampedNightCemeteryRepairAmount,
            NightCemeteryRepairIntervalSeconds = clampedNightCemeteryRepairIntervalSeconds,
            NightCemeteryRepairTimerProgress = 0f,
            CurrentDay = 1,
            CurrentNight = 0,
            CurrentPhase = GamePhase.Transition,
            LastDayReward = new DayRewardData(),
            PurchasedUpgradeIds = new HashSet<string>(),
            Keeper = KeeperState.CreateInitial(initialKeeperMoveSpeed)
        };
    }
}
