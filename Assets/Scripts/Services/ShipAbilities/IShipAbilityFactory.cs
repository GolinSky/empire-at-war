namespace EmpireAtWar.Services.ShipAbilities
{
    public interface IShipAbilityFactory
    {
        IShipAbility Create(ShipAbilityId id);
    }
}
