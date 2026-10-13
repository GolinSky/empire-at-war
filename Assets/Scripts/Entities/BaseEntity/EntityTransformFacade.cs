using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Extensions;
using UnityEngine;
using VContainer;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.Entities.BaseEntity
{
    public sealed class EntityTransformFacade : IEntityTransformFacade
    {
        public Transform Transform { get; }

        public EntityTransformFacade([Key(EntityBindType.ViewTransform)] Transform transform)
        {
            Transform = transform;
        }
    }
}
