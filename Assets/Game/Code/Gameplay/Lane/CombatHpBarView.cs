using UnityEngine;

[DisallowMultipleComponent]
public class CombatHpBarView : MonoBehaviour
{
    private static readonly Color AllyFillColor = new(0.38f, 1f, 0.55f, 1f);
    private static readonly Color EnemyFillColor = new(1f, 0.4f, 0.4f, 1f);
    private static readonly Color BackgroundColor = new(0f, 0f, 0f, 0.75f);

    private static Sprite runtimeBarSprite;

    [SerializeField] private SpriteRenderer backgroundRenderer;
    [SerializeField] private SpriteRenderer fillRenderer;
    [Min(0)] [SerializeField] private int sortingOffset = 10;

    private Vector3 initialFillLocalScale;
    private Vector3 initialFillLocalPosition;

    private int maxHp = 1;
    private bool isBound;
    private bool isInitialized;
    private SpriteRenderer ownerRenderer;

    private void Awake()
    {
        EnsureInitialized();
    }

    public void Bind(int maxHp, bool isEnemy, SpriteRenderer ownerRenderer)
    {
        EnsureInitialized();

        this.maxHp = Mathf.Max(1, maxHp);
        this.ownerRenderer = ownerRenderer;

        if (backgroundRenderer != null)
        {
            backgroundRenderer.color = BackgroundColor;
            ApplySorting(backgroundRenderer, this.ownerRenderer, sortingOffset);
        }

        if (fillRenderer != null)
        {
            fillRenderer.color = isEnemy ? EnemyFillColor : AllyFillColor;
            ApplySorting(fillRenderer, this.ownerRenderer, sortingOffset + 1);
        }

        isBound = true;
        Refresh(this.maxHp);
    }

    private void LateUpdate()
    {
        if (!isBound || ownerRenderer == null)
            return;

        ApplySorting(backgroundRenderer, ownerRenderer, sortingOffset);
        ApplySorting(fillRenderer, ownerRenderer, sortingOffset + 1);
    }

    public void Refresh(int currentHp)
    {
        if (!isBound || fillRenderer == null)
            return;

        float normalized = Mathf.Clamp01((float)Mathf.Max(0, currentHp) / maxHp);

        fillRenderer.enabled = normalized > 0f;
        if (!fillRenderer.enabled)
            return;

        Vector3 updatedScale = initialFillLocalScale;
        updatedScale.x = initialFillLocalScale.x * normalized;
        fillRenderer.transform.localScale = updatedScale;

        float initialLeftEdge = initialFillLocalPosition.x - (initialFillLocalScale.x * 0.5f);

        Vector3 updatedPosition = initialFillLocalPosition;
        updatedPosition.x = initialLeftEdge + (updatedScale.x * 0.5f);
        fillRenderer.transform.localPosition = updatedPosition;
    }

    private void EnsureInitialized()
    {
        if (isInitialized)
            return;

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
            return;

        renderer.sprite = GetRuntimeBarSprite();
        renderer.flipX = false;
        renderer.flipY = false;
    }

    private void ApplySorting(SpriteRenderer renderer, SpriteRenderer ownerRenderer, int offset)
    {
        if (renderer == null || ownerRenderer == null)
            return;

        renderer.sortingLayerID = ownerRenderer.sortingLayerID;
        renderer.sortingOrder = ownerRenderer.sortingOrder + offset;
    }

    private static Sprite GetRuntimeBarSprite()
    {
        if (runtimeBarSprite != null)
            return runtimeBarSprite;

        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
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