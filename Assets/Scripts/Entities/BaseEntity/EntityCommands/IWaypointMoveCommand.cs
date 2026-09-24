using System.Collections.Generic;
using UnityEngine;

namespace EmpireAtWar.Entities.BaseEntity.EntityCommands
{
    public interface IWaypointMoveCommand : IEntityCommand
    {
        Vector3 WorldPosition { get; }
        float NavigationRadius { get; }
        void MoveAlong(IReadOnlyList<Vector3> waypoints);
    }
}
