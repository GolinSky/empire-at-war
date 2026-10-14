using EmpireAtWar.Entities.Economy.Ui;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Models.Economy;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Economy;
using global::VContainer;

namespace EmpireAtWar.Services.VContainer
{
    public sealed class EconomyModule : IBattleModule
    {
        private readonly IAssetService _assets;
        public BattleFeatures Feature => BattleFeatures.Economy;
        public EconomyModule(IAssetService assets) => _assets = assets;

        public void ConfigureBattle(IContainerBuilder builder) =>
            AssetRegistrations.RegisterShared<EconomyData>(builder, _assets);

        public void ConfigurePlayer(IContainerBuilder builder, BattlePlayer player)
        {
            builder.Register<EconomyModel>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<EconomyService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            if (player.Role == PlayerRole.Human)
                builder.Register<EconomyUiController>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
        }
    }
}
