using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SoundsPool", menuName = "Audio/SoundsPool")]
public class SoundsDataContainer : ScriptableObject
{
    public List<SoundEntity> soundsEntities;
}

[System.Serializable]
public class SoundEntity
{
    [SerializeField] private SoundId soundId;
    public SoundId SoundId => soundId;
    public List<AudioClip> Clips;

    [SerializeField] private AudioType type;
    public AudioType Type => type;

    public bool Loop = false;
    [Range(0f, 1f)] public float Volume = 1f;

    public AudioClip GetClip()
    {
        if (Clips.Count > 1)
        {
            return Clips[Random.Range(0, Clips.Count)];
        }

        return Clips[0];
    }
}
