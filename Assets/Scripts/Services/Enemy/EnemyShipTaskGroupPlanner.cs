using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Presenters.ReinforcementZones;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.UnitOrders;
using EmpireAtWar.Ship;
using UnityEngine;

namespace EmpireAtWar.Services.Enemy
{
    public sealed class EnemyShipTaskGroupPlanner
    {
        private const int MAIN_FORCE_SHIP_COUNT = 6;
        private const float DETACHMENT_GRACE_SECONDS = 10f;
        // A zone or centre detachment that has only gathered this long hands its ships back to the main pool.
        private const float ASSEMBLE_RELEASE_SECONDS = 45f;
        // The centre group may grow to about half the ships outside expansion detachments, never below two.
        private const float CENTRE_GROUP_SHARE = 0.5f;
        private const int MIN_CENTRE_GROUP = 2;
        // At most one expansion detachment per two ships.
        private const int SHIPS_PER_EXPANSION_GROUP = 2;
        private static readonly EnemyOpeningFocus[] OPENING_FOCUSES =
            { EnemyOpeningFocus.Centre, EnemyOpeningFocus.Neighbour, EnemyOpeningFocus.Outer };
        private readonly EnemyStrategicContextBuilder _contextBuilder;
        private readonly IUnitOrderService _orders;
        private readonly List<EnemyShipTaskGroup> _groups = new List<EnemyShipTaskGroup>();
        private readonly List<IShipEntity> _unassigned = new List<IShipEntity>();
        private readonly HashSet<IShipEntity> _members = new HashSet<IShipEntity>();
        private readonly List<EnemyShipTaskGroup> _captureGroups = new List<EnemyShipTaskGroup>();
        private readonly HashSet<ReinforcementZonePresenter> _assignedExpansionZones = new HashSet<ReinforcementZonePresenter>();
        private EnemyShipTaskGroup _mainGroup;
        private readonly EnemyShipTaskGroup _idleGroup;
        private readonly EnemyStrategicContextBuilder _captureProbe;

        public IReadOnlyList<EnemyShipTaskGroup> Groups => _groups;
        /// <summary>Mining zones ships may expand to this cycle; production sizes capture wings from it.</summary>
        public int EligibleExpansionZoneCount { get; private set; }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private const float EXPANSION_IDLE_REPORT_INTERVAL = 20f;
        private float _nextExpansionIdleReport;
#endif

        public EnemyShipTaskGroupPlanner(EnemyStrategicContextBuilder contextBuilder,
            EnemyStrategicDecisionModel decisionModel, EnemyTaskForceExecutor executor,
            IUnitOrderService orders)
        {
            _contextBuilder = contextBuilder;
            _contextBuilder.AssignedExpansionZones = _assignedExpansionZones;
            _orders = orders;
            _idleGroup = new EnemyShipTaskGroup(contextBuilder, decisionModel, executor);
            _captureProbe = contextBuilder.CreateGroupBuilder();
            _groups.Add(_idleGroup);
        }

