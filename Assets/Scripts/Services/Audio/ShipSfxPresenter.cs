using System;
using EmpireAtWar.Components.Ship.Audio;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Camera;
using UnityEngine;
using ViewComponents;
using Zenject;

namespace EmpireAtWar.Services.Audio
{
    public sealed class ShipSfxPresenter : IInitializable, ITickable, IDisposable
    {
        // Remembered terrain remains at 0.35 visibility without revealing enemy activity.
        private const float MIN_ENEMY_VISIBILITY = 0.5f;
        private readonly IShipSfxView _view;
        private readonly IShipAbilityFacade _abilities;
        private readonly IShipMovement _movement;
        private readonly ShipMoveModel _movementModel;
        private readonly IShipMoveData _movementData;
        private readonly ICameraService _camera;
        private readonly FogOfWarSystem _fog;
        private readonly ShipSfxData _data;
        private readonly ShipEngineAudioModel _engine = new ShipEngineAudioModel();
        private readonly ShipAbilityState[] _states;
        private readonly ShipAbilityAudioProfile[] _sounds;
        private Vector3 _previousPosition;
        private bool _paused;
        private bool _disposed;

        public ShipSfxPresenter(IShipSfxView view, IShipAbilityFacade abilities,
            IShipMovement movement, ShipMoveModel movementModel, IShipMoveData movementData,
            ICameraService camera, FogOfWarSystem fog, AudioShipData data)
        {
            _view = view;
            _abilities = abilities;
            _movement = movement;
            _movementModel = movementModel;
            _movementData = movementData;
            _camera = camera;
            _fog = fog;
            _data = data.ShipSfx;
            _states = new ShipAbilityState[abilities.Slots.Count];
            _sounds = new ShipAbilityAudioProfile[abilities.Slots.Count];
        }

        public void Initialize()
        {
            _previousPosition = _movement.CurrentPosition;
            for (int i = 0; i < _states.Length; i++)
            {
                _sounds[i] = _data.GetAbility(_abilities.Slots[i].Id);
                _states[i] = _abilities.Slots[i].State;
                _abilities.Slots[i].Changed += OnAbilityChanged;
            }
            _abilities.Health.OnDestroy += Dispose;
            _view.Deactivated += Dispose;
        }

        public void Tick()
        {
            if (_disposed) return;
            bool paused = Time.timeScale == 0f;
            if (_paused != paused)
            {
                _paused = paused;
                _view.SetPaused(paused);
            }
            if (paused) return;
            UpdateMix();
            Vector3 position = _movement.CurrentPosition;
            // Arrival uses a hyperspace teleport/tween, not sublight engine acceleration.
            bool moving = _movementModel.Phase == MovementPhase.Moving;
            float speed = moving && Time.deltaTime > 0f
                ? Vector3.Distance(position, _previousPosition) / Time.deltaTime / _movementData.Speed
                : 0f;
            _previousPosition = position;
            if (_engine.Advance(speed, Time.deltaTime))
                _view.PlayCue(_data.AccelerationClip, _data.AccelerationVolume);
            _view.SetEngine(_data.EngineLoop, _data.EngineVolume * Mathf.Clamp01(_engine.Speed),
                0.65f + _engine.Speed * 0.45f);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            for (int i = 0; i < _states.Length; i++)
                _abilities.Slots[i].Changed -= OnAbilityChanged;
            _abilities.Health.OnDestroy -= Dispose;
            _view.Deactivated -= Dispose;
            _view.StopAll();
        }

        private void OnAbilityChanged()
        {
            if (_disposed || _abilities.Health.IsDestroyed) return;
            UpdateMix();
            for (int i = 0; i < _states.Length; i++)
            {
                ShipAbilityState state = _abilities.Slots[i].State;
                if (state == _states[i]) continue;
                ShipAbilityState previous = _states[i];
                _states[i] = state;
                ShipAbilityAudioProfile sound = _sounds[i];
                switch (state)
                {
                    case ShipAbilityState.Active:
                        _view.PlayCue(sound.StartClip, sound.CueVolume);
                        _view.StartExecution(i, sound.ExecutionLoop, sound.ExecutionVolume);
                        break;
                    case ShipAbilityState.Recovering:
                        _view.StopExecution(i);
                        _view.PlayCue(sound.EndClip, sound.CueVolume);
                        break;
                    case ShipAbilityState.Ready:
                        if (previous == ShipAbilityState.Recovering &&
                            _abilities.Health.PlayerType == PlayerType.Player)
                            _view.PlayCue(sound.RestoreClip, sound.CueVolume * 0.7f);
                        break;
                }
                if (Time.timeScale == 0f)
                {
                    _paused = true;
                    _view.SetPaused(true);
                }
            }
        }

        private void UpdateMix()
        {
            Vector3 position = _movement.CurrentPosition;
            Vector3 viewport = _camera.WorldToViewportPoint(position);
            float gain = 0f;
            if (viewport.z > 0f && (_abilities.Health.PlayerType == PlayerType.Player ||
                !_fog.IsHidden(position, MIN_ENEMY_VISIBILITY)))
            {
                Vector3 cameraPosition = _camera.CameraPosition;
                Vector3 forward = _camera.CameraForward;
                Vector3 focus = cameraPosition + forward * ((position.y - cameraPosition.y) / forward.y);
                gain = 1f - Mathf.InverseLerp(_data.MinDistance, _data.MaxDistance,
                    Vector3.Distance(position, focus));
                gain *= Mathf.Sqrt(Mathf.Min(1f,
                    _data.ZoomReferenceHeight / Mathf.Abs(cameraPosition.y - position.y)));
            }
            _view.SetMix(gain, Mathf.Clamp((viewport.x - 0.5f) * 1.6f, -0.8f, 0.8f));
        }
    }
}
