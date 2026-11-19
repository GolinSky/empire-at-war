using EmpireAtWar.Entities.BaseEntity.Orders;
using UnityEngine;

namespace EmpireAtWar.Entities.BaseEntity.EntityFacades
{
    public interface IMoveFacade: IEntityFacade
    {
        Vector3 WorldPosition { get; }
        float NavigationRadius { get; }
        float PaceSpeed { get; }

        void MoveTo(Vector2 screenPosition);

        void MoveTo(Vector3 worldPosition, PaceCluster pace = null);
    }
}
