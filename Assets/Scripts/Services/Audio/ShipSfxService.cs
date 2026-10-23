using System;
using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.Vision;
using EmpireAtWar.Services.Timing;
using UnityEngine;
using UnityEngine.Audio;
using VContainer;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.Services.Audio
{
    [EmpireAtWar.Lifecycle.LifecycleOrder(Tick = EmpireAtWar.Lifecycle.LifecycleOrders.SHIP_SFX_TICK,
        LateTick = EmpireAtWar.Lifecycle.LifecycleOrders.SHIP_SFX_LATE_TICK, Dispose = EmpireAtWar.Lifecycle.LifecycleOrders.SHIP_SFX_DISPOSE)]
    public sealed class ShipSfxService : IShipSfxService, ITickable, ILateTickable, IOrderedDisposable
    {
        private const float MIN_AUDIBLE_GAIN = 0.01f;
        private const float FADE_TIME = 0.04f;
        private const float LOOP_LEASE = 0.12f;
        private const float MAX_WEAPON_LOOP_INTERVAL = 0.2f;
        private const float MAX_MIX_VOLUME = 1f;

        private readonly IAudioService _audioService;
        private readonly ICameraService _cameraService;
        private readonly IVisionService _visionService;
        private readonly ILocalPlayer _localPlayer;
        private readonly IInterfaceSfxService _interfaceSfx;

        private readonly ShipSfxSources _sources;
        private readonly ShipSfxData _data;
        private readonly CameraData _cameraData;
        private readonly AudioMixer _mixer;
        private readonly VoiceSlot[] _slots;
        private readonly Dictionary<SfxProfile, float> _cooldowns = new Dictionary<SfxProfile, float>();
        private readonly Queue<float> _starts = new Queue<float>();

        private float _time;
        private float _voiceReadyAt;

        private int _timeFrame = -1;

        private bool _disposed;
        private bool _mixDirty = true;
        private bool _hasDuckState;
        private bool _voiceDucking;

        public ShipSfxService(IAudioService audioService, ICameraService cameraService, IVisionService visionService,
            ILocalPlayer localPlayer, ShipSfxSources sources, ShipSfxData data, CameraData cameraData,
            IInterfaceSfxService interfaceSfx)
        {
            _sources = sources;
            _data = data;
            _audioService = audioService;
            _cameraService = cameraService;
            _cameraData = cameraData;
            _visionService = visionService;
            _localPlayer = localPlayer;
            _interfaceSfx = interfaceSfx;
            _mixer = sources.Voice.outputAudioMixerGroup.audioMixer;
            _slots = new VoiceSlot[data.PoolVoices];
            sources.Voice.ignoreListenerPause = true;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            for (int i = 0; i < _slots.Length; i++) Stop(i);
            _audioService.Stop(_sources.Voice);
            _mixer.SetFloat("SfxDuckVolume", 0f);
            _audioService.SetGamePaused(false);
        }

        public bool TryPlayOneShot(ShipSfxHandle ship, SfxProfile sfx, Vector3 position) =>
            Request(ship, sfx, position, null, false, 1f, 1f, 0f);

        public bool TryHoldLoop(ShipSfxHandle ship, SfxProfile loop, Vector3 position, float volumeScale, float pitch) =>
            Request(ship, loop, position, null, true, volumeScale, pitch, LOOP_LEASE);

        public bool TryHoldEngine(ShipSfxHandle ship, SfxProfile loop, Vector3 position, float volumeScale, float pitch) =>
            Request(ship, loop, position, null, true, volumeScale, pitch, LOOP_LEASE, true);

        public bool TryPlayWeaponShot(ShipSfxHandle ship, WeaponProfile weapon, Transform muzzle)
        {
            if (_disposed || Time.timeScale == 0f || muzzle == null) return false;
            SfxProfile sound = _data.GetWeapon(weapon.WeaponType, weapon.Family);
            float interval = weapon.ShotInterval / Time.timeScale;
            bool loop = sound.Loop && weapon.ShotsPerSalvo > 1 && interval <= MAX_WEAPON_LOOP_INTERVAL;
            float pitch = UnityEngine.Random.Range(0.96f, 1.04f);
            return Request(ship, sound, muzzle.position, muzzle, loop, 1f, pitch,
                Mathf.Max(0.08f, interval) + FADE_TIME * 2f);
        }

        private bool Request(ShipSfxHandle ship, SfxProfile sound, Vector3 position, Transform emitter,
            bool loop, float scale, float pitch, float lease, bool isEngine = false)
        {
            if (_disposed || ship.IsDestroyed) return false;
            UpdateTime();
            float gain = GetGain(ship, position, isEngine, out float pan);
            for (int i = 0; loop && i < _slots.Length; i++)
            {
                if (_slots[i].Owner != ship || _slots[i].Sound != sound || !_slots[i].Loop) continue;
                if (gain * scale < MIN_AUDIBLE_GAIN) { Stop(i); return false; }
                _slots[i].Position = position;
                _slots[i].Emitter = emitter;
                _slots[i].Gain = gain;
                _slots[i].Pan = pan;
                _slots[i].VolumeScale = scale;
                _slots[i].EndsAt = _time + lease;
                _sources.Sfx[i].pitch = pitch;
                _mixDirty = true;
                return true;
            }
            if (Time.timeScale == 0f || gain * scale < MIN_AUDIBLE_GAIN) return false;
            if (_cooldowns.TryGetValue(sound, out float readyAt) && _time < readyAt) return false;
            while (_starts.Count > 0 && _time - _starts.Peek() >= _data.StartWindow) _starts.Dequeue();
            if (_starts.Count >= _data.StartsPerWindow) return false;
            int slot = FindSlot(ship, sound, gain, isEngine);
            if (slot < 0) return false;
            AudioClip clip = sound.GetClip();
            Stop(slot);
            _slots[slot] = new VoiceSlot
            {
                Owner = ship, Sound = sound, Position = position, Emitter = emitter,
                FollowsEmitter = emitter != null, Loop = loop, IsEngine = isEngine, Gain = gain, Pan = pan,
                VolumeScale = scale, EndsAt = _time + (loop ? lease : clip.length / pitch)
            };
            AudioSource source = _sources.Sfx[slot];
            source.pitch = pitch;
            _audioService.Play(source, clip, 0f, loop);
            _cooldowns[sound] = _time + sound.Cooldown;
            _starts.Enqueue(_time);
            _mixDirty = true;
            return true;
        }

        private int FindSlot(ShipSfxHandle ship, SfxProfile sound, float gain, bool isEngine)
        {
            int ownerCount = 0, soundCount = 0, engineCount = 0, free = -1;
            for (int i = 0; i < _slots.Length; i++)
            {
                VoiceSlot slot = _slots[i];
                if (slot.Sound == null) { free = i; continue; }
                // Engines have their own budget, so a ship's weapons never silence its engine and vice versa.
                if (slot.Owner == ship && !slot.IsEngine) ownerCount++;
                if (slot.Sound == sound) soundCount++;
                if (slot.IsEngine) engineCount++;
            }
            bool ownerCap = !isEngine && ownerCount >= _data.PerShipVoices;
            bool soundCap = soundCount >= sound.MaxInstances;
            bool engineCap = isEngine && engineCount >= _data.EngineVoices;
            if (free >= 0 && !ownerCap && !soundCap && !engineCap) return free;
            int victim = -1;
            float priority = sound.Priority * gain;
            for (int i = 0; i < _slots.Length; i++)
            {
                VoiceSlot slot = _slots[i];
                if (slot.Sound == null || ownerCap && (slot.Owner != ship || slot.IsEngine) ||
                    soundCap && slot.Sound != sound || engineCap && !slot.IsEngine) continue;
                float effective = slot.Sound.Priority * slot.Gain;
                if (effective >= priority) continue;
                victim = i;
                priority = effective;
            }
            return victim;
        }

        public bool TryPlayVoice(AudioClip clip)
        {
            if (_disposed || _interfaceSfx.IsAnnouncing || _sources.Voice.isPlaying || Time.unscaledTime < _voiceReadyAt) return false;
            _audioService.Play(_sources.Voice, clip, _data.VoiceVolume, false);
            _voiceReadyAt = Time.unscaledTime + _data.VoiceCooldown;
            _mixDirty = true;
            return true;
        }

        public void Tick()
        {
            if (_disposed) return;
            bool paused = Time.timeScale == 0f;
            _audioService.SetGamePaused(paused);
            UpdateTime();
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].Sound == null) continue;
                _mixDirty = true;
                if (_slots[i].FollowsEmitter)
                {
                    if (_slots[i].Emitter == null) { Stop(i); continue; }
                    _slots[i].Position = _slots[i].Emitter.position;
                }
                _slots[i].Gain = GetGain(_slots[i].Owner, _slots[i].Position, _slots[i].IsEngine, out _slots[i].Pan);
                if (_time >= _slots[i].EndsAt || _slots[i].Gain * _slots[i].VolumeScale < MIN_AUDIBLE_GAIN)
                    Stop(i);
            }
        }

        public void LateTick()
        {
            if (_disposed) return;
            if (_interfaceSfx.IsAnnouncing && _sources.Voice.isPlaying) _audioService.Stop(_sources.Voice);
            if (!_hasDuckState || (_sources.Voice.isPlaying || _interfaceSfx.IsAnnouncing) != _voiceDucking) _mixDirty = true;
            if (!_mixDirty) return;
            ApplyMix();
            _mixDirty = false;
        }

        private void UpdateTime()
        {
            if (_timeFrame == Time.frameCount) return;
            _timeFrame = Time.frameCount;
            if (Time.timeScale != 0f) _time += Time.unscaledDeltaTime;
        }

        private void ApplyMix()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var scope = BattleProfilerMarkers.SfxApplyMix.Auto();
