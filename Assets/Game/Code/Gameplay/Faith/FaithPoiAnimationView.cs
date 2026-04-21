using UnityEngine;

public class FaithPoiAnimationView : MonoBehaviour
{
    [SerializeField] private Animator animator;

    private static readonly int IsKeeperNearHash = Animator.StringToHash("IsKeeperNear");

    private void Reset()
    {
        animator = GetComponentInChildren<Animator>();
    }

    public void SetKeeperNearby(bool isKeeperNear)
    {
        if (animator == null)
        {
            return;
        }

        animator.SetBool(IsKeeperNearHash, isKeeperNear);
    }
}