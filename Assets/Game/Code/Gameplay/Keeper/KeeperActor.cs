using UnityEngine;

public class KeeperActor : MonoBehaviour
{
    public Vector2 GetWorldPosition()
    {
        var worldPosition = transform.position;
        return new Vector2(worldPosition.x, worldPosition.y);
    }

    public void SetWorldPosition(Vector2 worldPosition)
    {
        var currentPosition = transform.position;
        transform.position = new Vector3(worldPosition.x, worldPosition.y, currentPosition.z);
    }
}
