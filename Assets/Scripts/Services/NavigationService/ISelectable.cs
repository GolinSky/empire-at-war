using EmpireAtWar.Models.Factions;
using LightWeightFramework.Model;
using UnityEngine;

namespace EmpireAtWar.Services.NavigationService
{
    public interface ISelectable
    {
        IMovable Movable { get; }
        IModelObserver ModelObserver { get; }
        
        void SetActive(bool isActive);
        
        PlayerType PlayerType { get; }
    }

    public interface IMovable
    {
        bool CanMove { get; }
        void MoveToPosition(Vector2 screenPosition);
    }
  
}