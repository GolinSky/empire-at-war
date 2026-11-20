using System;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Controllers.Game;
using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Components.Movement.Formation;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;
using EmpireAtWar.Entities.EnemyFaction.Models.Intel;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Services.ReinforcementZones;
using UnityEngine;
using VContainer;
using EmpireAtWar.Lifecycle;
using static EmpireAtWar.Utils.FormationConversion;
using EmpireAtWar.Services.TacticalMarkers;
using EmpireAtWar.Services.Vision;
using EmpireAtWar.Presenters.ReinforcementZones;

namespace EmpireAtWar.Services.Enemy
{
    /// <summary>
    /// Orders AI wings. Station-launched and bought wings are free and form groups (<see cref="EnemyWingGroup"/>):
    /// group A takes and holds the centre; one group B per outer zone takes that zone (2 wings to the nearer one
    /// when wings are cheap, 1 otherwise), then the next outer zone, then raids hostile mines. Groups land on mining
    /// spots, where the mine and the satellite pads in reach flip together, and keep their zone until it is taken
    /// or they fall below half of their peak size.
    /// Carrier wings stay with their carrier and are the only wings used as escorts against hostile strikecraft.
    /// Wings without a group zone are spread over all capture targets; they hunt when nothing is left.
    /// </summary>
    public sealed class EnemySquadronCommander : IEnemySquadronCommander, IInitializable, ITickable,
        ILateDisposable, IObserver<BattleState>
    {
        private const float DECISION_INTERVAL = 3f;

        /// <summary>Escorts stop being added once they match the hostile strikecraft (advantage 1).</summary>
        private const float EVEN_FIGHT = 1f;

        /// <summary>Hostile strikecraft within this planar distance of an own ship count toward its escort need.</summary>
        private const float ESCORT_THREAT_RADIUS = 2000f;
        private const int OPENING_CAPTURE_WINGS = 3;
        /// <summary>Group A gives one wing to the patrol only while it keeps more than this many.</summary>
        private const int PATROL_SPARE_GROUP_WINGS = 4;
        /// <summary>The patrol wing reacts to hostile ships this close (its vision plus known intel).</summary>
        private const float PATROL_SIGHT_RADIUS = 1200f;
        private const float BEACON_STEP = 200f;
        private const float BEACON_RELAY_CLEARANCE = 1000f;
        private const float BEACON_SHIP_CLEARANCE = 1200f;
        private const float BEACON_HOLD_SECONDS = 60f;
        private const float BEACON_ARRIVED_RADIUS = 150f;
        /// <summary>A lone outer wing that died twice at its zone within this window leaves the zone for as long.</summary>
        private const float LONE_WING_DEATH_WINDOW = 180f;
        private const int LONE_WING_DEATHS = 2;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private const float WING_ASSIGNMENT_REPORT_INTERVAL = 6f;
        private string _lastWingAssignment;
        private float _nextWingAssignmentReport;
#endif
        /// <summary>
        /// A wing on an uncontested mining spot switches from attack-move to move within this many spot radii, so it
        /// settles inside the circle instead of drifting out to fight.
        /// </summary>
        private const float HOLD_ENTRY_SCALE = 2f;
        // Matches the ship capture rule: sites this close to an allied station are taken last.
        private const float ALLIED_BASE_SITE_RADIUS = 1000f;

        private readonly IReadOnlyList<IEnemyCaptureObjectiveSource> _captureSources;
        private readonly IReinforcementZoneSource _zones;
        private readonly IEntityLocator _entityLocator;
        private readonly IPlayerRoster _playerRoster;
        private readonly ForceCompositionBuilder _forceBuilder;
        private readonly UnitCombatProfileCatalog _profileCatalog;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public EnemyDecisionDiagnostics PeriodicDiagnostics { get; }
        public EnemyDecisionDiagnostics LaunchDiagnostics { get; }
#endif

        private readonly PlayerSlot _owner;
        private readonly Dictionary<ISquadron, CommandedSquadron> _squadrons =
            new Dictionary<ISquadron, CommandedSquadron>();
        private readonly List<ISquadron> _orderBuffer = new List<ISquadron>();
        private readonly List<ISquadron> _captureSquadrons = new List<ISquadron>();
        private readonly EnemySquadronCaptureAssignments _captureAssignments = new EnemySquadronCaptureAssignments();
        private readonly ForceComposition _hostileStrikecraft = new ForceComposition();
        private readonly ForceComposition _hostileShips = new ForceComposition();
        private readonly ForceComposition _squadronForce = new ForceComposition();
        private readonly ForceComposition _escortForce = new ForceComposition();
        private readonly List<IEntity> _escortTargets = new List<IEntity>();
        private readonly List<Vector3> _ownShipPositions = new List<Vector3>();
        private readonly HashSet<ISquadron> _escorts = new HashSet<ISquadron>();
        private readonly List<KeyValuePair<ISquadron, float>> _escortCandidates =
            new List<KeyValuePair<ISquadron, float>>();
        private readonly EnemyWingGroup _centreGroup = new EnemyWingGroup(EnemyWingRole.Centre);
        // One group per outer zone, nearest home first.
        private readonly List<EnemyWingGroup> _outerGroups = new List<EnemyWingGroup>();
        private readonly ForceComposition _groupForce = new ForceComposition();
        private readonly ForceComposition _zoneDefenders = new ForceComposition();
        private readonly List<ISquadron> _groupWings = new List<ISquadron>();
        private readonly List<ISquadron> _scoutPool = new List<ISquadron>();
        private readonly ForceComposition _raidForce = new ForceComposition();
        private readonly ForceComposition _raidDefenders = new ForceComposition();
        private readonly List<ISquadron> _raidWings = new List<ISquadron>();
        private readonly Predicate<ISquadron> _leftGroup;
        private int _outerWingCount = 1;
        private bool _wingPatrol;
        private ISquadron _patrolWing;
        private Vector3? _beacon;
        private float _beaconUntil;
        private readonly List<Vector3> _seenShips = new List<Vector3>();
        private readonly List<ISquadron> _idleBombers = new List<ISquadron>();
        // Bombers without a winnable mine hold at the centre of the nearest own held zone.
        private readonly Dictionary<ISquadron, Vector3> _bomberHolds = new Dictionary<ISquadron, Vector3>();

        private readonly IEnemyStructurePlacementService _structurePlacement;
        private readonly HostileIntelModel _intel;
        private readonly EnemyPatrolService _patrol;
        private readonly EnemyPatrolSafety _patrolSafety;
        private readonly INotifier<BattleState> _battleState;
        private readonly AiWorldSnapshot _worldSnapshot;
        private readonly EnemyAiScheduler _scheduler;
        private readonly IVisionService _vision;

        private EnemyAiScheduler.WorkHandle _workHandle;
        private bool _isRunning;

        private Vector3? _stationDefense;

        public void SetStationDefense(Vector3? position) => _stationDefense = position;

        public void SetOuterWingCount(int count) => _outerWingCount = count;

        public void SetWingPatrol(bool enabled) => _wingPatrol = enabled;

        public bool NeedsPatrolWing => _wingPatrol && _patrolWing == null && !_stationDefense.HasValue;

        public bool TryGetSpawnBeacon(out Vector3 position)
        {
            position = _beacon ?? default;
            return _patrolWing != null && _beacon.HasValue &&
                   PlanarDistanceSquared(_patrolWing.WorldPosition, position) <=
                   BEACON_ARRIVED_RADIUS * BEACON_ARRIVED_RADIUS;
        }

        private int _nextEscortTarget;

        /// <summary>Squadron revealing fog because no build space is visible; null when none is needed.</summary>
        private ISquadron _scout;

        private Vector3 _scoutTarget;
        private readonly Dictionary<ISquadron, HostileSighting> _raids =
            new Dictionary<ISquadron, HostileSighting>();
        private readonly HashSet<long> _reservedRaidTargets = new HashSet<long>();
        private readonly Action<Vector3, ForceComposition> _fillTargetDefenders;
        private readonly Action<ISquadron, ForceComposition> _addWingForce;
        private readonly Predicate<HostileSighting> _includeTargetDefender;
        private Vector3 _ratedTarget;

        public EnemySquadronCommander(
            IReadOnlyList<IEnemyCaptureObjectiveSource> captureSources,
            IReinforcementZoneSource zones,
            IEntityLocator entityLocator,
            IPlayerRoster playerRoster,
            ForceCompositionBuilder forceBuilder,
            UnitCombatProfileCatalog profileCatalog,
            IEnemyStructurePlacementService structurePlacement,
            TeamIntelRegistry intelRegistry,
            PlayerSlot owner, EnemyPatrolService patrol, EnemyPatrolSafety patrolSafety,
            INotifier<BattleState> battleState, AiWorldSnapshot worldSnapshot,
            EnemyAiScheduler scheduler, IVisionService vision)
        {
            _owner = owner;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            PeriodicDiagnostics = new EnemyDecisionDiagnostics("EnemyAI.Squadron.Periodic." + owner.Id);
            LaunchDiagnostics = new EnemyDecisionDiagnostics("EnemyAI.Squadron.Launch." + owner.Id);
#endif
            _patrol = patrol;
            _patrolSafety = patrolSafety;
            _battleState = battleState;
            _worldSnapshot = worldSnapshot;
            _scheduler = scheduler;
            _vision = vision;
            _intel = intelRegistry.Get(owner.Team);
            _structurePlacement = structurePlacement;
            _captureSources = captureSources;
            _zones = zones;
            _entityLocator = entityLocator;
            _playerRoster = playerRoster;
            _forceBuilder = forceBuilder;
            _profileCatalog = profileCatalog;
            _fillTargetDefenders = FillTargetDefenders;
            _addWingForce = AddWingForce;
            _includeTargetDefender = IncludeTargetDefender;
            _leftGroup = wing => !_squadrons.TryGetValue(wing, out CommandedSquadron commanded) || !IsFree(commanded);
        }

        public void Initialize()
        {
            float initialDelay = EnemyAiStagger.Offset(_owner.Id, DECISION_INTERVAL);
            _workHandle = _scheduler.Register(_owner.Id, DECISION_INTERVAL, initialDelay, RunPeriodicPass);
            _battleState.AddObserver(this);
        }

        public void UpdateState(BattleState state)
        {
            _isRunning = state == BattleState.Running;
        }

        public void LateDispose()
        {
            if (_workHandle != null)
            {
                _scheduler.Unregister(_workHandle);
                _workHandle = null;
            }
            _battleState.RemoveObserver(this);
            _isRunning = false;
            foreach (KeyValuePair<ISquadron, CommandedSquadron> pair in _squadrons)
            {
                pair.Key.Released -= pair.Value.ReleaseHandler;
            }

            _squadrons.Clear();
        }

        public void Command(ISquadron squadron, SquadronType squadronType)
        {
            Track(squadron, squadronType);
            if (!_isRunning) return;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            LaunchDiagnostics.Record();
            using var marker = LaunchDiagnostics.Marker.Auto();
#endif

            Decide();
            foreach (ISquadron wing in _squadrons.Keys)
                IssueOrder(wing);
        }

        public void Tick()
        {
            // Player scopes tick while the asynchronous map load is still in progress.
            if (!_isRunning) return;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            PeriodicDiagnostics.Tick(Time.deltaTime);
            LaunchDiagnostics.Tick(Time.deltaTime);
#endif
        }

        private void RunPeriodicPass()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            PeriodicDiagnostics.Record();
            using var marker = PeriodicDiagnostics.Marker.Auto();
#endif
            _patrol.Sample(Time.time);
            TrackOwnedSquadrons();
            Decide();
            _orderBuffer.Clear();
            _orderBuffer.AddRange(_squadrons.Keys);
            foreach (ISquadron squadron in _orderBuffer)
            {
                IssueOrder(squadron);
            }
        }

