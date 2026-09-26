using UnityEngine;

namespace EmpireAtWar.Entities.BaseEntity.EntityFacades
{
    public interface IMoveFacade: IEntityFacade
    {
        Vector3 WorldPosition { get; }
        float NavigationRadius { get; }
        void MoveTo(Vector2 screenPosition);
        void MoveTo(Vector3 worldPosition);
    }
}
