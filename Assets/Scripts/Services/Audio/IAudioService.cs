using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Services.Audio
{
    public interface IAudioService : IService
    {
        void Play(AudioSource source, AudioClip clip, float volume, bool loop);
        void PlayOneShot(AudioSource source, AudioClip clip, float volume);
        void Stop(AudioSource source);
        void Pause(AudioSource source);
        void UnPause(AudioSource source);
        void SetGamePaused(bool paused);
    }
}
