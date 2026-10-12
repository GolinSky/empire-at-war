using System;
using Zenject;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    [Serializable]
    public sealed class FighterReinforcementsSettings : ShipAbilitySettings
    {
        public override IShipAbility CreateAbility(IInstantiator instantiator) =>
            instantiator.Instantiate<FighterReinforcementsAbility>();
    }
}
