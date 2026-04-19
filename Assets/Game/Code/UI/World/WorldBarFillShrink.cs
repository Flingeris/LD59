using UnityEngine;

public class WorldBarFillShrink : MonoBehaviour
{
    [SerializeField] private Transform fill;
    [SerializeField] private bool hideWhenEmpty = false;

    private Vector3 initialScale;

    private void Awake()
    {
        if (fill == null)
            fill = transform;

        initialScale = fill.localScale;
    }

    public void SetNormalized(float value)
    {
        value = Mathf.Clamp01(value);

        var scale = initialScale;
        scale.x *= value;
        fill.localScale = scale;

        if (hideWhenEmpty)
            fill.gameObject.SetActive(value > 0.001f);
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }
}