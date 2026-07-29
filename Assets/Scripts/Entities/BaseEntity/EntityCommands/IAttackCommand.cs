using EmpireAtWar.Entities.BaseEntity;
using UnityEngine;

namespace EmpireAtWar.Entities.BaseEntity.EntityCommands
{
    public interface IAttackCommand : IEntityCommand
    {
        Vector3 WorldPosition { get; }
        float NavigationRadius { get; }
        void Attack(IEntity target, Vector3 formationOffset);
    }
}
