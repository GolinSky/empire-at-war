using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Extentions;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Entities.BaseEntity
{
    public sealed class EntityTransformFacade : IEntityTransformFacade
    {
        public Transform Transform { get; }

        public EntityTransformFacade([Inject(Id = EntityBindType.ViewTransform)] Transform transform)
        {
            Transform = transform;
        }
    }
}
