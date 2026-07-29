using UnityEngine;

namespace EmpireAtWar.Entities.BaseEntity.EntityCommands
{
    public interface IMoveCommand: IEntityCommand
    {
        Vector3 WorldPosition { get; }
        float NavigationRadius { get; }
        void MoveTo(Vector2 screenPosition);
        void MoveTo(Vector3 worldPosition);
    }
}
