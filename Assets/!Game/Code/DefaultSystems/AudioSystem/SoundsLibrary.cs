using System.Collections.Generic;
using UnityEngine;

public class SoundsLibrary : MonoBehaviour
{
    [SerializeField] private SoundsDataContainer soundsDataContainer;

    private Dictionary<SoundId, SoundEntity> soundsMap;

    private void Awake()
    {
        Initialize();
    }

    public void Initialize()
    {
        if (soundsDataContainer == null)
        {
            Debug.LogError("Не установлен SoundsDataContainer. Проверьте найстроки");
            return;
        }

        soundsMap = new Dictionary<SoundId, SoundEntity>();

        foreach (var sound in soundsDataContainer.soundsEntities)
        {
            if (!soundsMap.ContainsKey(sound.SoundId))
            {
                soundsMap.Add(sound.SoundId, sound);
            }
            else
            {
                Debug.LogWarning($"Дублирующийся SoundId: {sound.SoundId}. Проверьте настройки SoundsPool.");
            }
        }
    }

    public SoundEntity GetSound(SoundId soundId)
    {
        if (soundsMap == null)
        {
            return null;
        }

        if (soundsMap.TryGetValue(soundId, out SoundEntity sound))
        {
            if (sound.Clips == null)
            {
                Debug.LogWarning($"Звук с идентификатором {soundId} не имеет клипов");
                return null;
            }
            sound.Clips.RemoveAll(c => c == null);

            if (sound.Clips.Count == 0)
            {
                Debug.LogWarning($"Звук с идентификатором {soundId} не имеет клипов");
                return null;
            }

            return sound;
        }
        Debug.LogWarning($"Звук с идентификатором {soundId} не найден в пуле звуков.");
        return null;
    }
}