        private void Decide()
        {
            AssessThreats();
            CollectCaptureTargets();
            UpdateGroups();
            AssignRaiders();
            AssignEscorts();
            AssignCaptureTargets();
        }

        /// <summary>
        /// Wings appearing in the world without a purchase were launched from a hangar. Wings launched beside the own
        /// station are free from the start; wings launched beside a carrier stay with it.
        /// </summary>
        private void TrackOwnedSquadrons()
        {
            IReadOnlyList<AiWorldSnapshot.Entry> owned = _worldSnapshot.GetOwnedUnits(_owner.Id);
            for (int i = 0; i < owned.Count; i++)
            {
                AiWorldSnapshot.Entry entry = owned[i];
                if (!entry.UnitTypeId.IsSquadron ||
                    !entry.Entity.TryGetFacade(out ISquadron squadron) || _squadrons.ContainsKey(squadron)) continue;
                if (IsBesideOwnStation(entry.Position))
                    Track(squadron, entry.UnitTypeId.SquadronType);
                else
                    Track(squadron, entry.UnitTypeId.SquadronType,
                        entry.Entity.GetFacade<IUnitOrderObserverFacade>());
            }
        }

        /// <summary>An own station is nearer than every own ship: the wing came from the station hangar.</summary>
        private bool IsBesideOwnStation(Vector3 position)
        {
            float station = float.PositiveInfinity;
            foreach (AiWorldSnapshot.Entry entry in _worldSnapshot.Bases)
                if (entry.Owner == _owner.Id)
                    station = Mathf.Min(station, PlanarDistanceSquared(entry.Position, position));
            if (float.IsPositiveInfinity(station)) return false;
            IReadOnlyList<AiWorldSnapshot.Entry> owned = _worldSnapshot.GetOwnedUnits(_owner.Id);
            for (int i = 0; i < owned.Count; i++)
                if (owned[i].UnitTypeId.IsShip && !owned[i].IsPlayerBase &&
                    PlanarDistanceSquared(owned[i].Position, position) < station) return false;
            return true;
        }

