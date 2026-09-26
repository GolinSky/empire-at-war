using UnityEngine;

namespace EmpireAtWar.Entities.BaseEntity.EntityFacades
{
    public interface IRetreatFacade : IEntityFacade
    {
        Vector3 WorldPosition { get; }
        float NavigationRadius { get; }
        void Retreat(Vector3 destination);
    }
}
