using System.Collections.Generic;
using UnityEngine;

public class RunState
{
    public int Faith;
    public int Gold;
    public int StartingNightFaith;
    public float FaithCollectionPerSecond;
    public float FaithCollectionProgress;
    public int CemeteryState;
    public int CemeteryMaxState;
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
        float faithCollectionPerSecond)
    {
        var clampedInitialCemeteryState = Mathf.Max(0, initialCemeteryState);
        var clampedStartingNightFaith = Mathf.Max(0, startingNightFaith);
        var clampedFaithCollectionPerSecond = Mathf.Max(0f, faithCollectionPerSecond);

        return new RunState
        {
            Faith = 0,
            Gold = 0,
            StartingNightFaith = clampedStartingNightFaith,
            FaithCollectionPerSecond = clampedFaithCollectionPerSecond,
            FaithCollectionProgress = 0f,
            CemeteryState = clampedInitialCemeteryState,
            CemeteryMaxState = clampedInitialCemeteryState,
            CurrentDay = 1,
            CurrentNight = 0,
            CurrentPhase = GamePhase.Transition,
            LastDayReward = new DayRewardData(),
            PurchasedUpgradeIds = new HashSet<string>(),
            Keeper = KeeperState.CreateInitial(initialKeeperMoveSpeed)
        };
    }
}
