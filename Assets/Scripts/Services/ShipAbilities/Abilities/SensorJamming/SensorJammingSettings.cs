using System;
using EmpireAtWar.Components.Ship.Health;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    [Serializable]
    public sealed class SensorJammingSettings : ShipAbilitySettings
    {
        internal override HardPointType RequiredSubsystem => HardPointType.SensorArray;

        public override IShipAbility CreateAbility(ShipAbilityDependencies dependencies) =>
            new SensorJammingAbility(dependencies.Entities, dependencies.Relations, this);
    }
}
