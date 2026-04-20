using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
[DisallowMultipleComponent]
public class WorldYSortSpriteGroup : MonoBehaviour
{
    private const int SortingPrecision = 100;

    [SerializeField] private bool includeInactiveChildren = true;

    private SpriteRenderer[] spriteRenderers = new SpriteRenderer[0];
    private SortingGroup[] sortingGroups = new SortingGroup[0];

    private void Reset()
    {
        RefreshSpriteRenderers();
        ApplySortingOrders();
    }

    private void OnEnable()
    {
        RefreshSpriteRenderers();
        ApplySortingOrders();
    }

    private void OnValidate()
    {
        RefreshSpriteRenderers();
        ApplySortingOrders();
    }

    private void OnTransformChildrenChanged()
    {
        RefreshSpriteRenderers();
        ApplySortingOrders();
    }

    private void LateUpdate()
    {
        ApplySortingOrders();
    }

    private void RefreshSpriteRenderers()
    {
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(includeInactiveChildren);
        sortingGroups = GetComponentsInChildren<SortingGroup>(includeInactiveChildren);
    }

    private void ApplySortingOrders()
    {
        if (spriteRenderers == null || spriteRenderers.Length == 0)
        {
            RefreshSpriteRenderers();
        }

        for (var i = 0; i < sortingGroups.Length; i++)
        {
            var sortingGroup = sortingGroups[i];
            if (sortingGroup == null)
            {
                continue;
            }

            var sortingOrder = -Mathf.RoundToInt(sortingGroup.transform.position.y * SortingPrecision);
            if (sortingGroup.sortingOrder != sortingOrder)
            {
                sortingGroup.sortingOrder = sortingOrder;
            }
        }

        for (var i = 0; i < spriteRenderers.Length; i++)
        {
            var spriteRenderer = spriteRenderers[i];
            if (spriteRenderer == null)
            {
                continue;
            }

            var parentSortingGroup = spriteRenderer.GetComponentInParent<SortingGroup>();
            if (parentSortingGroup != null)
            {
                continue;
            }

            var sortingOrder = -Mathf.RoundToInt(spriteRenderer.transform.position.y * SortingPrecision);
            if (spriteRenderer.sortingOrder != sortingOrder)
            {
                spriteRenderer.sortingOrder = sortingOrder;
            }
        }
    }
}
