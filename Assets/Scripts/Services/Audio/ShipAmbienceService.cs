using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.Vision;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

namespace EmpireAtWar.Services.Audio
{
    [LifecycleOrder(Tick = LifecycleOrders.SHIP_SFX_TICK, Dispose = LifecycleOrders.SHIP_SFX_DISPOSE)]
    public sealed class ShipAmbienceService : IInitializable, ITickable, IOrderedDisposable
    {
        private const float MASK_INTERVAL = 1f;
        private readonly IEntityLocator _locator;
        private readonly ICameraService _camera;
        private readonly IVisionService _vision;
        private readonly ILocalPlayer _player;
        private readonly IAudioService _audio;
        private readonly AudioSource _source;
        private readonly ShipAmbienceData _data;
        private readonly ShipSfxData _sfxData;
        private readonly CameraData _cameraData;
        private readonly List<IEntity> _units = new List<IEntity>();
        private readonly Dictionary<IEntity, bool> _changes = new Dictionary<IEntity, bool>();
        private TransformAccessArray _transforms;
        private NativeArray<float2> _positions;
        private NativeArray<byte> _audible;
        private NativeArray<float> _distance;
        private JobHandle _job;
        private bool _pending;
        private bool _paused;
        private float _nextUpdate;
        private float _nextMask;
        private float _targetGain;
        private float _gain;

        public ShipAmbienceService(IEntityLocator locator, ICameraService camera, IVisionService vision,
            ILocalPlayer player, IAudioService audio, ShipSfxSources sources, ShipAmbienceData data,
            ShipSfxData sfxData, CameraData cameraData)
        {
            _locator = locator;
            _camera = camera;
            _vision = vision;
            _player = player;
            _audio = audio;
            var sourceObject = new GameObject("Ship Ambience");
            sourceObject.transform.SetParent(sources.transform, false);
            _source = sourceObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.outputAudioMixerGroup = sources.Sfx[0].outputAudioMixerGroup;
            _data = data;
            _sfxData = sfxData;
            _cameraData = cameraData;
        }

        public void Initialize()
        {
            _transforms = new TransformAccessArray(16);
            _positions = new NativeArray<float2>(16, Allocator.Persistent);
            _audible = new NativeArray<byte>(16, Allocator.Persistent);
            _distance = new NativeArray<float>(1, Allocator.Persistent);
            _source.spatialBlend = 0f;
            _source.ignoreListenerPause = false;
            _locator.EntityAdded += Add;
            _locator.EntityRemoved += Remove;
            foreach (IEntity entity in _locator.Entities) Add(entity);
        }

        private void Add(IEntity entity)
        {
            if (entity.TryGetFacade<IUnitTypeFacade>(out _)) _changes[entity] = true;
        }

        private void Remove(IEntity entity) => _changes[entity] = false;

        public void Tick()
        {
            bool paused = Time.timeScale == 0f;
            if (paused != _paused)
            {
                if (paused) _audio.Pause(_source);
                else _audio.UnPause(_source);
                _paused = paused;
            }
            if (paused) return;
            if (_pending && _job.IsCompleted)
            {
                _job.Complete();
                _pending = false;
                float proximity = Mathf.InverseLerp(_data.InnerRadius, _data.OuterRadius, Mathf.Sqrt(_distance[0]));
                _targetGain = 1f - Mathf.SmoothStep(0f, 1f, proximity);
            }
            if (!_pending)
            {
                ApplyChanges();
                if (Time.unscaledTime >= _nextMask) RefreshMask();
                if (Time.unscaledTime >= _nextUpdate) Schedule();
            }
            float zoom = Mathf.InverseLerp(_cameraData.ZoomRange.Min, _cameraData.ZoomRange.Max,
                Mathf.Abs(_camera.CameraPosition.y));
            float target = _targetGain * Mathf.Lerp(1f, _sfxData.MinZoomGain, zoom);
            _gain = Mathf.MoveTowards(_gain, target, Time.unscaledDeltaTime / _data.FadeTime);
            if (_gain > 0f && !_source.isPlaying) _audio.Play(_source, _data.GetClip(), 0f, true);
            _source.volume = _gain * _data.Volume;
            if (_gain == 0f && _source.isPlaying) _audio.Stop(_source);
        }

        private void ApplyChanges()
        {
            if (_changes.Count == 0) return;
            foreach (KeyValuePair<IEntity, bool> change in _changes)
            {
                int index = _units.IndexOf(change.Key);
                if (change.Value && index < 0)
                {
                    _units.Add(change.Key);
                    _transforms.Add(change.Key.GetFacade<IEntityTransformFacade>().Transform);
                }
                else if (!change.Value && index >= 0)
                {
                    _transforms.RemoveAtSwapBack(index);
                    _units[index] = _units[_units.Count - 1];
                    _units.RemoveAt(_units.Count - 1);
                }
            }
            _changes.Clear();
            if (_units.Count > _positions.Length)
            {
                _positions.Dispose();
                _audible.Dispose();
                int capacity = Mathf.NextPowerOfTwo(_units.Count);
                _positions = new NativeArray<float2>(capacity, Allocator.Persistent);
                _audible = new NativeArray<byte>(capacity, Allocator.Persistent);
            }
            _nextMask = 0f;
            _nextUpdate = 0f;
            if (_units.Count == 0) _targetGain = 0f;
        }

        private void RefreshMask()
        {
            for (int i = 0; i < _units.Count; i++)
            {
                IEntity unit = _units[i];
                Transform transform = _transforms[i];
                bool audible = transform != null && !unit.HealthModel.IsDestroyed &&
                    (_player.IsFriendly(unit.Owner) ||
                     (!unit.GetFacade<ICombatModifiersFacade>().Modifiers.IsCloaked &&
                      _vision.IsVisible(_player.Id, transform.position)));
                _audible[i] = audible ? (byte)1 : (byte)0;
            }
            _nextMask = Time.unscaledTime + MASK_INTERVAL;
        }

        private void Schedule()
        {
            Vector3 position = _camera.CameraPosition;
            Vector3 forward = _camera.CameraForward;
            Vector3 focus = position + forward * (position.y / Mathf.Max(-forward.y, 0.1f));
            JobHandle positions = new ShipAmbiencePositionJob { Positions = _positions }.Schedule(_transforms);
            _job = new ShipAmbienceDistanceJob
            {
                Positions = _positions, Audible = _audible, Count = _units.Count,
                Focus = new float2(focus.x, focus.z), MinimumDistanceSquared = _distance
            }.Schedule(positions);
            _pending = true;
            _nextUpdate = Time.unscaledTime + _data.UpdateInterval;
        }

        public void Dispose()
        {
            _locator.EntityAdded -= Add;
            _locator.EntityRemoved -= Remove;
            if (_pending) _job.Complete();
            _transforms.Dispose();
            _positions.Dispose();
            _audible.Dispose();
            _distance.Dispose();
            _audio.Stop(_source);
            if (Application.isPlaying) Object.Destroy(_source.gameObject);
            else Object.DestroyImmediate(_source.gameObject);
        }
    }
}
