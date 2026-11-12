using EmpireAtWar.Entities.Game;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Analytics;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.BattleNotifications;
using EmpireAtWar.Services.Networking.Online;
using EmpireAtWar.Services.Networking.States;
using EmpireAtWar.Services.Saves;
using EmpireAtWar.Services.ShipSpawning;
using EmpireAtWar.Services.StationFacing;
using global::VContainer;

namespace EmpireAtWar.Services.VContainer
{
    public sealed class SurvivalMode : IBattleMode
    {
        private readonly IAssetService _assets;
        private readonly OnlineMatchService _online;

        public SurvivalMode(IAssetService assets, OnlineMatchService online)
        {
            _assets = assets;
            _online = online;
        }

        public void ConfigureBattle(IContainerBuilder builder)
        {
            SurvivalWaveData waves = _assets.Load<SurvivalWaveData>(nameof(SurvivalWaveData));
            builder.Register<IMatchSession>(resolver => _online.CreateSession(
                () => OfflineSession.ForHuman(resolver.Resolve<IPlayerRoster>())), Lifetime.Singleton);
            builder.Register<LocalPlayer>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<BattleSaveInput>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<BattleAutosave>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<SurvivalAutosave>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.RegisterInstance(waves);
            builder.RegisterInstance(new SurvivalModel(waves.FirstPreparationSeconds, waves.PreparationSeconds));
            builder.Register<SurvivalOutcomeRule>(Lifetime.Singleton).As<IBattleOutcomeRule>();
            builder.Register<CenteredStationFacing>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<EdgeSpawnPoints>(Lifetime.Singleton).As<IEdgeSpawnPoints>();
            if (_online.IsInBattle && _online.Role == OnlineRole.Client)
                builder.Register<SurvivalWaveDirector>(Lifetime.Singleton).AsSelf();
            else
                builder.Register<SurvivalWaveDirector>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<SurvivalReplicatedState>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<SurvivalSaveSource>(Lifetime.Singleton).As<IBattleSaveSource>();
            builder.Register<SurvivalHudUiController>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<SurvivalNotifier>(Lifetime.Singleton)
                .AsImplementedInterfaces();
            builder.Register<SurvivalWaveAnalytics>(Lifetime.Singleton).AsImplementedInterfaces();
        }
    }
}
