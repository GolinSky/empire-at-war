using UnityEngine;

namespace EmpireAtWar.Entities.MiniMap
{
    public interface IMiniMapPresenter
    {
        void MoveCamera(Vector3 worldPoint);
        void OrderMove(Vector3 worldPoint);
    }
}
