using UnityEngine;

public class KeeperActor : MonoBehaviour
{
    private const float FacingThreshold = 0.001f;

    private SpriteRenderer spriteRenderer;

    public Vector2 GetWorldPosition()
    {
        var worldPosition = transform.position;
        return new Vector2(worldPosition.x, worldPosition.y);
    }

    public void SetWorldPosition(Vector2 worldPosition)
    {
        var currentPosition = transform.position;
        UpdateFacing(new Vector2(worldPosition.x - currentPosition.x, worldPosition.y - currentPosition.y));
        transform.position = new Vector3(worldPosition.x, worldPosition.y, currentPosition.z);
    }

    private void UpdateFacing(Vector2 movementOffset)
    {
        if (Mathf.Abs(movementOffset.x) <= FacingThreshold)
        {
            return;
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.flipX = movementOffset.x < 0f;
    }
}
