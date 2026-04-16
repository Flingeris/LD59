using UnityEngine;

public class TextPopupService : MonoBehaviour
{
    [SerializeField] private TextPopupInstance popupPrefab;

    public void Init(TextPopupInstance prefab)
    {
        popupPrefab = prefab;
    }

    public void Spawn(Vector3 worldPos, string text, Color color)
    {
        if (popupPrefab == null)
        {
            Debug.LogWarning("TextPopupService: popupPrefab is null");
            return;
        }

        if (string.IsNullOrWhiteSpace(text))
            return;

        TextPopupInstance inst = Instantiate(popupPrefab, worldPos, Quaternion.identity);
        inst.Play(text, color);
    }

    public void SpawnAbove(Transform anchor, float offsetY, string text, Color color)
    {
        if (anchor == null) return;
        Spawn(anchor.position + Vector3.up * offsetY, text, color);
    }
}