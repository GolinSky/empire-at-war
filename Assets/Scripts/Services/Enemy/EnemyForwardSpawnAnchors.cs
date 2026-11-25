using System.Collections.Generic;
using EmpireAtWar.Entities.EnemyFaction.Models.Intel;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Presenters.ReinforcementZones;
using EmpireAtWar.Services.ReinforcementZones;
using EmpireAtWar.Services.Vision;
using UnityEngine;
using static EmpireAtWar.Utils.FormationConversion;

namespace EmpireAtWar.Services.Enemy
{
    /// <summary>
    /// Picks the own units a new ship spawns beside (round 5 E1d, round 6 G3): ships landed this tick first, then
    /// every candidate by distance to the ship's task (kind only breaks ties); the spawn beacon leads only within
    /// <see cref="BEACON_LEAD_RADIUS"/> of the nearest other candidate. Rejected: a unit with a known hostile ship,
    /// platform, satellite or station within <see cref="HOSTILE_CLEAR_RADIUS"/>; a deep unit (nearer a hostile station
    /// or relay than any own one) without <see cref="DEEP_ESCORT_SHIPS"/> own ships near; for a capital, any unit
    /// without <see cref="CAPITAL_ESCORT_SHIPS"/> own ships near. Ships move away from the known hostile ships' centre,
    /// else from the map centre. A planned attack wave (<see cref="CollectPlannedWave"/>, W68) skips both escort filters.
    /// </summary>
    public sealed class EnemyForwardSpawnAnchors : IEnemyForwardSpawnAnchors
    {
        public const float HOSTILE_CLEAR_RADIUS = 800f;
        public const int MAX_ANCHORS = 3;
        public const float BEACON_LEAD_RADIUS = 1000f;
        public const float ESCORT_RADIUS = 600f;
        public const int DEEP_ESCORT_SHIPS = 3;
        public const int CAPITAL_ESCORT_SHIPS = 4;

        public const int LANDED = 0;
        public const int BEACON = 1;
        public const int WING = 2;
        public const int SHIP = 3;
        public const int STRUCTURE = 4;

        private static readonly string[] KINDS = { "landed", "beacon", "wing", "ship", "structure" };

        private readonly AiWorldSnapshot _world;
        private readonly HostileIntelModel _intel;
        private readonly IPlayerRoster _players;
        private readonly IMapModelObserver _map;
        private readonly IEnemySquadronCommander _squadrons;
        private readonly IReinforcementZoneSource _zones;
        private readonly IVisionService _vision;
        private readonly PlayerSlot _owner;
        private readonly List<Vector3> _hostileShips = new List<Vector3>();
        private readonly AnchorPicture _picture = new AnchorPicture();

        public EnemyForwardSpawnAnchors(AiWorldSnapshot world, TeamIntelRegistry intel, IPlayerRoster players,
            IMapModelObserver map, IEnemySquadronCommander squadrons, IReinforcementZoneSource zones,
            IVisionService vision, PlayerSlot owner)
        {
            _world = world;
            _intel = intel.Get(owner.Team);
            _players = players;
            _map = map;
            _squadrons = squadrons;
            _zones = zones;
            _vision = vision;
            _owner = owner;
        }

        /// <param name="landedShips">Ships placed earlier in this tick, not yet in the world snapshot.</param>
        /// <param name="capital">The ship is an assault capital: it needs an escorted anchor.</param>
        public bool Collect(Vector3 task, IReadOnlyList<Vector3> landedShips, bool capital,
            List<ForwardSpawnAnchor> anchors, out Vector3 awayFrom)
        {
            FillPicture(landedShips, out awayFrom);
            return Select(_picture, task, capital, anchors);
        }

        public bool CollectPlannedWave(Vector3 task, IReadOnlyList<Vector3> landedShips,
            List<ForwardSpawnAnchor> anchors, out Vector3 awayFrom)
        {
            FillPicture(landedShips, out awayFrom);
            return Select(_picture, task, false, anchors, plannedWave: true);
        }

