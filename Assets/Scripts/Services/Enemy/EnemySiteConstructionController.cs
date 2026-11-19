using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.CaptureSites;
using Utilities.ScriptUtils.Time;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Presenters.ReinforcementZones;
using EmpireAtWar.Services.ReinforcementZones;

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
        private readonly EnemyProductionStrategy _production;
        private readonly IReinforcementZoneSource _zones;

        public EnemySiteConstructionController(ICaptureSiteDirectory sites, ICaptureSitePresence presence,
            ICaptureSiteConstruction construction,
            PlayerSlot owner, EnemyDefenseSnapshotBuilder snapshotBuilder,
            EnemyDefenseStrategy defense,
            EnemyStationPressureTracker stationPressure, EnemyProductionStrategy production,
            IReinforcementZoneSource zones)
        {
            _owner = owner;
            _sites = sites;
            _presence = presence;
            _construction = construction;
            _snapshotBuilder = snapshotBuilder;
            _defense = defense;
            _stationPressure = stationPressure;
            _production = production;
            _zones = zones;
        }

        public void Tick()
        {
            if (_stationPressure.IsStationCritical || _production.OpeningMiningPending || _production.OpeningLevelReserve > 0f)
            {
                _production.PriorityConstructionPending = false;
                return;
            }
            if (!_decisionTimer.IsComplete) return;
            _decisionTimer.StartTimer();
            if (!_snapshotBuilder.IsReady) return;
            EnemyDefenseSnapshot snapshot = _snapshotBuilder.Build();
            ReinforcementZonePresenter centre = EnemyShipTaskGroupPlanner.GetCentre(_zones.Zones);
            int mineSite = FindMineSite(centre);
            bool mandatorySatellite = _defense.HasMandatoryPad(snapshot);
            _production.PriorityConstructionPending = mandatorySatellite || mineSite >= 0;
            BuildNext(snapshot, mineSite, mandatorySatellite);
        }

        internal void BuildNext(EnemyDefenseSnapshot snapshot, int mineSite, bool mandatorySatellite)
        {
            if (_defense.TrySelectMandatory(snapshot, out var required) && StartSatellite(snapshot, required))
                return;
            if (mineSite >= 0)
            {
                bool built = _construction.TryStartConstruction(mineSite, _owner.Id, SiteFacilityType.Mining);
                EnemyAiTelemetry.RecordEvent(_owner.Id, _owner.Faction, "siteBuild", $"kind=mine;started={built}");
                return;
            }
            if (!mandatorySatellite && _defense.TrySelectScored(snapshot, out var optional)) StartSatellite(snapshot, optional);
        }

        private bool StartSatellite(EnemyDefenseSnapshot snapshot, EnemyDefenseChoice choice)
        {
            if (_construction.TryStartConstruction(choice.SiteIndex, _owner.Id, choice.Type))
            {
                EnemyAiTelemetry.RecordSatellite(_owner.Id, _owner.Faction, choice, snapshot.Money, _defense.TopChoices);
                return true;
            }

            EnemyAiTelemetry.RecordEvent(_owner.Id, _owner.Faction, "siteBuild",
                $"kind=satellite;started=False;site={choice.SiteIndex};type={choice.Type}");
            return false;
        }

        private int FindMineSite(ReinforcementZonePresenter centre)
        {
            int centreSatellites = 0;
            if (centre != null)
                foreach (ICaptureSite site in _sites.Sites)
                    if (site.Kind == CaptureSiteKind.Satellite && site.Owner == _owner.Id && site.IsOperational &&
                        EnemyShipTaskGroupPlanner.IsInZone(centre, site.Center))
                        centreSatellites++;

            for (int i = 0; i < _sites.Sites.Count; i++)
            {
                ICaptureSite site = _sites.Sites[i];
                if (site.Kind != CaptureSiteKind.Mining || site.Owner != _owner.Id ||
                    site.State != CaptureSiteState.Owned || _presence.HasHostileUnits(site, _owner.Id)) continue;
                if (centre == null || centreSatellites >= EnemyDefenseStrategy.ZONE_SATELLITES ||
                    !EnemyShipTaskGroupPlanner.IsInZone(centre, site.Center))
                    return i;
            }
            return -1;
        }

    }
}
