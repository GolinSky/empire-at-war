using System;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    [Serializable]
    public sealed class FighterReinforcementsSettings : ShipAbilitySettings
    {
        public override IShipAbility CreateAbility(ShipAbilityDependencies dependencies) =>
            new FighterReinforcementsAbility();
    }
}
