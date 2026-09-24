using Zenject;

namespace EmpireAtWar.Services.ShipAbilities
{
    public sealed class ShipAbilityFactory : IShipAbilityFactory
    {
        private readonly IInstantiator _instantiator;

        public ShipAbilityFactory(IInstantiator instantiator) { _instantiator = instantiator; }

        public IShipAbility Create(ShipAbilityDefinition definition) =>
            definition.Settings.CreateAbility(_instantiator);
    }
}
