using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SFXSourcesPool
{
    private GameObject audioObject;
    private AudioSystem audioSystem;
    private int initialPoolSize = 10;
    private int maxPoolSize = 24;

    private List<AudioSource> SFXSources = new List<AudioSource>();

    public SFXSourcesPool(AudioSystem audioSystem, int initSize, int maxSize)
    {
        initialPoolSize = initSize;
        maxPoolSize = maxSize;
        audioObject = audioSystem.gameObject;
        this.audioSystem = audioSystem;
        Init();
    }

    private void Init()
    {
        for (int i = 0; i < initialPoolSize; i++)
        {
            var sfxSouce = audioObject.AddComponent<AudioSource>();
            sfxSouce.playOnAwake = false;
            SFXSources.Add(sfxSouce);
        }
    }

    public AudioSource GetAvailableAudioSource()
    {
        foreach (var src in SFXSources)
        {
            if (!src.isPlaying || src.clip == null) return src;
        }
        if (AddAudioSource())
        {
            return SFXSources[^1];
        }

        return null;
    }

    private bool AddAudioSource()
    {
        if (SFXSources.Count > maxPoolSize)
        {
            Debug.LogWarning("Пул SFX перегружен");
            return false;
        }

        var newSource = audioObject.AddComponent<AudioSource>();
        newSource.playOnAwake = false;
        SFXSources.Add(newSource);
        return true;
    }

    public void ReleaseAllSources()
    {
        foreach (var src in SFXSources)
            src.Stop();
    }

    public void ReleaseSource(AudioSource source, AudioClip initialClip)
    {
        audioSystem.StartCoroutine(ReleaseRoutine(source, initialClip));
    }

    private IEnumerator ReleaseRoutine(AudioSource source, AudioClip initialClip)
    {
        yield return new WaitForSeconds(initialClip.length);

        if (source.clip != initialClip) yield break;
        source.Stop();
        source.clip = null;
        source.loop = false;
    }
}