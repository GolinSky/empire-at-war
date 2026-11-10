using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using System;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Services.ShipAbilities;

namespace EmpireAtWar.Entities.Ship.Abilities
{
    public sealed class ShipAbilitySlot
    {
        public event Action Changed;
        public event Action StateChanged;

        public ShipAbilityId Id { get; }
        public ShipAbilityDefinition Definition { get; }
        public IShipAbilityFacade Owner { get; }
        // A restored cooldown can be longer than the recovery delay; the bar measures against the longer one.
        private float _recoveryTotal;

        public ShipAbilityState State { get; private set; } = ShipAbilityState.Ready;
        public float TimeLeft { get; private set; }
        public IShipAbility RunningAbility { get; private set; }
        internal bool IsReplicaState { get; private set; }
        public bool IsAvailable => !Owner.Health.IsDestroyed && !Owner.Modifiers.AbilitiesSuppressed &&
            (Id != ShipAbilityId.IonPulse ||
                HasLiveHardPoint(HardPointType.IonPulseCannon)) &&
            (Id != ShipAbilityId.TractorBeam ||
                HasLiveHardPoint(HardPointType.TractorBeam)) &&
            (Id != ShipAbilityId.DauntlessSensorJamming ||
                HasLiveHardPoint(HardPointType.SensorArray)) &&
            (Id != ShipAbilityId.FighterCoordination ||
                HasLiveHardPoint(HardPointType.CommandArray));
        public bool CanActivate => State == ShipAbilityState.Ready && IsAvailable &&
            (!(Definition.Settings is IShipAbilityReadiness readiness) || readiness.IsReady(Owner));
        public bool CanPress => CanActivate || State == ShipAbilityState.Active && Definition.CanCancel && IsAvailable;
        public float Progress01 => State switch
        {
            ShipAbilityState.Active => Definition.Duration > 0f ? TimeLeft / Definition.Duration : 0f,
            ShipAbilityState.Recovering => _recoveryTotal > 0f ? TimeLeft / _recoveryTotal : 0f,
            _ => 0f
        };

        public ShipAbilitySlot(IShipAbilityFacade owner, ShipAbilityDefinition definition,
            ShipAbilityId id)
        {
            Id = id;
            Definition = definition;
            Owner = owner;
            _recoveryTotal = definition.RecoveryDelay;
        }

        private bool HasLiveHardPoint(HardPointType type)
        {
            foreach (IReadOnlyHardPointModel point in Owner.Health.HardPointModels)
                if (point.HardPointType == type && !point.IsDestroyed) return true;
            return false;
        }

        internal void Activate(IShipAbility ability)
        {
            IsReplicaState = false;
            RunningAbility = ability;
            State = ShipAbilityState.Active;
            TimeLeft = Definition.Duration;
            Changed?.Invoke();
            StateChanged?.Invoke();
        }

        internal void Recover()
        {
            IsReplicaState = false;
            RunningAbility = null;
            State = ShipAbilityState.Recovering;
            TimeLeft = Definition.RecoveryDelay;
            _recoveryTotal = Definition.RecoveryDelay;
            Changed?.Invoke();
            StateChanged?.Invoke();
        }

        /// <summary>Loads a saved Ready or Recovering state; active abilities are saved as recovering.</summary>
        internal void Restore(ShipAbilityState state, float timeLeft)
        {
            if (state == ShipAbilityState.Active)
                throw new ArgumentException("Active abilities are restored as recovering.", nameof(state));
            IsReplicaState = false;
            RunningAbility = null;
            State = state;
            TimeLeft = state == ShipAbilityState.Recovering ? timeLeft : 0f;
            _recoveryTotal = Math.Max(Definition.RecoveryDelay, TimeLeft);
            Changed?.Invoke();
            StateChanged?.Invoke();
        }

        // No activation voice/event for a historical baseline. A transient phase has state but no restarted effect.
        internal void RestoreReplicaState(ShipAbilityState state, float timeLeft, IShipAbility runningAbility)
        {
            IsReplicaState = true;
            RunningAbility = runningAbility;
            State = state;
            TimeLeft = state == ShipAbilityState.Ready ? 0f : timeLeft;
            _recoveryTotal = Math.Max(Definition.RecoveryDelay, TimeLeft);
            Changed?.Invoke();
        }

        internal void Ready()
        {
            State = ShipAbilityState.Ready;
            TimeLeft = 0f;
            Changed?.Invoke();
            StateChanged?.Invoke();
        }

        internal void Elapse(float deltaTime)
        {
            TimeLeft = Math.Max(0f, TimeLeft - deltaTime);
            Changed?.Invoke();
        }
    }
}
