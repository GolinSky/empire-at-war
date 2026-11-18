using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.CaptureSites;
using Utilities.ScriptUtils.Time;
using VContainer;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Entities.EnemyFaction.Models;

namespace EmpireAtWar.Services.Enemy
{
    /// <summary>
    /// Builds mines on safe owned sites as soon as affordable, and the required satellites (three per zone, one per mining point) without waiting for a fleet.
    /// </summary>
    public sealed class EnemySiteConstructionController : ITickable
    {
        private const float DECISION_INTERVAL = 3f;
        private readonly ICaptureSiteDirectory _sites;
        private readonly ICaptureSitePresence _presence;
        private readonly ICaptureSiteConstruction _construction;
        private readonly ITimer _decisionTimer = TimerFactory.ConstructTimer(DECISION_INTERVAL);
        private readonly PlayerSlot _owner;
        private readonly EnemyDefenseSnapshotBuilder _snapshotBuilder;
        private readonly EnemyStationPressureTracker _stationPressure;
        private readonly EnemyDefenseStrategy _defense;

        public EnemySiteConstructionController(ICaptureSiteDirectory sites, ICaptureSitePresence presence,
            ICaptureSiteConstruction construction,
            PlayerSlot owner, EnemyDefenseSnapshotBuilder snapshotBuilder,
            EnemyDefenseStrategy defense,
            EnemyStationPressureTracker stationPressure)
        {
            _owner = owner;
            _sites = sites;
            _presence = presence;
            _construction = construction;
            _snapshotBuilder = snapshotBuilder;
            _defense = defense;
            _stationPressure = stationPressure;
        }

        public void Tick()
        {
            if (_stationPressure.IsStationCritical) return;
            if (!_decisionTimer.IsComplete) return;
            _decisionTimer.StartTimer();
            // At a captured point: required satellites first, then mining, then optional scored defense. All three
            // outrank ships. Satellite scores read the strategic snapshot, empty until the first strategic decision.
            bool ready = _snapshotBuilder.IsReady;
            EnemyDefenseSnapshot snapshot = ready ? _snapshotBuilder.Build() : default;
            if (ready && _defense.TrySelectMandatory(snapshot, out var required))
            {
                StartSatellite(snapshot, required);
                return;
            }
            // Mines fund everything else, so an empty owned mine keeps the credits until it is built.
            if (HasEmptyOwnedMine())
            {
                bool built = _construction.TryBuildOnOwnedSite(_owner.Id);
                EnemyAiTelemetry.RecordEvent(_owner.Id, _owner.Faction, "siteBuild", $"kind=mine;started={built}");
                if (built) return;
            }
            if (ready && _defense.TrySelectScored(snapshot, out var optional)) StartSatellite(snapshot, optional);
        }

        private void StartSatellite(EnemyDefenseSnapshot snapshot, EnemyDefenseChoice choice)
        {
            if (_construction.TryStartConstruction(choice.SiteIndex, _owner.Id, choice.Type))
                EnemyAiTelemetry.RecordSatellite(_owner.Id, _owner.Faction, choice, snapshot.Money, _defense.TopChoices);
            else
                EnemyAiTelemetry.RecordEvent(_owner.Id, _owner.Faction, "siteBuild",
                    $"kind=satellite;started=False;site={choice.SiteIndex};type={choice.Type}");
        }

        private bool HasEmptyOwnedMine()
        {
            foreach (ICaptureSite site in _sites.Sites)
                if (site.Kind != CaptureSiteKind.Satellite && site.Owner == _owner.Id &&
                    site.State == CaptureSiteState.Owned && !_presence.HasHostileUnits(site, _owner.Id))
                    return true;
            return false;
        }

    }
}
