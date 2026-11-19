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
    /// Orders AI wings. Station-launched and bought wings are free and form two groups (<see cref="EnemyWingGroup"/>):
    /// group A takes and holds the centre, group B the own-side outer zone, then the next outer zone, then raids
    /// hostile mines. Each group chooses free sites inside its zone and holds inside a pad's circle until it flips.
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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private const float WING_ASSIGNMENT_REPORT_INTERVAL = 6f;
        private string _lastWingAssignment;
        private float _nextWingAssignmentReport;
#endif
        /// <summary>
        /// A capturing wing this close to its site switches from attack-move to move: it stays inside the pad circle
        /// (radius 60) instead of drifting out while it fights.
        /// </summary>
        private const float CAPTURE_HOLD_DISTANCE = 400f;
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
        private readonly EnemyWingGroup _outerGroup = new EnemyWingGroup(EnemyWingRole.Outer);
        private readonly List<ISquadron> _groupWings = new List<ISquadron>();
        private readonly List<ISquadron> _scoutPool = new List<ISquadron>();
        private readonly ForceComposition _raidForce = new ForceComposition();
        private readonly ForceComposition _raidDefenders = new ForceComposition();
        private readonly Predicate<ISquadron> _leftGroup;
        private int _outerWingCount = 1;

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
                // The last leg is a move: a wing that stops to fight would drift out of the pad circle.
                if (_captureAssignments.IsSite(target) &&
                    PlanarDistanceSquared(squadron.WorldPosition, target) <= CAPTURE_HOLD_DISTANCE * CAPTURE_HOLD_DISTANCE)
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
        /// Keeps the two wing groups filled: a new free wing joins group B until it has its opening size (bombers
        /// always join B, the raid group), every other wing joins group A. A lost wing's replacement fills the gap.
        /// </summary>
        private void UpdateGroups()
        {
            _centreGroup.Members.RemoveAll(_leftGroup);
            _outerGroup.Members.RemoveAll(_leftGroup);
            int outerCapturers = 0;
            foreach (ISquadron wing in _outerGroup.Members)
                if (_squadrons[wing].Profile.ShipClass != ShipClass.Bomber) outerCapturers++;
            foreach (KeyValuePair<ISquadron, CommandedSquadron> pair in _squadrons)
            {
                if (!IsFree(pair.Value) || IsGrouped(pair.Key)) continue;
                bool bomber = pair.Value.Profile.ShipClass == ShipClass.Bomber;
                if (bomber || outerCapturers < _outerWingCount)
                {
                    _outerGroup.Members.Add(pair.Key);
                    if (!bomber) outerCapturers++;
                }
                else _centreGroup.Members.Add(pair.Key);
            }
            Vector3 home = GetHome();
            _centreGroup.Zone = EnemyShipTaskGroupPlanner.GetCentre(_zones.Zones);
            _outerGroup.Zone = SelectOuterZone(home);
        }

        private bool IsGrouped(ISquadron wing) =>
            _centreGroup.Members.Contains(wing) || _outerGroup.Members.Contains(wing);

        /// <summary>The outer zone nearest home that still has a site to take; null when none is left.</summary>
        private ReinforcementZonePresenter SelectOuterZone(Vector3 home)
        {
            ReinforcementZonePresenter best = null;
            float bestDistance = float.PositiveInfinity;
            foreach (ReinforcementZonePresenter zone in _zones.Zones)
            {
                if (!zone.IsCapturable || zone.IsMain ||
                    !_captureAssignments.HasTargetIn(ToVector(ToPoint(zone.Center)), 2f * zone.Radius)) continue;
                float distance = PlanarDistanceSquared(zone.Center, home);
                if (distance >= bestDistance) continue;
                best = zone;
                bestDistance = distance;
            }
            return best;
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
            Vector3 home = GetHome();
            AssignGroup(_centreGroup, home, relayBeforeMines: false);
            AssignGroup(_outerGroup, home, relayBeforeMines: true);
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
                $"wings={_squadrons.Count};groupA={_centreGroup.Members.Count};groupB={_outerGroup.Members.Count};groupBZone={(_outerGroup.Zone != null ? $"({_outerGroup.Zone.Center.x:F0} {_outerGroup.Zone.Center.z:F0})" : "none")};captureWings={_captureSquadrons.Count};openingBudget={captureWings};escorts={_escorts.Count};raids={_raids.Count};scout={(_scout != null)};targets=") +
                _captureAssignments.Describe();
            if (assignment != _lastWingAssignment && Time.time >= _nextWingAssignmentReport)
            {
                _lastWingAssignment = assignment;
                _nextWingAssignmentReport = Time.time + WING_ASSIGNMENT_REPORT_INTERVAL;
                EnemyAiTelemetry.RecordEvent(_owner.Id, _owner.Faction, "wingAssignment", assignment);
            }
