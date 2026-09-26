using UnityEngine;

namespace EmpireAtWar.Entities.BaseEntity.EntityFacades
{
    public interface IGuardFacade : IEntityFacade
    {
        Vector3 WorldPosition { get; }
        float NavigationRadius { get; }
        void Guard(IEntity friendly, Vector3 offset);
    }
}
