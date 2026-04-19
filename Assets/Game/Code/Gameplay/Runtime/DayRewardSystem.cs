public class DayRewardSystem
{
    public DayRewardData CreateInitialDayReward(int faithReward)
    {
        return CreateReward(0, faithReward, 0);
    }

    public DayRewardData CreateCompletedNightReward(int completedNightIndex, int faithReward, int goldReward)
    {
        return CreateReward(completedNightIndex, faithReward, goldReward);
    }

    public void ApplyReward(RunState runState, DayRewardData reward)
    {
        if (runState == null || reward == null)
        {
            return;
        }

        var faithReward = reward.FaithReward < 0 ? 0 : reward.FaithReward;
        var goldReward = reward.GoldReward < 0 ? 0 : reward.GoldReward;

        runState.Faith += faithReward;
        runState.Gold += goldReward;
        runState.LastDayReward = new DayRewardData
        {
            SourceNightIndex = reward.SourceNightIndex,
            FaithReward = faithReward,
            GoldReward = goldReward
        };
    }

    private DayRewardData CreateReward(int sourceNightIndex, int faithReward, int goldReward)
    {
        return new DayRewardData
        {
            SourceNightIndex = sourceNightIndex,
            FaithReward = faithReward,
            GoldReward = goldReward
        };
    }
}
