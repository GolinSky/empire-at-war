using System.Linq;
using EmpireAtWar.Controllers.Economy;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Factions;

namespace EmpireAtWar.Services.Saves
{
    /// <summary>The human's station level, research and production queue; queued items are already paid for.</summary>
    public sealed class HumanProductionSaveSource : PlayerSaveSource
    {
        private readonly PlayerFactionModel _faction;
        private readonly FactionResearchModel _research;
        private readonly FactionService _factionService;
        private readonly IEconomyProvider _economyProvider;
        private readonly IUnitRequestFactory _requests;
        private readonly FactionRoster _roster;
        private readonly StationLevelData _levels;

        public HumanProductionSaveSource(PlayerSaveRegistry registry, PlayerSlot owner, PlayerFactionModel faction,
            FactionResearchModel research, FactionService factionService, IEconomyProvider economyProvider,
            IUnitRequestFactory requests, FactionRoster roster, StationLevelData levels)
            : base(registry, owner)
        {
            _faction = faction;
            _research = research;
            _factionService = factionService;
            _economyProvider = economyProvider;
            _requests = requests;
            _roster = roster;
            _levels = levels;
        }

        public override void Capture(PlayerSave save)
        {
            save.StationLevel = _faction.CurrentLevel;
            save.Research = ResearchSave.CaptureAll(_research);
            save.Production = _faction.QueuedItems
                .Select(item => UnitRequestSave.From(item.UnitRequest, item.RemainingBuildTime)).ToArray();
        }

        public override void RestoreBeforeUnits(PlayerSave save)
        {
            _faction.CurrentLevel = save.StationLevel;
            _economyProvider.RecalculateIncome(_factionService);
            ResearchSave.RestoreAll(save.Research, _research);
            _faction.RestoreQueue(save.Production.Select(item =>
                new ProductionQueueItem(item.ToRequest(_requests, _roster, _levels), item.TimeLeft)));
        }
    }
}
