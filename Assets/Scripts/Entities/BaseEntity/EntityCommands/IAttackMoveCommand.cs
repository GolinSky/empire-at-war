using UnityEngine;

namespace EmpireAtWar.Entities.BaseEntity.EntityCommands
{
    public interface IAttackMoveCommand : IEntityCommand
    {
        Vector3 WorldPosition { get; }
        float NavigationRadius { get; }
        void AttackMoveTo(Vector3 worldPosition);
    }
}