        /// <summary>A wing takes commander orders unless it is a hangar wing still guarding its carrier.</summary>
        private static bool IsFree(CommandedSquadron commanded) =>
            !commanded.IsHangarLaunched || commanded.Orders.CurrentOrder != UnitOrderType.Guard;

        private void Track(ISquadron squadron, SquadronType squadronType)
        {
            if (_squadrons.ContainsKey(squadron))
            {
                return;
            }

            Action handler = () => Release(squadron);
            _squadrons.Add(squadron, new CommandedSquadron(
                _profileCatalog.Get(UnitTypeId.Squadron(squadronType)), handler));
            squadron.Released += handler;
        }

        private void Track(ISquadron squadron, SquadronType squadronType, IUnitOrderObserverFacade orders)
        {
            if (_squadrons.ContainsKey(squadron))
            {
                return;
            }

            Action handler = () => Release(squadron);
            _squadrons.Add(squadron, new CommandedSquadron(
                _profileCatalog.Get(UnitTypeId.Squadron(squadronType)), handler, orders));
            squadron.Released += handler;
        }

        /// <summary>
        /// One non-escort squadron scouts: first for build space when none is visible, otherwise to refresh the
        /// team's most valuable stale intel, or to find the enemy at its nearest base when nothing is known.
        /// </summary>
        private void AssignScout(int captureWings)
        {
            ISquadron previous = _scout;
            _scout = null;
            // Ungrouped wings scout when they are surplus. Group wings scout only for build space, while group A
            // keeps at least one wing capturing.
            _scoutPool.Clear();
            _scoutPool.AddRange(_captureSquadrons);
            int keep = captureWings;
            if (_scoutPool.Count == 0 && !_structurePlacement.TryGetPosition(out _) &&
                _structurePlacement.TryGetScoutTarget(out _))
            {
                foreach (ISquadron wing in _centreGroup.Members)
                    if (!_raids.ContainsKey(wing)) _scoutPool.Add(wing);
                keep = 1;
            }
            if (_scoutPool.Count == 0 ||
                _captureAssignments.HasTargets && _scoutPool.Count <= keep || _stationDefense.HasValue)
            {
                if (previous != null) _patrol.Release($"squadron:{previous.EntityId}");
                return;
            }

            float closest = float.MaxValue;
            foreach (ISquadron squadron in _scoutPool)
            {
                float distance = (squadron.WorldPosition - _scoutTarget).sqrMagnitude;
                if (!_squadrons[squadron].IsHangarLaunched && distance < closest)
                {
                    closest = distance;
                    _scout = squadron;
                }
            }
            if (previous != null && _scoutPool.Contains(previous) && !_squadrons[previous].IsHangarLaunched)
                _scout = previous;
            ISquadron candidate = _scout;
            if (_scout != null && !TryGetScoutTarget(_scout, out _scoutTarget)) _scout = null;
            if (previous != null && previous != _scout && previous != candidate)
                _patrol.Release($"squadron:{previous.EntityId}");
        }

        private bool TryGetScoutTarget(ISquadron scout, out Vector3 target)
        {
            Vector3 origin = scout.WorldPosition;
            foreach (IEnemyCaptureObjectiveSource source in _captureSources)
                if (source is AllyAiMarkerSource markers &&
                    markers.TryGetScoutTarget(out target)) return true;
            if (!_structurePlacement.TryGetPosition(out _) &&
                _structurePlacement.TryGetScoutTarget(out target))
            {
                return true;
            }

            if (_patrol.TryGetTarget($"squadron:{scout.EntityId}", origin,
                    point => _patrolSafety.IsObjectiveSafe(scout.EntityId, origin, point, 0f, false), out target))
            {
                return true;
            }

            target = default;
            return _intel.Sightings.Count == 0 && TryGetNearestHostileBase(out target);
        }

        private bool TryGetNearestHostileBase(out Vector3 position)
        {
            position = default;
            float closest = float.MaxValue;
            Vector3 origin = default;
            foreach (ISquadron squadron in _squadrons.Keys)
            {
                origin = squadron.WorldPosition;
                break;
            }

            // Station locations are part of the map, so the AI may head for them without having seen them.
            IReadOnlyList<AiWorldSnapshot.Entry> bases = _worldSnapshot.Bases;
            for (int i = 0; i < bases.Count; i++)
            {
                AiWorldSnapshot.Entry entry = bases[i];
                if (!_playerRoster.IsHostile(_owner.Id, entry.Owner))
                {
                    continue;
                }

                float distance = (entry.Position - origin).sqrMagnitude;
                if (distance < closest)
                {
                    closest = distance;
                    position = entry.Position;
                }
            }

            return closest < float.MaxValue;
        }

        private void IssueOrder(ISquadron squadron)
        {
            if (_stationDefense.HasValue && !_squadrons[squadron].IsHangarLaunched)
            {
                squadron.AttackMoveTo(_stationDefense.Value);
                return;
            }

            if (squadron == _scout)
            {
                squadron.AttackMoveTo(_scoutTarget);
                return;
            }

            if (squadron == _patrolWing)
            {
                IssuePatrolOrder(squadron);
                return;
            }

            if (_bomberHolds.TryGetValue(squadron, out Vector3 bomberHold))
            {
                squadron.AttackMoveTo(bomberHold);
                return;
            }

            if (_raids.TryGetValue(squadron, out HostileSighting raid) &&
                _entityLocator.TryGetEntity(raid.EntityId, out IEntity raidTarget))
            {
                if (_vision.IsVisible(_owner.Id, raidTarget))
                    _entityLocator.GetEntity(squadron.EntityId).GetFacade<IAttackFacade>()
                        .Attack(raidTarget, Vector3.zero, null);
                else
                    squadron.AttackMoveTo(ToVector(raid.Position));
                return;
            }

            if (_escorts.Contains(squadron))
            {
                // Guard re-issued to the same ship is ignored by the squadron; escorts spread across threatened ships.
                squadron.Guard(_escortTargets[_nextEscortTarget % _escortTargets.Count], Vector3.zero);
                _nextEscortTarget++;
                return;
            }

            // Hangar squadrons fight beside the carrier that launched them; only once their guard ends
            // (the carrier is gone) do they take capture and hunt orders like station purchases.
            CommandedSquadron commanded = _squadrons[squadron];
            if (commanded.IsHangarLaunched && commanded.Orders.CurrentOrder == UnitOrderType.Guard)
            {
                return;
            }

            // Attack-Move re-issued to the same point is ignored by the squadron, so repeats are cheap.
            if (_captureAssignments.TryGetTarget(squadron, out Vector3 target))
            {
                // On an uncontested mining spot the last leg is a move: a wing that stops to fight would drift out.
                float entry = 0f;
                if (_captureAssignments.TryGetHold(squadron, out float holdRadius))
                    entry = HOLD_ENTRY_SCALE * holdRadius;
                if (PlanarDistanceSquared(squadron.WorldPosition, target) <= entry * entry)
                    _entityLocator.GetEntity(squadron.EntityId).GetFacade<IMoveFacade>().MoveTo(target);
                else
                    squadron.AttackMoveTo(target);
            }
            else if (_captureAssignments.HasTargets)
            {
                squadron.AttackMoveTo(EnemyShipTaskGroupPlanner.GetOpeningHome(
                    _worldSnapshot, _owner.Id, squadron.WorldPosition));
            }
            else
            {
                squadron.Hunt();
            }
        }

