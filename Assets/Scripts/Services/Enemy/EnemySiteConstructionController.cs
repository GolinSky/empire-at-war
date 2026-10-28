using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.CaptureSites;
using EmpireAtWar.Ship;
using Utilities.ScriptUtils.Time;
using VContainer;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Services.Ships;

namespace EmpireAtWar.Services.Enemy
{
    /// <summary>
    /// Builds the site facility once the AI owns an empty capture site, can pay for it,
    /// and has enough of a fleet that the investment does not starve its defense.
    /// </summary>
    public sealed class EnemySiteConstructionController : ITickable
    {
        private const float DECISION_INTERVAL = 3f;
        private const int MINIMUM_FLEET_SIZE = 2;
        private readonly ICaptureSitesSystem _captureSites;
        private readonly IShipService _shipService;
        private readonly ITimer _decisionTimer = TimerFactory.ConstructTimer(DECISION_INTERVAL);
        private readonly PlayerSlot _owner;
        private readonly EnemyDefenseSnapshotBuilder _snapshotBuilder;
        private readonly EmpireAtWar.Entities.EnemyFaction.Models.EnemyDefenseStrategy _defense;

        public EnemySiteConstructionController(ICaptureSitesSystem captureSites, IShipService shipService,
            PlayerSlot owner, EnemyDefenseSnapshotBuilder snapshotBuilder,
            EmpireAtWar.Entities.EnemyFaction.Models.EnemyDefenseStrategy defense)
        {
            _owner = owner;
            _captureSites = captureSites;
            _shipService = shipService;
            _snapshotBuilder = snapshotBuilder;
            _defense = defense;
        }

        public void Tick()
        {
            if (!_decisionTimer.IsComplete) return;
            _decisionTimer.StartTimer();
            if (CountOwnShips() < MINIMUM_FLEET_SIZE) return;
            // Mines fund everything else, so an empty owned mine keeps the credits until it is built.
            if (HasEmptyOwnedMine())
            {
                _captureSites.TryBuildOnOwnedSite(_owner.Id);
                return;
            }
            // Satellite scores read the strategic snapshot, which is empty until the first strategic decision.
            if (!_snapshotBuilder.IsReady) return;
            var snapshot = _snapshotBuilder.Build();
            if (_defense.TrySelect(snapshot, out var choice) &&
                _captureSites.TryStartConstruction(choice.SiteIndex, _owner.Id, choice.Type))
                EnemyAiTelemetry.RecordSatellite(_owner.Id, _owner.Faction, choice, snapshot.Money, _defense.TopChoices);
        }

        private bool HasEmptyOwnedMine()
        {
            foreach (ICaptureSite site in _captureSites.Sites)
                if (site.Kind != CaptureSiteKind.Satellite && site.Owner == _owner.Id &&
                    site.State == CaptureSiteState.Owned && !_captureSites.HasHostileUnits(site, _owner.Id))
                    return true;
            return false;
        }

        private int CountOwnShips()
        {
            int count = 0;
            foreach (IShipEntity ship in _shipService.Ships)
                if (ship.Owner == _owner.Id) count++;
            return count;
        }
    }
}
