using System;
using UnityEngine;

namespace EmpireAtWar.Services.Settings
{
    /// <summary>Player volumes as 0–1 gain on top of the authored mixer levels; 1 keeps the authored mix.</summary>
    [Serializable]
    public sealed class AudioSettingsData
    {
        [SerializeField] private float masterVolume = 1f;
        [SerializeField] private float musicVolume = 1f;
        [SerializeField] private float voiceVolume = 1f;
        [SerializeField] private float sfxVolume = 1f;

        [SerializeField] private bool muteWhenUnfocused = true;

        public float MasterVolume
        {
            get => masterVolume;
            set => masterVolume = ClampVolume(value);
        }

        public float MusicVolume
        {
            get => musicVolume;
            set => musicVolume = ClampVolume(value);
        }

        public float VoiceVolume
        {
            get => voiceVolume;
            set => voiceVolume = ClampVolume(value);
        }

        public float SfxVolume
        {
            get => sfxVolume;
            set => sfxVolume = ClampVolume(value);
        }

        public bool MuteWhenUnfocused
        {
            get => muteWhenUnfocused;
            set => muteWhenUnfocused = value;
        }

        public bool Matches(AudioSettingsData other)
        {
            return masterVolume == other.masterVolume && musicVolume == other.musicVolume &&
                   voiceVolume == other.voiceVolume && sfxVolume == other.sfxVolume &&
                   muteWhenUnfocused == other.muteWhenUnfocused;
        }

        public void Sanitize()
        {
            masterVolume = ClampVolume(masterVolume);
            musicVolume = ClampVolume(musicVolume);
            voiceVolume = ClampVolume(voiceVolume);
            sfxVolume = ClampVolume(sfxVolume);
        }

        private static float ClampVolume(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return 1f;
            }

            return Mathf.Clamp01(value);
        }
    }
}
