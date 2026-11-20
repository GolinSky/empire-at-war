using System.Collections.Generic;
using EmpireAtWar.Entities.EnemyFaction.Models.Intel;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Models.Players;
using UnityEngine;
using static EmpireAtWar.Utils.FormationConversion;

namespace EmpireAtWar.Services.Enemy
{
    /// <summary>
    /// Picks the own units a new ship spawns beside (round 5, E1d): the spawn beacon, then wings, then ships, then
    /// mines and satellites; within a kind the one nearest the ship's task first. Units with a known hostile ship
    /// within <see cref="HOSTILE_CLEAR_RADIUS"/> are skipped. Ships move away from the known hostile ships' centre,
    /// else from the map centre.
    /// </summary>
    public sealed class EnemyForwardSpawnAnchors : IEnemyForwardSpawnAnchors
    {
        public const float HOSTILE_CLEAR_RADIUS = 800f;
        public const int MAX_ANCHORS = 3;

        private readonly AiWorldSnapshot _world;
        private readonly HostileIntelModel _intel;
        private readonly IPlayerRoster _players;
        private readonly IMapModelObserver _map;
        private readonly IEnemySquadronCommander _squadrons;
        private readonly PlayerSlot _owner;
        private readonly List<Vector3> _hostileShips = new List<Vector3>();
        private readonly List<(Vector3 Position, int Rank, float Distance)> _candidates =
            new List<(Vector3, int, float)>();

        private static readonly string[] KINDS = { "beacon", "wing", "ship", "structure" };

        public EnemyForwardSpawnAnchors(AiWorldSnapshot world, TeamIntelRegistry intel, IPlayerRoster players,
            IMapModelObserver map, IEnemySquadronCommander squadrons, PlayerSlot owner)
        {
            _world = world;
            _intel = intel.Get(owner.Team);
            _players = players;
            _map = map;
            _squadrons = squadrons;
            _owner = owner;
        }

        /// <param name="landedShips">Ships placed earlier in this tick, not yet in the world snapshot.</param>
        public bool Collect(Vector3 task, IReadOnlyList<Vector3> landedShips, List<ForwardSpawnAnchor> anchors,
            out Vector3 awayFrom)
        {
            anchors.Clear();
            _candidates.Clear();
            CollectHostileShips(Time.time);
            awayFrom = GetAwayPoint();

            if (_squadrons.TryGetSpawnBeacon(out Vector3 beacon)) Add(beacon, 0, task);
            IReadOnlyList<AiWorldSnapshot.Entry> owned = _world.GetOwnedUnits(_owner.Id);
            for (int i = 0; i < owned.Count; i++)
            {
                AiWorldSnapshot.Entry entry = owned[i];
                if (entry.UnitTypeId.IsSquadron) Add(entry.Position, 1, task);
                else if (entry.UnitTypeId.IsShip && !entry.IsPlayerBase) Add(entry.Position, 2, task);
            }
            for (int i = 0; i < landedShips.Count; i++) Add(landedShips[i], 2, task);
            IReadOnlyList<AiWorldSnapshot.Entry> structures = _world.RaidStructures;
            for (int i = 0; i < structures.Count; i++)
                if (structures[i].Owner == _owner.Id) Add(structures[i].Position, 3, task);

            _candidates.Sort((a, b) => a.Rank != b.Rank ? a.Rank.CompareTo(b.Rank) : a.Distance.CompareTo(b.Distance));
            for (int i = 0; i < _candidates.Count && anchors.Count < MAX_ANCHORS; i++)
                anchors.Add(new ForwardSpawnAnchor(_candidates[i].Position, KINDS[_candidates[i].Rank]));
            return anchors.Count > 0;
        }

        private void Add(Vector3 position, int rank, Vector3 task)
        {
            for (int i = 0; i < _hostileShips.Count; i++)
                if (PlanarDistanceSquared(_hostileShips[i], position) <= HOSTILE_CLEAR_RADIUS * HOSTILE_CLEAR_RADIUS)
                    return;
            _candidates.Add((position, rank, PlanarDistanceSquared(position, task)));
        }

        private void CollectHostileShips(float now)
        {
            _hostileShips.Clear();
            foreach (HostileSighting sighting in _intel.Sightings)
                if (sighting.UnitTypeId.IsShip && _players.IsHostile(_owner.Id, sighting.Owner) &&
                    _intel.GetConfidence(sighting, now) > 0f)
                    _hostileShips.Add(ToVector(sighting.Position));
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
    }
}
