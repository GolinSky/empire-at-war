using System;
using Zenject;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    [Serializable]
    public sealed class SensorJammingSettings : ShipAbilitySettings
    {
        public override IShipAbility CreateAbility(IInstantiator instantiator) =>
            instantiator.Instantiate<SensorJammingAbility>();
    }
}
