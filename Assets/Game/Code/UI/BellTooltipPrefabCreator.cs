#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class BellTooltipPrefabCreator
{
    private const string FolderPath = "Assets/GeneratedPrefabs";
    private const string PrefabPath = FolderPath + "/BellTooltipView.prefab";

    [MenuItem("Tools/Bellgrave/Create Bell Tooltip Prefab")]
    public static void CreatePrefab()
    {
        if (!Directory.Exists(FolderPath))
            Directory.CreateDirectory(FolderPath);

        var root = new GameObject("BellTooltipView", typeof(RectTransform), typeof(CanvasGroup), typeof(Image),
            typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        var rootRect = root.GetComponent<RectTransform>();
        var canvasGroup = root.GetComponent<CanvasGroup>();
        var bg = root.GetComponent<Image>();
        var layout = root.GetComponent<VerticalLayoutGroup>();
        var fitter = root.GetComponent<ContentSizeFitter>();

        rootRect.anchorMin = new Vector2(0f, 1f);
        rootRect.anchorMax = new Vector2(0f, 1f);
        rootRect.pivot = new Vector2(0f, 1f);
        rootRect.sizeDelta = new Vector2(110f, 0f);

        bg.color = new Color32(58, 68, 104, 245);
        bg.raycastTarget = false;

        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.spacing = 1f;
        layout.padding = new RectOffset(4, 4, 3, 3);

        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var outline = root.AddComponent<Outline>();
        outline.effectColor = new Color32(188, 208, 255, 255);
        outline.effectDistance = new Vector2(1f, -1f);

        var shadow = root.AddComponent<Shadow>();
        shadow.effectColor = new Color32(18, 22, 38, 180);
        shadow.effectDistance = new Vector2(1f, -1f);

        var title = CreateText("Title", root.transform, 6, FontStyles.Bold, new Color32(243, 245, 255, 255),
            "Bone Bell");
        var body = CreateText("Body", root.transform, 5, FontStyles.Normal, new Color32(209, 220, 255, 255),
            "Cost: 7 Faith\nSummons Skeleton\nHP 10  DMG 4");

        var view = root.AddComponent<BellTooltipView>();

        SerializedObject so = new SerializedObject(view);
        so.FindProperty("root").objectReferenceValue = rootRect;
        so.FindProperty("background").objectReferenceValue = bg;
        so.FindProperty("titleText").objectReferenceValue = title;
        so.FindProperty("bodyText").objectReferenceValue = body;
        so.FindProperty("canvasGroup").objectReferenceValue = canvasGroup;
        so.ApplyModifiedPropertiesWithoutUndo();

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);

        Debug.Log($"Bell tooltip prefab created at: {PrefabPath}");
    }

    private static TextMeshProUGUI CreateText(string objectName, Transform parent, float fontSize, FontStyles fontStyle,
        Color color, string sampleText)
    {
        var go = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        var text = go.GetComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = color;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.enableWordWrapping = true;
        text.raycastTarget = false;
        text.lineSpacing = 0f;
        text.text = sampleText;

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);

        return text;
    }
}
#endif