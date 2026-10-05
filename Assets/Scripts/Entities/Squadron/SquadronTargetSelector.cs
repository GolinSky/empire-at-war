using System.Collections.Generic;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using UnityEngine;
using EmpireAtWar.Services.Vision;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Components.Squadrons.Health;

namespace EmpireAtWar.Entities.Squadrons
{
    /// <summary>Prefers strikecraft for fighters and interceptors, and larger targets for bombers.</summary>
    public sealed class SquadronTargetSelector
    {
        private const float PREFERRED_TARGET_DISTANCE_WEIGHT = 0.25f;

        private readonly IEntityLocator _entityLocator;
        private readonly IVisionService _visionService;
        private readonly IPlayerRelations _relations;

        private readonly PlayerId _side;
        private readonly ShipClass _shipClass;

        private readonly bool _respectsFog;

        public SquadronTargetSelector(IEntityLocator entityLocator, IVisionService visionService,
            IPlayerRelations relations, ILocalPlayer localPlayer, PlayerId side, ISquadronHealthData data)
        {
            _relations = relations;
            // Only the human's squadrons are limited to what their side can see.
            _respectsFog = localPlayer.IsLocal(side);
            _entityLocator = entityLocator;
            _visionService = visionService;
            _side = side;
            _shipClass = data.ShipClass;
        }

        public IEntity SelectNear(IList<IEntity> candidates, Vector3 center, float radius)
        {
            IEntity best = null;
            float bestScore = radius * radius;
            for (int i = 0; i < candidates.Count; i++)
            {
                IEntity candidate = candidates[i];
                if (!IsValidEnemy(candidate)) continue;
                float distance = (candidate.GetFacade<IEntityTransformFacade>().Transform.position - center).sqrMagnitude;
                if (distance > radius * radius) continue;
                float score = Score(candidate, distance);
                if (score >= bestScore && best != null) continue;
                best = candidate;
                bestScore = score;
            }

            return best;
        }

        public IEntity SelectAnywhere(Vector3 origin)
        {
            IEntity best = null;
            float bestScore = float.PositiveInfinity;
            foreach (IEntity candidate in _entityLocator.Entities)
            {
                if (!IsValidEnemy(candidate)) continue;
                Vector3 position = candidate.GetFacade<IEntityTransformFacade>().Transform.position;
                if (_respectsFog &&
                    !_visionService.IsVisible(_side, position)) continue;
                float score = Score(candidate, (position - origin).sqrMagnitude);
                if (score >= bestScore) continue;
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
