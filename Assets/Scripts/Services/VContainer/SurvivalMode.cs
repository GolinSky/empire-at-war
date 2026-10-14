using EmpireAtWar.Entities.Game;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.StationFacing;
using global::VContainer;

namespace EmpireAtWar.Services.VContainer
{
    public sealed class SurvivalMode : IBattleMode
    {
        private readonly IAssetService _assets;
        public SurvivalMode(IAssetService assets) => _assets = assets;

        public void ConfigureBattle(IContainerBuilder builder)
        {
            SurvivalWaveData waves = _assets.Load<SurvivalWaveData>(nameof(SurvivalWaveData));
            builder.Register<LocalPlayer>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.RegisterInstance(waves);
            builder.RegisterInstance(new SurvivalModel(waves.FirstPreparationSeconds, waves.PreparationSeconds));
            builder.Register<SurvivalOutcomeRule>(Lifetime.Singleton).As<IBattleOutcomeRule>();
            builder.Register<CenteredStationFacing>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<EdgeSpawnPointProvider>(Lifetime.Singleton);
            builder.Register<SurvivalWaveDirector>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<SurvivalHudUiController>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
        }
    }
}
