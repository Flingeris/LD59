public class BellSystem
{
    public BellRingResult TryRingBell(string bellId, RunState runState)
    {
        if (runState == null)
        {
            return BellRingResult.Failure(BellRingFailureReason.WrongPhase);
        }

        if (runState.CurrentPhase != GamePhase.Night)
        {
            return BellRingResult.Failure(BellRingFailureReason.WrongPhase);
        }

        var bellDef = CMS.Get<BellDef>(bellId);
        if (bellDef == null)
        {
            return BellRingResult.Failure(BellRingFailureReason.BellNotFound);
        }

        var unitDef = CMS.Get<UnitDef>(bellDef.LinkedUnitId);
        if (unitDef == null)
        {
            return BellRingResult.Failure(BellRingFailureReason.UnitNotFound);
        }

        if (runState.Faith < bellDef.FaithCost)
        {
            return BellRingResult.Failure(BellRingFailureReason.NotEnoughFaith);
        }

        runState.Faith -= bellDef.FaithCost;
        return BellRingResult.Success(bellDef, unitDef);
    }
}
