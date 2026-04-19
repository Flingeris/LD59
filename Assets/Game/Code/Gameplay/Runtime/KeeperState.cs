using UnityEngine;

public class KeeperState
{
    public Vector2 Position;
    public Vector2 TargetPosition;
    public float MoveSpeed;
    public string CurrentPoiId;
    public string CurrentTargetPointId;
    public KeeperActivityState ActivityState;
    public KeeperInteractionState InteractionState;

    public static KeeperState CreateInitial(float initialMoveSpeed)
    {
        return new KeeperState
        {
            Position = Vector2.zero,
            TargetPosition = Vector2.zero,
            MoveSpeed = Mathf.Max(0f, initialMoveSpeed),
            CurrentPoiId = string.Empty,
            CurrentTargetPointId = string.Empty,
            ActivityState = KeeperActivityState.Idle,
            InteractionState = KeeperInteractionState.None
        };
    }
}
