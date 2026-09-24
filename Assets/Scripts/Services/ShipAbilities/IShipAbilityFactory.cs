namespace EmpireAtWar.Services.ShipAbilities
{
    public interface IShipAbilityFactory
    {
        IShipAbility Create(ShipAbilityDefinition definition);
    }
}
