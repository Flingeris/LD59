using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SoundsDataContainer))]
public class SoundsDataContainerEditor : Editor
{
    private SerializedProperty soundsEntitiesProp;

    // Foldout по каждому SoundEntity (индекс -> состояние). Поддерживаем при reorder.
    private bool[] foldoutStates;

    // Foldout по группам типа
    private readonly Dictionary<int, bool> typeFoldouts = new Dictionary<int, bool>();

    // Поиск
    private string searchQuery = string.Empty;

    // Данные enum-типа (type)
    private string[] soundTypeDisplayNames;
    private int soundTypeCount;

    // Drag&Drop (перетаскивание SoundEntity)
    private const string DragKey = "SoundsDataContainerEditor.SoundEntityIndex";

    // Стили инициализируем лениво
    private GUIStyle headerStyle;
    private GUIStyle subHeaderStyle;
    private GUIStyle boxStyle;
    private GUIStyle groupStyle;
    private GUIStyle dragHandleStyle;

    private void OnEnable()
    {
        soundsEntitiesProp = serializedObject.FindProperty("soundsEntities");

        ResolveSoundTypeEnumInfo();
        EnsureFoldoutStatesSize();
    }

    private void ResolveSoundTypeEnumInfo()
    {
        // 1) Если есть элементы — enumDisplayNames достанем напрямую.
        if (soundsEntitiesProp != null && soundsEntitiesProp.arraySize > 0)
        {
            var first = soundsEntitiesProp.GetArrayElementAtIndex(0);
            var typeProp = first.FindPropertyRelative("type");
            if (typeProp != null && typeProp.propertyType == SerializedPropertyType.Enum)
            {
                soundTypeDisplayNames = typeProp.enumDisplayNames;
                soundTypeCount = soundTypeDisplayNames != null ? soundTypeDisplayNames.Length : 0;
                return;
            }
        }

        // 2) Если список пустой — пытаемся через reflection достать enum тип из модели.
        try
        {
            var containerType = target.GetType();
            var field = containerType.GetField("soundsEntities", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null)
            {
                soundTypeDisplayNames = Array.Empty<string>();
                soundTypeCount = 0;
                return;
            }

            var listType = field.FieldType;
            Type elementType = null;

            if (listType.IsArray)
                elementType = listType.GetElementType();
            else if (listType.IsGenericType)
                elementType = listType.GetGenericArguments()[0];

            if (elementType == null)
            {
                soundTypeDisplayNames = Array.Empty<string>();
                soundTypeCount = 0;
                return;
            }

            // Ищем поле/свойство "type" в элементе
            var typeField = elementType.GetField("type", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Type enumType = typeField != null ? typeField.FieldType : null;

            if (enumType == null)
            {
                var typePropInfo = elementType.GetProperty("type", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                enumType = typePropInfo != null ? typePropInfo.PropertyType : null;
            }

            if (enumType != null && enumType.IsEnum)
            {
                soundTypeDisplayNames = Enum.GetNames(enumType);
                soundTypeCount = soundTypeDisplayNames.Length;
            }
            else
            {
                soundTypeDisplayNames = Array.Empty<string>();
                soundTypeCount = 0;
            }
        }
        catch
        {
            soundTypeDisplayNames = Array.Empty<string>();
            soundTypeCount = 0;
        }
    }

    private void EnsureFoldoutStatesSize()
    {
        int size = soundsEntitiesProp != null ? soundsEntitiesProp.arraySize : 0;

        if (foldoutStates == null)
        {
            foldoutStates = new bool[size];
            return;
        }

        if (foldoutStates.Length == size) return;

        var newArr = new bool[size];
        int copyLen = Mathf.Min(foldoutStates.Length, size);
        Array.Copy(foldoutStates, newArr, copyLen);
        foldoutStates = newArr;
    }

    private void InitializeStyles()
    {
        if (headerStyle == null)
        {
            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                margin = new RectOffset(5, 5, 10, 5)
            };
        }

        if (subHeaderStyle == null)
        {
            subHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12
            };
        }

        if (boxStyle == null)
        {
            boxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 10, 10),
                margin = new RectOffset(5, 5, 5, 5)
            };
        }

        if (groupStyle == null)
        {
            groupStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 8, 8),
                margin = new RectOffset(5, 5, 8, 8)
            };
        }

        if (dragHandleStyle == null)
        {
            dragHandleStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12
            };
        }
    }

    public override void OnInspectorGUI()
    {
        InitializeStyles();

        serializedObject.Update();
        EnsureFoldoutStatesSize();

        EditorGUILayout.LabelField("Звуковой пул", headerStyle);

        // Верхняя панель
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Добавить новый звук", GUILayout.Height(26)))
        {
            AddNewSoundEntity();
            GUIUtility.ExitGUI();
        }

        GUILayout.FlexibleSpace();

        // Быстрые действия
        if (GUILayout.Button("Раскрыть все", GUILayout.Width(100), GUILayout.Height(26)))
        {
            for (int i = 0; i < foldoutStates.Length; i++) foldoutStates[i] = true;
        }
        if (GUILayout.Button("Свернуть все", GUILayout.Width(100), GUILayout.Height(26)))
        {
            for (int i = 0; i < foldoutStates.Length; i++) foldoutStates[i] = false;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8);

        // Поиск
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Поиск", GUILayout.Width(40));
        searchQuery = EditorGUILayout.TextField(searchQuery);
        if (GUILayout.Button("×", GUILayout.Width(22)))
        {
            searchQuery = string.Empty;
            GUI.FocusControl(null);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // Если не смогли определить enum типов — показываем плоский список, но с drag&drop reorder.
        if (soundTypeCount <= 0)
        {
            EditorGUILayout.HelpBox(
                "Не удалось определить список типов звука (enum). Показываю общий список без группировки.",
                MessageType.Warning);
            DrawFlatListWithDrag();
        }
        else
        {
            DrawGroupedByType();
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawGroupedByType()
    {
        // Собираем индексы по типам, с учетом фильтра поиска
        var indicesByType = new Dictionary<int, List<int>>();
        for (int t = 0; t < soundTypeCount; t++)
            indicesByType[t] = new List<int>();

        string q = string.IsNullOrWhiteSpace(searchQuery) ? null : searchQuery.Trim().ToLowerInvariant();

        for (int i = 0; i < soundsEntitiesProp.arraySize; i++)
        {
            var el = soundsEntitiesProp.GetArrayElementAtIndex(i);
            var typeProp = el.FindPropertyRelative("type");
            if (typeProp == null || typeProp.propertyType != SerializedPropertyType.Enum)
                continue;

            int typeIndex = Mathf.Clamp(typeProp.enumValueIndex, 0, soundTypeCount - 1);

            if (q != null)
            {
                var idProp = el.FindPropertyRelative("soundId");
                var idText = GetPropertyAsSearchableText(idProp);
                if (string.IsNullOrEmpty(idText) || !idText.ToLowerInvariant().Contains(q))
                    continue;
            }

            indicesByType[typeIndex].Add(i);
        }

        // Рисуем группы в порядке enum
        for (int t = 0; t < soundTypeCount; t++)
        {
            string typeName = soundTypeDisplayNames[t];
            if (!typeFoldouts.ContainsKey(t))
                typeFoldouts[t] = true;

            EditorGUILayout.BeginVertical(groupStyle);

            // Заголовок группы
            EditorGUILayout.BeginHorizontal();
            typeFoldouts[t] = EditorGUILayout.Foldout(typeFoldouts[t], $"{typeName}  ({indicesByType[t].Count})", true);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("+", GUILayout.Width(28), GUILayout.Height(18)))
            {
                AddNewSoundEntity(typeIndexOverride: t);
                GUIUtility.ExitGUI();
            }
            EditorGUILayout.EndHorizontal();

            // Drop-zone для смены типа перетаскиванием
            Rect dropRect = GUILayoutUtility.GetRect(0f, 18f, GUILayout.ExpandWidth(true));
            GUI.Box(dropRect, "Перетащи сюда звук, чтобы сменить тип", EditorStyles.centeredGreyMiniLabel);
            HandleTypeDrop(dropRect, targetTypeIndex: t);

            if (typeFoldouts[t])
            {
                if (indicesByType[t].Count == 0)
                {
                    EditorGUILayout.HelpBox("В этой группе пока нет звуков.", MessageType.Info);
                }
                else
                {
                    for (int k = 0; k < indicesByType[t].Count; k++)
                    {
                        int indexInArray = indicesByType[t][k];
                        DrawSoundEntity(indexInArray, groupTypeIndex: t);
                        EditorGUILayout.Space(5);
                    }
                }
            }

            EditorGUILayout.EndVertical();
        }

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            EditorGUILayout.HelpBox("Показаны только элементы, которые совпали с поиском по soundId.", MessageType.None);
        }
    }

    private void DrawFlatListWithDrag()
    {
        if (soundsEntitiesProp.arraySize == 0)
        {
            EditorGUILayout.HelpBox("Нет добавленных звуков. Нажмите кнопку выше, чтобы добавить.", MessageType.Info);
            return;
        }

        string q = string.IsNullOrWhiteSpace(searchQuery) ? null : searchQuery.Trim().ToLowerInvariant();

        for (int i = 0; i < soundsEntitiesProp.arraySize; i++)
        {
            var el = soundsEntitiesProp.GetArrayElementAtIndex(i);
            if (q != null)
            {
                var idProp = el.FindPropertyRelative("soundId");
                var idText = GetPropertyAsSearchableText(idProp);
                if (string.IsNullOrEmpty(idText) || !idText.ToLowerInvariant().Contains(q))
                    continue;
            }

            DrawSoundEntity(i, groupTypeIndex: -1);
            EditorGUILayout.Space(5);
        }

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            EditorGUILayout.HelpBox("Показаны только элементы, которые совпали с поиском по soundId.", MessageType.None);
        }
    }

    private void DrawSoundEntity(int indexInArray, int groupTypeIndex)
    {
        EnsureFoldoutStatesSize();
        if (indexInArray < 0 || indexInArray >= soundsEntitiesProp.arraySize) return;

        SerializedProperty soundEntityProp = soundsEntitiesProp.GetArrayElementAtIndex(indexInArray);
        SerializedProperty soundIdProp = soundEntityProp.FindPropertyRelative("soundId");
        SerializedProperty typeProp = soundEntityProp.FindPropertyRelative("type");
        SerializedProperty clipsProp = soundEntityProp.FindPropertyRelative("Clips");

        EditorGUILayout.BeginVertical(boxStyle);

        // --- Header row ---
        Rect headerRect = GUILayoutUtility.GetRect(0f, EditorGUIUtility.singleLineHeight, GUILayout.ExpandWidth(true));

        float x = headerRect.x;
        float y = headerRect.y;
        float h = headerRect.height;

        Rect foldRect = new Rect(x, y, 16f, h);
        x += foldRect.width;

        Rect handleRect = new Rect(x, y, 18f, h);
        x += handleRect.width + 2f;

        float rightDelW = 22f;
        float rightClipsW = 80f;
        float rightTypeW = 120f;

        Rect delRect = new Rect(headerRect.xMax - rightDelW, y, rightDelW, h);
        Rect clipsRect = new Rect(delRect.xMin - 4f - rightClipsW, y, rightClipsW, h);
        Rect typeLabelRect = new Rect(clipsRect.xMin - 4f - rightTypeW, y, rightTypeW, h);

        Rect idRect = new Rect(x, y, Mathf.Max(40f, typeLabelRect.xMin - 6f - x), h);

        foldoutStates[indexInArray] = EditorGUI.Foldout(foldRect, foldoutStates[indexInArray], GUIContent.none, true);
        GUI.Label(handleRect, "≡", dragHandleStyle);

        if (soundIdProp != null)
            EditorGUI.PropertyField(idRect, soundIdProp, GUIContent.none);
        else
            EditorGUI.LabelField(idRect, "soundId не найден");

        string typeName = typeProp != null && typeProp.propertyType == SerializedPropertyType.Enum
            ? typeProp.enumDisplayNames[typeProp.enumValueIndex]
            : "?";

        int clipsCount = clipsProp != null && clipsProp.isArray ? clipsProp.arraySize : 0;

        EditorGUI.LabelField(typeLabelRect, $"Тип: {typeName}", EditorStyles.miniLabel);
        EditorGUI.LabelField(clipsRect, $"Клипов: {clipsCount}", EditorStyles.miniLabel);

        if (GUI.Button(delRect, "×"))
        {
            DeleteSoundEntity(indexInArray);
            GUIUtility.ExitGUI();
        }

        HandleReorderDrop(headerRect, handleRect, indexInArray, groupTypeIndex);

        // --- Details ---
        if (foldoutStates[indexInArray])
        {
            EditorGUI.indentLevel++;

            if (typeProp != null)
                EditorGUILayout.PropertyField(typeProp, new GUIContent("Тип"));

            SerializedProperty loopProp = soundEntityProp.FindPropertyRelative("Loop");
            SerializedProperty volumeProp = soundEntityProp.FindPropertyRelative("Volume");

            if (loopProp != null)
                EditorGUILayout.PropertyField(loopProp);

            if (volumeProp != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(volumeProp);
                EditorGUILayout.LabelField($"{volumeProp.floatValue * 100:0}%", GUILayout.Width(50));
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Аудиоклипы", subHeaderStyle);

            if (clipsProp == null || !clipsProp.isArray)
            {
                EditorGUILayout.HelpBox("Поле Clips не найдено или не является массивом.", MessageType.Error);
            }
            else
            {
                if (GUILayout.Button("Добавить клип"))
                {
                    clipsProp.arraySize++;
                    serializedObject.ApplyModifiedProperties();
                }

                if (clipsProp.arraySize == 0)
                {
                    EditorGUILayout.HelpBox("Не добавлено ни одного аудиоклипа.", MessageType.Warning);
                }
                else
                {
                    for (int j = 0; j < clipsProp.arraySize; j++)
                    {
                        EditorGUILayout.BeginHorizontal();

                        SerializedProperty clipProp = clipsProp.GetArrayElementAtIndex(j);
                        EditorGUILayout.PropertyField(clipProp, new GUIContent($"Клип {j + 1}"));

                        GUI.enabled = clipProp.objectReferenceValue != null;
                        if (GUILayout.Button("▶", GUILayout.Width(25)))
                        {
                            AudioClip clip = clipProp.objectReferenceValue as AudioClip;
                            if (clip != null)
                            {
                                float volume = volumeProp != null ? volumeProp.floatValue : 1f;
                                AudioPreview.StopAllClips();
                                AudioPreview.PlayClip(clip, volume);
                            }
                        }

                        if (GUILayout.Button("■", GUILayout.Width(25)))
                        {
                            AudioPreview.StopAllClips();
                        }
                        GUI.enabled = true;

                        if (GUILayout.Button("×", GUILayout.Width(25)))
                        {
                            clipsProp.DeleteArrayElementAtIndex(j);
                            serializedObject.ApplyModifiedProperties();
                            break;
                        }

                        EditorGUILayout.EndHorizontal();
                    }
                }
            }

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
    }

    private void HandleReorderDrop(Rect headerRect, Rect handleRect, int targetIndexInArray, int groupTypeIndex)
    {
        Event e = Event.current;

        if (e.type == EventType.MouseDown && e.button == 0 && handleRect.Contains(e.mousePosition))
        {
            DragAndDrop.PrepareStartDrag();
            DragAndDrop.SetGenericData(DragKey, targetIndexInArray);
            DragAndDrop.objectReferences = Array.Empty<UnityEngine.Object>();
            e.Use();
        }
        else if (e.type == EventType.MouseDrag && e.button == 0)
        {
            if (DragAndDrop.GetGenericData(DragKey) != null)
            {
                DragAndDrop.StartDrag("Move Sound");
                e.Use();
            }
        }

        if ((e.type == EventType.DragUpdated || e.type == EventType.DragPerform) && headerRect.Contains(e.mousePosition))
        {
            object data = DragAndDrop.GetGenericData(DragKey);
            if (data is int sourceIndex)
            {
                if (sourceIndex < 0 || sourceIndex >= soundsEntitiesProp.arraySize) return;

                if (groupTypeIndex >= 0)
                {
                    int srcType = GetElementTypeIndex(sourceIndex);
                    int dstType = GetElementTypeIndex(targetIndexInArray);
                    if (srcType != dstType)
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                        return;
                    }
                }

                DragAndDrop.visualMode = DragAndDropVisualMode.Move;

                if (e.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();

                    int correctedDest = targetIndexInArray;
                    if (sourceIndex < correctedDest) correctedDest--;

                    MoveSoundEntity(sourceIndex, correctedDest);

                    DragAndDrop.SetGenericData(DragKey, null);
                    e.Use();
                    GUIUtility.ExitGUI();
                }
            }
        }

        if (e.type == EventType.DragExited)
        {
            DragAndDrop.SetGenericData(DragKey, null);
        }
    }

    private void HandleTypeDrop(Rect dropRect, int targetTypeIndex)
    {
        Event e = Event.current;

        if ((e.type == EventType.DragUpdated || e.type == EventType.DragPerform) && dropRect.Contains(e.mousePosition))
        {
            object data = DragAndDrop.GetGenericData(DragKey);
            if (data is int sourceIndex)
            {
                if (sourceIndex < 0 || sourceIndex >= soundsEntitiesProp.arraySize) return;

                DragAndDrop.visualMode = DragAndDropVisualMode.Move;

                if (e.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();

                    var el = soundsEntitiesProp.GetArrayElementAtIndex(sourceIndex);
                    var typeProp = el.FindPropertyRelative("type");
                    if (typeProp != null && typeProp.propertyType == SerializedPropertyType.Enum)
                    {
                        typeProp.enumValueIndex = Mathf.Clamp(targetTypeIndex, 0, soundTypeCount - 1);

                        int destIndex = FindInsertIndexAtEndOfType(targetTypeIndex, excludeIndex: sourceIndex);
                        if (destIndex >= 0 && destIndex < soundsEntitiesProp.arraySize)
                        {
                            int insertAt = Mathf.Min(destIndex + 1, soundsEntitiesProp.arraySize - 1);
                            if (sourceIndex != insertAt)
                            {
                                int corrected = insertAt;
                                if (sourceIndex < corrected) corrected--;
                                MoveSoundEntity(sourceIndex, corrected);
                            }
                        }

                        serializedObject.ApplyModifiedProperties();
                    }

                    DragAndDrop.SetGenericData(DragKey, null);
                    e.Use();
                    GUIUtility.ExitGUI();
                }
            }
        }
    }

    private int FindInsertIndexAtEndOfType(int typeIndex, int excludeIndex)
    {
        int last = -1;
        for (int i = 0; i < soundsEntitiesProp.arraySize; i++)
        {
            if (i == excludeIndex) continue;
            if (GetElementTypeIndex(i) == typeIndex)
                last = i;
        }
        return last;
    }

    private int GetElementTypeIndex(int index)
    {
        if (index < 0 || index >= soundsEntitiesProp.arraySize) return -1;
        var el = soundsEntitiesProp.GetArrayElementAtIndex(index);
        var typeProp = el.FindPropertyRelative("type");
        if (typeProp == null || typeProp.propertyType != SerializedPropertyType.Enum) return -1;
        return typeProp.enumValueIndex;
    }

    private void MoveSoundEntity(int from, int to)
    {
        if (from == to) return;
        if (from < 0 || to < 0) return;
        if (from >= soundsEntitiesProp.arraySize || to >= soundsEntitiesProp.arraySize) return;

        soundsEntitiesProp.MoveArrayElement(from, to);

        EnsureFoldoutStatesSize();
        bool state = foldoutStates[from];

        if (from < to)
        {
            for (int i = from; i < to; i++)
                foldoutStates[i] = foldoutStates[i + 1];
            foldoutStates[to] = state;
        }
        else
        {
            for (int i = from; i > to; i--)
                foldoutStates[i] = foldoutStates[i - 1];
            foldoutStates[to] = state;
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void AddNewSoundEntity(int? typeIndexOverride = null)
    {
        soundsEntitiesProp.arraySize++;
        serializedObject.ApplyModifiedProperties();
        serializedObject.Update();

        EnsureFoldoutStatesSize();

        int newIndex = soundsEntitiesProp.arraySize - 1;
        foldoutStates[newIndex] = true;

        if (typeIndexOverride.HasValue)
        {
            var el = soundsEntitiesProp.GetArrayElementAtIndex(newIndex);
            var typeProp = el.FindPropertyRelative("type");
            if (typeProp != null && typeProp.propertyType == SerializedPropertyType.Enum)
            {
                typeProp.enumValueIndex = Mathf.Clamp(typeIndexOverride.Value, 0, Math.Max(0, soundTypeCount - 1));
                serializedObject.ApplyModifiedProperties();
            }
        }
    }

    private void AddNewSoundEntity()
    {
        AddNewSoundEntity(typeIndexOverride: null);
    }

    private void DeleteSoundEntity(int index)
    {
        if (index < 0 || index >= soundsEntitiesProp.arraySize) return;

        soundsEntitiesProp.DeleteArrayElementAtIndex(index);
        serializedObject.ApplyModifiedProperties();

        EnsureFoldoutStatesSize();
    }

    private static string GetPropertyAsSearchableText(SerializedProperty prop)
    {
        if (prop == null) return string.Empty;

        switch (prop.propertyType)
        {
            case SerializedPropertyType.String:
                return prop.stringValue;
            case SerializedPropertyType.Enum:
                return prop.enumDisplayNames != null && prop.enumValueIndex >= 0 && prop.enumValueIndex < prop.enumDisplayNames.Length
                    ? prop.enumDisplayNames[prop.enumValueIndex]
                    : prop.enumValueIndex.ToString();
            case SerializedPropertyType.Integer:
                return prop.intValue.ToString();
            case SerializedPropertyType.ObjectReference:
                return prop.objectReferenceValue != null ? prop.objectReferenceValue.name : string.Empty;
            default:
                return prop.displayName;
        }
    }
}

// Вспомогательный класс для проигрывания аудио в редакторе
public static class AudioPreview
{
    private static AudioClip currentPlayingClip;
    private static AudioSource previewAudioSource;

    public static void PlayClip(AudioClip clip, float volume = 1.0f)
    {
        if (clip == null) return;

        StopAllClips();

        try
        {
            if (previewAudioSource == null)
            {
                GameObject tempGO = GameObject.Find("__AudioPreviewObject");
                if (tempGO == null)
                {
                    tempGO = new GameObject("__AudioPreviewObject");
                    tempGO.hideFlags = HideFlags.HideAndDontSave;
                }

                previewAudioSource = tempGO.GetComponent<AudioSource>();
                if (previewAudioSource == null)
                {
                    previewAudioSource = tempGO.AddComponent<AudioSource>();
                }
            }

            previewAudioSource.clip = clip;
            previewAudioSource.volume = volume;
            previewAudioSource.loop = false;
            previewAudioSource.Play();

            currentPlayingClip = clip;

            Debug.Log($"Воспроизводится клип: {clip.name} с громкостью {volume}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Ошибка воспроизведения аудио: {e.Message}");
        }
    }

    public static void StopAllClips()
    {
        try
        {
            if (previewAudioSource != null && previewAudioSource.isPlaying)
            {
                previewAudioSource.Stop();

                if (currentPlayingClip != null)
                {
                    Debug.Log($"Остановлен клип: {currentPlayingClip.name}");
                }
            }

            currentPlayingClip = null;

            Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
            Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
            MethodInfo method = audioUtilClass.GetMethod(
                "StopAllPreviewClips",
                BindingFlags.Static | BindingFlags.Public
            );

            method?.Invoke(null, null);
        }
        catch (Exception e)
        {
            Debug.LogError($"Ошибка остановки аудио: {e.Message}");
        }
    }

    public static bool IsPlaying()
    {
        return previewAudioSource != null && previewAudioSource.isPlaying;
    }

    [UnityEditor.InitializeOnLoadMethod]
    private static void Initialize()
    {
        UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange state)
    {
        if (state == UnityEditor.PlayModeStateChange.EnteredPlayMode ||
            state == UnityEditor.PlayModeStateChange.ExitingEditMode)
        {
            StopAllClips();

            if (previewAudioSource != null)
            {
                GameObject tempGO = previewAudioSource.gameObject;
                previewAudioSource = null;
                UnityEngine.Object.DestroyImmediate(tempGO);
            }
        }
    }
}
