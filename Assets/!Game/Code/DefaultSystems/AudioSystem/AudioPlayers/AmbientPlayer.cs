using UnityEngine;

public class AmbientPlayer : IAudioPlayer
{
    public AudioType Type => AudioType.Ambient;
    private AudioSystem audioSystem;
    private AudioSource audioSource;
    private float volume = 1.0f;
    private SoundEntity currentSound;
    private bool isMuted = false;

    public AmbientPlayer(AudioSystem audioSystem)
    {
        this.audioSystem = audioSystem;
        Init();
    }

    private void Init()
    {
        audioSource = audioSystem.gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    public void Play(SoundEntity sound)
    {
        if (sound == null) return;
        if (audioSource == null) return;

        currentSound = sound;
        var clip = sound.GetClip();

        audioSource.clip = clip;
        audioSource.loop = true;
        audioSource.volume = volume * sound.Volume;
        if (!isMuted) audioSource.Play();
    }

    public void Stop()
    {
        audioSource.Stop();
        audioSource.clip = null;
        currentSound = null;
    }

    //public void SetMute(bool isMuted)
    //{
    //    if (audioSource == null) return;
    //    this.isMuted = isMuted;

    //    if (isMuted && audioSource.isPlaying) audioSource.Stop();
    //    else if (currentSound != null) Play(currentSound);
    //}

    public void SetVolume(float volume)
    {
        this.volume = Mathf.Clamp01(volume);

        //if (volume <= 0.0f) SetMute(true);
        //else if (volume > 0 && isMuted) SetMute(false);

        if (audioSource.clip != null && audioSource.isPlaying)
        {
            audioSource.volume = volume * currentSound.Volume;
        }
    }
}