using UnityEngine;

namespace EmpireAtWar.Entities.BaseEntity.EntityCommands
{
    public interface IGuardCommand : IEntityCommand
    {
        Vector3 WorldPosition { get; }
        float NavigationRadius { get; }
        void Guard(IEntity friendly, Vector3 offset);
    }
}
