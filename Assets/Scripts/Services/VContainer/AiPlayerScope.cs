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
            UnitFactoryRegistrations.Register(builder);
            builder.Register<SquadronPool>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<EnemyService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<EnemyStrategicDecisionModel>(Lifetime.Singleton);
            builder.Register<EnemyProductionDecisionModel>(Lifetime.Singleton);
            builder.Register<EnemyCounterProductionModel>(Lifetime.Singleton);
            builder.Register<UnitCombatProfileCatalog>(Lifetime.Singleton);
            builder.Register<ForceCompositionBuilder>(Lifetime.Singleton);
            builder.Register<HostileIntelGatherer>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<EnemyStrategicContextBuilder>(Lifetime.Singleton);
            builder.Register<EnemyTaskForceExecutor>(Lifetime.Singleton);
            builder.Register<EnemyProductionStrategy>(Lifetime.Singleton);
            builder.Register<EnemyStructurePlacementService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf()
                .WithParameter(Assets.LoadComponent<BoxCollider>(_owner.Faction + "SpaceStationView"));
            builder.Register<EnemyUnitCommander>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<EnemyShipAbilityController>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<SuperWeaponModel>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<EnemySuperWeaponController>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<EnemyUnitLimitModel>(Lifetime.Singleton);
            builder.Register<EnemyFactionController>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<EconomyModel>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<EconomyService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            GameObject debugRoot = new GameObject(nameof(EnemyEconomyDebugView));
            debugRoot.transform.SetParent(transform, false);
            builder.RegisterComponent(debugRoot.AddComponent<EnemyEconomyDebugView>())
                .AsImplementedInterfaces().AsSelf();
#endif
            builder.Register<SiteFacilityBuilder>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<EnemySiteConstructionController>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<EnemySquadronCommander>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<FactionRoster>(Lifetime.Singleton)
                .WithParameter(((SkirmishScope)Parent).Factions.Get(_owner.Faction));
            builder.Register<EnemyFactionModel>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<FactionResearchModel>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.RegisterLifecycle();
        }
    }
}
