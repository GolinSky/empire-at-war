using EmpireAtWar.Entities.Game;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.Heroes;
using EmpireAtWar.Entities.Reinforcement;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Services.Factory;
using EmpireAtWar.Services.Player;
using EmpireAtWar.Services.Reinforcement;
using EmpireAtWar.Services.Saves;
using EmpireAtWar.Services.Squadrons;
using global::VContainer;

namespace EmpireAtWar.Services.VContainer
{
    public sealed class LocalPlayerScope : AssetLifetimeScope
    {
        private PlayerSlot _owner;

        public void ConfigurePlayer(PlayerSlot owner) => _owner = owner;

        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);
            SkirmishScope battleScope = (SkirmishScope)Parent;
            BattlePlayer player = battleScope.Battle.GetPlayer(_owner.Id);
            ProductionAccessRegistrations.Register(builder, battleScope.Battle, player);
            RegisterPlayerServices(builder, _owner, battleScope.Factions.Get(_owner.Faction), player.Features,
                battleScope.Battle.InitialStationLevel);
            battleScope.Modules.ConfigurePlayer(builder, player, transform);
            builder.RegisterInstance(new UnitRequestFactory(player.Has(BattleFeatures.SuperWeapons)))
                .AsImplementedInterfaces().AsSelf();
        }

        public static void RegisterPlayerServices(IContainerBuilder builder, PlayerSlot owner, FactionDefinition faction,
            BattleFeatures features = BattleFeatures.All, int initialStationLevel = 1)
        {
            builder.RegisterInstance(owner);
            UnitFactoryRegistrations.Register(builder);
            builder.Register<SquadronPool>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<ReinforcementModel>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<ReinforcementPreviewFactory>(Lifetime.Singleton);
            builder.Register<ReinforcementPlacementFactory>(Lifetime.Singleton);
            builder.Register<ReinforcementService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<ReinforcementUiController>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<FactionRoster>(Lifetime.Singleton)
                .WithParameter(faction).WithParameter(features);
            builder.Register(resolver => new PlayerFactionModel(resolver.Resolve<StationLevelData>(), resolver.Resolve<FactionRoster>())
                { CurrentLevel = System.Math.Min(initialStationLevel, resolver.Resolve<StationLevelData>().MaxLevel) },
                Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<FactionResearchModel>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<SuperWeaponModel>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<SuperWeaponSaveSource>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<ReinforcementSaveSource>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<HeroUiController>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<PlayerService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
        }
    }
}
