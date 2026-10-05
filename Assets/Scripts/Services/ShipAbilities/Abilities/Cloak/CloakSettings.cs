using System;
using Zenject;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    [Serializable]
    public sealed class CloakSettings : ShipAbilitySettings
    {
        public override IShipAbility CreateAbility(IInstantiator instantiator) =>
            instantiator.Instantiate<CloakAbility>();
    }
}
