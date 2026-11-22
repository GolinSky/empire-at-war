using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;
using UnityEngine;

namespace EmpireAtWar.Services.Enemy
{
    /// <summary>
    /// Groups known hostile ships: ships within <see cref="LINK_RADIUS"/> of another ship of the group belong to it.
    /// Whether a group is an armada depends on who looks at it (<see cref="IsArmada"/>). Its heading comes from the movement
    /// of its centre between samples; the asset it threatens is the nearest team asset within
    /// <see cref="THREAT_REACH"/> of where it stands or will stand after <see cref="LOOKAHEAD_SECONDS"/>.
    /// </summary>
    public sealed class HostileArmadaTracker
    {
        /// <summary>One engagement radius: twice the common ship weapon range (500).</summary>
        public const float LINK_RADIUS = 1000f;
        /// <summary>A lone ship is a defender or a raider, never an armada: a single zone guard must not switch on late-game rules.</summary>
        public const int MIN_SHIPS = 2;
        /// <summary>A group with this many capital-class ships is an armada whatever the own fleet (W37).</summary>
        public const int MIN_CAPITAL_SHIPS = 2;
        /// <summary>A group the own fleet does not beat by this ratio is an armada (W37).</summary>
        public const float OUTMATCH_RATIO = 2f;
        /// <summary>An armada this close to an asset's edge, now or after the look-ahead, threatens it.</summary>
        public const float THREAT_REACH = 1000f;
        public const float LOOKAHEAD_SECONDS = 20f;
        /// <summary>The heading is measured over at least this long, so one frame's jitter does not steer it.</summary>
        public const float HEADING_SAMPLE_SECONDS = 2f;
        /// <summary>A new armada centre this close to a previous one continues it (heading and sample carry over).</summary>
        public const float MATCH_DISTANCE = 1500f;

        private readonly List<HostileArmada> _armadas = new List<HostileArmada>();
        private readonly List<HostileArmada> _previous = new List<HostileArmada>();
        private readonly List<HostileArmada> _spare = new List<HostileArmada>();
        private readonly HashSet<HostileArmada> _matched = new HashSet<HostileArmada>();
        private readonly List<int> _group = new List<int>();
        private readonly List<int> _members = new List<int>();
        private readonly Stack<int> _open = new Stack<int>();

        public IReadOnlyList<HostileArmada> Armadas => _armadas;

        /// <param name="ships">Ground positions of the known hostile ships.</param>
        /// <param name="addShip">Adds the ship at that index to an armada's strength.</param>
        /// <param name="assets">Team stations and held zones.</param>
        public void Update(IReadOnlyList<Vector3> ships, Action<int, ForceComposition> addShip,
            IReadOnlyList<ArmadaAsset> assets, float now)
        {
            _previous.Clear();
            _previous.AddRange(_armadas);
            _armadas.Clear();
            _matched.Clear();
            Group(ships);
            for (int start = 0; start < ships.Count; start++)
            {
                if (_group[start] != start) continue;
                _members.Clear();
                for (int i = start; i < ships.Count; i++)
                    if (_group[i] == start) _members.Add(i);
                HostileArmada armada = NextArmada();
                Vector3 sum = Vector3.zero;
                foreach (int ship in _members)
                {
                    sum += ships[ship];
                    addShip(ship, armada.Strength);
                }
                armada.Ships = _members.Count;
                armada.Centre = sum / _members.Count;
                Continue(armada, now);
                FindThreatenedAsset(armada, assets);
                _armadas.Add(armada);
            }
            _spare.AddRange(_previous);
        }

        /// <summary>
        /// A group of at least <see cref="MIN_SHIPS"/> ships is an armada by strength, not by count: it holds
        /// <see cref="MIN_CAPITAL_SHIPS"/> capital-class ships, or the own fleet's advantage over it is below
        /// <see cref="OUTMATCH_RATIO"/>. Human fleets of 4–5 capitals were never armadas under the old six-ship count.
        /// </summary>
        public static bool IsArmada(HostileArmada group, float fleetAdvantage) =>
            group.Strength.UnitCount >= MIN_SHIPS &&
            (group.CapitalShips >= MIN_CAPITAL_SHIPS || fleetAdvantage < OUTMATCH_RATIO);

        /// <summary>Single-linkage groups: <see cref="_group"/>[i] is the lowest index of ship i's group.</summary>
        private void Group(IReadOnlyList<Vector3> ships)
        {
            _group.Clear();
            for (int i = 0; i < ships.Count; i++) _group.Add(-1);
            const float LINK_SQUARED = LINK_RADIUS * LINK_RADIUS;
            for (int start = 0; start < ships.Count; start++)
            {
                if (_group[start] >= 0) continue;
                _group[start] = start;
                _open.Push(start);
                while (_open.Count > 0)
                {
                    int ship = _open.Pop();
                    for (int other = 0; other < ships.Count; other++)
                    {
                        if (_group[other] >= 0 || PlanarSquared(ships[ship], ships[other]) > LINK_SQUARED) continue;
                        _group[other] = start;
                        _open.Push(other);
                    }
                }
            }
        }

        // The nearest previous armada continues: its heading carries over and is re-measured once the sample is old enough.
        private void Continue(HostileArmada armada, float now)
        {
            HostileArmada match = null;
            float best = MATCH_DISTANCE * MATCH_DISTANCE;
            foreach (HostileArmada old in _previous)
            {
                if (_matched.Contains(old)) continue;
                float distance = PlanarSquared(old.Centre, armada.Centre);
                if (distance > best) continue;
                best = distance;
                match = old;
            }
            if (match == null)
            {
                armada.Velocity = Vector3.zero;
                armada.SampleCentre = armada.Centre;
                armada.SampleTime = now;
                return;
            }
            _matched.Add(match);
            float elapsed = now - match.SampleTime;
            if (elapsed >= HEADING_SAMPLE_SECONDS)
            {
                Vector3 moved = armada.Centre - match.SampleCentre;
                moved.y = 0f;
                armada.Velocity = moved / elapsed;
                armada.SampleCentre = armada.Centre;
                armada.SampleTime = now;
                return;
            }
            armada.Velocity = match.Velocity;
            armada.SampleCentre = match.SampleCentre;
            armada.SampleTime = match.SampleTime;
        }

        private static void FindThreatenedAsset(HostileArmada armada, IReadOnlyList<ArmadaAsset> assets)
        {
            Vector3 ahead = armada.Centre + armada.Velocity * LOOKAHEAD_SECONDS;
            armada.AssetKind = ArmadaAssetKind.None;
            float best = THREAT_REACH;
            foreach (ArmadaAsset asset in assets)
            {
                float gap = Mathf.Sqrt(PlanarSquared(ahead, asset.Position)) - asset.Radius;
                if (gap > best) continue;
                best = gap;
                armada.AssetKind = asset.Kind;
                armada.AssetPosition = asset.Position;
            }
        }

        private HostileArmada NextArmada()
        {
            HostileArmada armada;
            if (_spare.Count > 0)
            {
                armada = _spare[_spare.Count - 1];
                _spare.RemoveAt(_spare.Count - 1);
            }
            else armada = new HostileArmada();
            armada.Strength.Clear();
            return armada;
        }

        private static float PlanarSquared(Vector3 first, Vector3 second)
        {
            float x = first.x - second.x;
            float z = first.z - second.z;
            return x * x + z * z;
        }
    }
}