        /// <summary>
        /// Rebuilds the hostile strikecraft and ship forces and orders own ships by how much damage
        /// the hostile strikecraft deal to their class.
        /// </summary>
        private void AssessThreats()
        {
            PlayerId self = _owner.Id;
            float now = Time.time;
            _ownShipPositions.Clear();
            IReadOnlyList<AiWorldSnapshot.Entry> owned = _worldSnapshot.GetOwnedUnits(self);
            for (int i = 0; i < owned.Count; i++)
            {
                if (owned[i].UnitTypeId.IsShip)
                    _ownShipPositions.Add(owned[i].Position);
            }

            // Only strikecraft that can reach an own ship need escorts; distant wings are not a local threat.
            _forceBuilder.BuildKnown(_hostileStrikecraft, _intel, now, sighting =>
                sighting.UnitTypeId.IsSquadron && _playerRoster.IsHostile(self, sighting.Owner) &&
                IsNearOwnShip(ToVector(sighting.Position)));
            _forceBuilder.BuildKnown(_hostileShips, _intel, now, sighting => sighting.UnitTypeId.IsShip);
            _escortTargets.Clear();
            _nextEscortTarget = 0;
            if (_hostileStrikecraft.IsEmpty)
            {
                return;
            }

            for (int i = 0; i < owned.Count; i++)
            {
                AiWorldSnapshot.Entry entry = owned[i];
                if (entry.UnitTypeId.IsShip &&
                    StrikecraftThreat(entry.Entity) > 0f &&
                    HasHostileStrikecraftNear(entry.Position, now))
                {
                    _escortTargets.Add(entry.Entity);
                }
            }

            _escortTargets.Sort((first, second) =>
            {
                int byThreat = StrikecraftThreat(second).CompareTo(StrikecraftThreat(first));
                return byThreat != 0
                    ? byThreat
                    : (second.HealthModel.Hull + second.HealthModel.Shields)
                    .CompareTo(first.HealthModel.Hull + first.HealthModel.Shields);
            });
        }

        private bool IsNearOwnShip(Vector3 position)
        {
            foreach (Vector3 ship in _ownShipPositions)
                if (PlanarDistanceSquared(ship, position) <= ESCORT_THREAT_RADIUS * ESCORT_THREAT_RADIUS) return true;
            return false;
        }

        private bool HasHostileStrikecraftNear(Vector3 position, float now)
        {
            foreach (HostileSighting sighting in _intel.Sightings)
            {
                if (sighting.UnitTypeId.IsSquadron && _playerRoster.IsHostile(_owner.Id, sighting.Owner) &&
                    _intel.GetConfidence(sighting, now) > 0f &&
                    PlanarDistanceSquared(position, ToVector(sighting.Position)) <=
                    ESCORT_THREAT_RADIUS * ESCORT_THREAT_RADIUS)
                    return true;
            }
            return false;
        }

        private static float PlanarDistanceSquared(Vector3 a, Vector3 b)
        {
            Vector3 offset = a - b;
            offset.y = 0f;
            return offset.sqrMagnitude;
        }

        private float StrikecraftThreat(IEntity ship)
        {
            ShipClass shipClass = ship.HealthModel.ShipClass;
            return _hostileStrikecraft.HullDps(shipClass) + _hostileStrikecraft.PiercingDps(shipClass);
        }

        /// <summary>
        /// Picks the best anti-strikecraft carrier wings, in order, until together they match the hostile strikecraft.
        /// Group wings capture and are never escorts.
        /// </summary>
        private void AssignEscorts()
        {
            _escorts.Clear();
            if (_escortTargets.Count == 0)
            {
                return;
            }

            _escortCandidates.Clear();
            foreach (KeyValuePair<ISquadron, CommandedSquadron> pair in _squadrons)
            {
                if (pair.Value.IsHangarLaunched && !_raids.ContainsKey(pair.Key) && !IsGrouped(pair.Key) &&
                    TryRateEscort(pair.Value.Profile, out float timeToClearStrikecraft))
                {
                    _escortCandidates.Add(new KeyValuePair<ISquadron, float>(pair.Key, timeToClearStrikecraft));
                }
            }

            _escortCandidates.Sort((first, second) => first.Value.CompareTo(second.Value));
            _escortForce.Clear();
            foreach (KeyValuePair<ISquadron, float> candidate in _escortCandidates)
            {
                if (CombatMatchup.Advantage(_escortForce, _hostileStrikecraft) >= EVEN_FIGHT)
                {
                    return;
                }

                // Damaged escorts count at their current strength, like ordinary fleet estimates.
                _forceBuilder.AddUnit(_escortForce, _entityLocator.GetEntity(candidate.Key.EntityId));
                _escorts.Add(candidate.Key);
            }
        }

        /// <summary>A squadron suits escort duty when it clears hostile strikecraft faster than hostile ships.</summary>
        private bool TryRateEscort(UnitCombatProfile profile, out float timeToClearStrikecraft)
        {
            _squadronForce.Clear();
            _squadronForce.AddNew(profile, false);
            timeToClearStrikecraft = CombatMatchup.TimeToDestroy(_squadronForce, _hostileStrikecraft);
            return _hostileShips.IsEmpty ||
                   timeToClearStrikecraft <= CombatMatchup.TimeToDestroy(_squadronForce, _hostileShips);
        }

        private void CollectCaptureTargets()
        {
            _captureAssignments.ClearTargets();
            foreach (var zone in _zones.Zones)
            {
                if (zone.IsCapturable && !_playerRoster.IsAllied(zone.Owner, _owner.Id))
                    _captureAssignments.AddTarget(ToVector(ToPoint(zone.Center)));
            }
            foreach (IEnemyCaptureObjectiveSource source in _captureSources)
                source.AddSquadronTargets(_captureAssignments, _intel);
            _captureAssignments.RateTargets(_fillTargetDefenders);
            foreach (AiWorldSnapshot.Entry station in _worldSnapshot.Bases)
                if (station.Owner != _owner.Id && _playerRoster.IsAllied(_owner.Id, station.Owner))
                    _captureAssignments.DeprioritizeNear(station.Position, ALLIED_BASE_SITE_RADIUS);
        }

