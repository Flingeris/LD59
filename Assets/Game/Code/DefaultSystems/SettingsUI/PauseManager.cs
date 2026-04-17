using UnityEngine;
using UnityEngine.UI;

public class PauseManager : MonoBehaviour
{
    [SerializeField] private Slider slider_SFX;
    [SerializeField] private Slider slider_Music;
    [SerializeField] private GameObject pausePanel;
    public bool IsOpen { get; private set; } = false;

    private void Start()
    {
        if (G.audioSystem == null) return;
        Init();
    }

    private void Init()
    {
        slider_Music.value = G.audioSystem.MusicVolume;
        slider_SFX.value = G.audioSystem.SfxVolume;
        SetToggle(false);
    }

    public void SetMusicVolume(float volume)
    {
        if (G.audioSystem == null) return;
        G.audioSystem.SetMusicVolume(volume);
        G.audioSystem.SetAmbientVolume(volume);
    }

    public void SetSFXVolume(float volume)
    {
        if (G.audioSystem == null) return;
        G.audioSystem.SetSFXVolume(volume);
    }

    public void SetToggle(bool toggle)
    {
        IsOpen = toggle;
        pausePanel.SetActive(IsOpen);
        //if (IsOpen)
        //{
        //    Time.timeScale = 0f;
        //}
        //else
        //{
        //    Time.timeScale = 1f;
        //}
    }

    public void Toggle()
    {
        bool b = !IsOpen;
        SetToggle(b);
    }
}