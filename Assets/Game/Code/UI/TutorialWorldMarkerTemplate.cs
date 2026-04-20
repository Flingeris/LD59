using UnityEngine;

[DisallowMultipleComponent]
public class TutorialWorldMarkerTemplate : MonoBehaviour
{
    [SerializeField] private Transform bellPosition;
    [SerializeField] private Transform faithPosition;
    [SerializeField] private Transform repairPosition;

    public Transform GetWorldAnchor(TutorialWorldMarkerAnchor anchor)
    {
        return anchor switch
        {
            TutorialWorldMarkerAnchor.BellPosition => bellPosition,
            TutorialWorldMarkerAnchor.FaithPosition => faithPosition,
            TutorialWorldMarkerAnchor.RepairPosition => repairPosition,
            _ => null
        };
    }
}

public enum TutorialWorldMarkerAnchor
{
    Default = 0,
    BellPosition = 1,
    FaithPosition = 2,
    RepairPosition = 3
}
