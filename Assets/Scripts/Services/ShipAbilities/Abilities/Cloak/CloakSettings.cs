using System;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    [Serializable]
    public sealed class CloakSettings : ShipAbilitySettings
    {
        internal override bool CanActivateWhileCloaked => true;

        public override IShipAbility CreateAbility(ShipAbilityDependencies dependencies) =>
            new CloakAbility();
    }
}
