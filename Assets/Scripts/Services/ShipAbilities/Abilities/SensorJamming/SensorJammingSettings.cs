using System;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    [Serializable]
    public sealed class SensorJammingSettings : ShipAbilitySettings
    {
        public override IShipAbility CreateAbility(ShipAbilityDependencies dependencies) =>
            new SensorJammingAbility(dependencies.Entities, dependencies.Relations);
    }
}