#endif
        }

        /// <summary>
        /// Assigns a group's wings inside its zone. Without a zone they join the ungrouped wings spread over every
        /// target. Raiding wings and escorts are left out.
        /// </summary>
        private void AssignGroup(EnemyWingGroup group, Vector3 home, bool relayBeforeMines)
        {
            _groupWings.Clear();
            foreach (ISquadron wing in group.Members)
                if (!_raids.ContainsKey(wing) && !_escorts.Contains(wing)) _groupWings.Add(wing);
            if (group.Zone == null)
            {
                _captureSquadrons.AddRange(_groupWings);
                return;
            }
            Vector3 centre = ToVector(ToPoint(group.Zone.Center));
            Vector3 toHome = home - centre;
            toHome.y = 0f;
            Vector3 bait = toHome.sqrMagnitude > 1f ? centre + toHome.normalized * group.Zone.Radius : centre;
            _captureAssignments.AssignInZone(_groupWings, centre, 2f * group.Zone.Radius, home, relayBeforeMines, bait);
        }

        private void PrioritizeZone(ReinforcementZonePresenter zone)
        {
            if (zone != null) _captureAssignments.PrioritizeArea(ToVector(ToPoint(zone.Center)), 2f * zone.Radius);
        }

        /// <summary>
        /// Group B raids known hostile mines: all of it once no outer zone is left to take, its bombers at any time.
        /// A wing raids a mine only when it at least matches the units and armed structures there (the power rule);
        /// no blanket structure-near-route block applies. One wing per mine, best damage per distance first.
        /// </summary>
        private void AssignRaiders()
        {
            _raids.Clear();
            _reservedRaidTargets.Clear();
            if (_stationDefense.HasValue || _intel.Mines.Count == 0) return;

            while (true)
            {
                ISquadron bestWing = null;
                HostileSighting bestMine = default;
                float bestScore = 0f;
                foreach (ISquadron candidate in _outerGroup.Members)
                {
                    CommandedSquadron commanded = _squadrons[candidate];
                    if (_raids.ContainsKey(candidate) ||
                        _outerGroup.Zone != null && commanded.Profile.ShipClass != ShipClass.Bomber) continue;
                    float damage = commanded.Profile.HullDps(ShipClass.Structure) +
                                   commanded.Profile.PiercingDps(ShipClass.Structure);
                    if (damage <= 0f) continue;

                    foreach (HostileSighting mine in _intel.Mines)
                    {
                        if (_reservedRaidTargets.Contains(mine.EntityId) ||
                            !_entityLocator.TryGetEntity(mine.EntityId, out IEntity target) ||
                            !_playerRoster.IsHostile(_owner.Id, target.Owner) || target.HealthModel.IsDestroyed) continue;
                        Vector3 position = ToVector(mine.Position);
                        float distance = Mathf.Sqrt(PlanarDistanceSquared(candidate.WorldPosition, position));
                        float score = damage / Mathf.Max(1f, distance);
                        if (score <= bestScore || !WinsRaid(candidate, position)) continue;
                        bestWing = candidate;
                        bestMine = mine;
                        bestScore = score;
                    }
                }

                if (bestWing == null) break;
                _raids.Add(bestWing, bestMine);
                _reservedRaidTargets.Add(bestMine.EntityId);
            }
        }

        private bool WinsRaid(ISquadron wing, Vector3 mine)
        {
            _raidDefenders.Clear();
            FillTargetDefenders(mine, _raidDefenders);
            if (_raidDefenders.IsEmpty) return true;
            _raidForce.Clear();
            AddWingForce(wing, _raidForce);
            return CombatMatchup.Advantage(_raidForce, _raidDefenders) >= EVEN_FIGHT;
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
                _owner.Id, target, EnemyZoneObjectiveCollector.DEFENDER_REACH);
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
            squadron.Released -= _squadrons[squadron].ReleaseHandler;
            _squadrons.Remove(squadron);
            _escorts.Remove(squadron);
            _centreGroup.Members.Remove(squadron);
            _outerGroup.Members.Remove(squadron);
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
