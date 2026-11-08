using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Mvc;
using EmpireAtWar.Presenters.MiniMap;
using EmpireAtWar.Services.CaptureSites;
using EmpireAtWar.Services.Enemy;
using EmpireAtWar.Services.Networking.States;
using EmpireAtWar.Services.Saves;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.Tooltip;
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
            builder.Register<CaptureSiteTooltipSource>(Lifetime.Singleton).As<IWorldTooltipSource>();
            builder.Register<ConstructingSitesSurvival>(Lifetime.Singleton).As<IBattleSurvivalSource>();
            builder.Register<CaptureSiteMiniMapPresenter>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<CaptureSiteBuildUiController>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<CaptureSitesSaveSource>(Lifetime.Singleton).As<IBattleSaveSource>();
            builder.Register<CaptureSitesReplicatedState>(Lifetime.Singleton).AsImplementedInterfaces();
        }

        public void ConfigureDisabled(IContainerBuilder builder)
        {
            AssetRegistrations.RegisterShared<CaptureSiteData>(builder, _assets);
            _sites.gameObject.SetActive(false);
        }

        public void ConfigurePlayer(IContainerBuilder builder, BattlePlayer player, Transform playerRoot, bool runsAi)
        {
            if (runsAi)
                builder.Register<EnemySiteObjectives>(Lifetime.Singleton).As<IEnemyCaptureObjectiveSource>();
            if (!player.Has(BattleFeatures.Economy)) return;
            builder.Register<SiteFacilityBuilder>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            if (runsAi)
            {
                builder.Register<EnemyDefenseStrategy>(Lifetime.Singleton);
                builder.Register<EnemyDefenseSnapshotBuilder>(Lifetime.Singleton);
                builder.Register<EnemySiteConstructionController>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            }
        }

        public void ConfigureLocalPlayer(IContainerBuilder builder, BattlePlayer player, Transform playerRoot)
        {
            if (player.Has(BattleFeatures.Economy))
                builder.Register<CaptureSiteAnnouncer>(Lifetime.Singleton).AsImplementedInterfaces();
        }
    }
}
