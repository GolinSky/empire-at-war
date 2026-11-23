using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;
using EmpireAtWar.Entities.EnemyFaction.Models.Intel;
using EmpireAtWar.Entities.EnemyFaction.Models.Objectives;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Presenters.ReinforcementZones;
using EmpireAtWar.Services.ReinforcementZones;
using UnityEngine;
using static EmpireAtWar.Utils.FormationConversion;

namespace EmpireAtWar.Services.Enemy
{
    /// <summary>
    /// Fills the hold inputs and the plan of every pool zone (W47, decision D4) and keeps the player's
    /// <see cref="EnemyZoneCaution"/> memory up to date: retakes, failed captures and own ships lost at each zone.
    /// Help time = the nearest own or allied group, or a reinforcement dropped nearby when money and capacity allow.
    /// Threat = known hostiles that reach the zone before help plus what the hostile income drops near it in that time.
    /// </summary>
    public sealed class EnemyZoneHoldAssessor
    {
        /// <summary>Help is the arrival of this many own or allied ships: one ship is no relief.</summary>
        public const int HELP_GROUP_SHIPS = 3;
        /// <summary>Help never counts as later than this: a garrison need not outlast more.</summary>
        public const float MAX_HELP_SECONDS = 180f;
        /// <summary>A team drop point (held relay, station) this close to the zone edge lands reinforcements for it.</summary>
        public const float SPAWN_NEAR_DISTANCE = 1500f;
        /// <summary>Hostiles holding a relay or station this close to the zone edge have spawn access to it.</summary>
        public const float HOSTILE_SPAWN_NEAR_DISTANCE = 2000f;

        private readonly IReinforcementZoneSource _zones;
        private readonly IPlayerRoster _roster;
        private readonly AiWorldSnapshot _world;
        private readonly TeamAiPicture _picture;
        private readonly ForceCompositionBuilder _forces;
        private readonly PlayerId _owner;
        private readonly EnemyZoneCaution _caution;
        private readonly EnemyZoneEvaluator _evaluator = new EnemyZoneEvaluator();
        private readonly ForceComposition _hostileReference = new ForceComposition();
        private readonly ForceComposition _withAllies = new ForceComposition();
        private readonly ForceComposition _reinforcement = new ForceComposition();
        private readonly List<float> _arrivals = new List<float>();
        private readonly List<string> _reservationIds = new List<string>();
        private readonly HashSet<string> _present = new HashSet<string>();
        private readonly HashSet<long> _alive = new HashSet<long>();
        private Dictionary<long, (string Zone, float Price)> _near = new Dictionary<long, (string, float)>();
        private Dictionary<long, (string Zone, float Price)> _nearNext = new Dictionary<long, (string, float)>();
        private readonly Predicate<HostileSighting> _includeHostileShip;
        private readonly Predicate<HostileSighting> _includeThreat;
        private Vector3 _threatCentre;
        private float _threatRadius;
        private float _threatSeconds;
        private float _cheapestShip;

        public EnemyZoneHoldAssessor(IReinforcementZoneSource zones, IPlayerRoster roster, AiWorldSnapshot world,
            TeamAiPicture picture, ForceCompositionBuilder forces, PlayerId owner, EnemyZoneCaution caution)
        {
            _zones = zones;
            _roster = roster;
            _world = world;
            _picture = picture;
            _forces = forces;
            _owner = owner;
            _caution = caution;
            _includeHostileShip = sighting => sighting.UnitTypeId.IsShip && _roster.IsHostile(_owner, sighting.Owner);
            _includeThreat = IncludeThreat;
        }