        public void Reconcile(IReadOnlyList<IShipEntity> ships, bool stationPressure = false)
        {
            _members.Clear();
            foreach (IShipEntity ship in ships) _members.Add(ship);
            foreach (EnemyShipTaskGroup group in _groups)
                group.Ships.RemoveAll(ship => !_members.Contains(ship));
            _captureProbe.BeginCaptureProbe(null);
            for (int i = _captureGroups.Count - 1; i >= 0; i--)
            {
                EnemyShipTaskGroup captureGroup = _captureGroups[i];
                bool assembledTooLong = captureGroup.NoViableCaptureSince >= 0f &&
                    Time.time - captureGroup.NoViableCaptureSince >= ASSEMBLE_RELEASE_SECONDS;
                if (captureGroup.ContextBuilder.ExpansionZone is ReinforcementZonePresenter zone)
                {
                    if (_captureProbe.HasExpansionObjective(zone) && !assembledTooLong) continue;
                    _assignedExpansionZones.Remove(zone);
                    _captureProbe.RecordExpansionReleased(zone, captureGroup.Ships.Count);
                }
                else if (captureGroup.ContextBuilder.OpeningFocus == EnemyOpeningFocus.Centre &&
                    !stationPressure && captureGroup.Ships.Count != 0 && !assembledTooLong &&
                    _captureProbe.HasCentreObjective()) continue;
                else if (!stationPressure && captureGroup.Ships.Count != 0 &&
                    !(captureGroup.NoViableCaptureSince >= 0f &&
                      Time.time - captureGroup.NoViableCaptureSince >= DETACHMENT_GRACE_SECONDS)) continue;
                _groups.Remove(captureGroup);
                captureGroup.Ships.Clear();
                _captureGroups.RemoveAt(i);
            }

            _members.Clear();
            foreach (EnemyShipTaskGroup group in _groups)
            {
                if (group.Ships.Count == 0) continue;
                // Keep travel companions stable until they are clearly outside the cohesion envelope.
                Vector3 anchor = group.Ships[0].WorldPosition;
                for (int i = group.Ships.Count - 1; i > 0; i--)
                    if (!group.IsCaptureDetachment && group != _mainGroup && DistanceSquared(anchor, group.Ships[i].WorldPosition) >
                        4f * FleetCohesion.RADIUS * FleetCohesion.RADIUS)
                        group.Ships.RemoveAt(i);
                foreach (IShipEntity ship in group.Ships) _members.Add(ship);
            }
            foreach (IShipEntity ship in ships)
            {
                if (_members.Contains(ship)) continue;
                EnemyShipTaskGroup closest = null;
                float bestDistance = FleetCohesion.RADIUS * FleetCohesion.RADIUS;
                foreach (EnemyShipTaskGroup group in _groups)
                {
                    if (group.IsCaptureDetachment) continue;
                    float distance = group.Ships.Count == 0 ? bestDistance :
                        DistanceSquared(group.Ships[0].WorldPosition, ship.WorldPosition);
                    if (distance > bestDistance) continue;
                    closest = group;
                    bestDistance = distance;
                }
                if (closest == null)
                {
                    closest = CreateGroup(false);
                    _groups.Add(closest);
                }
                closest.Ships.Add(ship);
            }
            MergeNearbyGroups();
            AssembleMainForce(stationPressure);
            _groups.RemoveAll(group => group.Ships.Count == 0);
            if (_mainGroup != null && !_groups.Contains(_mainGroup))
            {
                _mainGroup.IsMainForce = false;
                _mainGroup = null;
            }
            if (_groups.Count == 0) _groups.Add(_idleGroup);
        }

        private void AssembleMainForce(bool stationPressure)
        {
            int available = 0;
            foreach (EnemyShipTaskGroup group in _groups)
                if (!group.IsCaptureDetachment)
                    available += group.Ships.Count;
            if (_mainGroup == null && (stationPressure || available >= MAIN_FORCE_SHIP_COUNT))
            {
                foreach (EnemyShipTaskGroup group in _groups)
                    if (!group.IsCaptureDetachment &&
                        (_mainGroup == null || group.Ships.Count > _mainGroup.Ships.Count))
                        _mainGroup = group;
                if (_mainGroup != null) _mainGroup.IsMainForce = true;
            }
            if (_mainGroup == null) return;
            bool mainFighting = IsFighting(_mainGroup);
            for (int i = _groups.Count - 1; i >= 0; i--)
            {
                EnemyShipTaskGroup group = _groups[i];
                if (group == _mainGroup || group.IsCaptureDetachment) continue;
                // No ship flies alone into a fight: far reinforcements gather on their own until the main force is
                // free or back at the gathering point.
                if (_mainGroup.Ships.Count > 0 && group.Ships.Count > 0 && (mainFighting || IsFighting(group)) &&
                    DistanceSquared(_mainGroup.Ships[0].WorldPosition, group.Ships[0].WorldPosition) >
                    4f * FleetCohesion.RADIUS * FleetCohesion.RADIUS) continue;
                _mainGroup.Ships.AddRange(group.Ships);
                group.Ships.Clear();
                _groups.RemoveAt(i);
            }
            _groups.Remove(_mainGroup);
            _groups.Insert(0, _mainGroup);
        }

