using System;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Audio;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Camera;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Services.Audio
{
    public sealed class WeaponAudioPresenter : ITickable, IDisposable
    {
        private const float FADE_TIME = 0.04f;
        private const float MAX_LOOP_INTERVAL = 0.2f;
        private const float MIN_AUDIBLE_GAIN = 0.01f;
        private const float MAX_MIX_VOLUME = 1f;

        private readonly IWeaponAudioView _view;
        private readonly ICameraService _camera;
        private readonly AudioShipData _data;
        private readonly WeaponAudioBudget _budget;
        private readonly Transform[] _emitters;
        private readonly WeaponAudioProfile[] _sounds;
        private readonly float[] _volumes;
        private readonly float[] _pans;
        private readonly System.Random _random = new System.Random();
        private float _playbackTime;
        private bool _paused;
        private bool _isDisposed;

        public WeaponAudioPresenter(IWeaponAudioView view, ICameraService camera, IAssetService repository)
        {
            _view = view;
            _camera = camera;
            _data = repository.Load<AudioShipData>(nameof(AudioShipData));
            _budget = new WeaponAudioBudget(view.Capacity);
            _emitters = new Transform[view.Capacity];
            _sounds = new WeaponAudioProfile[view.Capacity];
            _volumes = new float[view.Capacity];
            _pans = new float[view.Capacity];
            _view.Deactivated += OnViewDeactivated;
        }

        public void PlayShot(object owner, WeaponProfile weapon, Transform muzzle)
        {
            if (_isDisposed || Time.timeScale == 0f) return;
            float gain = GetGain(muzzle.position, out float pan);
            if (gain < MIN_AUDIBLE_GAIN) return;

            WeaponAudioProfile sound = _data.GetWeaponSound(weapon.WeaponType);
            float shotInterval = weapon.ShotInterval / Time.timeScale;
            bool loop = sound.Loop && weapon.ShotsPerSalvo > 1 && shotInterval <= MAX_LOOP_INTERVAL;
            float pitch = 0.96f + (float)_random.NextDouble() * 0.08f;
            float duration = loop
                ? Mathf.Max(0.08f, shotInterval) + FADE_TIME * 2f
                : sound.Clip.length / pitch;
            int voice = _budget.Request(owner, sound.Clip, loop, duration, _playbackTime, out bool start);
            if (voice < 0) return;

            _emitters[voice] = muzzle;
            _sounds[voice] = sound;
            _volumes[voice] = gain * sound.Volume;
            _pans[voice] = pan;
            ApplyMix();
            if (start)
            {
                _view.Play(voice, sound.Clip, loop, pitch);
            }
        }

        public void Tick()
        {
            if (_isDisposed) return;
            bool paused = Time.timeScale == 0f;
            if (paused != _paused)
            {
                _paused = paused;
                _view.SetPaused(paused);
            }
            if (paused) return;
            _playbackTime += Time.unscaledDeltaTime;

            for (int i = 0; i < _emitters.Length; i++)
            {
                if (_sounds[i] == null) continue;
                if (_emitters[i] == null || _playbackTime >= _budget.EndsAt(i))
                {
                    Stop(i);
                    continue;
                }
                float gain = GetGain(_emitters[i].position, out _pans[i]);
                if (gain < MIN_AUDIBLE_GAIN)
                {
                    Stop(i);
                    continue;
                }
                float fade = Mathf.Clamp01((_budget.EndsAt(i) - _playbackTime) / FADE_TIME);
                _volumes[i] = gain * _sounds[i].Volume * fade;
            }
            ApplyMix();
        }

        public void Release(object owner)
        {
            if (_isDisposed) return;
            for (int i = 0; i < _emitters.Length; i++)
                if (_budget.IsOwnedBy(i, owner)) Stop(i);
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            for (int i = 0; i < _emitters.Length; i++) Stop(i);
            OnViewDeactivated();
        }

        private void OnViewDeactivated()
        {
            // Unity stops the sources when their hierarchy is disabled during scene teardown.
            _isDisposed = true;
            _view.Deactivated -= OnViewDeactivated;
        }

        private void Stop(int voice)
        {
            _view.Stop(voice);
            _budget.Release(voice);
            _emitters[voice] = null;
            _sounds[voice] = null;
            _volumes[voice] = 0f;
        }

        private void ApplyMix()
        {
            float totalVolume = 0f;
            for (int i = 0; i < _volumes.Length; i++) totalVolume += _volumes[i];
            float mixGain = MAX_MIX_VOLUME / Mathf.Max(MAX_MIX_VOLUME, totalVolume);
            for (int i = 0; i < _sounds.Length; i++)
                if (_sounds[i] != null) _view.SetMix(i, _volumes[i] * mixGain, _pans[i]);
        }

        private float GetGain(Vector3 position, out float pan)
        {
            Vector3 viewport = _camera.WorldToViewportPoint(position);
            pan = Mathf.Clamp((viewport.x - 0.5f) * 1.6f, -0.8f, 0.8f);
            if (viewport.z <= 0f) return 0f;

            // Measure from the RTS camera's focus on the firing plane, not its elevated listener.
            Vector3 cameraPosition = _camera.CameraPosition;
            Vector3 forward = _camera.CameraForward;
            Vector3 focus = cameraPosition + forward * ((position.y - cameraPosition.y) / forward.y);
            float distance = Vector3.Distance(position, focus);
            float attenuation = 1f - Mathf.InverseLerp(_data.WeaponMinDistance, _data.WeaponMaxDistance, distance);
            float zoomGain = Mathf.Min(1f, _data.WeaponZoomReferenceHeight / Mathf.Abs(cameraPosition.y - position.y));
            return attenuation * Mathf.Sqrt(zoomGain);
        }
    }
}
