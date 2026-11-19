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
using EmpireAtWar.Services.Saves;
using EmpireAtWar.Services.Vision;
using UnityEngine;
using static EmpireAtWar.Utils.FormationConversion;

namespace EmpireAtWar.Services.Enemy
{
    /// <summary>
    /// Groups capture candidates into zone objectives: one per reinforcement zone holding a candidate, one per lone
    /// site outside every zone. Defenders are the known hostile units around the zone plus its visible armed
    /// hostile structures, so a fortified zone is a stronger target, never a hidden one. The AI's own armed
    /// structures and wings in the zone are its support; a zone whose relay the team holds or where the AI has a
    /// satellite standing is held. No zone is denied for the route to it: ships path around.
    /// </summary>
    public sealed class EnemyZoneObjectiveCollector
    {
        /// <summary>Hostiles this close to a zone's edge fight for it: twice the common ship weapon range (500).</summary>
        public const float DEFENDER_REACH = 1000f;
        /// <summary>
        /// Hostiles this close to a zone's edge defend it: one common ship weapon range. A force fighting just
        /// outside the zone is not counted against it.
        /// </summary>
        public const float ZONE_DEFENDER_MARGIN = 500f;
        /// <summary>
        /// Armed structures have no weapon profile in the AI catalog, so each one counts as this damage per second
        /// against every class. Estimate, not calibrated against satellite or platform loadouts.
        /// </summary>
        public const float STATIC_DEFENSE_DPS = 60f;
        /// <summary>Weapon reach assumed for every armed structure: the common ship weapon range.</summary>
        public const float STATIC_DEFENSE_REACH = 500f;
        /// <summary>Before this battle time a zone nobody has seen is rated empty, so the opening is not stalled.</summary>
        public const float UNSCOUTED_GRACE_SECONDS = 180f;

        private static readonly UnitCombatProfile STATIC_PROFILE = CreateStaticProfile();

        private readonly IReinforcementZoneSource _zones;
        private readonly IPlayerRoster _roster;
        private readonly AiWorldSnapshot _world;
        private readonly IVisionService _vision;
        private readonly TeamAiPicture _picture;
        private readonly ForceCompositionBuilder _forceBuilder;
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
        private readonly Predicate<HostileSighting> _includeHostile;
        private readonly ForceComposition _knownHostileForce = new ForceComposition();
        private Vector3 _defenderCentre;
        private float _defenderRadiusSquared;

        public EnemyZoneObjectiveCollector(IReinforcementZoneSource zones, IPlayerRoster roster, AiWorldSnapshot world,
            IVisionService vision, TeamAiPicture picture, ForceCompositionBuilder forceBuilder, PlayerId owner)
        {
            _forceBuilder = forceBuilder;
            _zones = zones;
            _roster = roster;
            _world = world;
            _vision = vision;
            _picture = picture;
            _owner = owner;
            _includeDefender = IncludeDefender;
            _includeHostile = sighting => _roster.IsHostile(_owner, sighting.Owner);
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
                objective.OwnSupport.Clear();
                int ownStructures = AddOwnArmedStructures(objective.OwnSupport, centre, 2f * _radii[area]);
                AddOwnWings(objective.OwnSupport, centre, 2f * _radii[area]);
                bool held = ownStructures > 0 || zone != null && !zone.Owner.IsNone && _roster.IsAllied(_owner, zone.Owner);
                bool isCentre = zone != null && zone.IsMain;
                bool ownSide = IsOwnSide(centre);
                string deny = !_threatened[area] && isNearHostileBase(centre) ? "hostileBase" : null;
                if (deny == null && !ownSide && objective.Defenders.IsEmpty) AddUnscoutedShare(objective.Defenders, centre);
                objective.Reset(zone != null ? $"zone:{IndexOf(zone)}" : $"site:({centre.x:F0} {centre.z:F0})",
                    isCentre, ownSide, _priorities[area],
                    PlanarDistance(origin, centre) / Mathf.Max(1f, speed), statics, deny, _threatened[area], held,
                    ownStructures);
                _objectives.Add(objective);
            }
        }

