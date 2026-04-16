using UnityEngine;

public abstract class ContentDef : ScriptableObject
{
    [Tooltip("Уникальный ID. Если пустой, будет использовано имя файла ассета.")]
    [SerializeField] private string id;

    public string Id => string.IsNullOrEmpty(id) ? name : id;
}