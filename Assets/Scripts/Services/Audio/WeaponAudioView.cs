using UnityEngine;

namespace EmpireAtWar.Services.Audio
{
    public sealed class WeaponAudioView : MonoBehaviour, IWeaponAudioView
    {
        [SerializeField] private AudioSource[] sources;

        public event System.Action Deactivated;
        public int Capacity => sources.Length;

        private void OnDisable()
        {
            if (Deactivated != null) Deactivated.Invoke();
        }

        public void Play(int voice, AudioClip clip, bool loop, float pitch)
        {
            AudioSource source = sources[voice];
            source.Stop();
            source.clip = clip;
            source.loop = loop;
            source.pitch = pitch;
            source.Play();
        }

        public void SetMix(int voice, float volume, float pan)
        {
            sources[voice].volume = volume;
            sources[voice].panStereo = pan;
        }

        public void Stop(int voice) => sources[voice].Stop();

        public void SetPaused(bool paused)
        {
            foreach (AudioSource source in sources)
            {
                if (paused) source.Pause();
                else source.UnPause();
            }
        }
    }
}
