using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Services.Audio
{
    public sealed class AudioService : Service, IAudioService
    {
        public void Play(AudioSource source, AudioClip clip, float volume, bool loop)
        {
            source.clip = clip;
            source.volume = volume;
            source.loop = loop;
            source.Play();
        }

        public void PlayOneShot(AudioSource source, AudioClip clip, float volume) => source.PlayOneShot(clip, volume);

        public void Stop(AudioSource source) => source.Stop();

        public void Pause(AudioSource source) => source.Pause();

        public void UnPause(AudioSource source) => source.UnPause();

        public void SetGamePaused(bool paused) => AudioListener.pause = paused;
    }
}