        /// <summary>
        /// An enemy-side zone the team cannot see is never rated empty: it is assumed to hold an even share of the
        /// hostile force the team remembers, split over the zones the hostiles hold.
        /// </summary>
        private void AddUnscoutedShare(ForceComposition defenders, Vector3 centre)
        {
            if (BattleClock.CurrentElapsed < UNSCOUTED_GRACE_SECONDS) return;
            _picture.BuildKnownFromConfident(_knownHostileForce, _includeHostile);
            if (_knownHostileForce.IsEmpty || _vision.IsVisible(_owner, centre)) return;
            int hostileZones = 0;
            foreach (ReinforcementZonePresenter zone in _zones.Zones)
                if (!zone.Owner.IsNone && _roster.IsHostile(_owner, zone.Owner)) hostileZones++;
            defenders.AddShare(_knownHostileForce, 1f / Mathf.Max(1, hostileZones));
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

        /// <summary>
        /// Adds every visible armed hostile structure within <paramref name="reach"/> of <paramref name="centre"/> to
        /// <paramref name="defenders"/> and returns how many. Satellites and defend platforms are not units, so
        /// intel never lists them.
        /// </summary>
        public static int AddArmedStructures(ForceComposition defenders, AiWorldSnapshot world, IPlayerRoster roster,
            IVisionService vision, PlayerId owner, Vector3 centre, float reach)
        {
            int count = 0;
            IReadOnlyList<AiWorldSnapshot.Entry> structures = world.RaidStructures;
            for (int i = 0; i < structures.Count; i++)
            {
                AiWorldSnapshot.Entry entry = structures[i];
                if (!IsArmedHostileStructure(entry, roster, vision, owner) ||
                    PlanarDistance(entry.Position, centre) > reach) continue;
                defenders.Add(STATIC_PROFILE, entry.Entity.HealthModel.Hull, entry.Entity.HealthModel.Shields, 1f);
                count++;
            }
            return count;
        }

        /// <summary>
        /// Adds this AI's living armed structures within <paramref name="reach"/> of the zone centre and returns how many.
        /// </summary>
        private int AddOwnArmedStructures(ForceComposition support, Vector3 centre, float reach)
        {
            int count = 0;
            IReadOnlyList<AiWorldSnapshot.Entry> structures = _world.RaidStructures;
            for (int i = 0; i < structures.Count; i++)
            {
                AiWorldSnapshot.Entry entry = structures[i];
                if (entry.Owner != _owner || entry.Entity.HealthModel.IsDestroyed ||
                    !entry.Entity.TryGetFacade(out IStructureKindFacade structure) ||
                    structure.Kind != StructureKind.DefendPlatform ||
                    PlanarDistance(entry.Position, centre) > reach) continue;
                support.Add(STATIC_PROFILE, entry.Entity.HealthModel.Hull, entry.Entity.HealthModel.Shields, 1f);
                count++;
            }
            return count;
        }

        /// <summary>Adds this AI's wings within <paramref name="reach"/> of the zone centre at their current strength.</summary>
        private void AddOwnWings(ForceComposition support, Vector3 centre, float reach)
        {
            IReadOnlyList<AiWorldSnapshot.Entry> owned = _world.GetOwnedUnits(_owner);
            for (int i = 0; i < owned.Count; i++)
            {
                AiWorldSnapshot.Entry entry = owned[i];
                if (!entry.UnitTypeId.IsSquadron || PlanarDistance(entry.Position, centre) > reach) continue;
                _forceBuilder.AddUnit(support, entry.Entity);
            }
        }

        /// <summary>A living, visible hostile satellite or defend platform.</summary>
        public static bool IsArmedHostileStructure(AiWorldSnapshot.Entry entry, IPlayerRoster roster,
            IVisionService vision, PlayerId owner) =>
            roster.IsHostile(owner, entry.Owner) && !entry.Entity.HealthModel.IsDestroyed &&
            entry.Entity.TryGetFacade(out IStructureKindFacade structure) &&
            structure.Kind == StructureKind.DefendPlatform && vision.IsVisible(owner, entry.Entity);

        /// <summary>Adds the visible armed hostile structures whose weapons cover <paramref name="position"/>.</summary>
        public int AddStaticDefenders(ForceComposition defenders, Vector3 position) =>
            AddArmedStructures(defenders, _world, _roster, _vision, _owner, position, STATIC_DEFENSE_REACH);

        private int BuildDefenders(ForceComposition defenders, Vector3 centre, float radius)
        {
            _defenderCentre = centre;
            float reach = ZONE_DEFENDER_MARGIN + radius;
            _defenderRadiusSquared = reach * reach;
            _picture.BuildKnownFromConfident(defenders, _includeDefender);
            return AddArmedStructures(defenders, _world, _roster, _vision, _owner, centre,
                2f * radius + STATIC_DEFENSE_REACH);
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
