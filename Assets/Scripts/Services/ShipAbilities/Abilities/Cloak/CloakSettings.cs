using System;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    [Serializable]
    public sealed class CloakSettings : ShipAbilitySettings
    {
        public override IShipAbility CreateAbility(ShipAbilityDependencies dependencies) =>
            new CloakAbility();
    }
}
