using UnityEngine;

namespace EmpireAtWar.Entities.BaseEntity.EntityFacades
{
    public interface IEntityTransformFacade : IEntityFacade
    {
        Transform Transform { get; }
    }
}
