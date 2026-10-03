using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Components.Ship.Audio
{
    public interface IAudioDialogShipComponent : IComponent
    {
        void HandleEnemyDetected();

        void HandleStopped();

        void HandleMove(Vector3 position);

        void HandleAttack(Vector3 target);

        void HandleSelection(bool isSelected);
    }
}
