    using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class LoopingCloud : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private CloudLoopController owner;
    private float speed;
    private float directionSign;
    private float minX;
    private float maxX;
    private float recyclePadding;

    public Sprite CurrentSprite => spriteRenderer != null ? spriteRenderer.sprite : null;
    public float CurrentSpeed => speed;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (speed <= 0f)
        {
            return;
        }

        var localPosition = transform.localPosition;
        localPosition.x += directionSign * speed * Time.deltaTime;
        transform.localPosition = localPosition;

        if (ShouldRecycle(localPosition.x))
        {
            owner?.RecycleCloud(this);
        }
    }

    public void Configure(
        CloudLoopController cloudOwner,
        Sprite sprite,
        Vector2 localPosition,
        float moveSpeed,
        float horizontalDirection,
        float minLocalX,
        float maxLocalX,
        float recycleOffset)
    {
        owner = cloudOwner;
        speed = Mathf.Max(0f, moveSpeed);
        directionSign = Mathf.Approximately(horizontalDirection, 0f) ? 1f : Mathf.Sign(horizontalDirection);
        minX = Mathf.Min(minLocalX, maxLocalX);
        maxX = Mathf.Max(minLocalX, maxLocalX);
        recyclePadding = Mathf.Max(0f, recycleOffset);

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        spriteRenderer.sprite = sprite;

        var currentPosition = transform.localPosition;
        transform.localPosition = new Vector3(localPosition.x, localPosition.y, currentPosition.z);
    }

    private bool ShouldRecycle(float localX)
    {
        return directionSign > 0f
            ? localX > maxX + recyclePadding
            : localX < minX - recyclePadding;
    }
}
