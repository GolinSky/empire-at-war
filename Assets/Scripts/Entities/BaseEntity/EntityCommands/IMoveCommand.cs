using UnityEngine;

namespace EmpireAtWar.Entities.BaseEntity.EntityCommands
{
    public interface IMoveCommand: IEntityCommand
    {
        void MoveTo(Vector2 screenPosition);
    }
}