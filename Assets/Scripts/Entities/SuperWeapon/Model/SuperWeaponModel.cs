using System;
using System.Collections.Generic;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Entities.SuperWeapons
{
    /// <summary>
    /// Superweapon charges of one faction. One purchase is one shot: a weapon charges, becomes ready,
    /// fires once and must be bought again.
    /// </summary>
    public sealed class SuperWeaponModel : PureModel, ISuperWeaponModelObserver
    {
        private readonly Dictionary<SuperWeaponType, SuperWeaponState> _states = new();

        public event Action<SuperWeaponType, SuperWeaponState> OnStateChanged;

        public SuperWeaponState GetState(SuperWeaponType type)
        {
            return _states.TryGetValue(type, out SuperWeaponState state) ? state : SuperWeaponState.Unavailable;
        }

        public bool CanPurchase(SuperWeaponType type) => GetState(type) == SuperWeaponState.Unavailable;

        public void StartCharging(SuperWeaponType type) =>
            Transition(type, SuperWeaponState.Unavailable, SuperWeaponState.Charging);

        public void CancelCharging(SuperWeaponType type) =>
            Transition(type, SuperWeaponState.Charging, SuperWeaponState.Unavailable);

        public void CompleteCharging(SuperWeaponType type) =>
            Transition(type, SuperWeaponState.Charging, SuperWeaponState.Ready);

        public void Consume(SuperWeaponType type) =>
            Transition(type, SuperWeaponState.Ready, SuperWeaponState.Unavailable);

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
