using UnityEngine;

public class KeeperMovementSystem
{
    public void SetMoveTarget(KeeperState keeperState, Vector2 targetPosition, string targetPointId)
    {
        if (keeperState == null)
        {
            return;
        }

        keeperState.TargetPosition = targetPosition;
        keeperState.CurrentTargetPointId = string.IsNullOrWhiteSpace(targetPointId) ? string.Empty : targetPointId;
        keeperState.ActivityState = KeeperActivityState.Moving;
        keeperState.InteractionState = KeeperInteractionState.None;
    }

    public void Stop(KeeperState keeperState)
    {
        if (keeperState == null)
        {
            return;
        }

        keeperState.TargetPosition = keeperState.Position;
        keeperState.CurrentTargetPointId = string.Empty;
        keeperState.ActivityState = KeeperActivityState.Idle;
        keeperState.InteractionState = KeeperInteractionState.None;
    }

    public void UpdateMovement(KeeperState keeperState, float deltaTime, float arrivalDistance)
    {
        if (keeperState == null || keeperState.ActivityState != KeeperActivityState.Moving)
        {
            return;
        }

        var clampedArrivalDistance = Mathf.Max(0f, arrivalDistance);
        var nextPosition = Vector2.MoveTowards(
            keeperState.Position,
            keeperState.TargetPosition,
            Mathf.Max(0f, keeperState.MoveSpeed) * Mathf.Max(0f, deltaTime));

        keeperState.Position = nextPosition;

        if (Vector2.Distance(nextPosition, keeperState.TargetPosition) > clampedArrivalDistance)
        {
            return;
        }

        keeperState.Position = keeperState.TargetPosition;
        keeperState.TargetPosition = keeperState.Position;
        keeperState.CurrentTargetPointId = string.Empty;
        keeperState.ActivityState = KeeperActivityState.Idle;
        keeperState.InteractionState = KeeperInteractionState.None;
    }
}
