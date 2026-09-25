using System;
using UnityEngine;

namespace EmpireAtWar.Services.Audio
{
    public sealed class ShipSfxView : MonoBehaviour, IShipSfxView
    {
        [SerializeField] private AudioSource cueSource;
        [SerializeField] private AudioSource engineSource;
        [SerializeField] private AudioSource[] executionSources;
        private float[] _executionVolumes;
        private float _gain;

        public event Action Deactivated;

        private void Awake() => _executionVolumes = new float[executionSources.Length];

        private void OnDisable()
        {
            StopAll();
            if (Deactivated != null) Deactivated.Invoke();
        }

        public void PlayCue(AudioClip clip, float volume)
        {
            if (_gain > 0.01f) cueSource.PlayOneShot(clip, volume);
        }

        public void StartExecution(int slot, AudioClip clip, float volume)
        {
            AudioSource source = executionSources[slot];
            _executionVolumes[slot] = volume;
            source.clip = clip;
            source.volume = volume * _gain;
            source.Play();
        }

        public void StopExecution(int slot) => executionSources[slot].Stop();

        public void SetEngine(AudioClip clip, float volume, float pitch)
        {
            engineSource.volume = volume * _gain;
            engineSource.pitch = pitch;
            if (engineSource.volume <= 0.002f)
            {
                engineSource.Stop();
                return;
            }
            if (engineSource.isPlaying) return;
            engineSource.clip = clip;
            engineSource.Play();
        }

        public void SetMix(float gain, float pan)
        {
            _gain = gain;
            cueSource.volume = gain;
            cueSource.panStereo = pan;
            engineSource.panStereo = pan;
            for (int i = 0; i < executionSources.Length; i++)
            {
                executionSources[i].volume = gain * _executionVolumes[i];
                executionSources[i].panStereo = pan;
            }
        }

        public void SetPaused(bool paused)
        {
            PauseSource(cueSource, paused);
            PauseSource(engineSource, paused);
            foreach (AudioSource source in executionSources) PauseSource(source, paused);
        }

        public void StopAll()
        {
            cueSource.Stop();
            engineSource.Stop();
            foreach (AudioSource source in executionSources) source.Stop();
        }

        private static void PauseSource(AudioSource source, bool paused)
        {
            if (paused) source.Pause();
            else source.UnPause();
        }
    }
}