        private void FillPicture(IReadOnlyList<Vector3> landedShips, out Vector3 awayFrom)
        {
            _picture.Clear();
            CollectHostileShips(Time.time);
            awayFrom = GetAwayPoint();
            CollectHostiles();
            CollectFronts();

            if (_squadrons.TryGetSpawnBeacon(out Vector3 beacon)) _picture.Candidates.Add((beacon, BEACON));
            IReadOnlyList<AiWorldSnapshot.Entry> owned = _world.GetOwnedUnits(_owner.Id);
            for (int i = 0; i < owned.Count; i++)
            {
                AiWorldSnapshot.Entry entry = owned[i];
                if (entry.UnitTypeId.IsSquadron) _picture.Candidates.Add((entry.Position, WING));
                else if (entry.UnitTypeId.IsShip && !entry.IsPlayerBase)
                {
                    _picture.Candidates.Add((entry.Position, SHIP));
                    _picture.OwnShips.Add(entry.Position);
                }
            }
            for (int i = 0; i < landedShips.Count; i++)
            {
                _picture.Candidates.Add((landedShips[i], LANDED));
                _picture.OwnShips.Add(landedShips[i]);
            }
            IReadOnlyList<AiWorldSnapshot.Entry> structures = _world.RaidStructures;
            for (int i = 0; i < structures.Count; i++)
                if (structures[i].Owner == _owner.Id) _picture.Candidates.Add((structures[i].Position, STRUCTURE));
        }

        /// <summary>The ranking and rejection rules on plain positions.</summary>
        /// <param name="plannedWave">A planned attack wave (W68): no deep or capital escort filter.</param>
        public static bool Select(AnchorPicture picture, Vector3 task, bool capital, List<ForwardSpawnAnchor> anchors,
            bool plannedWave = false)
        {
            anchors.Clear();
            List<(Vector3 Position, int Kind, float Distance)> ranked = picture.Ranked;
            ranked.Clear();
            for (int i = 0; i < picture.Candidates.Count; i++)
            {
                (Vector3 position, int kind) = picture.Candidates[i];
                if (IsRejected(picture, position, capital, plannedWave)) continue;
                ranked.Add((position, kind, PlanarDistanceSquared(position, task)));
            }

            // Landed ships first, then by distance to the task; kind breaks ties.
            ranked.Sort((a, b) =>
            {
                bool aLanded = a.Kind == LANDED;
                bool bLanded = b.Kind == LANDED;
                if (aLanded != bLanded) return aLanded ? -1 : 1;
                int byDistance = a.Distance.CompareTo(b.Distance);
                return byDistance != 0 ? byDistance : a.Kind.CompareTo(b.Kind);
            });
            PromoteBeacon(ranked);
            for (int i = 0; i < ranked.Count && anchors.Count < MAX_ANCHORS; i++)
                anchors.Add(new ForwardSpawnAnchor(ranked[i].Position, KINDS[ranked[i].Kind]));
            return anchors.Count > 0;
        }

        /// <summary>The beacon leads only when it stands within <see cref="BEACON_LEAD_RADIUS"/> of the best other anchor.</summary>
        private static void PromoteBeacon(List<(Vector3 Position, int Kind, float Distance)> ranked)
        {
            int beacon = ranked.FindIndex(entry => entry.Kind == BEACON);
            if (beacon <= 0) return;
            int first = ranked[0].Kind == LANDED ? -1 : 0;
            if (first < 0 || PlanarDistanceSquared(ranked[beacon].Position, ranked[first].Position) >
                BEACON_LEAD_RADIUS * BEACON_LEAD_RADIUS) return;
            var entry = ranked[beacon];
            ranked.RemoveAt(beacon);
            ranked.Insert(0, entry);
        }

        private static bool IsRejected(AnchorPicture picture, Vector3 position, bool capital, bool plannedWave)
        {
            foreach (Vector3 hostile in picture.Hostiles)
                if (PlanarDistanceSquared(hostile, position) <= HOSTILE_CLEAR_RADIUS * HOSTILE_CLEAR_RADIUS) return true;
            if (plannedWave) return false;
            int escorts = CountWithin(picture.OwnShips, position, ESCORT_RADIUS);
            if (capital && escorts < CAPITAL_ESCORT_SHIPS) return true;
            return IsDeep(picture, position) && escorts < DEEP_ESCORT_SHIPS;
        }

        /// <summary>Nearer a hostile station or relay than any own station or relay.</summary>
        private static bool IsDeep(AnchorPicture picture, Vector3 position)
        {
            float hostile = NearestSquared(picture.HostileFronts, position);
            return hostile < NearestSquared(picture.OwnFronts, position);
        }

