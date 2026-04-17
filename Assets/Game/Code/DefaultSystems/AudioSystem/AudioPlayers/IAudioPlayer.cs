public interface IAudioPlayer
{
    AudioType Type { get; }

    void Play(SoundEntity sound);

    void Stop();

    //void SetMute(bool isMuted);

    void SetVolume(float volume);
}