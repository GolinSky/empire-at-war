using System;
using System.Collections.Generic;
using EmpireAtWar.Services.Settings;
using UnityEngine;
using UnityEngine.Audio;
using Zenject;

namespace EmpireAtWar.Services.Audio
{
    /// <summary>
    /// Drives the exposed group volumes of the game <see cref="AudioMixer"/>. Player gain is added to each
    /// authored level, so full volume keeps the authored mix. <c>SfxDuckVolume</c> stays owned by ship SFX.
    /// </summary>
    public sealed class AudioSettingsApplier : ISettingsApplier, IAudioSettingsPreview, IInitializable, IDisposable
    {
        private const string MASTER_VOLUME_PARAMETER = "MasterVolume";
        private const string MUSIC_VOLUME_PARAMETER = "MusicVolume";
        private const string VOICE_VOLUME_PARAMETER = "VoiceVolume";
        private const string SFX_VOLUME_PARAMETER = "SfxVolume";

        private const float SILENT_DECIBELS = -80f;
        private const float MIN_AUDIBLE_VOLUME = 0.0001f;

        private readonly AudioMixer _mixer;
        private readonly Dictionary<string, float> _authoredDecibels = new Dictionary<string, float>();
        private AudioSettingsData _active = new AudioSettingsData();

        public AudioSettingsApplier(AudioMixer mixer)
        {
            _mixer = mixer;
        }

        public void Initialize()
        {
            Application.focusChanged += HandleFocusChanged;
        }

        public void Dispose()
        {
            Application.focusChanged -= HandleFocusChanged;
        }

        public void Apply(SettingsData settings)
        {
            Preview(settings.Audio);
        }

        public void Preview(AudioSettingsData audio)
        {
            _active = audio;
            SetVolumes();
        }

        private void HandleFocusChanged(bool hasFocus)
        {
            SetVolumes();
        }

        // Unfocused muting is temporary: it silences the master bus without touching the stored volume.
        private void SetVolumes()
        {
            bool isMuted = _active.MuteWhenUnfocused && !Application.isFocused;
            SetVolume(MASTER_VOLUME_PARAMETER, isMuted ? 0f : _active.MasterVolume);
            SetVolume(MUSIC_VOLUME_PARAMETER, _active.MusicVolume);
            SetVolume(VOICE_VOLUME_PARAMETER, _active.VoiceVolume);
            SetVolume(SFX_VOLUME_PARAMETER, _active.SfxVolume);
        }

        private void SetVolume(string parameter, float volume)
        {
            // The first read happens before any write, so it returns the level authored in the mixer snapshot.
            if (!_authoredDecibels.TryGetValue(parameter, out float authored))
            {
                if (!_mixer.GetFloat(parameter, out authored))
                {
                    throw new InvalidOperationException($"AudioMixer '{_mixer.name}' does not expose '{parameter}'.");
                }

                _authoredDecibels.Add(parameter, authored);
            }

            float decibels = volume < MIN_AUDIBLE_VOLUME ? SILENT_DECIBELS : authored + 20f * Mathf.Log10(volume);
            _mixer.SetFloat(parameter, decibels);
        }
    }
}
