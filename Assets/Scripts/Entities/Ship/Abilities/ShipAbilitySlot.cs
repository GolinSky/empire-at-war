using System;
using EmpireAtWar.Services.ShipAbilities;

namespace EmpireAtWar.Entities.Ship.Abilities
{
    public sealed class ShipAbilitySlot
    {
        public event Action Changed;

        public ShipAbilityId Id { get; }
        public ShipAbilityDefinition Definition { get; }
        public IShipAbilityFacade Owner { get; }
        public ShipAbilityState State { get; private set; } = ShipAbilityState.Ready;
        public float TimeLeft { get; private set; }
        public IShipAbility RunningAbility { get; private set; }
        public float Progress01 => State switch
        {
            ShipAbilityState.Active => Definition.Duration > 0f ? TimeLeft / Definition.Duration : 0f,
            ShipAbilityState.Recovering => Definition.RecoveryDelay > 0f ? TimeLeft / Definition.RecoveryDelay : 0f,
            _ => 0f
        };

        public ShipAbilitySlot(ShipAbilityId id, ShipAbilityDefinition definition,
            IShipAbilityFacade owner)
        {
            Id = id;
            Definition = definition;
            Owner = owner;
        }

        internal void Activate(IShipAbility ability)
        {
            RunningAbility = ability;
            State = ShipAbilityState.Active;
            TimeLeft = Definition.Duration;
            Changed?.Invoke();
        }

        internal void Recover()
        {
            RunningAbility = null;
            State = ShipAbilityState.Recovering;
            TimeLeft = Definition.RecoveryDelay;
            Changed?.Invoke();
        }

        internal void Ready()
        {
            State = ShipAbilityState.Ready;
            TimeLeft = 0f;
            Changed?.Invoke();
        }

        internal void Elapse(float deltaTime)
        {
            TimeLeft = Math.Max(0f, TimeLeft - deltaTime);
            Changed?.Invoke();
        }
    }
}