        private static bool IsFighting(EnemyShipTaskGroup group) =>
            group.Context != null && group.Context.Snapshot.HasLocalThreat;

        public void TryCreateCaptureGroup(IReadOnlyList<IShipEntity> ships,
            ISet<string> reservedObjectives, bool stationPressure)
        {
            if (stationPressure || ships.Count == 0) return;
            _unassigned.Clear();
            foreach (IShipEntity ship in ships)
                if (ship.CurrentOrder != UnitOrderType.Retreat && !IsInCaptureGroup(ship) &&
                    !IsInAssaultGroup(ship))
                    _unassigned.Add(ship);
            _unassigned.Sort((a, b) => b.NavigationSpeed.CompareTo(a.NavigationSpeed));
            _captureProbe.BeginCaptureProbe(reservedObjectives);
            // Allied AIs see whole expansion zones as taken, not only their individual sites.
            foreach (ReinforcementZonePresenter zone in _assignedExpansionZones)
                reservedObjectives.Add(_captureProbe.GetExpansionReservationId(zone));
            if (_assignedExpansionZones.Count > 0 &&
                !_captureGroups.Exists(group => group.ContextBuilder.OpeningFocus == EnemyOpeningFocus.Centre) &&
                TryDetachForCapture(EnemyOpeningFocus.Centre, ships.Count)) return;
            // A centre reinforcement and an expansion ship can both be assigned in one cycle.
            bool reinforced = TryReinforceCentreGroup(ships.Count);
            if (TryAssignExpansionShip(ships.Count)) return;
            if (reinforced) return;
            if (_mainGroup != null && _captureGroups.Count >= OPENING_FOCUSES.Length) return;
            foreach (EnemyOpeningFocus focus in OPENING_FOCUSES)
            {

                if (_captureGroups.Exists(group => group.ContextBuilder.OpeningFocus == focus)) continue;
                if (TryDetachForCapture(focus, ships.Count)) return;
            }
            if (_captureGroups.Count == 0) TryDetachForCapture(EnemyOpeningFocus.None, ships.Count);
        }

        private bool TryReinforceCentreGroup(int shipCount)
        {
            EnemyShipTaskGroup centreGroup = _captureGroups.Find(group =>
                group.ContextBuilder.OpeningFocus == EnemyOpeningFocus.Centre);
            if (centreGroup == null) return false;
            int expansionShips = 0;
            foreach (EnemyShipTaskGroup group in _captureGroups)
                if (group.ContextBuilder.ExpansionZone != null) expansionShips += group.Ships.Count;
            int cap = Mathf.Max(MIN_CENTRE_GROUP, Mathf.CeilToInt(CENTRE_GROUP_SHARE * (shipCount - expansionShips)));
            // At the cap a stronger centre is not fed ship by ship: the group waits for the main force.
            centreGroup.ContextBuilder.CentreGroupAtCap = centreGroup.Ships.Count >= cap;
            if (centreGroup.ContextBuilder.CentreGroupAtCap || !centreGroup.ContextBuilder.NeedsCentreReinforcement)
                return false;
            foreach (IShipEntity ship in _unassigned)
            {
                if (_mainGroup != null && _mainGroup.Ships.Contains(ship) && _mainGroup.Ships.Count <= 3)
                    continue;
                foreach (EnemyShipTaskGroup group in _groups) group.Ships.Remove(ship);
                centreGroup.Ships.Add(ship);
                _unassigned.Remove(ship);
                return true;
            }
            return false;
        }

