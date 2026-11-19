using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;
using EmpireAtWar.Entities.EnemyFaction.Models.Intel;
using EmpireAtWar.Entities.EnemyFaction.Models.Objectives;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Presenters.ReinforcementZones;
using EmpireAtWar.Services.ReinforcementZones;
using EmpireAtWar.Services.Vision;
using UnityEngine;
using static EmpireAtWar.Utils.FormationConversion;

namespace EmpireAtWar.Services.Enemy
{
    /// <summary>
    /// Groups capture candidates into zone objectives: one per reinforcement zone holding a candidate, one per lone
    /// site outside every zone. Defenders are the known hostile units around the zone plus its visible armed
    /// hostile structures, so a fortified zone is a stronger target, never a hidden one.
    /// </summary>
    public sealed class EnemyZoneObjectiveCollector
    {
        /// <summary>Hostiles this close to a zone's edge fight for it: twice the common ship weapon range (500).</summary>
        public const float DEFENDER_REACH = 1000f;
        /// <summary>
        /// Armed structures have no weapon profile in the AI catalog, so each one counts as this damage per second
        /// against every class. Estimate, not calibrated against satellite or platform loadouts.
        /// </summary>
        public const float STATIC_DEFENSE_DPS = 60f;

        private static readonly UnitCombatProfile STATIC_PROFILE = CreateStaticProfile();

        private readonly IReinforcementZoneSource _zones;
        private readonly IPlayerRoster _roster;
        private readonly AiWorldSnapshot _world;
        private readonly IVisionService _vision;
        private readonly TeamAiPicture _picture;
        private readonly PlayerId _owner;
        private readonly List<EnemyZoneObjective> _pool = new List<EnemyZoneObjective>();
        private readonly List<EnemyZoneObjective> _objectives = new List<EnemyZoneObjective>();
        private readonly List<Vector3> _centres = new List<Vector3>();
        private readonly List<float> _radii = new List<float>();
        private readonly List<float> _priorities = new List<float>();
        private readonly List<bool> _threatened = new List<bool>();
        private readonly List<ReinforcementZonePresenter> _areaZones = new List<ReinforcementZonePresenter>();
        private readonly Dictionary<EnemyCaptureObjective, int> _areaOf = new Dictionary<EnemyCaptureObjective, int>();
        private readonly Dictionary<ReinforcementZonePresenter, int> _zoneArea =
            new Dictionary<ReinforcementZonePresenter, int>();
        private readonly Predicate<HostileSighting> _includeDefender;
        private Vector3 _defenderCentre;
        private float _defenderRadiusSquared;

        public EnemyZoneObjectiveCollector(IReinforcementZoneSource zones, IPlayerRoster roster, AiWorldSnapshot world,
            IVisionService vision, TeamAiPicture picture, PlayerId owner)
        {
            _zones = zones;
            _roster = roster;
            _world = world;
            _vision = vision;
            _picture = picture;
            _owner = owner;
            _includeDefender = IncludeDefender;
        }

        public IReadOnlyList<EnemyZoneObjective> Objectives => _objectives;

        public int GetArea(EnemyCaptureObjective candidate) => _areaOf[candidate];

        public Vector3 GetCentre(int area) => _centres[area];

        /// <summary>The reinforcement zone of the area; null for a lone site.</summary>
        public ReinforcementZonePresenter GetZone(int area) => _areaZones[area];

        /// <param name="priorities">Per candidate, value minus travel cost (same order); null rates every zone 0.</param>
        public void Collect(IReadOnlyList<EnemyCaptureObjective> candidates, IReadOnlyList<float> priorities,
            Vector3 origin, float speed, Func<Vector3, bool> isNearHostileBase)
        {
            _objectives.Clear();
            _centres.Clear();
            _radii.Clear();
            _priorities.Clear();
            _threatened.Clear();
            _areaZones.Clear();
            _areaOf.Clear();
            _zoneArea.Clear();
            for (int i = 0; i < candidates.Count; i++)
            {
                EnemyCaptureObjective candidate = candidates[i];
                ReinforcementZonePresenter zone = FindZone(candidate.Position);
                float priority = priorities == null ? 0f : priorities[i];
                if (zone == null || !_zoneArea.TryGetValue(zone, out int area))
                {
                    area = _centres.Count;
                    _centres.Add(zone != null ? zone.Center : candidate.Position);
                    _radii.Add(zone != null ? zone.Radius : candidate.Radius);
                    _priorities.Add(priority);
                    _threatened.Add(false);
                    _areaZones.Add(zone);
                    if (zone != null) _zoneArea.Add(zone, area);
                }
                _areaOf.Add(candidate, area);
                _priorities[area] = Mathf.Max(_priorities[area], priority);
                _threatened[area] |= candidate.IsThreatened;
            }

            for (int area = 0; area < _centres.Count; area++)
            {
                if (_pool.Count == area) _pool.Add(new EnemyZoneObjective());
                EnemyZoneObjective objective = _pool[area];
                Vector3 centre = _centres[area];
                ReinforcementZonePresenter zone = _areaZones[area];
                int statics = BuildDefenders(objective.Defenders, centre, _radii[area]);
                string deny = !_threatened[area] && isNearHostileBase(centre) ? "hostileBase" : null;
                objective.Reset(zone != null ? $"zone:{IndexOf(zone)}" : $"site:({centre.x:F0} {centre.z:F0})",
                    zone != null && zone.IsMain, IsOwnSide(centre), _priorities[area],
                    PlanarDistance(origin, centre) / Mathf.Max(1f, speed), statics, deny);
                _objectives.Add(objective);
            }
        }

