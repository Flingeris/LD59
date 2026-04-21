using UnityEngine;

public class SFXPlayer : IAudioPlayer
{
    private AudioSystem audioSystem;
    public AudioType Type => AudioType.SFX;
    private SFXSourcesPool sourcePool;
    private float volume = 1.0f;
    private bool isMuted = false;

    public SFXPlayer(AudioSystem audioSystem, int initPoolSize, int maxPoolSize)
    {
        sourcePool = new SFXSourcesPool(audioSystem, initPoolSize, maxPoolSize);
        this.audioSystem = audioSystem;
    }

    public void Play(SoundEntity sound)
    {
        Play(sound, 1f, 1f);
    }

    public void Play(SoundEntity sound, float pitch, float volumeMul = 1f)
    {
        if (isMuted) return;
        if (sound == null) return;

        var clip = sound.GetClip();
        var source = sourcePool.GetAvailableAudioSource();
        if (source == null)
        {
            Debug.LogWarning($"No available SFX AudioSource for '{sound.SoundId}'");
            return;
        }

        source.clip = clip;
        source.volume = audioSystem.SfxVolume * sound.Volume * volumeMul;
        source.pitch = pitch;
        source.Play();

        sourcePool.ReleaseSource(source, clip);
    }

    public void Stop()
    {
        sourcePool.ReleaseAllSources();
    }

    //public void SetMute(bool isMuted)
    //{
    //    this.isMuted = isMuted;

    //    if (isMuted) Stop();
    //}

    public void SetVolume(float volume)
    {
        this.volume = Mathf.Clamp01(volume);

        //if (volume <= 0.0f) SetMute(true);
        //else if (volume > 0 && isMuted) SetMute(false);
    }
}
