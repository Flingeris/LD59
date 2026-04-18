public class EnemySpawnResult
{
    public bool IsSuccess;
    public UnitSpawnFailureReason FailureReason;
    public LaneEnemy SpawnedEnemy;

    public static EnemySpawnResult Success(LaneEnemy spawnedEnemy)
    {
        return new EnemySpawnResult
        {
            IsSuccess = true,
            FailureReason = UnitSpawnFailureReason.None,
            SpawnedEnemy = spawnedEnemy
        };
    }

    public static EnemySpawnResult Failure(UnitSpawnFailureReason failureReason)
    {
        return new EnemySpawnResult
        {
            IsSuccess = false,
            FailureReason = failureReason
        };
    }
}
