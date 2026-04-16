using UnityEditor.Rendering;
using UnityEngine;

public static class Popups
{
    private const string PrefabPath = "TextPopupo";
    private static TextPopupService service;
    private static TextPopupInstance cachedPrefab;

    public static void Damage(Vector3 worldPos, int amount)
    {
        if (amount <= 0) return;
        EnsureService();
        service.Spawn(worldPos, "-" + amount, new Color(1f, 0.4f, 0.4f));
    }

    public static void Heal(Vector3 worldPos, int amount)
    {
        if (amount <= 0) return;
        EnsureService();
        service.Spawn(worldPos, "+" + amount, new Color(0.4f, 1f, 0.4f));
    }

    public static void Text(Vector3 worldPos, string text, Color color)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        EnsureService();
        service.Spawn(worldPos, text, color);
    }

    public static void DamageAbove(Transform anchor, float offsetY, int amount)
    {
        if (amount <= 0 || anchor == null) return;
        EnsureService();
        service.SpawnAbove(anchor, offsetY, "-" + amount, new Color(1f, 0.4f, 0.4f));
    }

    public static void HealAbove(Transform anchor, float offsetY, int amount)
    {
        if (amount <= 0 || anchor == null) return;
        EnsureService();
        service.SpawnAbove(anchor, offsetY, "+" + amount, new Color(0.4f, 1f, 0.4f));
    }

    public static void BlockAbove(Transform anchor, int amount, float offsetY = 1.2f)
    {
        if (amount == 0 || anchor == null) return;

        EnsureService();

        string text = amount > 0
            ? $"+{amount} Block"
            : $"{amount} Block";

        Color color = amount > 0
            ? Color.cyan
            : Color.gray;

        service.SpawnAbove(anchor, offsetY, text, color);
    }

    public static void TextAbove(Transform anchor, float offsetY, string text, Color color)
    {
        if (anchor == null || string.IsNullOrWhiteSpace(text)) return;
        EnsureService();
        service.SpawnAbove(anchor, offsetY, text, color);
    }

    private static void EnsureService()
    {
        if (service != null) return;

        if (cachedPrefab == null)
            cachedPrefab = Resources.Load<TextPopupInstance>(PrefabPath);

        if (cachedPrefab == null)
        {
            Debug.LogError($"Popups: prefab not found in Resources/{PrefabPath}");
            return;
        }

        GameObject root = new GameObject("[TextPopupService]");
        Object.DontDestroyOnLoad(root);

        service = root.AddComponent<TextPopupService>();
        service.Init(cachedPrefab);
    }
}