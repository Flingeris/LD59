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
        runState.FaithCollectionProgress = 0f;
    }

    public void EndNight(RunState runState)
    {
        if (runState == null)
        {
            return;
        }

        runState.Faith = 0;
        runState.FaithCollectionProgress = 0f;
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

        var collectionRate = Mathf.Max(0f, runState.FaithCollectionPerSecond);
        if (collectionRate <= 0f)
        {
            return 0;
        }

        runState.FaithCollectionProgress += deltaTime * collectionRate;
        var collectedFaith = Mathf.FloorToInt(runState.FaithCollectionProgress);
        if (collectedFaith <= 0)
        {
            return 0;
        }

        runState.FaithCollectionProgress -= collectedFaith;
        runState.Faith += collectedFaith;
        return collectedFaith;
    }
}
