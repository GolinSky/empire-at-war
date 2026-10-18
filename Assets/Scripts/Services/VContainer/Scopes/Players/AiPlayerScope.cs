using EmpireAtWar.Entities.Game;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Enemy;
using EmpireAtWar.Services.Factory;
using EmpireAtWar.Services.Player;
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
            BattlePlayer player = battleScope.Battle.GetPlayer(_owner.Id);
            ProductionAccessRegistrations.Register(builder, battleScope.Battle, player);
            battleScope.Modules.ConfigurePlayer(builder, player, transform);
            builder.RegisterInstance(new UnitRequestFactory(player.Has(BattleFeatures.SuperWeapons)))
                .AsImplementedInterfaces().AsSelf();
            UnitFactoryRegistrations.Register(builder);
            builder.Register<SquadronPool>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            if (player.IsHosted)
                builder.Register<HostedSquadronLauncher>(Lifetime.Singleton).As<ISquadronLauncher>();
            builder.Register<EnemyStrategicDecisionModel>(Lifetime.Singleton);
            builder.Register<UnitCombatProfileCatalog>(Lifetime.Singleton);
            builder.Register<ForceCompositionBuilder>(Lifetime.Singleton);
            builder.Register<HostileIntelGatherer>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<EnemyStrategicContextBuilder>(Lifetime.Singleton);
            builder.Register<EnemyTaskForceExecutor>(Lifetime.Singleton);
            builder.Register<EnemyStructurePlacementService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf()
                .WithParameter(Assets.LoadComponent<BoxCollider>(
                    battleScope.Battle.GetProductionHost(player).Slot.Faction + "SpaceStationView"));
            if (player.HasDirectCombatAi)
                builder.Register<DirectCombatAiState>(Lifetime.Singleton).AsImplementedInterfaces();
            else
                builder.Register<EnemyUnitCommander>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<EnemyShipAbilityController>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<SuperWeaponModel>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<EnemyUnitLimitModel>(Lifetime.Singleton);
            builder.Register<EnemySquadronCommander>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<FactionRoster>(Lifetime.Singleton)
                .WithParameter(battleScope.Factions.Get(_owner.Faction))
                .WithParameter(player.Features);
            builder.Register<EnemyFactionModel>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            // With an economy, EnemyService spawns the station; without one only the station spawner remains.
            if (player.OwnsStation && !player.Has(BattleFeatures.Economy))
                builder.Register<PlayerService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
        }
    }
}
