using System.Collections.Generic;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using UnityEngine;
using EmpireAtWar.Services.Vision;
using EmpireAtWar.Components.Squadrons.Health;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Services.Squadrons;

namespace EmpireAtWar.Entities.Squadrons
{
    /// <summary>
    /// Fighters and interceptors prefer strikecraft. Bombers take any ship before any strikecraft,
    /// and fall back to strikecraft only when no ship is available.
    /// </summary>
    public sealed class SquadronTargetSelector
    {
        private const float PREFERRED_TARGET_DISTANCE_WEIGHT = 0.25f;

        private readonly SquadronTargetIndex _targetIndex;
        private readonly List<SquadronTargetIndex.Target> _nearby = new List<SquadronTargetIndex.Target>();
        private readonly IVisionService _visionService;
        private readonly IPlayerRelations _relations;

        private readonly PlayerId _side;
        private readonly ShipClass _shipClass;

        public SquadronTargetSelector(SquadronTargetIndex targetIndex, IVisionService visionService,
            IPlayerRelations relations, PlayerId side, ISquadronHealthData data)
        {
            _relations = relations;
            _targetIndex = targetIndex;
            _visionService = visionService;
            _side = side;
            _shipClass = data.ShipClass;
        }

        /// <summary>Bombers break off a strikecraft fight when a ship becomes available.</summary>
        public bool PrefersShips => _shipClass == ShipClass.Bomber;

        public IEntity SelectNear(IRadarComponent radar, Vector3 center, float radius)
        {
            _targetIndex.Query(_side, center, radius, _nearby);
            IEntity best = null;
            int bestTier = 0;
            float bestScore = radius * radius;
            float radiusSquared = radius * radius;
            for (int i = 0; i < _nearby.Count; i++)
            {
                IEntity candidate = _nearby[i].Entity;
                if (!radar.HasEnemy(candidate) || !IsValidEnemy(candidate)) continue;
                Vector3 position = _nearby[i].Position;
                float distance = (position - center).sqrMagnitude;
                if (distance > radiusSquared) continue;
                int tier = Tier(candidate);
                float score = Score(candidate, distance);
                if (best != null && (tier > bestTier || tier == bestTier && (score > bestScore ||
                    score == bestScore && radar.Enemies.IndexOf(candidate) >= radar.Enemies.IndexOf(best)))) continue;
                if (candidate.IsCloaked()) continue;
                if (!_visionService.IsVisible(_side, position)) continue;
                best = candidate;
                bestTier = tier;
                bestScore = score;
            }

            return best;
        }

        public IEntity SelectAnywhere(Vector3 origin) => SelectSeen(origin, float.PositiveInfinity);

        /// <summary>Best enemy within <paramref name="radius"/> that the squadron's team can see, not only its own radar.</summary>
        public IEntity SelectSeen(Vector3 center, float radius)
        {
            _targetIndex.Query(_side, center, radius, _nearby);
            IEntity best = null;
            int bestTier = int.MaxValue;
            float bestScore = float.PositiveInfinity;
            float radiusSquared = radius * radius;
            foreach (SquadronTargetIndex.Target target in _nearby)
            {
                IEntity candidate = target.Entity;
                if (!IsValidEnemy(candidate)) continue;
                Vector3 position = target.Position;
                float distance = (position - center).sqrMagnitude;
                if (distance > radiusSquared) continue;
                int tier = Tier(candidate);
                float score = Score(candidate, distance);
                if (tier > bestTier || tier == bestTier && score >= bestScore) continue;
                if (candidate.IsCloaked()) continue;
                if (!_visionService.IsVisible(_side, position)) continue;
                best = candidate;
                bestTier = tier;
                bestScore = score;
            }

            return best;
        }

        /// <summary>
        /// Escort target search around <paramref name="guarded"/>. Bombers first join the ship that
        /// <paramref name="guarded"/> is fighting, wherever it is, then fall back to the guard area.
        /// </summary>
        public IEntity SelectGuardTarget(IEntity guarded, Vector3 center, float radius)
        {
            if (PrefersShips && guarded.TryGetFacade(out IUnitCombatObserverFacade combat))
            {
                IEntity fought = combat.Target;
                if (fought != null && IsValidEnemy(fought) && !fought.HealthModel.ShipClass.IsStrikecraft() &&
                    !fought.IsCloaked() &&
                    _visionService.IsVisible(_side, fought.GetFacade<IEntityTransformFacade>().Transform.position))
                    return fought;
            }

            return SelectSeen(center, radius);
        }

        public static bool IsAlive(IEntity entity) =>
            entity != null && !entity.HealthModel.IsDestroyed && entity.HealthModel.HasUnits;

        private bool IsValidEnemy(IEntity entity) => _relations.IsHostile(_side, entity.Owner) && IsAlive(entity);

        /// <summary>Lower tiers always win: for bombers every ship ranks ahead of every strikecraft.</summary>
        private int Tier(IEntity entity) =>
            PrefersShips && entity.HealthModel.ShipClass.IsStrikecraft() ? 1 : 0;

        private float Score(IEntity entity, float sqrDistance)
        {
            if (PrefersShips) return sqrDistance;
            return entity.HealthModel.ShipClass.IsStrikecraft()
                ? sqrDistance * PREFERRED_TARGET_DISTANCE_WEIGHT
                : sqrDistance;
        }
    }
}
