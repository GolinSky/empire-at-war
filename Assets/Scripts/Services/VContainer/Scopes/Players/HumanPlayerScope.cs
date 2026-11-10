using EmpireAtWar.Entities.Game;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.Heroes;
using EmpireAtWar.Entities.Reinforcement;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Services.Factory;
using EmpireAtWar.Services.Networking.States;
using EmpireAtWar.Services.Networking;
using EmpireAtWar.Services.Player;
using EmpireAtWar.Services.Reinforcement;
using EmpireAtWar.Services.Saves;
using EmpireAtWar.Services.Squadrons;
using EmpireAtWar.Services.Audio;
using global::VContainer;

namespace EmpireAtWar.Services.VContainer
{
    /// <summary>
    /// A human slot. Every machine builds the simulation services; only the machine where this human sits
    /// (<see cref="IMatchSession.LocalPlayerId"/>) adds UI, input and announcements.
    /// </summary>
    public sealed class HumanPlayerScope : AssetLifetimeScope
    {
        private PlayerSlot _owner;
        private bool _isLocal;

        public void ConfigurePlayer(PlayerSlot owner, bool isLocal)
        {
            _owner = owner;
            _isLocal = isLocal;
        }

        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);
            SkirmishScope battleScope = (SkirmishScope)Parent;
            BattlePlayer player = battleScope.Battle.GetPlayer(_owner.Id);
            ProductionAccessRegistrations.Register(builder, battleScope.Battle, player);
            RegisterPlayerServices(builder, _owner, battleScope.Factions.Get(_owner.Faction), player.Features,
                battleScope.Battle.InitialStationLevel);
            battleScope.Modules.ConfigurePlayer(builder, player, transform, runsAi: false);
            builder.RegisterInstance(new UnitRequestFactory(player.Has(BattleFeatures.SuperWeapons)))
                .AsImplementedInterfaces().AsSelf();
            builder.Register<PlayerReserveReplicatedState>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<StationLevelReplicatedState>(Lifetime.Singleton).AsImplementedInterfaces();
            if (player.Has(BattleFeatures.Economy))
                builder.Register<HostPlayerEconomyFeedback>(Lifetime.Singleton).AsImplementedInterfaces();
            if (!_isLocal) return;
            RegisterLocalPlayerServices(builder, player.Features);
            battleScope.Modules.ConfigureLocalPlayer(builder, player, transform);
            // Simulations are a balance tool, not matches players play.
            if (battleScope.Battle.Mode == GameMode.Tutorial)
                EmpireAtWar.Services.Tutorial.Guided.GuidedTutorialRegistrations.Register(builder, Assets);
            else if (battleScope.Battle.Mode != GameMode.BattleSimulation)
                HintRegistrations.Register(builder, Assets,
                    hasCheatPanel: battleScope.Battle.HasFeature(BattleFeatures.Economy) && player.Has(BattleFeatures.Economy));
        }

        public static void RegisterPlayerServices(IContainerBuilder builder, PlayerSlot owner, FactionDefinition faction,
            BattleFeatures features = BattleFeatures.All, int initialStationLevel = 1)
        {
            builder.RegisterInstance(owner);
            UnitFactoryRegistrations.Register(builder);
            builder.Register<SquadronPool>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<ReinforcementModel>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            // Placement and deploy input still live inside ReinforcementService; they move out with player commands.
            builder.Register<ReinforcementPreviewFactory>(Lifetime.Singleton);
            builder.Register<ReinforcementPlacementFactory>(Lifetime.Singleton);
            builder.Register<ReinforcementDeployInput>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<ReinforcementService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<FactionRoster>(Lifetime.Singleton)
                .WithParameter(faction).WithParameter(features);
            builder.Register(resolver => new PlayerFactionModel(resolver.Resolve<StationLevelData>(), resolver.Resolve<FactionRoster>())
                { CurrentLevel = System.Math.Min(initialStationLevel, resolver.Resolve<StationLevelData>().MaxLevel) },
                Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<FactionResearchModel>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<SuperWeaponModel>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<SuperWeaponSaveSource>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<ReinforcementSaveSource>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<PlayerService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
        }

        public static void RegisterLocalPlayerServices(IContainerBuilder builder, BattleFeatures features)
        {
            builder.Register<ReinforcementUiController>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<HeroUiController>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            if ((features & BattleFeatures.Economy) != 0)
            {
                builder.Register<FactionAnnouncementService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
                builder.Register<EmpireAtWar.Services.BattleNotifications.EconomyNotifier>(Lifetime.Singleton)
                    .AsImplementedInterfaces().AsSelf();
                builder.Register<ClientPlayerEconomyFeedback>(Lifetime.Singleton).AsImplementedInterfaces();
            }
        }
    }
}
