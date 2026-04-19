using UnityEngine;

[DisallowMultipleComponent]
public class WorldProgressBarView : MonoBehaviour
{
    private static Sprite runtimeBarSprite;

    [SerializeField] private SpriteRenderer backgroundRenderer;
    [SerializeField] private SpriteRenderer fillRenderer;
    [SerializeField] private Color backgroundColor = new(0f, 0f, 0f, 0.75f);
    [SerializeField] private Color fillColor = new(0.95f, 0.88f, 0.35f, 1f);
    [Min(0)] [SerializeField] private int sortingOffset = 10;

    private Vector3 initialFillLocalScale;
    private Vector3 initialFillLocalPosition;
    private SpriteRenderer ownerRenderer;
    private bool isBound;
    private bool isInitialized;

    private void Awake()
    {
        EnsureInitialized();
        SetInactive();
    }

    private void LateUpdate()
    {
        if (!isBound || ownerRenderer == null)
        {
            return;
        }

        ApplySorting(backgroundRenderer, ownerRenderer, sortingOffset);
        ApplySorting(fillRenderer, ownerRenderer, sortingOffset + 1);
    }

    public void Bind(SpriteRenderer ownerRenderer)
    {
        EnsureInitialized();

        this.ownerRenderer = ownerRenderer;
        isBound = ownerRenderer != null;

        if (backgroundRenderer != null)
        {
            backgroundRenderer.color = backgroundColor;
            ApplySorting(backgroundRenderer, ownerRenderer, sortingOffset);
        }

        if (fillRenderer != null)
        {
            fillRenderer.color = fillColor;
            ApplySorting(fillRenderer, ownerRenderer, sortingOffset + 1);
        }

        SetInactive();
    }

    public void Refresh(float normalizedProgress, bool useCountdownFill, bool isVisible)
    {
        EnsureInitialized();

        if (!isBound || fillRenderer == null)
        {
            SetInactive();
            return;
        }

        if (!isVisible)
        {
            SetInactive();
            return;
        }

        var clampedProgress = Mathf.Clamp01(normalizedProgress);
        var fillNormalized = useCountdownFill ? 1f - clampedProgress : clampedProgress;
        fillNormalized = Mathf.Clamp01(fillNormalized);

        if (backgroundRenderer != null)
        {
            backgroundRenderer.enabled = true;
        }

        fillRenderer.enabled = fillNormalized > 0f;
        if (!fillRenderer.enabled)
        {
            return;
        }

        var updatedScale = initialFillLocalScale;
        updatedScale.x = initialFillLocalScale.x * fillNormalized;
        fillRenderer.transform.localScale = updatedScale;

        var initialLeftEdge = initialFillLocalPosition.x - (initialFillLocalScale.x * 0.5f);
        var updatedPosition = initialFillLocalPosition;
        updatedPosition.x = initialLeftEdge + (updatedScale.x * 0.5f);
        fillRenderer.transform.localPosition = updatedPosition;
    }

    public void SetInactive()
    {
        EnsureInitialized();

        if (backgroundRenderer != null)
        {
            backgroundRenderer.enabled = false;
        }

        if (fillRenderer != null)
        {
            fillRenderer.enabled = false;
            fillRenderer.transform.localScale = initialFillLocalScale;
            fillRenderer.transform.localPosition = initialFillLocalPosition;
        }
    }

    private void EnsureInitialized()
    {
        if (isInitialized)
        {
            return;
        }

        EnsureBarSprite(backgroundRenderer);
        EnsureBarSprite(fillRenderer);

        if (fillRenderer != null)
        {
            initialFillLocalScale = fillRenderer.transform.localScale;
            initialFillLocalPosition = fillRenderer.transform.localPosition;
        }

        isInitialized = true;
    }

    private void EnsureBarSprite(SpriteRenderer renderer)
    {
        if (renderer == null || renderer.sprite != null)
        {
            return;
        }

        renderer.sprite = GetRuntimeBarSprite();
        renderer.flipX = false;
        renderer.flipY = false;
    }

    private static void ApplySorting(SpriteRenderer renderer, SpriteRenderer ownerRenderer, int offset)
    {
        if (renderer == null || ownerRenderer == null)
        {
            return;
        }

        renderer.sortingLayerID = ownerRenderer.sortingLayerID;
        renderer.sortingOrder = ownerRenderer.sortingOrder + offset;
    }

    private static Sprite GetRuntimeBarSprite()
    {
        if (runtimeBarSprite != null)
        {
            return runtimeBarSprite;
        }

        var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        runtimeBarSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f);

        return runtimeBarSprite;
    }
}
