public class BellSystem
{
    public BellRingResult TryRingBell(string bellId, RunState runState)
    {
        if (runState == null || runState.CurrentPhase != GamePhase.Night)
        {
            return BellRingResult.Failure(BellRingFailureReason.WrongPhase);
        }

        if (runState.Keeper == null || runState.Keeper.InteractionState != KeeperInteractionState.Bells)
        {
            return BellRingResult.Failure(BellRingFailureReason.NotAtBellPoint);
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

        var faithCost = GetFaithCost(bellDef, runState);
        if (runState.Faith < faithCost)
        {
            return BellRingResult.Failure(BellRingFailureReason.NotEnoughFaith);
        }

        runState.Faith -= faithCost;
        G.audioSystem.Play(SoundId.SFX_BellRing);
        return BellRingResult.Success(bellDef, unitDef);
    }

    private static int GetFaithCost(BellDef bellDef, RunState runState)
    {
        if (bellDef == null)
        {
            return 0;
        }

        var costModifier = runState != null ? runState.BellFaithCostModifier : 0;
        return UnityEngine.Mathf.Max(0, bellDef.FaithCost + costModifier);
    }
}