        /// <param name="speed">Speed of the pool: travel of reinforcements to and from drop points.</param>
        /// <param name="ownFleet">Every own ship: loss shares and the "even with ally help" rating.</param>
        /// <param name="hostileIncome">Estimated hostile income per second this player faces.</param>
        /// <param name="rateReach">Reach rating of the own fleet at a point (fleet-advantage scale).</param>
        public void Apply(EnemyZoneObjectiveCollector collector, float now, float speed, ForceComposition ownFleet,
            EnemyReinforcementBudget budget, float hostileIncome, float tradeCorrection, Func<Vector3, float> rateReach)
        {
            Observe(collector, now);
            _picture.BuildKnownFromConfident(_hostileReference, _includeHostileShip);
            _withAllies.CopyFrom(ownFleet);
            _picture.Cooperation.AddAvailableAlliedStrength(_withAllies, _owner, now);
            IReadOnlyList<EnemyZoneObjective> zones = collector.Objectives;
            for (int area = 0; area < zones.Count; area++)
            {
                EnemyZoneObjective zone = zones[area];
                Vector3 centre = collector.GetCentre(area);
                float radius = collector.GetRadius(area);
                float help = GetHelpSeconds(centre, radius, speed, budget);
                BuildThreat(zone.Threat, centre, radius, help, speed, budget.ReadySeconds, hostileIncome);
                bool hostileNear = collector.HasHostileNear(centre, radius) || IsArmadaHeadingFor(centre, radius);
                zone.SetHold(zone.IsOwned ? _caution.GetHeldSeconds(zone.Id, now) : 0f, hostileNear, help, tradeCorrection);

                int failures = _caution.CountFailures(zone.Id, now);
                int retakes = _caution.CountRetakes(zone.Id, now);
                bool spawnNear = GetHostileSpawnDistance(centre) <= HOSTILE_SPAWN_NEAR_DISTANCE + radius;
                float level = EnemyZoneCaution.GetLevel(failures,
                    _caution.GetLosses(zone.Id, now) / Mathf.Max(1f, ownFleet.Value), spawnNear,
                    GetArmadaDistance(centre), rateReach(centre));
                bool important = zone.IsCentre || zone.WorthShare >= EnemyZoneCaution.IMPORTANT_WORTH_SHARE;
                bool gainsWithAlly = zone.IsDenied || _evaluator.Assess(zone, _withAllies,
                    EnemyZoneEvaluator.COMMIT_ADVANTAGE).Verdict == EnemyZoneVerdict.Go;
                EnemyZonePlan plan = _caution.IsSkipped(zone.Id, now) && !zone.IsHeld ? EnemyZonePlan.Skip :
                    EnemyZoneCaution.SelectPlan(level, failures, retakes, zone.IsHeld, important, gainsWithAlly, spawnNear);
                if (plan == EnemyZonePlan.Skip && !_caution.IsSkipped(zone.Id, now)) _caution.Skip(zone.Id, now);
                zone.SetPlan(plan, level);
            }
        }

        /// <summary>
        /// Own ships lost near a zone count as losses there; ownership changes and own forces sent into a zone feed the
        /// retake and failed-capture memory. Lone sites outside every zone have no memory.
        /// </summary>
        private void Observe(EnemyZoneObjectiveCollector collector, float now)
        {
            _alive.Clear();
            _present.Clear();
            _nearNext.Clear();
            _cheapestShip = float.PositiveInfinity;
            IReadOnlyList<ReinforcementZonePresenter> zones = _zones.Zones;
            while (_reservationIds.Count < zones.Count) _reservationIds.Add("expansion:" + _reservationIds.Count);
            IReadOnlyList<AiWorldSnapshot.Entry> owned = _world.GetOwnedUnits(_owner);
            for (int i = 0; i < owned.Count; i++)
            {
                AiWorldSnapshot.Entry entry = owned[i];
                if (!entry.UnitTypeId.IsShip || entry.Entity.HealthModel.IsDestroyed) continue;
                _alive.Add(entry.Id);
                float price = _forces.GetProfile(entry.UnitTypeId).Price;
                if (price > 0f) _cheapestShip = Mathf.Min(_cheapestShip, price);
                for (int index = 0; index < zones.Count; index++)
                {
                    ReinforcementZonePresenter zone = zones[index];
                    if (!zone.IsCapturable || PlanarDistance(entry.Position, zone.Center) >
                        zone.Radius + EnemyZoneObjectiveCollector.ZONE_DEFENDER_MARGIN) continue;
                    string id = _picture.GetZoneId(index);
                    _nearNext[entry.Id] = (id, price);
                    _present.Add(id);
                    break;
                }
            }
            foreach (KeyValuePair<long, (string Zone, float Price)> ship in _near)
                if (!_alive.Contains(ship.Key)) _caution.RecordLoss(ship.Value.Zone, ship.Value.Price, now);
            (_near, _nearNext) = (_nearNext, _near);

            for (int index = 0; index < zones.Count; index++)
            {
                ReinforcementZonePresenter zone = zones[index];
                if (!zone.IsCapturable) continue;
                string id = _picture.GetZoneId(index);
                bool teamOwned = !zone.Owner.IsNone && _roster.IsAllied(_owner, zone.Owner);
                // A capture attempt: a force of this player was sent to the zone and stands in it.
                bool attempting = _present.Contains(id) &&
                    _picture.Cooperation.IsReservedBy(_owner, _reservationIds[index]);
                _caution.Observe(id, teamOwned, attempting, now);
            }
        }

