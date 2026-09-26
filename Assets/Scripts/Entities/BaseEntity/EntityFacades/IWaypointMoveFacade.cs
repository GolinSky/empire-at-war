using System.Collections.Generic;
using UnityEngine;

namespace EmpireAtWar.Entities.BaseEntity.EntityFacades
{
    public interface IWaypointMoveFacade : IEntityFacade
    {
        Vector3 WorldPosition { get; }
        float NavigationRadius { get; }
        void MoveAlong(IReadOnlyList<Vector3> waypoints);
    }
}
