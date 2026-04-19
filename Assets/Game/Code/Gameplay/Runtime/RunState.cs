using System.Collections.Generic;

public class RunState
{
    private const int InitialCemeteryState = 100;

    public int Faith;
    public int Gold;
    public int DayFaithIncome;
    public int CemeteryState;
    public int CemeteryMaxState;
    public int CurrentDay;
    public int CurrentNight;
    public GamePhase CurrentPhase;
    public DayRewardData LastDayReward;
    public HashSet<string> PurchasedUpgradeIds;

    public static RunState CreateInitial()
    {
        return new RunState
        {
            Faith = 0,
            Gold = 0,
            DayFaithIncome = 0,
            CemeteryState = InitialCemeteryState,
            CemeteryMaxState = InitialCemeteryState,
            CurrentDay = 1,
            CurrentNight = 0,
            CurrentPhase = GamePhase.Transition,
            LastDayReward = new DayRewardData(),
            PurchasedUpgradeIds = new HashSet<string>()
        };
    }
}