        /// <summary>
        /// Seconds until <see cref="HELP_GROUP_SHIPS"/> own or allied ships from outside the zone arrive, or until a
        /// reinforcement dropped at a team drop point near the zone gets there; at most <see cref="MAX_HELP_SECONDS"/>.
        /// </summary>
        private float GetHelpSeconds(Vector3 centre, float radius, float speed, EnemyReinforcementBudget budget)
        {
            _arrivals.Clear();
            float outside = radius + EnemyZoneObjectiveCollector.ZONE_DEFENDER_MARGIN;
            IReadOnlyList<AiWorldSnapshot.Entry> units = _world.LivingUnits;
            for (int i = 0; i < units.Count; i++)
            {
                AiWorldSnapshot.Entry entry = units[i];
                if (!entry.UnitTypeId.IsShip || !_roster.IsAllied(_owner, entry.Owner) ||
                    entry.Entity.HealthModel.IsDestroyed) continue;
                float distance = PlanarDistance(entry.Position, centre);
                float shipSpeed = _forces.GetProfile(entry.UnitTypeId).Speed;
                if (distance <= outside || shipSpeed <= 0f) continue;
                _arrivals.Add((distance - radius) / shipSpeed);
            }
            float help = float.PositiveInfinity;
            if (_arrivals.Count > 0)
            {
                _arrivals.Sort();
                help = _arrivals[Mathf.Min(HELP_GROUP_SHIPS, _arrivals.Count) - 1];
            }
            float drop = GetTeamSpawnDistance(centre);
            if (!float.IsPositiveInfinity(_cheapestShip) &&
                Mathf.Min(budget.Bank, budget.CapacityCredits) >= _cheapestShip &&
                drop <= SPAWN_NEAR_DISTANCE + radius)
                help = Mathf.Min(help, budget.ReadySeconds + Mathf.Max(0f, drop - radius) / Mathf.Max(1f, speed));
            return Mathf.Min(MAX_HELP_SECONDS, help);
        }

        /// <summary>
        /// Known hostiles that can be inside the zone within <paramref name="seconds"/>, plus the hostile ships the
        /// hostile income buys in that time and drops at its nearest spawn point, rated like the known hostile fleet.
        /// </summary>
        private void BuildThreat(ForceComposition threat, Vector3 centre, float radius, float seconds, float speed,
            float readySeconds, float hostileIncome)
        {
            _threatCentre = centre;
            _threatRadius = radius;
            _threatSeconds = seconds;
            _picture.BuildKnownFromConfident(threat, _includeThreat);
            if (_hostileReference.Value <= 0f) return;
            float travel = Mathf.Max(0f, GetHostileSpawnDistance(centre) - radius) / Mathf.Max(1f, speed);
            float credits = hostileIncome * Mathf.Max(0f, seconds - readySeconds - travel);
            if (credits <= 0f) return;
            _reinforcement.Clear();
            _reinforcement.AddShare(_hostileReference, credits / _hostileReference.Value);
            threat.Add(_reinforcement);
        }

        private bool IncludeThreat(HostileSighting sighting) =>
            _roster.IsHostile(_owner, sighting.Owner) && EnemyFleetRating.CanReach(
                PlanarDistance(ToVector(sighting.Position), _threatCentre), _threatRadius,
                _forces.GetProfile(sighting.UnitTypeId).Speed, _threatSeconds);

        private bool IsArmadaHeadingFor(Vector3 centre, float radius)
        {
            foreach (HostileArmada armada in _picture.Armadas)
                if (armada.ThreatensAsset && PlanarDistance(armada.AssetPosition, centre) <=
                    radius + EnemyZoneObjectiveCollector.ZONE_DEFENDER_MARGIN) return true;
            return false;
        }

        private float GetArmadaDistance(Vector3 centre)
        {
            float nearest = float.PositiveInfinity;
            foreach (HostileArmada armada in _picture.Armadas)
                nearest = Mathf.Min(nearest, PlanarDistance(armada.Centre, centre));
            return nearest;
        }

        // Nearest relay or station the team can drop reinforcements at.
        private float GetTeamSpawnDistance(Vector3 centre) => GetSpawnDistance(centre, hostile: false);

        // Nearest relay or station the hostiles can drop reinforcements at.
        private float GetHostileSpawnDistance(Vector3 centre) => GetSpawnDistance(centre, hostile: true);

        private float GetSpawnDistance(Vector3 centre, bool hostile)
        {
            float nearest = float.PositiveInfinity;
            foreach (ReinforcementZonePresenter zone in _zones.Zones)
                if (!zone.Owner.IsNone && !zone.IsContested && IsSide(zone.Owner, hostile))
                    nearest = Mathf.Min(nearest, PlanarDistance(zone.Center, centre));
            IReadOnlyList<AiWorldSnapshot.Entry> bases = _world.Bases;
            for (int i = 0; i < bases.Count; i++)
                if (IsSide(bases[i].Owner, hostile) && !bases[i].Entity.HealthModel.IsDestroyed)
                    nearest = Mathf.Min(nearest, PlanarDistance(bases[i].Position, centre));
            return nearest;
        }

        private bool IsSide(PlayerId player, bool hostile) =>
            hostile ? _roster.IsHostile(_owner, player) : _roster.IsAllied(_owner, player);

        private static float PlanarDistance(Vector3 a, Vector3 b)
        {
            Vector3 offset = a - b;
            offset.y = 0f;
            return offset.magnitude;
        }
    }
}
