using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Models.Factions;
using UnityEngine;
using ViewComponents;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;

namespace EmpireAtWar.Entities.Squadrons
{
    /// <summary>Picks the closest enemy, with fighters and bombers weighted as if they were twice as close.</summary>
    public sealed class SquadronTargetSelector
    {
        private const float STRIKECRAFT_DISTANCE_WEIGHT = 0.25f;
        private const float VISIBLE_THRESHOLD = 0.5f;

        private readonly IEntityLocator _entityLocator;
        private readonly FogOfWarSystem _fogOfWarSystem;
        private readonly PlayerType _side;

        public SquadronTargetSelector(IEntityLocator entityLocator, FogOfWarSystem fogOfWarSystem,
            PlayerType side)
        {
            _entityLocator = entityLocator;
            _fogOfWarSystem = fogOfWarSystem;
            _side = side;
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
                if (_side == PlayerType.Player &&
                    _fogOfWarSystem.GetVisibilityAtPosition(position) < VISIBLE_THRESHOLD) continue;
                float score = Score(candidate, (position - origin).sqrMagnitude);
                if (score >= bestScore) continue;
                best = candidate;
                bestScore = score;
            }

            return best;
        }

        public static bool IsAlive(IEntity entity) =>
            entity != null && !entity.HealthModel.IsDestroyed && entity.HealthModel.HasUnits;

        private bool IsValidEnemy(IEntity entity) => entity.PlayerType != _side && IsAlive(entity);

        private static float Score(IEntity entity, float sqrDistance)
        {
            ShipClass shipClass = entity.HealthModel.ShipClass;
            bool isStrikecraft = shipClass == ShipClass.Fighter || shipClass == ShipClass.Bomber;
            return isStrikecraft ? sqrDistance * STRIKECRAFT_DISTANCE_WEIGHT : sqrDistance;
        }
    }
}
