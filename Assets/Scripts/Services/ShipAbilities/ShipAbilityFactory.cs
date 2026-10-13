namespace EmpireAtWar.Services.ShipAbilities
{
    public sealed class ShipAbilityFactory : IShipAbilityFactory
    {
        private readonly ShipAbilityDependencies _dependencies;

        public ShipAbilityFactory(ShipAbilityDependencies dependencies) { _dependencies = dependencies; }

        public IShipAbility Create(ShipAbilityDefinition definition) =>
            definition.Settings.CreateAbility(_dependencies);
    }
}