#endif
            float total = 0f;
            for (int i = 0; i < _slots.Length; i++)
                if (_slots[i].Sound != null) total += GetVolume(_slots[i]);
            float mix = MAX_MIX_VOLUME / Mathf.Max(MAX_MIX_VOLUME, total);
            bool voiceDucking = _sources.Voice.isPlaying || _interfaceSfx.IsAnnouncing;
            if (!_hasDuckState || voiceDucking != _voiceDucking)
            {
                _mixer.SetFloat("SfxDuckVolume", voiceDucking ? 20f * Mathf.Log10(_data.VoiceDuckGain) : 0f);
                _voiceDucking = voiceDucking;
                _hasDuckState = true;
            }
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].Sound == null) continue;
                _sources.Sfx[i].volume = GetVolume(_slots[i]) * mix;
                _sources.Sfx[i].panStereo = _slots[i].Pan;
            }
        }

        private float GetVolume(VoiceSlot slot) => slot.Sound.Volume * slot.VolumeScale * slot.Gain *
            Mathf.Clamp01((slot.EndsAt - _time) / FADE_TIME);

        private float GetGain(ShipSfxHandle ship, Vector3 position, bool isEngine, out float pan)
        {
            Vector3 viewport = _cameraService.WorldToViewportPoint(position);
            pan = Mathf.Clamp((viewport.x - 0.5f) * 1.6f, -0.8f, 0.8f);
            if (viewport.z <= 0f || !_localPlayer.IsFriendly(ship.Owner) &&
                (ship.IsCloaked || !_visionService.IsVisible(_localPlayer.Id, position))) return 0f;
            // Two-argument Max: the params overload allocates an array on every call.
            float outside = Mathf.Max(Mathf.Max(0f, Mathf.Max(-viewport.x, viewport.x - 1f)),
                Mathf.Max(-viewport.y, viewport.y - 1f));
            float edge = Mathf.Clamp01(Vector2.Distance(new Vector2(viewport.x, viewport.y),
                new Vector2(0.5f, 0.5f)) / Mathf.Sqrt(0.5f));
            float gain = Mathf.Lerp(1f, _data.EdgeGain, edge) *
                (1f - Mathf.Clamp01(outside / _data.OffscreenMargin));
            float zoom = Mathf.InverseLerp(_cameraData.ZoomRange.Min, _cameraData.ZoomRange.Max,
                Mathf.Abs(_cameraService.CameraPosition.y - position.y));
            return gain * Mathf.Lerp(1f, isEngine ? _data.EngineMinZoomGain : _data.MinZoomGain, zoom);
        }

        public void ReleaseShip(ShipSfxHandle ship)
        {
            if (_disposed) return;
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].Owner != ship) continue;
                _mixDirty = true;
                _slots[i].EndsAt = Mathf.Min(_slots[i].EndsAt, _time + FADE_TIME);
                _slots[i].FollowsEmitter = false;
                _slots[i].Emitter = null;
            }
        }

        private void Stop(int index)
        {
            _audioService.Stop(_sources.Sfx[index]);
            _slots[index] = default;
            _mixDirty = true;
        }

        private struct VoiceSlot
        {
            public ShipSfxHandle Owner;

            public SfxProfile Sound;
            public Transform Emitter;

            public Vector3 Position;

            public float EndsAt;
            public float Gain;
            public float VolumeScale;
            public float Pan;

            public bool Loop;
            public bool IsEngine;
            public bool FollowsEmitter;
        }
    }
}