        /// <summary>
        /// Keeps the wing groups filled. A new free wing goes to the outer groups first: one wing to every outer zone,
        /// then up to the outer wing count for the zone nearest home; every other wing joins group A. Bombers join the
        /// outer group nearest home, the raid group. A lost wing's replacement fills the gap.
        /// </summary>
        private void UpdateGroups()
        {
            Vector3 home = GetHome();
            if (_outerGroups.Count == 0) CreateOuterGroups(home);
            _centreGroup.Members.RemoveAll(_leftGroup);
            foreach (EnemyWingGroup group in _outerGroups)
            {
                group.Members.RemoveAll(_leftGroup);
                UpdateOuterZone(group);
            }
            UpdatePatrolWing();
            foreach (KeyValuePair<ISquadron, CommandedSquadron> pair in _squadrons)
            {
                if (!IsFree(pair.Value) || IsGrouped(pair.Key) || pair.Key == _patrolWing) continue;
                EnemyWingGroup group = pair.Value.Profile.ShipClass == ShipClass.Bomber
                    ? _outerGroups.Count > 0 ? _outerGroups[0] : _centreGroup
                    : SelectGroupWithRoom() ?? _centreGroup;
                group.Members.Add(pair.Key);
            }
            foreach (EnemyWingGroup group in _outerGroups) group.Peak = Mathf.Max(group.Peak, CountCapturers(group));
            _centreGroup.Zone = EnemyShipTaskGroupPlanner.GetCentre(_zones.Zones);
        }

        private void CreateOuterGroups(Vector3 home)
        {
            foreach (ReinforcementZonePresenter zone in _zones.Zones)
                if (zone.IsCapturable && !zone.IsMain) _outerGroups.Add(new EnemyWingGroup(EnemyWingRole.Outer, zone));
            _outerGroups.Sort((left, right) => PlanarDistanceSquared(left.HomeZone.Center, home)
                .CompareTo(PlanarDistanceSquared(right.HomeZone.Center, home)));
        }

        /// <summary>
        /// An outer group keeps its zone while the zone has something to take and the group holds at least half of its
        /// peak size. A taken zone → the next outer zone with targets (one no other group works first); a group below
        /// half → the nearest zone it beats, else it stays. Either way it commits again from its current size.
        /// </summary>
        private void UpdateOuterZone(EnemyWingGroup group)
        {
            int capturers = CountCapturers(group);
            float now = Time.time;
            group.ZoneDeaths.RemoveAll(time => now - time > LONE_WING_DEATH_WINDOW);
            if (group.AbandonedZone != null && now >= group.AbandonedUntil) group.AbandonedZone = null;
            if (group.Zone != null && capturers <= 1 && group.ZoneDeaths.Count >= LONE_WING_DEATHS)
            {
                // A lone wing that keeps dying at its zone takes the next outer zone with targets, else raids.
                group.AbandonedZone = group.Zone;
                group.AbandonedUntil = now + LONE_WING_DEATH_WINDOW;
                EnemyAiTelemetry.RecordEvent(_owner.Id, _owner.Faction, "wingZoneLeft", FormattableString.Invariant(
                    $"zone=({group.Zone.Center.x:F0} {group.Zone.Center.z:F0});deaths={group.ZoneDeaths.Count}"));
                SetZone(group, SelectNextOuterZone(group), capturers);
                return;
            }
            // A home zone already taken and now attacked is defended first, wherever the group works.
            if (group.HomeZone != group.AbandonedZone && IsHeldUnderAttack(group.HomeZone))
            {
                if (group.Zone != group.HomeZone) SetZone(group, group.HomeZone, capturers);
                return;
            }
            bool taken = group.Zone == null || !HasTargetsIn(group.Zone);
            bool weak = capturers * 2 < group.Peak;
            if (!taken && !weak) return;
            SetZone(group, taken ? SelectNextOuterZone(group) : SelectWinnableZone(group) ?? group.Zone, capturers);
        }

        private static void SetZone(EnemyWingGroup group, ReinforcementZonePresenter zone, int capturers)
        {
            if (zone != group.Zone) group.ZoneDeaths.Clear();
            group.Zone = zone;
            group.Peak = capturers;
        }

        /// <summary>
        /// Nothing is left to take in the zone, an own mine or satellite stands in it, and known hostiles or armed
        /// hostile structures are there.
        /// </summary>
        private bool IsHeldUnderAttack(ReinforcementZonePresenter zone)
        {
            if (HasTargetsIn(zone) || !HasOwnStructureIn(zone)) return false;
            _zoneDefenders.Clear();
            FillTargetDefenders(ToVector(ToPoint(zone.Center)), _zoneDefenders);
            return !_zoneDefenders.IsEmpty;
        }

        private bool HasOwnStructureIn(ReinforcementZonePresenter zone)
        {
            IReadOnlyList<AiWorldSnapshot.Entry> structures = _worldSnapshot.RaidStructures;
            for (int i = 0; i < structures.Count; i++)
                if (structures[i].Owner == _owner.Id && EnemyShipTaskGroupPlanner.IsInZone(zone, structures[i].Position))
                    return true;
            return false;
        }

        /// <summary>
        /// The nearest outer zone with targets that no other group works; null when none is left (the group raids).
        /// Two groups never share a zone.
        /// </summary>
        private ReinforcementZonePresenter SelectNextOuterZone(EnemyWingGroup group)
        {
            Vector3 from = (group.Zone ?? group.HomeZone).Center;
            ReinforcementZonePresenter best = null;
            float bestDistance = float.PositiveInfinity;
            foreach (EnemyWingGroup candidate in _outerGroups)
            {
                ReinforcementZonePresenter zone = candidate.HomeZone;
                if (zone == group.AbandonedZone || !HasTargetsIn(zone) || IsWorkedByOther(zone, group)) continue;
                float distance = PlanarDistanceSquared(zone.Center, from);
                if (distance >= bestDistance) continue;
                best = zone;
                bestDistance = distance;
            }
            return best;
        }

        /// <summary>
        /// The outer zone with targets nearest home, not worked by another group, whose defenders the group at least
        /// matches; null when none.
        /// </summary>
        private ReinforcementZonePresenter SelectWinnableZone(EnemyWingGroup group)
        {
            _groupForce.Clear();
            foreach (ISquadron wing in group.Members) AddWingForce(wing, _groupForce);
            foreach (EnemyWingGroup candidate in _outerGroups)
            {
                ReinforcementZonePresenter zone = candidate.HomeZone;
                if (zone == group.AbandonedZone || !HasTargetsIn(zone) || IsWorkedByOther(zone, group)) continue;
                _zoneDefenders.Clear();
                FillTargetDefenders(ToVector(ToPoint(zone.Center)), _zoneDefenders);
                if (_zoneDefenders.IsEmpty || CombatMatchup.Advantage(_groupForce, _zoneDefenders) >= EVEN_FIGHT)
                    return zone;
            }
            return null;
        }