        private static float NearestSquared(List<Vector3> points, Vector3 position)
        {
            float nearest = float.PositiveInfinity;
            foreach (Vector3 point in points) nearest = Mathf.Min(nearest, PlanarDistanceSquared(point, position));
            return nearest;
        }

        private static int CountWithin(List<Vector3> points, Vector3 position, float radius)
        {
            int count = 0;
            foreach (Vector3 point in points)
                if (PlanarDistanceSquared(point, position) <= radius * radius) count++;
            return count;
        }

        private void CollectHostileShips(float now)
        {
            _hostileShips.Clear();
            foreach (HostileSighting sighting in _intel.Sightings)
                if (sighting.UnitTypeId.IsShip && _players.IsHostile(_owner.Id, sighting.Owner) &&
                    _intel.GetConfidence(sighting, now) > 0f)
                    _hostileShips.Add(ToVector(sighting.Position));
        }

        /// <summary>Known hostile ships, armed structures seen or remembered, and every hostile station.</summary>
        private void CollectHostiles()
        {
            _picture.Hostiles.AddRange(_hostileShips);
            IReadOnlyList<AiWorldSnapshot.Entry> structures = _world.RaidStructures;
            for (int i = 0; i < structures.Count; i++)
            {
                AiWorldSnapshot.Entry entry = structures[i];
                if (entry.IsMiningFacility || !_players.IsHostile(_owner.Id, entry.Owner)) continue;
                if (_vision.IsVisible(_owner.Id, entry.Entity)) _intel.RememberStructure(entry.Id);
                else if (!_intel.IsStructureKnown(entry.Id)) continue;
                _picture.Hostiles.Add(entry.Position);
            }
            IReadOnlyList<AiWorldSnapshot.Entry> bases = _world.Bases;
            for (int i = 0; i < bases.Count; i++)
                if (_players.IsHostile(_owner.Id, bases[i].Owner)) _picture.Hostiles.Add(bases[i].Position);
        }

        private void CollectFronts()
        {
            IReadOnlyList<AiWorldSnapshot.Entry> bases = _world.Bases;
            for (int i = 0; i < bases.Count; i++)
            {
                if (_players.IsAllied(_owner.Id, bases[i].Owner)) _picture.OwnFronts.Add(bases[i].Position);
                else if (_players.IsHostile(_owner.Id, bases[i].Owner)) _picture.HostileFronts.Add(bases[i].Position);
            }
            foreach (ReinforcementZone zone in _zones.Zones)
            {
                if (_players.IsAllied(_owner.Id, zone.Owner)) _picture.OwnFronts.Add(zone.Center);
                else if (_players.IsHostile(_owner.Id, zone.Owner)) _picture.HostileFronts.Add(zone.Center);
            }
        }

        private Vector3 GetAwayPoint()
        {
            if (_hostileShips.Count == 0)
            {
                Vector2 centre = (_map.SizeRange.Min + _map.SizeRange.Max) * 0.5f;
                return new Vector3(centre.x, 0f, centre.y);
            }
            Vector3 sum = Vector3.zero;
            foreach (Vector3 ship in _hostileShips) sum += ship;
            return sum / _hostileShips.Count;
        }

        private static float PlanarDistanceSquared(Vector3 a, Vector3 b)
        {
            float x = a.x - b.x;
            float z = a.z - b.z;
            return x * x + z * z;
        }

        /// <summary>Plain positions the anchor rules read; reused between calls.</summary>
        public sealed class AnchorPicture
        {
            public readonly List<(Vector3 Position, int Kind)> Candidates = new List<(Vector3, int)>();
            public readonly List<Vector3> Hostiles = new List<Vector3>();
            public readonly List<Vector3> OwnShips = new List<Vector3>();
            public readonly List<Vector3> OwnFronts = new List<Vector3>();
            public readonly List<Vector3> HostileFronts = new List<Vector3>();
            internal readonly List<(Vector3 Position, int Kind, float Distance)> Ranked =
                new List<(Vector3, int, float)>();

            public void Clear()
            {
                Candidates.Clear();
                Hostiles.Clear();
                OwnShips.Clear();
                OwnFronts.Clear();
                HostileFronts.Clear();
                Ranked.Clear();
            }
        }
    }
}