        /// <summary>
        /// The nearest uncontested team-held zone to the area with no known hostile near it; otherwise
        /// <paramref name="home"/>. Never a zone the team does not hold.
        /// </summary>
        public Vector3 GetStagingPoint(int area, Vector3 home)
        {
            Vector3 target = _centres[area];
            ReinforcementZonePresenter targetZone = _areaZones[area];
            Vector3 best = home;
            float bestDistance = PlanarDistance(home, target);
            foreach (ReinforcementZonePresenter zone in _zones.Zones)
            {
                if (zone == targetZone || zone.IsContested || zone.Owner.IsNone ||
                    !_roster.IsAllied(_owner, zone.Owner) || HasHostileNear(zone.Center, zone.Radius)) continue;
                float distance = PlanarDistance(zone.Center, target);
                if (distance >= bestDistance) continue;
                best = ToVector(ToPoint(zone.Center));
                bestDistance = distance;
            }
            return best;
        }

        /// <summary>A known hostile unit is within <see cref="DEFENDER_REACH"/> of the circle.</summary>
        public bool HasHostileNear(Vector3 centre, float radius)
        {
            float reach = DEFENDER_REACH + radius;
            IReadOnlyList<HostileSighting> sightings = _picture.ConfidentSightings;
            for (int i = 0; i < sightings.Count; i++)
                if (_roster.IsHostile(_owner, sightings[i].Owner) &&
                    PlanarDistance(ToVector(sightings[i].Position), centre) <= reach) return true;
            return false;
        }

        /// <summary>A visible armed hostile structure covers <paramref name="position"/> with its weapons.</summary>
        public bool HasStaticDefenseNear(Vector3 position)
        {
            IReadOnlyList<AiWorldSnapshot.Entry> structures = _world.RaidStructures;
            for (int i = 0; i < structures.Count; i++)
            {
                AiWorldSnapshot.Entry entry = structures[i];
                if (_roster.IsHostile(_owner, entry.Owner) && !entry.Entity.HealthModel.IsDestroyed &&
                    entry.Entity.TryGetFacade(out IUnitCombatObserverFacade combat) && combat.WeaponRange > 0f &&
                    PlanarDistance(entry.Position, position) <= combat.WeaponRange &&
                    _vision.IsVisible(_owner, entry.Entity)) return true;
            }
            return false;
        }

        private int BuildDefenders(ForceComposition defenders, Vector3 centre, float radius)
        {
            _defenderCentre = centre;
            float reach = DEFENDER_REACH + radius;
            _defenderRadiusSquared = reach * reach;
            _picture.BuildKnownFromConfident(defenders, _includeDefender);
            int statics = 0;
            IReadOnlyList<AiWorldSnapshot.Entry> structures = _world.RaidStructures;
            for (int i = 0; i < structures.Count; i++)
            {
                AiWorldSnapshot.Entry entry = structures[i];
                if (!_roster.IsHostile(_owner, entry.Owner) || entry.Entity.HealthModel.IsDestroyed ||
                    !entry.Entity.TryGetFacade(out IUnitCombatObserverFacade combat) || combat.WeaponRange <= 0f ||
                    PlanarDistance(entry.Position, centre) > 2f * radius + combat.WeaponRange ||
                    !_vision.IsVisible(_owner, entry.Entity)) continue;
                defenders.Add(STATIC_PROFILE, entry.Entity.HealthModel.Hull, entry.Entity.HealthModel.Shields, 1f);
                statics++;
            }
            return statics;
        }

        private bool IncludeDefender(HostileSighting sighting)
        {
            if (!_roster.IsHostile(_owner, sighting.Owner)) return false;
            Vector3 offset = ToVector(sighting.Position) - _defenderCentre;
            offset.y = 0f;
            return offset.sqrMagnitude <= _defenderRadiusSquared;
        }

        private ReinforcementZonePresenter FindZone(Vector3 position)
        {
            foreach (ReinforcementZonePresenter zone in _zones.Zones)
                if (EnemyShipTaskGroupPlanner.IsInZone(zone, position)) return zone;
            return null;
        }

        private int IndexOf(ReinforcementZonePresenter zone)
        {
            for (int i = 0; i < _zones.Zones.Count; i++)
                if (_zones.Zones[i] == zone) return i;
            return -1;
        }

        private bool IsOwnSide(Vector3 position)
        {
            float allied = float.PositiveInfinity;
            float hostile = float.PositiveInfinity;
            foreach (AiWorldSnapshot.Entry station in _world.Bases)
            {
                float distance = PlanarDistance(position, station.Position);
                if (_roster.IsAllied(_owner, station.Owner)) allied = Mathf.Min(allied, distance);
                else if (_roster.IsHostile(_owner, station.Owner)) hostile = Mathf.Min(hostile, distance);
            }
            return allied <= hostile;
        }

        private static UnitCombatProfile CreateStaticProfile()
        {
            float[] dps = new float[ForceComposition.ClassSlots];
            for (int i = 0; i < dps.Length; i++) dps[i] = STATIC_DEFENSE_DPS;
            return new UnitCombatProfile(ShipClass.Structure, 0f, 0f, dps, dps,
                new float[ForceComposition.ClassSlots], Array.Empty<UnitCombatProfile>());
        }

        private static float PlanarDistance(Vector3 a, Vector3 b)
        {
            Vector3 offset = a - b;
            offset.y = 0f;
            return offset.magnitude;
        }
    }
}
