using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class CMS
{
    private static bool isInit;
    private static readonly Dictionary<string, ContentDef> _byId = new();
    private static readonly List<ContentDef> _all = new();

    public static void Init()
    {
        if (isInit) return;
        isInit = true;

        AutoAdd();
    }

    private static void AutoAdd()
    {
        _byId.Clear();
        _all.Clear();

        var allDefs = Resources.LoadAll<ContentDef>("Content");

        foreach (var def in allDefs)
        {
            if (def == null) continue;

            var id = def.Id;

            if (string.IsNullOrWhiteSpace(id))
            {
                Debug.LogWarning($"[CSM] Def '{def.name}' has empty Id");
                continue;
            }

            if (_byId.ContainsKey(id))
            {
                Debug.LogError($"[CSM] Duplicate Id '{id}' on asset '{def.name}' and '{_byId[id].name}'");
                continue;
            }

            _byId.Add(id, def);
            _all.Add(def);
        }

        Debug.Log($"[CSM] Loaded {_all.Count} defs");
    }

    // --- Публичное API ---

    public static ContentDef Get(string id)
    {
        Init();

        if (string.IsNullOrWhiteSpace(id))
        {
            Debug.LogError("[CSM] Get called with null/empty id");
            return null;
        }

        if (_byId.TryGetValue(id, out var def))
            return def;

        Debug.LogError($"[CSM] Def with Id '{id}' not found");
        return null;
    }

    public static T Get<T>(string id) where T : ContentDef
    {
        Init();

        if (!_byId.TryGetValue(id, out var def))
        {
            Debug.LogError($"[CSM] Def with Id '{id}' not found");
            return null;
        }

        if (def is T typed)
            return typed;

        Debug.LogError($"[CSM] Def '{id}' is '{def.GetType().Name}', not '{typeof(T).Name}'");
        return null;
    }

    /// <summary>Все дефы конкретного типа (например, все ItemDef).</summary>
    public static IEnumerable<T> GetAll<T>() where T : ContentDef
    {
        Init();
        return _all.OfType<T>();
    }

    /// <summary>Все ContentDef вообще.</summary>
    public static IEnumerable<ContentDef> GetAll()
    {
        Init();
        return _all;
    }
}

public static class CMSUtil
{
    public static T Load<T>(this string path) where T : Object
    {
        return Resources.Load<T>(path);
    }

    public static Sprite LoadFromSpritesheet(string imageName, string spriteName)
    {
        Sprite[] all = Resources.LoadAll<Sprite>(imageName);

        foreach (var s in all)
        {
            if (s.name == spriteName)
            {
                return s;
            }
        }

        return null;
    }
}