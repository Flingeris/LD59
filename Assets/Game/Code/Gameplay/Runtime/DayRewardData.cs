public class DayRewardData
{
    public int SourceNightIndex;
    public int FaithReward;
    public int GoldReward;

    public bool HasAnyReward => FaithReward > 0 || GoldReward > 0;
}
