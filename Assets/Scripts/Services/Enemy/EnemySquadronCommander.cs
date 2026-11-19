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
    /// Spreads available AI squadrons across capture objectives, retaining valid assignments;
    /// squadrons hunt only when nothing is left to capture. While hostile strikecraft
    /// are in play, the squadrons that destroy them fastest escort the
    /// own ships those strikecraft hurt the most, only until the escorts match the strikecraft.
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
        private const int MAX_RAID_WINGS = 3;

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

        private int _nextEscortTarget;

        /// <summary>Squadron revealing fog because no build space is visible; null when none is needed.</summary>
        private ISquadron _scout;

        private Vector3 _scoutTarget;
        private readonly Dictionary<ISquadron, HostileSighting> _raids =
            new Dictionary<ISquadron, HostileSighting>();
        private readonly HashSet<long> _reservedRaidTargets = new HashSet<long>();

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

            AssessThreats();
            AssignEscorts();
            AssignCaptureTargets();
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
            AssessThreats();
            AssignEscorts();
            AssignCaptureTargets();
            _orderBuffer.Clear();
            _orderBuffer.AddRange(_squadrons.Keys);
            foreach (ISquadron squadron in _orderBuffer)
            {
                IssueOrder(squadron);
            }
        }

        private void TrackOwnedSquadrons()
        {
            IReadOnlyList<AiWorldSnapshot.Entry> owned = _worldSnapshot.GetOwnedUnits(_owner.Id);
            for (int i = 0; i < owned.Count; i++)
            {
                AiWorldSnapshot.Entry entry = owned[i];
                if (entry.UnitTypeId.IsSquadron &&
                    entry.Entity.TryGetFacade(out ISquadron squadron) && !_squadrons.ContainsKey(squadron))
                {
                    Track(squadron, entry.UnitTypeId.SquadronType,
                        entry.Entity.GetFacade<IUnitOrderObserverFacade>());
                }
            }
        }

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
            // Keep one available squadron capturing; only surplus squadrons scout while objectives remain.
            if (_captureSquadrons.Count == 0 ||
                _captureAssignments.HasTargets && _captureSquadrons.Count <= captureWings || _stationDefense.HasValue)
            {
                if (previous != null) _patrol.Release($"squadron:{previous.EntityId}");
                return;
            }

            float closest = float.MaxValue;
            foreach (ISquadron squadron in _captureSquadrons)
            {
                float distance = (squadron.WorldPosition - _scoutTarget).sqrMagnitude;
                if (!_squadrons[squadron].IsHangarLaunched && distance < closest)
                {
                    closest = distance;
                    _scout = squadron;
                }
            }
            if (previous != null && _captureSquadrons.Contains(previous) && !_squadrons[previous].IsHangarLaunched)
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
        /// Picks the best anti-strikecraft squadrons, in order, until together they match the hostile strikecraft.
        /// Everyone else keeps capturing, so escorts never absorb the whole wing.
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
                if (TryRateEscort(pair.Value.Profile, out float timeToClearStrikecraft))
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

        private void AssignCaptureTargets()
        {
            _captureAssignments.ClearTargets();
            foreach (var zone in _zones.Zones)
            {
                if (zone.IsCapturable && !_playerRoster.IsAllied(zone.Owner, _owner.Id))
                {
                    var strength = _zones.GetTeamStrengths(zone, _owner.Id);
                    int requiredWings = Mathf.FloorToInt(strength.Hostile / _zones.SquadronCaptureWeight) + 1;
                    _captureAssignments.AddTarget(ToVector(ToPoint(zone.Center)), requiredWings: requiredWings);
                }
            }
            foreach (IEnemyCaptureObjectiveSource source in _captureSources)
                source.AddSquadronTargets(_captureAssignments, _intel);
            _captureSquadrons.Clear();
            foreach (KeyValuePair<ISquadron, CommandedSquadron> pair in _squadrons)
            {
                if (_escorts.Contains(pair.Key) ||
                    pair.Value.IsHangarLaunched && pair.Value.Orders.CurrentOrder == UnitOrderType.Guard) continue;
                _captureSquadrons.Add(pair.Key);
            }
            ReinforcementZonePresenter centre = EnemyShipTaskGroupPlanner.GetCentre(_zones.Zones);
            if (_captureSquadrons.Count > 0)
            {
                Vector3 home = EnemyShipTaskGroupPlanner.GetOpeningHome(_worldSnapshot, _owner.Id,
                    _captureSquadrons[0].WorldPosition);
                ReinforcementZonePresenter neighbour = EnemyShipTaskGroupPlanner.GetNeighbour(_zones.Zones, home);
                PrioritizeZone(centre);
                PrioritizeZone(neighbour);
                foreach (ReinforcementZonePresenter zone in _zones.Zones)
                    if (zone.IsCapturable && zone != centre && zone != neighbour) PrioritizeZone(zone);
            }
            int captureWings = Mathf.Max(centre != null ? OPENING_CAPTURE_WINGS : 1,
                _captureAssignments.GetOpeningWingBudget(_captureSquadrons.Count));
            AssignRaiders(_captureAssignments.HasTargets ? captureWings : 1);
            AssignScout(captureWings);
            if (_scout != null) _captureSquadrons.Remove(_scout);
            _captureAssignments.Assign(_captureSquadrons);
        }

        private void PrioritizeZone(ReinforcementZonePresenter zone)
        {
            if (zone != null) _captureAssignments.PrioritizeArea(ToVector(ToPoint(zone.Center)), 2f * zone.Radius);
        }

        private void AssignRaiders(int captureWings)
        {
            _raids.Clear();
            _reservedRaidTargets.Clear();
            if (_stationDefense.HasValue || _intel.Mines.Count == 0) return;

            while (_raids.Count < MAX_RAID_WINGS && _captureSquadrons.Count > captureWings)
            {
                ISquadron bestWing = null;
                HostileSighting bestMine = default;
                float bestScore = 0f;
                foreach (ISquadron candidate in _captureSquadrons)
                {
                    CommandedSquadron commanded = _squadrons[candidate];
                    if (commanded.IsHangarLaunched) continue;
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
                        if (score <= bestScore || !_patrolSafety.IsObjectiveSafe(
                            candidate.EntityId, candidate.WorldPosition, position, 0f, false, raiding: true)) continue;
                        bestWing = candidate;
                        bestMine = mine;
                        bestScore = score;
                    }
                }

                if (bestWing == null) break;
                _raids.Add(bestWing, bestMine);
                _reservedRaidTargets.Add(bestMine.EntityId);
                _captureSquadrons.Remove(bestWing);
            }
        }

        private void Release(ISquadron squadron)
        {
            squadron.Released -= _squadrons[squadron].ReleaseHandler;
            _squadrons.Remove(squadron);
            _escorts.Remove(squadron);
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
