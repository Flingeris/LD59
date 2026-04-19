using UnityEngine;

public class CemeteryStateSystem
{
    public bool ApplyBreakthroughDamage(RunState runState, int damageAmount)
    {
        if (runState == null || damageAmount <= 0)
        {
            return false;
        }

        var clampedMaxState = Mathf.Max(0, runState.CemeteryMaxState);
        var nextState = Mathf.Clamp(runState.CemeteryState - damageAmount, 0, clampedMaxState);
        if (nextState == runState.CemeteryState)
        {
            return false;
        }

        runState.CemeteryState = nextState;
        return true;
    }

    public int ApplyNightRepair(RunState runState, float deltaTime)
    {
        if (runState == null || deltaTime <= 0f)
        {
            return 0;
        }

        if (runState.NightCemeteryRepairAmount <= 0 ||
            runState.NightCemeteryRepairIntervalSeconds <= 0f ||
            runState.CemeteryState >= runState.CemeteryMaxState)
        {
            return 0;
        }

        runState.NightCemeteryRepairTimerProgress += deltaTime;

        var totalRepaired = 0;
        while (runState.NightCemeteryRepairTimerProgress >= runState.NightCemeteryRepairIntervalSeconds)
        {
            runState.NightCemeteryRepairTimerProgress -= runState.NightCemeteryRepairIntervalSeconds;

            var previousState = runState.CemeteryState;
            runState.CemeteryState = Mathf.Clamp(
                runState.CemeteryState + runState.NightCemeteryRepairAmount,
                0,
                Mathf.Max(0, runState.CemeteryMaxState));

            var repairedAmount = runState.CemeteryState - previousState;
            if (repairedAmount <= 0)
            {
                break;
            }

            totalRepaired += repairedAmount;

            if (runState.CemeteryState >= runState.CemeteryMaxState)
            {
                runState.NightCemeteryRepairTimerProgress = 0f;
                break;
            }
        }

        return totalRepaired;
    }

    public void ResetNightRepairProgress(RunState runState)
    {
        if (runState == null)
        {
            return;
        }

        runState.NightCemeteryRepairTimerProgress = 0f;
    }
}
