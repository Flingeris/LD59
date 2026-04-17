using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SoundsLibrary))]
public class AudioSystem : MonoBehaviour
{
    public const string MUSIC_VOL_KEY = "Music_volume";
    public const string SFX_VOL_KEY = "SFX_volume";
    public const string AMBIENT_VOL_KEY = "Ambient_volume";

    [SerializeField] private SoundsLibrary soundsLibrary;
    private Dictionary<AudioType, IAudioPlayer> audioPlayers = new();

    public int initSFXPoolSize = 10;
    public int maxSFXPoolSize = 24;

    [Range(0f, 1f)] public float MusicVolume = 1f;
    [Range(0f, 1f)] public float SfxVolume = 1f;
    [Range(0f, 1f)] public float AmbientVolume = 1f;

    private void OnValidate()
    {
        if (soundsLibrary == null) soundsLibrary = GetComponent<SoundsLibrary>();
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        if (G.audioSystem != null && G.audioSystem != this)
        {
            Destroy(gameObject);
            return;
        }

        G.audioSystem = this;
        Init();
        MusicVolume = PlayerPrefs.GetFloat(MUSIC_VOL_KEY, MusicVolume);
        AmbientVolume = PlayerPrefs.GetFloat(AMBIENT_VOL_KEY, AmbientVolume);
        SfxVolume = PlayerPrefs.GetFloat(SFX_VOL_KEY, SfxVolume);
    }

    private void Init()
    {
        audioPlayers[AudioType.SFX] = new SFXPlayer(this, initSFXPoolSize, maxSFXPoolSize);
        audioPlayers[AudioType.Ambient] = new AmbientPlayer(this);
        audioPlayers[AudioType.Music] = new MusicPlayer(this);
    }

    private void PlayInternal(SoundId soundId, float pitch = 1f, float volumeMul = 1f)
    {
        if (!TryGetSound(soundId, out var soundEntity))
            return;

        if (!TryGetPlayer(soundEntity.Type, out var player))
            return;

        if (soundEntity.Type == AudioType.SFX && player is SFXPlayer sfxPlayer)
        {
            sfxPlayer.Play(soundEntity, pitch, volumeMul);
            return;
        }

        player.Play(soundEntity);
    }


    public void Play(SoundId soundID)
    {
        PlayInternal(soundID);
    }

    public void PlayPitched(SoundId soundId, float pitch = 1f, float volumeMul = 1f)
    {
        PlayInternal(soundId, pitch, volumeMul);
    }

    public void PlayRandomPitched(SoundId soundId, float minPitch = 0.9f, float maxPitch = 1.1f, float volumeMul = 1f)
    {
        float pitch = Random.Range(minPitch, maxPitch);
        PlayInternal(soundId, pitch, volumeMul);
    }

    private bool TryGetSound(SoundId soundID, out SoundEntity soundEntity)
    {
        soundEntity = soundsLibrary.GetSound(soundID);
        if (soundEntity == null)
        {
            Debug.LogWarning("No sounds found with id: " + soundID);
            return false;
        }

        return true;
    }

    private bool TryGetPlayer(AudioType type, out IAudioPlayer player)
    {
        if (audioPlayers.TryGetValue(type, out player))
            return true;

        Debug.LogWarning("No audio player found for type: " + type);
        return false;
    }

    public void StopAll()
    {
        foreach (var player in audioPlayers.Values)
        {
            player.Stop();
        }
    }

    public void Stop(AudioType type)
    {
        if (audioPlayers.TryGetValue(type, out IAudioPlayer player))
        {
            player.Stop();
        }
    }

    public void PlayMusicWithFade(SoundId soundID, float fadeDur = 1f)
    {
        var soundEntity = soundsLibrary.GetSound(soundID);

        if (audioPlayers[soundEntity.Type] is MusicPlayer musicPlayer)
        {
            musicPlayer.PlayWithFade(soundEntity, fadeDur);
        }
    }

    public void SetMusicVolume(float volume)
    {
        MusicVolume = volume;

        SetPlayerVolume(volume, AudioType.Music);
    }

    public void SetAmbientVolume(float volume)
    {
        AmbientVolume = volume;

        SetPlayerVolume(volume, AudioType.Ambient);
    }

    public void SetSFXVolume(float volume)
    {
        SfxVolume = volume;

        SetPlayerVolume(volume, AudioType.SFX);
    }

    private void SetPlayerVolume(float volume, AudioType audioType)
    {
        if (audioPlayers.TryGetValue(audioType, out IAudioPlayer player))
        {
            player.SetVolume(volume);
        }

        UpdateVolumePrefs();
    }

    private void UpdateVolumePrefs()
    {
        PlayerPrefs.SetFloat(MUSIC_VOL_KEY, MusicVolume);
        PlayerPrefs.SetFloat(AMBIENT_VOL_KEY, AmbientVolume);
        PlayerPrefs.SetFloat(SFX_VOL_KEY, SfxVolume);
        PlayerPrefs.Save();
    }

    private void OnApplicationQuit()
    {
        UpdateVolumePrefs();
    }
}