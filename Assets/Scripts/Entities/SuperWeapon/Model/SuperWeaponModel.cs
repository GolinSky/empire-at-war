using System;
using System.Collections.Generic;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Entities.SuperWeapons
{
    /// <summary>
    /// Superweapon charges of one faction. One purchase is one shot: a weapon charges, becomes ready,
    /// fires once and must be bought again. After firing, that weapon type cannot fire again for
    /// <see cref="COOLDOWN_DURATION"/> seconds, but a new charge can be built meanwhile.
    /// </summary>
    public sealed class SuperWeaponModel : PureModel, ISuperWeaponModelObserver
    {
        public const float COOLDOWN_DURATION = 120f;

        private readonly Dictionary<SuperWeaponType, SuperWeaponState> _states = new();
        private readonly Dictionary<SuperWeaponType, float> _cooldownLeft = new();
        private readonly List<SuperWeaponType> _cooling = new();

        public event Action<SuperWeaponType, SuperWeaponState> OnStateChanged;

        public SuperWeaponState GetState(SuperWeaponType type)
        {
            return _states.TryGetValue(type, out SuperWeaponState state) ? state : SuperWeaponState.Unavailable;
        }

        public float GetCooldownRemaining(SuperWeaponType type)
        {
            return _cooldownLeft.TryGetValue(type, out float timeLeft) ? timeLeft : 0f;
        }

        public bool CanPurchase(SuperWeaponType type) => GetState(type) == SuperWeaponState.Unavailable;

        public bool CanFire(SuperWeaponType type) =>
            GetState(type) == SuperWeaponState.Ready && !_cooldownLeft.ContainsKey(type);

        public void StartCharging(SuperWeaponType type) =>
            Transition(type, SuperWeaponState.Unavailable, SuperWeaponState.Charging);

        public void CancelCharging(SuperWeaponType type) =>
            Transition(type, SuperWeaponState.Charging, SuperWeaponState.Unavailable);

        public void CompleteCharging(SuperWeaponType type) =>
            Transition(type, SuperWeaponState.Charging, SuperWeaponState.Ready);

        public void Consume(SuperWeaponType type)
        {
            if (_cooldownLeft.ContainsKey(type))
            {
                throw new InvalidOperationException($"{type} is still cooling down.");
            }

            Transition(type, SuperWeaponState.Ready, SuperWeaponState.Unavailable);
            _cooldownLeft[type] = COOLDOWN_DURATION;
        }

        public void Tick(float deltaTime)
        {
            _cooling.Clear();
            _cooling.AddRange(_cooldownLeft.Keys);
            foreach (SuperWeaponType type in _cooling)
            {
                float timeLeft = _cooldownLeft[type] - deltaTime;
                if (timeLeft > 0f)
                {
                    _cooldownLeft[type] = timeLeft;
                    continue;
                }

                _cooldownLeft.Remove(type);
                // Re-announce the unchanged state so a charge that waited out the cooldown becomes fireable.
                OnStateChanged?.Invoke(type, GetState(type));
            }
        }

        private void Transition(SuperWeaponType type, SuperWeaponState from, SuperWeaponState to)
        {
            SuperWeaponState current = GetState(type);
            if (current != from)
            {
                throw new InvalidOperationException($"{type} cannot move from {current} to {to}.");
            }

            _states[type] = to;
            OnStateChanged?.Invoke(type, to);
        }
    }
}
