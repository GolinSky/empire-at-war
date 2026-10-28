using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Mvc;
using EmpireAtWar.Presenters.MiniMap;
using EmpireAtWar.Services.CaptureSites;
using EmpireAtWar.Services.Enemy;
using EmpireAtWar.Services.Saves;
using global::VContainer;
using global::VContainer.Unity;
using UnityEngine;

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
            builder.Register<CaptureSitesSaveSource>(Lifetime.Singleton).As<IBattleSaveSource>();
        }

        public void ConfigureDisabled(IContainerBuilder builder)
        {
            AssetRegistrations.RegisterShared<CaptureSiteData>(builder, _assets);
            _sites.gameObject.SetActive(false);
            builder.Register<NoCaptureSites>(Lifetime.Singleton).As<ICaptureSitesSystem>();
        }

        public void ConfigurePlayer(IContainerBuilder builder, BattlePlayer player, Transform playerRoot)
        {
            if (!player.Has(BattleFeatures.Economy)) return;
            builder.Register<SiteFacilityBuilder>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            if (player.Slot.IsAi)
            {
                builder.Register<EnemyDefenseStrategy>(Lifetime.Singleton);
                builder.Register<EnemyDefenseSnapshotBuilder>(Lifetime.Singleton);
                builder.Register<EnemySiteConstructionController>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            }
        }
    }
}
