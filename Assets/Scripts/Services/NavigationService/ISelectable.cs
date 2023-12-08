using UnityEngine;

namespace EmpireAtWar.Services.NavigationService
{
    public interface ISelectable
    {
        bool CanMove { get; }
        void MoveToPosition(Vector2 screenPosition);
        void SetActive(bool isActive);
        
    }
}