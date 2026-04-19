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

    private void Awake()
    {
        EnsureInitialized();
    }

    public void Bind(int maxHp, bool isEnemy, SpriteRenderer ownerRenderer)
    {
        EnsureInitialized();

        this.maxHp = Mathf.Max(1, maxHp);

        if (backgroundRenderer != null)
        {
            backgroundRenderer.color = BackgroundColor;
            ApplySorting(backgroundRenderer, ownerRenderer, sortingOffset);
        }

        if (fillRenderer != null)
        {
            fillRenderer.color = isEnemy ? EnemyFillColor : AllyFillColor;
            ApplySorting(fillRenderer, ownerRenderer, sortingOffset + 1);
        }

        isBound = true;
        Refresh(this.maxHp);
    }

    public void Refresh(int currentHp)
    {
        if (!isBound || fillRenderer == null)
        {
            return;
        }

        var normalized = Mathf.Clamp01((float)Mathf.Max(0, currentHp) / maxHp);
        var updatedScale = initialFillLocalScale;
        updatedScale.x = initialFillLocalScale.x * normalized;

        fillRenderer.enabled = normalized > 0f;
        fillRenderer.transform.localScale = updatedScale;

        var updatedPosition = initialFillLocalPosition;
        updatedPosition.x -= (initialFillLocalScale.x - updatedScale.x) * 0.5f;
        fillRenderer.transform.localPosition = updatedPosition;
    }

    private void EnsureInitialized()
    {
        if (fillRenderer != null)
        {
            initialFillLocalScale = fillRenderer.transform.localScale;
            initialFillLocalPosition = fillRenderer.transform.localPosition;
        }

        EnsureBarSprite(backgroundRenderer);
        EnsureBarSprite(fillRenderer);
    }

    private void EnsureBarSprite(SpriteRenderer renderer)
    {
        if (renderer == null || renderer.sprite != null)
        {
            return;
        }

        renderer.sprite = GetRuntimeBarSprite();
    }

    private void ApplySorting(SpriteRenderer renderer, SpriteRenderer ownerRenderer, int offset)
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

        runtimeBarSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        return runtimeBarSprite;
    }
}
