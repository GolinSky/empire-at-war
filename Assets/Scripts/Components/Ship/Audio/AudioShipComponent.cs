using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Extensions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Audio;
using UnityEngine;
using VContainer;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.Components.Ship.Audio
{
    public sealed class AudioShipComponent : MonoBehaviour, IAudioShipComponent, IMonoComponent
    {
        private IShipSfxService _shipSfxService;
        private IShipEngineAudioObserver _shipEngineAudioObserver;
        private ILocalPlayer _localPlayer;
        private ShipSfxHandle _ship;
        private IReadOnlyList<ShipAbilitySlot> _abilities;

        private ShipSfxData _data;
        private ShipUnitAudioProfile _unitAudio;
        private readonly ShipShieldAudioState _shields = new ShipShieldAudioState();
        private float _hardPointReadyAt;
        private Transform _viewTransform;
        private ShipAbilityState[] _states;
        private ShipAbilityAudioProfile[] _sounds;
        private readonly ShipEngineAudioState _engine = new ShipEngineAudioState();

        private Vector3 _previousPosition;

        private float _alarmReadyAt;

        private bool _initialized;

        public string Id => nameof(AudioShipComponent);

        [Inject]
        private void Construct(IShipSfxService shipSfxService, IShipEngineAudioObserver shipEngineAudioObserver, ILocalPlayer localPlayer,
            ShipSfxData data, ShipType shipType, ShipSfxHandle ship, [Key(EntityBindType.ViewTransform)] Transform viewTransform)
        {
            _shipSfxService = shipSfxService;
            _data = data;
            _unitAudio = data.GetUnit(shipType);
            _shipEngineAudioObserver = shipEngineAudioObserver;
            _localPlayer = localPlayer;
            _viewTransform = viewTransform;
            _ship = ship;
        }

        public void InitializeAudio(IReadOnlyList<ShipAbilitySlot> abilities)
        {
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
            float speed = _shipEngineAudioObserver.Phase == MovementPhase.Moving && _shipEngineAudioObserver.Speed > 0f
                ? Vector3.Distance(position, _previousPosition) / Time.deltaTime / _shipEngineAudioObserver.Speed : 0f;
            _previousPosition = position;
            if (_engine.Advance(speed, Time.deltaTime))
                _shipSfxService.TryPlayOneShot(_ship, _data.Acceleration, position);
            if (speed > 0f)
                _shipSfxService.TryHoldEngine(_ship, _unitAudio.Engine, position, Mathf.Clamp01(_engine.Speed),
                    0.65f + _engine.Speed * 0.45f);
            for (int i = 0; i < _abilities.Count; i++)
                if (_states[i] == ShipAbilityState.Active)
                    _shipSfxService.TryHoldLoop(_ship, _sounds[i].Execution, position, 1f, 1f);
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
                        _shipSfxService.TryPlayOneShot(_ship, _sounds[i].Start, _viewTransform.position);
                        if (previous == ShipAbilityState.Ready)
                            _shipSfxService.TryPlayOneShot(_ship, _data.AbilityActivated, _viewTransform.position);
                        break;
                    case ShipAbilityState.Recovering:
                        _shipSfxService.TryPlayOneShot(_ship, _sounds[i].End, _viewTransform.position);
                        if (previous == ShipAbilityState.Active)
                            _shipSfxService.TryPlayOneShot(_ship, _data.AbilityCanceled, _viewTransform.position);
                        break;
                    case ShipAbilityState.Ready:
                        if (previous == ShipAbilityState.Recovering && _localPlayer.IsLocal(_ship.Owner))
                            _shipSfxService.TryPlayOneShot(_ship, _sounds[i].Restore, _viewTransform.position);
                        break;
                }
            }
        }

        public void PlayWeaponShot(WeaponProfile profile, Transform muzzle)
        {
            if (_initialized) _shipSfxService.TryPlayWeaponShot(_ship, profile, muzzle);
        }

        public void HandleEnemyDetected()
        {
            if (Time.time < _alarmReadyAt) return;
            if (_shipSfxService.TryPlayOneShot(_ship, _unitAudio.Alarm ?? _data.Alarm, _viewTransform.position))
                _alarmReadyAt = Time.time + _data.AlarmDelay;
        }

        public void ResetShields(IHealthModelObserver health) => _shields.Reset(health.Shields);

        public void HandleHealthChanged(IHealthModelObserver health)
        {
            if (!_initialized) return;
            int transition = _shields.Advance(health.Shields, health.ShieldPercentage, Time.time, _data.ShieldUpThreshold, _data.ShieldCueCooldown);
            if (transition != 0)
                _shipSfxService.TryPlayOneShot(_ship, transition < 0 ? _data.ShieldsDown : _data.ShieldsUp,
                    _viewTransform.position);
        }

        public void PlayShieldImpact(Vector3 position)
        {
            if (_initialized) _shipSfxService.TryPlayOneShot(_ship, _data.ShieldImpact, position);
        }

        public void PlayHardPointLost()
        {
            if (!_initialized || Time.time < _hardPointReadyAt) return;
            if (_shipSfxService.TryPlayOneShot(_ship, _unitAudio.Explosion, _viewTransform.position))
                _hardPointReadyAt = Time.time + 1f;
        }

        public void PlayHyperSpace() => _shipSfxService.TryPlayOneShot(_ship, _data.Hyperspace, _viewTransform.position);

        public void Release()
        {
            if (!_initialized) return;
            _initialized = false;
            _shipSfxService.ReleaseShip(_ship);
        }

        private void OnDisable() => Release();
    }
}
