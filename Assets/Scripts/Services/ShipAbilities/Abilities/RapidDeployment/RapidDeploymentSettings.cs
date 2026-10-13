using System;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    [Serializable]
    public sealed class RapidDeploymentSettings : ShipAbilitySettings
    {
        public override IShipAbility CreateAbility(ShipAbilityDependencies dependencies) =>
            new RapidDeploymentAbility();
    }
}