        private bool TryAssignExpansionShip(int shipCount)
        {
            ReinforcementZonePresenter bestZone = null;
            EnemyShipTaskGroup bestGroup = null;
            IShipEntity bestShip = null;
            float bestEta = float.PositiveInfinity;
            int bestRequired = 0;
            int expansionGroups = 0;
            foreach (EnemyShipTaskGroup group in _captureGroups)
                if (group.ContextBuilder.ExpansionZone != null) expansionGroups++;
            bool canOpenGroup = expansionGroups < Mathf.CeilToInt(shipCount / (float)SHIPS_PER_EXPANSION_GROUP);
            EligibleExpansionZoneCount = 0;
            foreach (ReinforcementZonePresenter zone in _captureProbe.ExpansionZones)
            {
                if (zone.IsMain || !zone.IsCapturable || !_captureProbe.HasMiningSites(zone) ||
                    !_captureProbe.HasExpansionObjective(zone)) continue;
                // Enemy-side zones are raided by wings until the team holds the centre.
                if (!_captureProbe.IsShipExpansionEligible(zone)) continue;
                EnemyShipTaskGroup group = _captureGroups.Find(candidate => candidate.ContextBuilder.ExpansionZone == zone);
                if (group == null && _captureProbe.IsExpansionZoneReservedByAlly(zone)) continue;
                EligibleExpansionZoneCount++;
                if (group == null && !canOpenGroup) continue;
                foreach (IShipEntity ship in _unassigned)
                {
                    int required = _captureProbe.GetRequiredExpansionShips(zone, group?.Ships, ship, out float eta);
                    if (required == 0 || (group != null && group.Ships.Count >= required &&
                        (group.NoViableCaptureSince < 0f || group.Ships.Count >= required + 1)) ||
                        eta >= bestEta) continue;
                    if (_mainGroup != null && _mainGroup.Ships.Contains(ship) && _mainGroup.Ships.Count <= 3)
                        continue;
                    bestZone = zone;
                    bestGroup = group;
                    bestShip = ship;
                    bestEta = eta;
                    bestRequired = required;
                }
            }
            if (bestShip == null)
            {
                RecordExpansionIdle(canOpenGroup);
                return false;
            }
            if (bestGroup == null)
            {
                bestGroup = CreateGroup(true);
                bestGroup.ContextBuilder.ExpansionZone = bestZone;
                _assignedExpansionZones.Add(bestZone);
                _captureGroups.Add(bestGroup);
                _groups.Add(bestGroup);
            }
            foreach (EnemyShipTaskGroup group in _groups) group.Ships.Remove(bestShip);
            bestGroup.Ships.Add(bestShip);
            _captureProbe.RecordExpansionAssigned(bestZone, bestShip, bestEta, bestRequired, bestGroup.Ships.Count);
            return true;
        }

