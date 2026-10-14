using EmpireAtWar.Entities.Game;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Stations;
using global::VContainer;

namespace EmpireAtWar.Services.VContainer
{
    public static class ProductionAccessRegistrations
    {
        public static void Register(IContainerBuilder builder, BattleDefinition battle, BattlePlayer player)
        {
            PlayerId host = battle.GetProductionHost(player).Slot.Id;
            if (player.IsHosted)
                builder.Register<IProductionAccess>(resolver =>
                    new HostedProductionAccess(host, resolver.Resolve<IStationRegistry>()), Lifetime.Singleton);
            else
                builder.Register<IProductionAccess>(resolver =>
                    new SelfProductionAccess(host, resolver.Resolve<IStationRegistry>()), Lifetime.Singleton);
        }
    }
}
