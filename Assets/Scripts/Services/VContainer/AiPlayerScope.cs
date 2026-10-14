using EmpireAtWar.Entities.Game;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Controllers.Economy;
using EmpireAtWar.Entities.EnemyFaction.Controllers;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Economy;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.CaptureSites;
using EmpireAtWar.Services.Economy;
using EmpireAtWar.Services.Enemy;
using EmpireAtWar.Services.Factory;
using EmpireAtWar.Services.Squadrons;
using UnityEngine;
using global::VContainer;
using global::VContainer.Unity;

namespace EmpireAtWar.Services.VContainer
{
    public sealed class AiPlayerScope : AssetLifetimeScope
    {
        private PlayerSlot _owner;

        public void ConfigurePlayer(PlayerSlot owner) => _owner = owner;

        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);
            builder.RegisterInstance(_owner);
            SkirmishScope battleScope = (SkirmishScope)Parent;
            var player = battleScope.Battle.GetPlayer(_owner.Id);
            battleScope.Modules.ConfigurePlayer(builder, player);
            builder.RegisterInstance(new UnitRequestFactory(
                battleScope.Battle.HasFeature(BattleFeatures.SuperWeapons) &&
                (player.Features & BattleFeatures.SuperWeapons) != 0))
                .AsImplementedInterfaces().AsSelf();
            UnitFactoryRegistrations.Register(builder);
            builder.Register<SquadronPool>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<EnemyStrategicDecisionModel>(Lifetime.Singleton);
            builder.Register<UnitCombatProfileCatalog>(Lifetime.Singleton);
            builder.Register<ForceCompositionBuilder>(Lifetime.Singleton);
            builder.Register<HostileIntelGatherer>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<EnemyStrategicContextBuilder>(Lifetime.Singleton);
            builder.Register<EnemyTaskForceExecutor>(Lifetime.Singleton);
            builder.Register<EnemyStructurePlacementService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf()
                .WithParameter(Assets.LoadComponent<BoxCollider>(_owner.Faction + "SpaceStationView"));
            builder.Register<EnemyUnitCommander>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<EnemyShipAbilityController>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<EnemyUnitLimitModel>(Lifetime.Singleton);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (player.CanProduce && battleScope.Battle.HasFeature(BattleFeatures.Economy) &&
                (player.Features & BattleFeatures.Economy) != 0)
            {
                GameObject debugRoot = new GameObject(nameof(EnemyEconomyDebugView));
                debugRoot.transform.SetParent(transform, false);
                builder.RegisterComponent(debugRoot.AddComponent<EnemyEconomyDebugView>())
                .AsImplementedInterfaces().AsSelf();
            }
#endif
            builder.Register<EnemySquadronCommander>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<FactionRoster>(Lifetime.Singleton)
                .WithParameter(battleScope.Factions.Get(_owner.Faction))
                .WithParameter(battleScope.Battle.Features & player.Features);
            builder.Register<EnemyFactionModel>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            if (player.CanProduce && battleScope.Battle.HasFeature(BattleFeatures.Economy) &&
                (player.Features & BattleFeatures.Economy) != 0)
            {
                builder.Register<EnemyService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
                builder.Register<EnemyProductionDecisionModel>(Lifetime.Singleton);
                builder.Register<EnemyCounterProductionModel>(Lifetime.Singleton);
                builder.Register<EnemyProductionStrategy>(Lifetime.Singleton);
                builder.Register<EnemyFactionController>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
                builder.Register<FactionResearchModel>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            }
            else if (player.OwnsStation)
                builder.Register<EmpireAtWar.Services.Player.PlayerService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.RegisterLifecycle();
        }
    }
}