        private bool IsWorkedByOther(ReinforcementZonePresenter zone, EnemyWingGroup group)
        {
            foreach (EnemyWingGroup other in _outerGroups)
                if (other != group && other.Zone == zone) return true;
            return false;
        }

        /// <summary>
        /// The outer group that still has room: every group to one capturing wing first, then the group of the zone
        /// nearest home up to the outer wing count. A group whose zones are all taken keeps its quota and raids.
        /// Null when every group is full.
        /// </summary>
        private EnemyWingGroup SelectGroupWithRoom()
        {
            for (int level = 1; level <= Mathf.Max(1, _outerWingCount); level++)
                for (int i = 0; i < _outerGroups.Count; i++)
                {
                    EnemyWingGroup group = _outerGroups[i];
                    int quota = i == 0 ? _outerWingCount : 1;
                    if (CountCapturers(group) < Mathf.Min(level, quota)) return group;
                }
            return null;
        }

        private int CountCapturers(EnemyWingGroup group)
        {
            int count = 0;
            foreach (ISquadron wing in group.Members)
                if (_squadrons[wing].Profile.ShipClass != ShipClass.Bomber) count++;
            return count;
        }

        private bool HasTargetsIn(ReinforcementZonePresenter zone) =>
            _captureAssignments.HasTargetIn(ToVector(ToPoint(zone.Center)), 2f * zone.Radius);

        private bool IsGrouped(ISquadron wing)
        {
            if (_centreGroup.Members.Contains(wing)) return true;
            foreach (EnemyWingGroup group in _outerGroups)
                if (group.Members.Contains(wing)) return true;
            return false;
        }

        private Vector3 GetHome()
        {
            Vector3 fallback = default;
            foreach (ISquadron wing in _squadrons.Keys)
            {
                fallback = wing.WorldPosition;
                break;
            }
            return EnemyShipTaskGroupPlanner.GetOpeningHome(_worldSnapshot, _owner.Id, fallback);
        }

