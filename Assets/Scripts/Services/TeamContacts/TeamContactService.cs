using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Vision;
using UnityEngine;

namespace EmpireAtWar.Services.TeamContacts
{
    /// <summary>
    /// Visible hostiles per viewer, rebuilt at most every <see cref="REFRESH_SECONDS"/>. Callers ask only when a
    /// ship has nothing left to fight, so the entity scan is shared by every ship that asks in the same interval.
    /// </summary>
    public sealed class TeamContactService : ITeamContacts
    {
        private const float REFRESH_SECONDS = 0.5f;

        private readonly IEntityLocator _entities;
        private readonly IVisionService _vision;
        private readonly IPlayerRelations _relations;
        private readonly Dictionary<PlayerId, Snapshot> _snapshots = new Dictionary<PlayerId, Snapshot>();

        public TeamContactService(IEntityLocator entities, IVisionService vision, IPlayerRelations relations)
        {
            _entities = entities;
            _vision = vision;
            _relations = relations;
        }

        public void FillVisibleHostiles(PlayerId viewer, Vector3 center, float radius, List<IEntity> results)
        {
            if (!_snapshots.TryGetValue(viewer, out Snapshot snapshot))
            {
                snapshot = new Snapshot();
                _snapshots.Add(viewer, snapshot);
            }
            if (Time.time - snapshot.BuiltAt >= REFRESH_SECONDS) Rebuild(viewer, snapshot);

            float radiusSquared = radius * radius;
            foreach (IEntity entity in snapshot.Hostiles)
            {
                if (entity.HealthModel.IsDestroyed) continue;
                Vector3 offset = entity.GetFacade<IEntityTransformFacade>().Transform.position - center;
                offset.y = 0f;
                if (offset.sqrMagnitude <= radiusSquared) results.Add(entity);
            }
        }

        private void Rebuild(PlayerId viewer, Snapshot snapshot)
        {
            snapshot.BuiltAt = Time.time;
            snapshot.Hostiles.Clear();
            foreach (IEntity entity in _entities.Entities)
            {
                if (!_relations.IsHostile(viewer, entity.Owner) || entity.HealthModel.IsDestroyed ||
                    !entity.HealthModel.HasUnits || entity.IsCloaked() ||
                    !entity.TryGetFacade(out IVisionFootprintFacade footprint) ||
                    !_vision.IsVisible(viewer, footprint)) continue;
                snapshot.Hostiles.Add(entity);
            }
        }

        private sealed class Snapshot
        {
            public float BuiltAt = float.NegativeInfinity;
            public readonly List<IEntity> Hostiles = new List<IEntity>();
        }
    }
}
