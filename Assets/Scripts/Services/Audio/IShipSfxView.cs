using System;
using UnityEngine;

namespace EmpireAtWar.Services.Audio
{
    public interface IShipSfxView
    {
        event Action Deactivated;
        void PlayCue(AudioClip clip, float volume);
        void StartExecution(int slot, AudioClip clip, float volume);
        void StopExecution(int slot);
        void SetEngine(AudioClip clip, float volume, float pitch);
        void SetMix(float gain, float pan);
        void SetPaused(bool paused);
        void StopAll();
    }
}