        private void AssignCaptureTargets()
        {
            _captureAssignments.BeginAssignment();
            _captureSquadrons.Clear();
            _bomberHolds.Clear();
            Vector3 home = GetHome();
            AssignGroup(_centreGroup, home);
            foreach (EnemyWingGroup group in _outerGroups) AssignGroup(group, home);
            ReinforcementZonePresenter centre = EnemyShipTaskGroupPlanner.GetCentre(_zones.Zones);
            if (_captureSquadrons.Count > 0)
            {
                ReinforcementZonePresenter neighbour = EnemyShipTaskGroupPlanner.GetNeighbour(_zones.Zones, home);
                PrioritizeZone(centre);
                PrioritizeZone(neighbour);
                foreach (ReinforcementZonePresenter zone in _zones.Zones)
                    if (zone.IsCapturable && zone != centre && zone != neighbour) PrioritizeZone(zone);
            }
            int captureWings = Mathf.Max(centre != null ? OPENING_CAPTURE_WINGS : 1,
                _captureAssignments.GetOpeningWingBudget(_captureSquadrons.Count));
            AssignScout(captureWings);
            if (_scout != null) _captureSquadrons.Remove(_scout);
            _captureAssignments.AssignRemaining(_captureSquadrons, _addWingForce);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Which wings go where: the opening race for zones is decided here, before any ship exists.
            string assignment = FormattableString.Invariant(
                $"wings={_squadrons.Count};groupA={_centreGroup.Members.Count};groupB={DescribeOuterGroups()};captureWings={_captureSquadrons.Count};openingBudget={captureWings};escorts={_escorts.Count};raids={_raids.Count};scout={(_scout != null)};targets=") +
                _captureAssignments.Describe();
            if (assignment != _lastWingAssignment && Time.time >= _nextWingAssignmentReport)
            {
                _lastWingAssignment = assignment;
                _nextWingAssignmentReport = Time.time + WING_ASSIGNMENT_REPORT_INTERVAL;
                EnemyAiTelemetry.RecordEvent(_owner.Id, _owner.Faction, "wingAssignment", assignment);
            }
#endif
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Per outer group: wings @ zone worked, or "raid" when it has no zone.
        private string DescribeOuterGroups()
        {
            var text = new System.Text.StringBuilder();
            foreach (EnemyWingGroup group in _outerGroups)
                text.Append(group.Zone != null
                    ? FormattableString.Invariant($"{group.Members.Count}@({group.Zone.Center.x:F0} {group.Zone.Center.z:F0}) ")
                    : $"{group.Members.Count}@raid ");
            return text.ToString();
        }
#endif

        /// <summary>
        /// Assigns a group's wings inside its zone, onto its mining spots. Raiding wings and escorts are left out. A
        /// group without a zone and without a raid works group A's zone together with it; wings are never spread one
        /// per target. Only with no centre on the map do they join the ungrouped spread.
        /// </summary>
        private void AssignGroup(EnemyWingGroup group, Vector3 home)
        {
            _groupWings.Clear();
            _idleBombers.Clear();
            foreach (ISquadron wing in group.Members)
            {
                if (_raids.ContainsKey(wing) || _escorts.Contains(wing)) continue;
                if (_squadrons[wing].Profile.ShipClass == ShipClass.Bomber) _idleBombers.Add(wing);
                else _groupWings.Add(wing);
            }
            HoldIdleBombers(home);
            // The centre fallback (D5) is for fighters and interceptors only.
            ReinforcementZonePresenter zone = group.Zone ?? _centreGroup.Zone;
            if (zone == null)
            {
                _captureSquadrons.AddRange(_groupWings);
                return;
            }
            _captureAssignments.AssignInZone(_groupWings, ToVector(ToPoint(zone.Center)), 2f * zone.Radius,
                home, _addWingForce, spread: group == _centreGroup);
        }

        /// <summary>Bombers without a winnable mine or satellite hold at the centre of the nearest own held zone
        /// (home when none is held) instead of joining a fight on a defended pad.</summary>
        private void HoldIdleBombers(Vector3 home)
        {
            if (_idleBombers.Count == 0) return;
            Vector3 hold = home;
            float nearest = float.PositiveInfinity;
            foreach (ReinforcementZonePresenter zone in _zones.Zones)
            {
                if (!zone.IsCapturable || !_playerRoster.IsAllied(zone.Owner, _owner.Id)) continue;
                float distance = PlanarDistanceSquared(zone.Center, _idleBombers[0].WorldPosition);
                if (distance >= nearest) continue;
                nearest = distance;
                hold = ToVector(ToPoint(zone.Center));
            }
            foreach (ISquadron bomber in _idleBombers) _bomberHolds[bomber] = hold;
        }

        /// <summary>
        /// From level 2 one fighter or interceptor wing patrols: a free ungrouped wing first (a purchase made for it),
        /// else a spare wing of group A while it keeps more than <see cref="PATROL_SPARE_GROUP_WINGS"/>.
        /// </summary>
        private void UpdatePatrolWing()
        {
            if (!_wingPatrol)
            {
                ReleasePatrolWing("disabled");
                return;
            }
            if (_patrolWing != null) return;
            foreach (KeyValuePair<ISquadron, CommandedSquadron> pair in _squadrons)
                if (IsFree(pair.Value) && !pair.Value.IsHangarLaunched && !IsGrouped(pair.Key) &&
                    IsPatrolClass(pair.Value.Profile.ShipClass))
                {
                    AssignPatrolWing(pair.Key, "free");
                    return;
                }
            if (_centreGroup.Members.Count <= PATROL_SPARE_GROUP_WINGS) return;
            for (int i = _centreGroup.Members.Count - 1; i >= 0; i--)
            {
                ISquadron wing = _centreGroup.Members[i];
                if (_squadrons[wing].IsHangarLaunched || !IsPatrolClass(_squadrons[wing].Profile.ShipClass)) continue;
                _centreGroup.Members.RemoveAt(i);
                AssignPatrolWing(wing, "groupA");
                return;
            }
        }

        private static bool IsPatrolClass(ShipClass shipClass) =>
            shipClass == ShipClass.Fighter || shipClass == ShipClass.Interceptor;

        private void AssignPatrolWing(ISquadron wing, string source)
        {
            _patrolWing = wing;
            _beacon = null;
            EnemyAiTelemetry.RecordEvent(_owner.Id, _owner.Faction, "patrolWing",
                $"wing={wing.EntityId};action=assigned;source={source}");
        }

        private void ReleasePatrolWing(string reason)
        {
            if (_patrolWing == null) return;
            EnemyAiTelemetry.RecordEvent(_owner.Id, _owner.Faction, "patrolWing",
                $"wing={_patrolWing.EntityId};action=released;reason={reason}");
            _patrol.Release($"squadron:{_patrolWing.EntityId}");
            _patrolWing = null;
            _beacon = null;
        }

        /// <summary>
        /// The patrol wing visits stale map points. When it meets hostile ships it pulls back toward home to a spawn
        /// beacon (outside hostile block circles, away from hostile relays and the ships) and holds there, so new
        /// ships can arrive beside it.
        /// </summary>
        private void IssuePatrolOrder(ISquadron wing)
        {
            float now = Time.time;
            CollectSeenShips(now);
            if (_beacon is Vector3 held && (now >= _beaconUntil || !IsBeaconSafe(held))) _beacon = null;
            if (_beacon == null && SeesHostileShip(wing.WorldPosition) &&
                TryFindBeacon(wing.WorldPosition, out Vector3 beacon))
            {
                _beacon = beacon;
                _beaconUntil = now + BEACON_HOLD_SECONDS;
                EnemyAiTelemetry.RecordEvent(_owner.Id, _owner.Faction, "spawnBeacon", FormattableString.Invariant(
                    $"wing={wing.EntityId};beacon=({beacon.x:F0} {beacon.z:F0});ships={_seenShips.Count}"));
            }
            if (_beacon is Vector3 point)
            {
                _entityLocator.GetEntity(wing.EntityId).GetFacade<IMoveFacade>().MoveTo(point);
                return;
            }
            Vector3 origin = wing.WorldPosition;
            if (_patrol.TryGetTarget($"squadron:{wing.EntityId}", origin,
                    target => _patrolSafety.IsObjectiveSafe(wing.EntityId, origin, target, 0f, false),
                    out Vector3 patrolTarget))
                wing.AttackMoveTo(patrolTarget);
            else
                wing.AttackMoveTo(GetHome());
        }

        private void CollectSeenShips(float now)
        {
            _seenShips.Clear();
            foreach (HostileSighting sighting in _intel.Sightings)
                if (sighting.UnitTypeId.IsShip && _playerRoster.IsHostile(_owner.Id, sighting.Owner) &&
                    _intel.GetConfidence(sighting, now) > 0f)
                    _seenShips.Add(ToVector(sighting.Position));
        }

        private bool SeesHostileShip(Vector3 position)
        {
            foreach (Vector3 ship in _seenShips)
                if (PlanarDistanceSquared(ship, position) <= PATROL_SIGHT_RADIUS * PATROL_SIGHT_RADIUS) return true;
            return false;
        }

        /// <summary>The first point toward home, in <see cref="BEACON_STEP"/> steps, that is a safe beacon.</summary>
        private bool TryFindBeacon(Vector3 origin, out Vector3 beacon)
        {
            Vector3 toHome = GetHome() - origin;
            toHome.y = 0f;
            float length = toHome.magnitude;
            Vector3 direction = length > 0f ? toHome / length : Vector3.zero;
            for (float travelled = 0f; travelled <= length; travelled += BEACON_STEP)
            {
                beacon = origin + direction * travelled;
                beacon.y = 0f;
                if (IsBeaconSafe(beacon)) return true;
            }
            beacon = default;
            return false;
        }

        private bool IsBeaconSafe(Vector3 point)
        {
            if (!_patrolSafety.IsOutsideHostileBlock(point)) return false;
            foreach (ReinforcementZonePresenter zone in _zones.Zones)
                if (_playerRoster.IsHostile(_owner.Id, zone.Owner) &&
                    PlanarDistanceSquared(zone.Center, point) < BEACON_RELAY_CLEARANCE * BEACON_RELAY_CLEARANCE)
                    return false;
            foreach (Vector3 ship in _seenShips)
                if (PlanarDistanceSquared(ship, point) < BEACON_SHIP_CLEARANCE * BEACON_SHIP_CLEARANCE) return false;
            return true;
        }

        private void PrioritizeZone(ReinforcementZonePresenter zone)
        {
            if (zone != null) _captureAssignments.PrioritizeArea(ToVector(ToPoint(zone.Center)), 2f * zone.Radius);
        }

        /// <summary>
        /// Outer groups raid known hostile mines: a group without a zone with all its wings, bombers at any time,
        /// always together. Bombers left without a mine then hit the hostile satellites of their zone
        /// (mine → satellites → relay).
        /// A wing raids a mine only when it at least matches the units and armed structures there (the power rule);
        /// no blanket structure-near-route block applies. One wing per mine, best damage per distance first.
        /// </summary>
        private void AssignRaiders()
        {
            _raids.Clear();
            _reservedRaidTargets.Clear();
            if (_stationDefense.HasValue) return;
            foreach (EnemyWingGroup group in _outerGroups) AssignMineRaid(group);
            foreach (EnemyWingGroup group in _outerGroups) AssignSatelliteRaid(group);
        }

        /// <summary>
        /// A group's raiders (all its wings without a zone, else its bombers) go together to the known hostile mine
        /// with the best damage per distance that they at least match together with whatever covers it. One group per
        /// mine. A lone bomber never takes on a mine under satellites.
        /// </summary>
        private void AssignMineRaid(EnemyWingGroup group)
        {
            _raidWings.Clear();
            float damage = 0f;
            foreach (ISquadron wing in group.Members)
            {
                UnitCombatProfile profile = _squadrons[wing].Profile;
                float structureDamage = profile.HullDps(ShipClass.Structure) + profile.PiercingDps(ShipClass.Structure);
                if (_raids.ContainsKey(wing) || structureDamage <= 0f ||
                    group.Zone != null && profile.ShipClass != ShipClass.Bomber) continue;
                _raidWings.Add(wing);
                damage += structureDamage;
            }
            if (_raidWings.Count == 0) return;

            _raidForce.Clear();
            foreach (ISquadron wing in _raidWings) AddWingForce(wing, _raidForce);
            Vector3 origin = _raidWings[0].WorldPosition;
            HostileSighting best = default;
            float bestScore = 0f;
            foreach (HostileSighting mine in _intel.Mines)
            {
                if (_reservedRaidTargets.Contains(mine.EntityId) ||
                    !_entityLocator.TryGetEntity(mine.EntityId, out IEntity target) ||
                    !_playerRoster.IsHostile(_owner.Id, target.Owner) || target.HealthModel.IsDestroyed) continue;
                Vector3 position = ToVector(mine.Position);
                float score = damage / Mathf.Max(1f, Mathf.Sqrt(PlanarDistanceSquared(origin, position)));
                if (score <= bestScore || !WinsRaid(position)) continue;
                best = mine;
                bestScore = score;
            }
            if (bestScore <= 0f) return;
            _reservedRaidTargets.Add(best.EntityId);
            foreach (ISquadron wing in _raidWings) _raids.Add(wing, best);
        }

        /// <summary>
        /// The group's idle bombers go together after the visible hostile satellite nearest them (inside the group's
        /// zone while it has one) when together they at least match it and whatever else covers it.
        /// </summary>
        private void AssignSatelliteRaid(EnemyWingGroup group)
        {
            _raidWings.Clear();
            foreach (ISquadron wing in group.Members)
                if (!_raids.ContainsKey(wing) && _squadrons[wing].Profile.ShipClass == ShipClass.Bomber)
                    _raidWings.Add(wing);
            if (_raidWings.Count == 0) return;

            Vector3 origin = _raidWings[0].WorldPosition;
            AiWorldSnapshot.Entry best = default;
            float bestDistance = float.PositiveInfinity;
            IReadOnlyList<AiWorldSnapshot.Entry> structures = _worldSnapshot.RaidStructures;
            for (int i = 0; i < structures.Count; i++)
            {
                AiWorldSnapshot.Entry entry = structures[i];
                if (!EnemyZoneObjectiveCollector.IsArmedHostileStructure(entry, _playerRoster, _vision, _owner.Id) ||
                    group.Zone != null && !EnemyShipTaskGroupPlanner.IsInZone(group.Zone, entry.Position)) continue;
                float distance = PlanarDistanceSquared(origin, entry.Position);
                if (distance >= bestDistance) continue;
                best = entry;
                bestDistance = distance;
            }
            if (float.IsPositiveInfinity(bestDistance)) return;

            _raidForce.Clear();
            foreach (ISquadron wing in _raidWings) AddWingForce(wing, _raidForce);
            _raidDefenders.Clear();
            FillTargetDefenders(best.Position, _raidDefenders);
            if (CombatMatchup.Advantage(_raidForce, _raidDefenders) < EVEN_FIGHT) return;
            var sighting = new HostileSighting(best.Id, default, best.Owner, ToPoint(best.Position),
                best.Entity.HealthModel.Hull, best.Entity.HealthModel.Shields, 1f, Time.time);
            foreach (ISquadron wing in _raidWings) _raids.Add(wing, sighting);
        }

        // The raiders in _raidForce at least match the units and armed structures at the mine.
        private bool WinsRaid(Vector3 mine)
        {
            _raidDefenders.Clear();
            FillTargetDefenders(mine, _raidDefenders);
            return _raidDefenders.IsEmpty || CombatMatchup.Advantage(_raidForce, _raidDefenders) >= EVEN_FIGHT;
        }

        /// <summary>
        /// Hostile force at a capture target: known hostile units in reach plus visible armed structures (satellites,
        /// defend platforms). Wings go in only when they are at least an even match for it.
        /// </summary>
        private void FillTargetDefenders(Vector3 target, ForceComposition defenders)
        {
            _ratedTarget = target;
            _forceBuilder.BuildKnown(defenders, _intel, Time.time, _includeTargetDefender);
            EnemyZoneObjectiveCollector.AddArmedStructures(defenders, _worldSnapshot, _playerRoster, _vision,
                _forceBuilder, _intel, _owner.Id, target, EnemyZoneObjectiveCollector.DEFENDER_REACH);
        }

        private bool IncludeTargetDefender(HostileSighting sighting) =>
            _playerRoster.IsHostile(_owner.Id, sighting.Owner) &&
            PlanarDistanceSquared(ToVector(sighting.Position), _ratedTarget) <=
            EnemyZoneObjectiveCollector.DEFENDER_REACH * EnemyZoneObjectiveCollector.DEFENDER_REACH;

        // A wing counts at its current strength: type profile, surviving craft and remaining hull.
        private void AddWingForce(ISquadron squadron, ForceComposition force) =>
            _forceBuilder.AddUnit(force, _entityLocator.GetEntity(squadron.EntityId));

        private void Release(ISquadron squadron)
        {
            foreach (EnemyWingGroup group in _outerGroups)
                if (group.Zone != null && group.Members.Contains(squadron) &&
                    EnemyShipTaskGroupPlanner.IsInZone(group.Zone, squadron.WorldPosition))
                    group.ZoneDeaths.Add(Time.time);
            if (squadron == _patrolWing) ReleasePatrolWing("lost");
            _bomberHolds.Remove(squadron);
            squadron.Released -= _squadrons[squadron].ReleaseHandler;
            _squadrons.Remove(squadron);
            _escorts.Remove(squadron);
            _centreGroup.Members.Remove(squadron);
            foreach (EnemyWingGroup group in _outerGroups) group.Members.Remove(squadron);
            _captureAssignments.Release(squadron);
            _raids.Remove(squadron);
            if (_scout == squadron)
            {
                _patrol.Release($"squadron:{squadron.EntityId}");
                _scout = null;
            }
        }
    }
}
