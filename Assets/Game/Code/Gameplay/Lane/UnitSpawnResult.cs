public class UnitSpawnResult
{
    public bool IsSuccess;
    public UnitSpawnFailureReason FailureReason;
    public LaneUnit SpawnedUnit;

    public static UnitSpawnResult Success(LaneUnit spawnedUnit)
    {
        return new UnitSpawnResult
        {
            IsSuccess = true,
            FailureReason = UnitSpawnFailureReason.None,
            SpawnedUnit = spawnedUnit
        };
    }

    public static UnitSpawnResult Failure(UnitSpawnFailureReason failureReason)
    {
        return new UnitSpawnResult
        {
            IsSuccess = false,
            FailureReason = failureReason
        };
    }
}
