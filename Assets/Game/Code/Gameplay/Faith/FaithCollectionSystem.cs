using UnityEngine;

public class FaithCollectionSystem
{
    public void StartNight(RunState runState)
    {
        if (runState == null)
        {
            return;
        }

        runState.Faith = Mathf.Max(0, runState.StartingNightFaith);
        runState.FaithCollectionTimerProgress = 0f;
    }

    public void EndNight(RunState runState)
    {
        if (runState == null)
        {
            return;
        }

        runState.Faith = 0;
        runState.FaithCollectionTimerProgress = 0f;
    }

    public int UpdateCollection(RunState runState, float deltaTime)
    {
        if (runState == null || runState.CurrentPhase != GamePhase.Night || deltaTime <= 0f)
        {
            return 0;
        }

        if (runState.Keeper == null ||
            runState.Keeper.ActivityState == KeeperActivityState.Moving ||
            runState.Keeper.InteractionState != KeeperInteractionState.FaithPoint)
        {
            return 0;
        }

        var payoutAmount = Mathf.Max(0, runState.FaithCollectionPayoutAmount);
        var collectionIntervalSeconds = Mathf.Max(0f, runState.FaithCollectionIntervalSeconds);
        if (payoutAmount <= 0 || collectionIntervalSeconds <= 0f)
        {
            return 0;
        }

        runState.FaithCollectionTimerProgress += deltaTime;
        if (runState.FaithCollectionTimerProgress < collectionIntervalSeconds)
        {
            return 0;
        }

        var completedPayoutCycles = Mathf.FloorToInt(runState.FaithCollectionTimerProgress / collectionIntervalSeconds);
        if (completedPayoutCycles <= 0)
        {
            return 0;
        }

        var collectedFaith = completedPayoutCycles * payoutAmount;
        runState.FaithCollectionTimerProgress -= completedPayoutCycles * collectionIntervalSeconds;
        runState.Faith += collectedFaith;
        return collectedFaith;
    }
}