        /// <summary>Why no ship was sent to each outer zone this cycle; at most one report per interval.</summary>
        private void RecordExpansionIdle(bool canOpenGroup)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Time.time < _nextExpansionIdleReport) return;
            _nextExpansionIdleReport = Time.time + EXPANSION_IDLE_REPORT_INTERVAL;
            foreach (ReinforcementZonePresenter zone in _captureProbe.ExpansionZones)
            {
                if (zone.IsMain || !zone.IsCapturable) continue;
                EnemyShipTaskGroup group = _captureGroups.Find(candidate => candidate.ContextBuilder.ExpansionZone == zone);
                float eta = float.PositiveInfinity;
                int required = _unassigned.Count == 0 ? -1 :
                    _captureProbe.GetRequiredExpansionShips(zone, group?.Ships, _unassigned[0], out eta);
                _captureProbe.RecordExpansionIdle(zone, System.FormattableString.Invariant(
                    $"mining={_captureProbe.HasMiningSites(zone)};objective={_captureProbe.HasExpansionObjective(zone)};eligible={_captureProbe.IsShipExpansionEligible(zone)};reservedByAlly={_captureProbe.IsExpansionZoneReservedByAlly(zone)};groupShips={(group == null ? -1 : group.Ships.Count)};groupNoViableSince={(group == null ? -1f : group.NoViableCaptureSince):F1};canOpenGroup={canOpenGroup};unassigned={_unassigned.Count};mainShips={(_mainGroup == null ? -1 : _mainGroup.Ships.Count)};required={required};eta={eta:F1}"));
            }
#endif
        }

        private bool IsInAssaultGroup(IShipEntity ship) =>
            _mainGroup != null && _mainGroup.Decision.State == EnemyStrategicState.AssaultBase &&
            _mainGroup.Ships.Contains(ship);

        private bool TryDetachForCapture(EnemyOpeningFocus focus, int shipCount)
        {
            foreach (IShipEntity ship in _unassigned)
            {
                bool mineOnly = shipCount == 1 && focus == EnemyOpeningFocus.None;
                if (!_captureProbe.SetOpeningFocus(focus, ship.WorldPosition)) continue;
                if (!_captureProbe.CanDetachForCapture(ship, mineOnly)) continue;
                EnemyShipTaskGroup candidate = CreateGroup(true, unbuiltMinesOnly: mineOnly);
                candidate.ContextBuilder.SetOpeningFocus(focus, ship.WorldPosition);
                candidate.Ships.Add(ship);
                foreach (EnemyShipTaskGroup group in _groups) group.Ships.Remove(ship);
                candidate.NoViableCaptureSince = -1f;
                _captureGroups.Add(candidate);
                _groups.Add(candidate);
                return true;
            }
            return false;
        }

        internal static ReinforcementZonePresenter GetCentre(IReadOnlyList<ReinforcementZonePresenter> zones)
        {
            foreach (ReinforcementZonePresenter zone in zones)
                if (zone.IsMain) return zone;
            return null;
        }

        internal static ReinforcementZonePresenter GetNeighbour(IReadOnlyList<ReinforcementZonePresenter> zones,
            Vector3 home, ReinforcementZonePresenter exclude = null)
        {
            ReinforcementZonePresenter closest = null;
            float distance = float.PositiveInfinity;
            foreach (ReinforcementZonePresenter zone in zones)
            {
                if (!zone.IsCapturable || zone.IsMain || zone == exclude) continue;
                float candidate = DistanceSquared(home, zone.Center);
                if (candidate >= distance) continue;
                closest = zone;
                distance = candidate;
            }
            return closest;
        }

        internal static bool IsInZone(ReinforcementZonePresenter zone, Vector3 position) =>
            DistanceSquared(zone.Center, position) <= 4f * zone.Radius * zone.Radius;

        internal static Vector3 GetOpeningHome(AiWorldSnapshot world, PlayerId owner, Vector3 fallback)
        {
            foreach (AiWorldSnapshot.Entry station in world.Bases)
                if (station.Owner == owner) return station.Position;
            return fallback;
        }

        private bool IsInCaptureGroup(IShipEntity ship)
        {
            foreach (EnemyShipTaskGroup group in _captureGroups)
                if (group.Ships.Contains(ship)) return true;
            return false;
        }

        private void MergeNearbyGroups()
        {
            for (int i = 0; i < _groups.Count; i++)
            {
                EnemyShipTaskGroup first = _groups[i];
                if (first.IsCaptureDetachment || first.Ships.Count == 0) continue;
                for (int j = _groups.Count - 1; j > i; j--)
                {
                    EnemyShipTaskGroup second = _groups[j];
                    if (second == _mainGroup) continue;
                    if (second.IsCaptureDetachment || second.Ships.Count == 0 ||
                        first.Decision.State != second.Decision.State) continue;
                    bool fits = true;
                    foreach (IShipEntity ship in second.Ships)
                        fits &= DistanceSquared(first.Ships[0].WorldPosition, ship.WorldPosition) <=
                            FleetCohesion.RADIUS * FleetCohesion.RADIUS;
                    if (!fits) continue;
                    first.Ships.AddRange(second.Ships);
                    second.Ships.Clear();
                    _groups.RemoveAt(j);
                }
            }
        }

        private EnemyShipTaskGroup CreateGroup(bool capture, bool unbuiltMinesOnly = false) => new EnemyShipTaskGroup(
            _contextBuilder.CreateGroupBuilder(), new EnemyStrategicDecisionModel(),
            new EnemyTaskForceExecutor(_orders), capture, unbuiltMinesOnly);

        private static float DistanceSquared(Vector3 first, Vector3 second)
        {
            Vector3 offset = first - second;
            return offset.x * offset.x + offset.z * offset.z;
        }
    }
}
