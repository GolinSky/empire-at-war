using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Mvc;
using EmpireAtWar.Presenters.MiniMap;
using EmpireAtWar.Services.CaptureSites;
using EmpireAtWar.Services.Enemy;
using global::VContainer;
using global::VContainer.Unity;

namespace EmpireAtWar.Services.VContainer
{
    public sealed class CaptureSitesModule : IBattleModule
    {
        private readonly IAssetService _assets;
        private readonly CaptureSitesSystem _sites;
        public BattleFeatures Feature => BattleFeatures.CaptureSites;
        public CaptureSitesModule(IAssetService assets, CaptureSitesSystem sites)
        {
            _assets = assets;
            _sites = sites;
        }

        public void ConfigureBattle(IContainerBuilder builder)
        {
            AssetRegistrations.RegisterShared<CaptureSiteData>(builder, _assets);
            builder.RegisterComponent(_sites).AsImplementedInterfaces().AsSelf();
            builder.Register<CaptureSiteMiniMapPresenter>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<CaptureSiteBuildUiController>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
        }

        public void ConfigurePlayer(IContainerBuilder builder, BattlePlayer player)
        {
            if (!player.CanProduce || (player.Features & BattleFeatures.Economy) == 0) return;
            builder.Register<SiteFacilityBuilder>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            if (player.Slot.IsAi)
                builder.Register<EnemySiteConstructionController>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
        }
    }
}
