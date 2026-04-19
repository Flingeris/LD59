using DG.Tweening;
using TMPro;
using UnityEngine;

public class TextPopupInstance : MonoBehaviour
{
    [SerializeField] private TMP_Text label;

    [Header("Motion")] [SerializeField] private float riseDistance = 1f;
    [SerializeField] private float duration = 0.6f;
    [SerializeField] private float startScale = 0.9f;
    [SerializeField] private float endScale = 1.1f;
    [SerializeField] private float fadeDelayNormalized = 0.3f;
    [SerializeField] private int sortingOrder = 20;

    private Sequence sequence;

    public void Play(string text, Color color)
    {
        if (label == null)
            label = GetComponentInChildren<TMP_Text>(true);

        if (label == null)
        {
            Debug.LogWarning("TextPopupInstance: TMP_Text not found");
            Destroy(gameObject);
            return;
        }

        KillTweens();

        label.text = text;
        label.color = color;
        label.alpha = 1f;

        if (label is TextMeshPro worldText)
        {
            worldText.sortingOrder = sortingOrder;
        }
        else
        {
            var labelRenderer = label.GetComponent<Renderer>();
            if (labelRenderer != null)
            {
                labelRenderer.sortingOrder = sortingOrder;
            }
        }

        Transform tr = transform;
        Vector3 startPos = tr.position;

        tr.localScale = Vector3.one * startScale;
        tr.position = startPos;

        sequence = DOTween.Sequence();
        sequence.Append(tr.DOMoveY(startPos.y + riseDistance, duration).SetEase(Ease.OutQuad));
        sequence.Join(tr.DOScale(endScale, duration).SetEase(Ease.OutQuad));
        sequence.Join(label.DOFade(0f, duration).SetEase(Ease.InQuad).SetDelay(duration * fadeDelayNormalized));
        sequence.OnComplete(() => Destroy(gameObject));
    }

    private void OnDestroy()
    {
        KillTweens();
    }

    private void KillTweens()
    {
        if (sequence != null && sequence.IsActive())
        {
            sequence.Kill();
            sequence = null;
        }

        transform.DOKill();
        if (label != null)
            label.DOKill();
    }
}
