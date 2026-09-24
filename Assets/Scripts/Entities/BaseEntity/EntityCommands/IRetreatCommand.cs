using UnityEngine;

namespace EmpireAtWar.Entities.BaseEntity.EntityCommands
{
    public interface IRetreatCommand : IEntityCommand
    {
        Vector3 WorldPosition { get; }
        float NavigationRadius { get; }
        bool IsRetreatPending { get; }
        float RetreatRemaining { get; }
        void Retreat(Vector3 destination, float delay);
        void CancelRetreat();
    }
}
