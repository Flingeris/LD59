using UnityEngine;
using UnityEngine.Rendering;

public class KeeperActor : MonoBehaviour
{
    private const float FacingThreshold = 0.001f;
    private const int SortingPrecision = 100;

    private SpriteRenderer spriteRenderer;
    private SortingGroup sortingGroup;

    [SerializeField] private Animator animator;
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private const float MovingAnimationThreshold = 0.0001f;

    private void LateUpdate()
    {
        UpdateSortingOrder();
    }

    public Vector2 GetWorldPosition()
    {
        var worldPosition = transform.position;
        return new Vector2(worldPosition.x, worldPosition.y);
    }

    public void SetWorldPosition(Vector2 worldPosition)
    {
        var currentPosition = transform.position;
        var movementOffset = new Vector2(worldPosition.x - currentPosition.x, worldPosition.y - currentPosition.y);

        UpdateFacing(movementOffset);
        UpdateMovementAnimation(movementOffset);

        transform.position = new Vector3(worldPosition.x, worldPosition.y, currentPosition.z);
    }

    private void UpdateMovementAnimation(Vector2 movementOffset)
    {
        var targetAnimator = GetAnimator();
        if (targetAnimator == null)
        {
            return;
        }

        var isMoving = movementOffset.sqrMagnitude > MovingAnimationThreshold;
        targetAnimator.SetBool(IsMovingHash, isMoving);
    }

    private Animator GetAnimator()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        return animator;
    }

    public void SetFacingDirection(PoiKeeperFacingDirection facingDirection)
    {
        var targetRenderer = GetSpriteRenderer();
        if (targetRenderer == null)
        {
            return;
        }

        targetRenderer.flipX = facingDirection == PoiKeeperFacingDirection.Left;
    }

    private void UpdateFacing(Vector2 movementOffset)
    {
        if (Mathf.Abs(movementOffset.x) <= FacingThreshold)
        {
            return;
        }

        var targetRenderer = GetSpriteRenderer();
        if (targetRenderer == null)
        {
            return;
        }

        targetRenderer.flipX = movementOffset.x < 0f;
    }

    private void UpdateSortingOrder()
    {
        if (sortingGroup == null)
        {
            sortingGroup = GetComponentInChildren<SortingGroup>();
        }

        if (sortingGroup != null)
        {
            sortingGroup.sortingOrder = -Mathf.RoundToInt(transform.position.y * SortingPrecision);
            return;
        }

        var targetRenderer = GetSpriteRenderer();
        if (targetRenderer == null)
        {
            return;
        }

        targetRenderer.sortingOrder = -Mathf.RoundToInt(transform.position.y * SortingPrecision);
    }

    private SpriteRenderer GetSpriteRenderer()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        return spriteRenderer;
    }
}