using System.Collections.Generic;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using UnityEngine;
using EmpireAtWar.Services.Vision;
using EmpireAtWar.Components.Squadrons.Health;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Services.Squadrons;

namespace EmpireAtWar.Entities.Squadrons
{
    /// <summary>Prefers strikecraft for fighters and interceptors, and larger targets for bombers.</summary>
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

        public IEntity SelectNear(IRadarComponent radar, Vector3 center, float radius)
        {
            _targetIndex.Query(_side, center, radius, _nearby);
            IEntity best = null;
            float bestScore = radius * radius;
            float radiusSquared = radius * radius;
            for (int i = 0; i < _nearby.Count; i++)
            {
                IEntity candidate = _nearby[i].Entity;
                if (!radar.HasEnemy(candidate) || !IsValidEnemy(candidate)) continue;
                Vector3 position = _nearby[i].Position;
                float distance = (position - center).sqrMagnitude;
                if (distance > radiusSquared) continue;
                float score = Score(candidate, distance);
                if (best != null && (score > bestScore ||
                    score == bestScore && radar.Enemies.IndexOf(candidate) >= radar.Enemies.IndexOf(best))) continue;
                if (candidate.IsCloaked()) continue;
                if (!_visionService.IsVisible(_side, position)) continue;
                best = candidate;
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
            float bestScore = float.PositiveInfinity;
            float radiusSquared = radius * radius;
            foreach (SquadronTargetIndex.Target target in _nearby)
            {
                IEntity candidate = target.Entity;
                if (!IsValidEnemy(candidate)) continue;
                Vector3 position = target.Position;
                float distance = (position - center).sqrMagnitude;
                if (distance > radiusSquared) continue;
                float score = Score(candidate, distance);
                if (score >= bestScore) continue;
                if (candidate.IsCloaked()) continue;
                if (!_visionService.IsVisible(_side, position)) continue;
                best = candidate;
                bestScore = score;
            }

            return best;
        }

        public static bool IsAlive(IEntity entity) =>
            entity != null && !entity.HealthModel.IsDestroyed && entity.HealthModel.HasUnits;

        private bool IsValidEnemy(IEntity entity) => _relations.IsHostile(_side, entity.Owner) && IsAlive(entity);

        private float Score(IEntity entity, float sqrDistance)
        {
            ShipClass shipClass = entity.HealthModel.ShipClass;
            bool isStrikecraft = shipClass.IsStrikecraft();
            bool isPreferred = _shipClass == ShipClass.Bomber ? !isStrikecraft : isStrikecraft;
            return isPreferred ? sqrDistance * PREFERRED_TARGET_DISTANCE_WEIGHT : sqrDistance;
        }
    }
}
