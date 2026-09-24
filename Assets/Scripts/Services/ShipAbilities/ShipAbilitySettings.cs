using System;
using Zenject;

namespace EmpireAtWar.Services.ShipAbilities
{
    /// <summary>
    /// Data owned by one ability feature. Each subclass holds only its own ability's numbers
    /// and creates that ability, which receives the settings through its constructor.
    /// </summary>
    [Serializable]
    public abstract class ShipAbilitySettings
    {
        public abstract IShipAbility CreateAbility(IInstantiator instantiator);
    }
}
