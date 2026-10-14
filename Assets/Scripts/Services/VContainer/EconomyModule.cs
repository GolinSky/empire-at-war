using EmpireAtWar.Entities.Economy.Ui;
using EmpireAtWar.Entities.EnemyFaction.Controllers;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;
using EmpireAtWar.Entities.Faction.Ui;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Models.Economy;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Mvc;
using EmpireAtWar.Presenters.Cheats;
using EmpireAtWar.Services.Cheats;
using EmpireAtWar.Services.Economy;
using EmpireAtWar.Services.Enemy;
using EmpireAtWar.Services.Factions;
using EmpireAtWar.Views.Cheats;
using global::VContainer;
using global::VContainer.Unity;
using UnityEngine;

namespace EmpireAtWar.Services.VContainer
{
    /// <summary>Money and everything bought with it: the human's shop and cheats, the AI's production.</summary>
    public sealed class EconomyModule : IBattleModule
    {
        private readonly IAssetService _assets;
        public BattleFeatures Feature => BattleFeatures.Economy;
        public EconomyModule(IAssetService assets) => _assets = assets;

        public void ConfigureBattle(IContainerBuilder builder) =>
            AssetRegistrations.RegisterShared<EconomyData>(builder, _assets);

        public void ConfigureDisabled(IContainerBuilder builder) { }

        public void ConfigurePlayer(IContainerBuilder builder, BattlePlayer player, Transform playerRoot)
        {
            builder.Register<EconomyModel>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<EconomyService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            if (player.Slot.IsAi) ConfigureAi(builder, playerRoot);
            else ConfigureHuman(builder, playerRoot);
        }

        private static void ConfigureHuman(IContainerBuilder builder, Transform playerRoot)
        {
            builder.Register<EconomyUiController>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<FactionService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<FactionUiController>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<ShipBuildUiController>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<CheatService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<CheatWorldService>(Lifetime.Singleton);
            builder.Register<CheatPresenter>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            GameObject cheatRoot = new GameObject(nameof(CheatView));
            cheatRoot.transform.SetParent(playerRoot, false);
            builder.RegisterComponent(cheatRoot.AddComponent<CheatView>()).AsImplementedInterfaces().AsSelf();
        }

        private static void ConfigureAi(IContainerBuilder builder, Transform playerRoot)
        {
            builder.Register<EnemyService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<EnemyProductionDecisionModel>(Lifetime.Singleton);
            builder.Register<EnemyCounterProductionModel>(Lifetime.Singleton);
            builder.Register<EnemyProductionStrategy>(Lifetime.Singleton);
            builder.Register<EnemyFactionController>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<FactionResearchModel>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            GameObject debugRoot = new GameObject(nameof(EnemyEconomyDebugView));
            debugRoot.transform.SetParent(playerRoot, false);
            builder.RegisterComponent(debugRoot.AddComponent<EnemyEconomyDebugView>()).AsImplementedInterfaces().AsSelf();
#endif
        }
    }
}
