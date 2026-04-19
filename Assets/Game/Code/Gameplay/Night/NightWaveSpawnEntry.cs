public class NightWaveSpawnEntry
{
    public NightWaveSpawnEntry(string enemyId, int count)
    {
        EnemyId = enemyId;
        Count = count < 0 ? 0 : count;
    }

    public string EnemyId { get; }
    public int Count { get; }
}
