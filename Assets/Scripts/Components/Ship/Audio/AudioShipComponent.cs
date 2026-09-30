using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Audio;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Components.Ship.Audio
{
    public sealed class AudioShipComponent : MonoBehaviour, IAudioShipComponent, IMonoComponent
    {
        private IShipSfxService _audio;
        private ShipSfxData _data;
        private IShipEngineAudioObserver _movement;
        private ILocalPlayer _localPlayer;
        private Transform _viewTransform;
        private IEntity _ship;
        private IReadOnlyList<ShipAbilitySlot> _abilities;
        private ShipAbilityState[] _states;
        private ShipAbilityAudioProfile[] _sounds;
        private readonly ShipEngineAudioState _engine = new ShipEngineAudioState();
        private Vector3 _previousPosition;
        private float _alarmReadyAt;
        private bool _initialized;

        public string Id => nameof(AudioShipComponent);

        [Inject]
        private void Construct(IShipSfxService audio, ShipSfxData data, IShipEngineAudioObserver movement,
            ILocalPlayer localPlayer, [Inject(Id = EntityBindType.ViewTransform)] Transform viewTransform)
        {
            _audio = audio;
            _data = data;
            _movement = movement;
            _localPlayer = localPlayer;
            _viewTransform = viewTransform;
        }

        public void InitializeAudio(IEntity ship, IReadOnlyList<ShipAbilitySlot> abilities)
        {
            _ship = ship;
            _abilities = abilities;
            _states = new ShipAbilityState[abilities.Count];
            _sounds = new ShipAbilityAudioProfile[abilities.Count];
            for (int i = 0; i < abilities.Count; i++)
            {
                _states[i] = abilities[i].State;
                _sounds[i] = _data.GetAbility(abilities[i].Id);
            }
            _previousPosition = _viewTransform.position;
            _initialized = true;
        }

        public void UpdateAudio()
        {
            if (!_initialized || Time.deltaTime <= 0f) return;
            Vector3 position = _viewTransform.position;
            float speed = _movement.Phase == MovementPhase.Moving && _movement.Speed > 0f
                ? Vector3.Distance(position, _previousPosition) / Time.deltaTime / _movement.Speed : 0f;
            _previousPosition = position;
            if (_engine.Advance(speed, Time.deltaTime))
                _audio.TryPlayOneShot(_ship, _data.Acceleration, position);
            _audio.TryHoldLoop(_ship, _data.Engine, position, Mathf.Clamp01(_engine.Speed),
                0.65f + _engine.Speed * 0.45f);
            for (int i = 0; i < _abilities.Count; i++)
                if (_states[i] == ShipAbilityState.Active)
                    _audio.TryHoldLoop(_ship, _sounds[i].Execution, position, 1f, 1f);
        }

        public void HandleAbilityChanged()
        {
            if (!_initialized) return;
            for (int i = 0; i < _states.Length; i++)
            {
                ShipAbilityState state = _abilities[i].State;
                if (state == _states[i]) continue;
                ShipAbilityState previous = _states[i];
                _states[i] = state;
                switch (state)
                {
                    case ShipAbilityState.Active:
                        _audio.TryPlayOneShot(_ship, _sounds[i].Start, _viewTransform.position);
                        break;
                    case ShipAbilityState.Recovering:
                        _audio.TryPlayOneShot(_ship, _sounds[i].End, _viewTransform.position);
                        break;
                    case ShipAbilityState.Ready:
                        if (previous == ShipAbilityState.Recovering && _localPlayer.IsLocal(_ship.Owner))
                            _audio.TryPlayOneShot(_ship, _sounds[i].Restore, _viewTransform.position);
                        break;
                }
            }
        }

        public void PlayWeaponShot(WeaponProfile profile, Transform muzzle)
        {
            if (_initialized) _audio.TryPlayWeaponShot(_ship, profile, muzzle);
        }

        public void HandleEnemyDetected()
        {
            if (Time.time < _alarmReadyAt) return;
            if (_audio.TryPlayOneShot(_ship, _data.Alarm, _viewTransform.position))
                _alarmReadyAt = Time.time + _data.AlarmDelay;
        }

        public void PlayHyperSpace() => _audio.TryPlayOneShot(_ship, _data.Hyperspace, _viewTransform.position);

        public void Release()
        {
            if (!_initialized) return;
            _initialized = false;
            _audio.ReleaseShip(_ship);
        }

        private void OnDisable() => Release();
    }
}
