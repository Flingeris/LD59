using System.Collections;
using UnityEngine;

public class MusicPlayer : IAudioPlayer
{
    public AudioType Type => AudioType.Music;
    private AudioSystem audioSystem;

    private AudioSource sourceA;
    private AudioSource sourceB;
    private bool isAActive = false;
    private bool isMuted = false;
    private float volume = 1.0f;

    private SoundEntity currentSound;

    private AudioSource GetCurrentSource() => isAActive ? sourceA : sourceB;

    private AudioSource GetInactiveSource() => isAActive ? sourceB : sourceA;

    private Coroutine fadeRoutine;

    public MusicPlayer(AudioSystem audioSystem)
    {
        this.audioSystem = audioSystem;
        Init();
    }

    private void Init()
    {
        var audioGO = audioSystem.gameObject;

        sourceA = AddMusicSource(audioGO);
        sourceB = AddMusicSource(audioGO);
    }

    private AudioSource AddMusicSource(GameObject gameObject)
    {
        var source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;

        return source;
    }

    public void Play(SoundEntity sound)
    {
        PlayWithFade(sound);
    }

    public void PlayWithFade(SoundEntity sound, float fadeDuration = 0f)
    {
        if (sourceA == null || sourceB == null) return;
        if (sound == null) return;

        var currentSource = GetCurrentSource();
        var nextSource = GetInactiveSource();

        currentSound = sound;

        nextSource.clip = sound.GetClip();
        float targetVolume = audioSystem.MusicVolume * sound.Volume;

        if (fadeDuration <= 0f)
        {
            currentSource.Stop();
            nextSource.volume = targetVolume;
            nextSource.Play();
        }
        else
        {
            nextSource.volume = 0f;
            if (!isMuted) nextSource.Play();
            StartFadeRoutine(currentSource, nextSource, targetVolume, fadeDuration);
        }

        isAActive = !isAActive;
    }

    private void StartFadeRoutine(AudioSource from, AudioSource to, float targetVolume, float duration)
    {
        if (fadeRoutine != null) audioSystem.StopCoroutine(fadeRoutine);

        fadeRoutine = audioSystem.StartCoroutine(FadeRoutine(from, to, targetVolume, duration));
    }

    private IEnumerator FadeRoutine(AudioSource from, AudioSource to, float targetVolume, float duration)
    {
        float time = 0f;
        float fromStartVolume = from.volume;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            from.volume = Mathf.Lerp(fromStartVolume, 0f, t);
            to.volume = Mathf.Lerp(0, targetVolume, t);
            yield return null;
        }

        from.Stop();
        from.volume = 0f;
        to.volume = targetVolume;
    }

    public void Stop()
    {
        if (sourceA.isPlaying) sourceA.Stop();
        if (sourceB.isPlaying) sourceB.Stop();

        if (fadeRoutine != null)
        {
            audioSystem.StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }
    }

    //public void SetMute(bool isMuted)
    //{
    //    if (sourceA == null || sourceB == null) return;
    //    this.isMuted = isMuted;

    //    if (isMuted && GetCurrentSource().isPlaying) GetCurrentSource().Stop();
    //    else if (currentSound != null) Play(currentSound);
    //}

    public void SetVolume(float volume)
    {
        this.volume = Mathf.Clamp01(volume);

        //if (volume <= 0.0f) SetMute(true);
        //else if (volume > 0 && isMuted) SetMute(false);

        if (GetCurrentSource().isPlaying && GetCurrentSource().clip != null)
        {
            GetCurrentSource().volume = volume * currentSound.Volume;
        }
    }
}