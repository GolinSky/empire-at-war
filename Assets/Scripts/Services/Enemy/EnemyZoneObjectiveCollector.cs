using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;
using EmpireAtWar.Entities.EnemyFaction.Models.Intel;
using EmpireAtWar.Entities.EnemyFaction.Models.Objectives;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Presenters.ReinforcementZones;
using EmpireAtWar.Services.ReinforcementZones;
using EmpireAtWar.Services.Repair;
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
    /// satellite standing is held. No zone is denied for the route to it: ships path around. Each zone is worth its
    /// share of the team income plus relay access and the own platforms standing there (W47, W67).
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
        /// <summary>Weapon reach assumed for every armed structure: the common ship weapon range.</summary>
        public const float STATIC_DEFENSE_REACH = 500f;
        /// <summary>Before this battle time a zone nobody has seen is rated empty, so the opening is not stalled.</summary>
        public const float UNSCOUTED_GRACE_SECONDS = 180f;
        /// <summary>Team income before any mine: the station income floor (same estimate as the strategic context).</summary>
        public const float TEAM_BASE_INCOME = 15f;
        /// <summary>Worth of holding a relay (a forward drop point), as a share of the team income.</summary>
        public const float RELAY_ACCESS_SHARE = 0.15f;
        /// <summary>Worth of each own platform or satellite already standing in the zone, as a share of the team income.</summary>
        public const float PLATFORM_WORTH_SHARE = 0.05f;
        /// <summary>A zone's value in credits is its worth share of the team income over this time.</summary>
        public const float ZONE_VALUE_HORIZON_SECONDS = 180f;
        /// <summary>A held-back garrison waits this far outside the zone edge: one common ship weapon range.</summary>
        public const float HOLD_BACK_MARGIN = 500f;

        private readonly IReinforcementZoneSource _zones;
        private readonly IPlayerRoster _roster;
        private readonly AiWorldSnapshot _world;
        private readonly IVisionService _vision;
        private readonly TeamAiPicture _picture;
        private readonly ForceCompositionBuilder _forceBuilder;
        private readonly HostileIntelModel _intel;
        private readonly PlayerId _owner;
        private readonly List<EnemyZoneObjective> _pool = new List<EnemyZoneObjective>();
        private readonly List<EnemyZoneObjective> _objectives = new List<EnemyZoneObjective>();
        private readonly List<Vector3> _centres = new List<Vector3>();
        private readonly List<float> _radii = new List<float>();
        private readonly List<float> _priorities = new List<float>();
        private readonly List<bool> _threatened = new List<bool>();
        private readonly List<int> _openSites = new List<int>();
        private readonly List<int> _unbuiltMines = new List<int>();
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
            IVisionService vision, TeamAiPicture picture, ForceCompositionBuilder forceBuilder, HostileIntelModel intel,
            PlayerId owner)
        {
            _forceBuilder = forceBuilder;
            _intel = intel;
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

        /// <summary>The zone's radius; a lone site's capture radius.</summary>
        public float GetRadius(int area) => _radii[area];

        /// <summary>
        /// Where a held-back garrison waits (<see cref="EnemyZonePlan.HoldBackAndCounter"/>): just outside the zone edge,
        /// toward the area's staging point.
        /// </summary>
        public Vector3 GetHoldBackPoint(int area, Vector3 home)
        {
            Vector3 centre = _centres[area];
            Vector3 direction = GetStagingPoint(area, home) - centre;
            direction.y = 0f;
            if (direction.sqrMagnitude < 1f) direction = home - centre;
            direction.y = 0f;
            if (direction.sqrMagnitude < 1f) return centre;
            return centre + direction.normalized * (_radii[area] + HOLD_BACK_MARGIN);
        }

        /// <param name="priorities">Per candidate, value minus travel cost (same order); null rates every zone 0.</param>
        public void Collect(IReadOnlyList<EnemyCaptureObjective> candidates, IReadOnlyList<float> priorities,
            Vector3 origin, float speed, Func<Vector3, bool> isNearHostileBase)
        {
            _objectives.Clear();
            _centres.Clear();
            _radii.Clear();
            _priorities.Clear();
            _threatened.Clear();
            _openSites.Clear();
            _unbuiltMines.Clear();
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
                    _openSites.Add(0);
                    _unbuiltMines.Add(0);
                    _areaZones.Add(zone);
                    if (zone != null) _zoneArea.Add(zone, area);
                }
                _areaOf.Add(candidate, area);
                _priorities[area] = Mathf.Max(_priorities[area], priority);
                _threatened[area] |= candidate.IsThreatened;
                if (!_roster.IsAllied(_owner, candidate.Owner)) _openSites[area]++;
                if (candidate.IsMine && !candidate.BlockingFacilityId.HasValue) _unbuiltMines[area]++;
            }

            float teamIncome = GetTeamIncome(out float mineIncome);

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
                AddAlliedRepairSources(objective.OwnSupport, _world, _roster, _owner, centre);
                bool held = ownStructures > 0 || zone != null && !zone.Owner.IsNone && _roster.IsAllied(_owner, zone.Owner);
                bool isCentre = zone != null && zone.IsMain;
                bool ownSide = IsOwnSide(centre);
                string deny = !_threatened[area] && isNearHostileBase(centre) ? "hostileBase" : null;
                if (deny == null && !ownSide && objective.Defenders.IsEmpty) AddUnscoutedShare(objective.Defenders, centre);
                objective.Reset(zone != null ? $"zone:{IndexOf(zone)}" : $"site:({centre.x:F0} {centre.z:F0})",
                    isCentre, ownSide, _priorities[area],
                    PlanarDistance(origin, centre) / Mathf.Max(1f, speed), statics, deny, _threatened[area], held,
                    ownStructures, _openSites[area]);
                bool owned = zone != null ? !zone.Owner.IsNone && _roster.IsAllied(_owner, zone.Owner) : _openSites[area] == 0;
                float worthShare = (GetZoneIncome(area, mineIncome) + (zone != null ? RELAY_ACCESS_SHARE * teamIncome : 0f)) /
                    teamIncome + ownStructures * PLATFORM_WORTH_SHARE;
                objective.SetWorth(owned, worthShare * teamIncome * ZONE_VALUE_HORIZON_SECONDS, worthShare);
                _objectives.Add(objective);
            }
        }

        /// <summary>
        /// Team income: <see cref="TEAM_BASE_INCOME"/> plus every running allied mine. <paramref name="mineIncome"/> is the
        /// average running mine on the map (the base income while none runs): what an unbuilt mine site is worth.
        /// </summary>
        private float GetTeamIncome(out float mineIncome)
        {
            float team = TEAM_BASE_INCOME;
            float all = 0f;
            int running = 0;
            IReadOnlyList<AiWorldSnapshot.Entry> structures = _world.RaidStructures;
            for (int i = 0; i < structures.Count; i++)
            {
                AiWorldSnapshot.Entry entry = structures[i];
                if (!entry.IsMiningFacility || entry.Entity.HealthModel.IsDestroyed) continue;
                float income = entry.Entity.GetFacade<IMiningFacilityFacade>().GrossIncome;
                if (income <= 0f) continue;
                all += income;
                running++;
                if (_roster.IsAllied(_owner, entry.Owner)) team += income;
            }
            mineIncome = running > 0 ? all / running : TEAM_BASE_INCOME;
            return team;
        }

        /// <summary>Income of the running mines in the area (any owner) plus its unbuilt mine sites at <paramref name="mineIncome"/>.</summary>
        private float GetZoneIncome(int area, float mineIncome)
        {
            float income = _unbuiltMines[area] * mineIncome;
            ReinforcementZonePresenter zone = _areaZones[area];
            IReadOnlyList<AiWorldSnapshot.Entry> structures = _world.RaidStructures;
            for (int i = 0; i < structures.Count; i++)
            {
                AiWorldSnapshot.Entry entry = structures[i];
                if (!entry.IsMiningFacility || entry.Entity.HealthModel.IsDestroyed ||
                    (zone != null ? !EnemyShipTaskGroupPlanner.IsInZone(zone, entry.Position)
                        : PlanarDistance(entry.Position, _centres[area]) > _radii[area])) continue;
                income += entry.Entity.GetFacade<IMiningFacilityFacade>().GrossIncome;
            }
            return income;
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
        /// intel never lists them. Each is rated by its platform type's weapon loadout. One seen once stays counted
        /// while it stands (<paramref name="intel"/> remembers it): a zone with known platforms is never rated empty.
        /// </summary>
        public static int AddArmedStructures(ForceComposition defenders, AiWorldSnapshot world, IPlayerRoster roster,
            IVisionService vision, ForceCompositionBuilder forces, HostileIntelModel intel, PlayerId owner,
            Vector3 centre, float reach)
        {
            int count = 0;
            IReadOnlyList<AiWorldSnapshot.Entry> structures = world.RaidStructures;
            for (int i = 0; i < structures.Count; i++)
            {
                AiWorldSnapshot.Entry entry = structures[i];
                if (PlanarDistance(entry.Position, centre) > reach || !IsArmedHostileStructure(entry, roster, owner))
                    continue;
                if (vision.IsVisible(owner, entry.Entity)) intel.RememberStructure(entry.Id);
                else if (!intel.IsStructureKnown(entry.Id)) continue;
                forces.AddStructure(defenders, entry.Entity);
                count++;
            }
            return count;
        }

        /// <summary>Adds every living armed structure of the team within <paramref name="reach"/> of <paramref name="centre"/>.</summary>
        public static void AddAlliedArmedStructures(ForceComposition force, AiWorldSnapshot world, IPlayerRoster roster,
            ForceCompositionBuilder forces, PlayerId owner, Vector3 centre, float reach)
        {
            IReadOnlyList<AiWorldSnapshot.Entry> structures = world.RaidStructures;
            for (int i = 0; i < structures.Count; i++)
            {
                AiWorldSnapshot.Entry entry = structures[i];
                if (!roster.IsAllied(owner, entry.Owner) || entry.Entity.HealthModel.IsDestroyed ||
                    !entry.Entity.TryGetFacade(out IStructureKindFacade structure) ||
                    structure.Kind != StructureKind.DefendPlatform ||
                    PlanarDistance(entry.Position, centre) > reach) continue;
                forces.AddStructure(force, entry.Entity);
            }
        }

        /// <summary>
        /// Adds the known hostile repairers whose repair radius covers <paramref name="centre"/>: they heal the hostile
        /// side of a fight there. Seen once, a repairer stays counted while it stands, like an armed structure.
        /// </summary>
        public static void AddHostileRepairSources(ForceComposition force, AiWorldSnapshot world, IPlayerRoster roster,
            IVisionService vision, HostileIntelModel intel, PlayerId owner, Vector3 centre)
        {
            IReadOnlyList<AiWorldSnapshot.Entry> structures = world.RaidStructures;
            for (int i = 0; i < structures.Count; i++)
            {
                AiWorldSnapshot.Entry entry = structures[i];
                if (!roster.IsHostile(owner, entry.Owner) || !TryGetRepairerCovering(entry, centre, out RepairProfile profile))
                    continue;
                if (vision.IsVisible(owner, entry.Entity)) intel.RememberStructure(entry.Id);
                else if (!intel.IsStructureKnown(entry.Id)) continue;
                force.AddRepairSource(profile.FractionPerTick / profile.Interval, profile.MaxHealersPerTarget);
            }
        }

        /// <summary>Adds the team's working repairers whose repair radius covers <paramref name="centre"/>.</summary>
        public static void AddAlliedRepairSources(ForceComposition force, AiWorldSnapshot world, IPlayerRoster roster,
            PlayerId owner, Vector3 centre)
        {
            IReadOnlyList<AiWorldSnapshot.Entry> structures = world.RaidStructures;
            for (int i = 0; i < structures.Count; i++)
            {
                AiWorldSnapshot.Entry entry = structures[i];
                if (!roster.IsAllied(owner, entry.Owner) || !TryGetRepairerCovering(entry, centre, out RepairProfile profile))
                    continue;
                force.AddRepairSource(profile.FractionPerTick / profile.Interval, profile.MaxHealersPerTarget);
            }
        }

        private static bool TryGetRepairerCovering(AiWorldSnapshot.Entry entry, Vector3 centre, out RepairProfile profile)
        {
            profile = null;
            if (entry.Entity.HealthModel.IsDestroyed || !entry.Entity.TryGetFacade(out IRepairSourceFacade source) ||
                !source.CanRepair || PlanarDistance(entry.Position, centre) > source.Profile.Radius) return false;
            profile = source.Profile;
            return true;
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
                _forceBuilder.AddStructure(support, entry.Entity);
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
            IsArmedHostileStructure(entry, roster, owner) && vision.IsVisible(owner, entry.Entity);

        private static bool IsArmedHostileStructure(AiWorldSnapshot.Entry entry, IPlayerRoster roster, PlayerId owner) =>
            roster.IsHostile(owner, entry.Owner) && !entry.Entity.HealthModel.IsDestroyed &&
            entry.Entity.TryGetFacade(out IStructureKindFacade structure) &&
            structure.Kind == StructureKind.DefendPlatform;

        /// <summary>Adds the visible armed hostile structures whose weapons cover <paramref name="position"/>.</summary>
        public int AddStaticDefenders(ForceComposition defenders, Vector3 position) =>
            AddArmedStructures(defenders, _world, _roster, _vision, _forceBuilder, _intel, _owner, position,
                STATIC_DEFENSE_REACH);

        private int BuildDefenders(ForceComposition defenders, Vector3 centre, float radius)
        {
            _defenderCentre = centre;
            float reach = ZONE_DEFENDER_MARGIN + radius;
            _defenderRadiusSquared = reach * reach;
            _picture.BuildKnownFromConfident(defenders, _includeDefender);
            AddHostileRepairSources(defenders, _world, _roster, _vision, _intel, _owner, centre);
            return AddArmedStructures(defenders, _world, _roster, _vision, _forceBuilder, _intel, _owner, centre,
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

        /// <summary>
        /// The nearest allied station is no farther than the nearest hostile one. With every allied station lost, the
        /// team's fixed home relays stand in for them, so the own half of the map stays own side.
        /// </summary>
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
            if (float.IsPositiveInfinity(allied))
                foreach (ReinforcementZonePresenter zone in _zones.Zones)
                    if (!zone.IsCapturable && !zone.Owner.IsNone && _roster.IsAllied(_owner, zone.Owner))
                        allied = Mathf.Min(allied, PlanarDistance(position, zone.Center));
            return allied <= hostile;
        }

        private static float PlanarDistance(Vector3 a, Vector3 b)
        {
            Vector3 offset = a - b;
            offset.y = 0f;
            return offset.magnitude;
        }
    }
}
