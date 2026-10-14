using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Mvc;
using global::VContainer;

namespace EmpireAtWar.Services.VContainer
{
    public sealed class MiningSitesModule : IBattleModule
    {
        private readonly IAssetService _assets;
        public BattleFeatures Feature => BattleFeatures.MiningSites;
        public MiningSitesModule(IAssetService assets) => _assets = assets;

        public void ConfigureBattle(IContainerBuilder builder) =>
            AssetRegistrations.RegisterShared<MiningFacilityCatalog>(builder, _assets);

        public void ConfigurePlayer(IContainerBuilder builder, BattlePlayer player) { }
    }
}
