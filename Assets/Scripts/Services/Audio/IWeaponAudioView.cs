using UnityEngine;

namespace EmpireAtWar.Services.Audio
{
    public interface IWeaponAudioView
    {
        event System.Action Deactivated;
        int Capacity { get; }
        void Play(int voice, AudioClip clip, bool loop, float pitch);
        void SetMix(int voice, float volume, float pan);
        void Stop(int voice);
        void SetPaused(bool paused);
    }
}
