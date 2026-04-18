public class BellRingResult
{
    public bool IsSuccess;
    public BellRingFailureReason FailureReason;
    public BellDef BellDef;
    public UnitDef UnitDef;
    public UnitSpawnResult SpawnResult;

    public static BellRingResult Success(BellDef bellDef, UnitDef unitDef)
    {
        return new BellRingResult
        {
            IsSuccess = true,
            FailureReason = BellRingFailureReason.None,
            BellDef = bellDef,
            UnitDef = unitDef
        };
    }

    public static BellRingResult Failure(BellRingFailureReason failureReason)
    {
        return new BellRingResult
        {
            IsSuccess = false,
            FailureReason = failureReason,
            SpawnResult = UnitSpawnResult.Failure(UnitSpawnFailureReason.NotRequested)
        };
    }
}
