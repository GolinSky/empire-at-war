using EmpireAtWar.Entities.Game;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.Economy.Ui;
using EmpireAtWar.Entities.Faction.Ui;
using EmpireAtWar.Entities.Heroes;
using EmpireAtWar.Entities.Reinforcement;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Entities.SuperWeapons.Controller;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Economy;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Presenters.Cheats;
using EmpireAtWar.Services.CaptureSites;
using EmpireAtWar.Services.Cheats;
using EmpireAtWar.Services.Economy;
using EmpireAtWar.Services.Factions;
using EmpireAtWar.Services.Factory;
using EmpireAtWar.Services.Player;
using EmpireAtWar.Services.Reinforcement;
using EmpireAtWar.Services.Squadrons;
using EmpireAtWar.Views.Cheats;
using UnityEngine;
using global::VContainer;
using global::VContainer.Unity;

namespace EmpireAtWar.Services.VContainer
{
    public sealed class LocalPlayerScope : AssetLifetimeScope
    {
        private PlayerSlot _owner;

        public void ConfigurePlayer(PlayerSlot owner) => _owner = owner;

        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);
            RegisterPlayerServices(builder, _owner, ((SkirmishScope)Parent).Factions.Get(_owner.Faction),
                ((SkirmishScope)Parent).Battle.Features & ((SkirmishScope)Parent).Battle.GetPlayer(_owner.Id).Features);
            SkirmishScope battleScope = (SkirmishScope)Parent;
            var player = battleScope.Battle.GetPlayer(_owner.Id);
            battleScope.Modules.ConfigurePlayer(builder, player);
            builder.RegisterInstance(new UnitRequestFactory(
                battleScope.Battle.HasFeature(BattleFeatures.SuperWeapons) &&
                (player.Features & BattleFeatures.SuperWeapons) != 0))
                .AsImplementedInterfaces().AsSelf();
            if (battleScope.Battle.HasFeature(BattleFeatures.Economy) && (player.Features & BattleFeatures.Economy) != 0)
            {
                GameObject cheatRoot = new GameObject(nameof(CheatView));
                cheatRoot.transform.SetParent(transform, false);
                builder.RegisterComponent(cheatRoot.AddComponent<CheatView>()).AsImplementedInterfaces().AsSelf();
            }
            builder.RegisterLifecycle();
        }

        public static void RegisterPlayerServices(IContainerBuilder builder, PlayerSlot owner, FactionDefinition faction,
            BattleFeatures features = BattleFeatures.All)
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
            builder.Register<PlayerFactionModel>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<FactionResearchModel>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<HeroUiController>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            if ((features & BattleFeatures.Economy) != 0)
            {
                builder.Register<FactionService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
                builder.Register<FactionUiController>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
                builder.Register<ShipBuildUiController>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
                builder.Register<CheatService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
                builder.Register<CheatWorldService>(Lifetime.Singleton);
                builder.Register<CheatPresenter>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            }
            builder.Register<PlayerService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
        }
    }
}
