using UnityEngine;

namespace EmpireAtWar.Views.MiniMap
{
    public interface IMiniMapPositionConvector
    {
        Vector2 GetPosition(Vector3 worldPos);
        Vector2 GetSize(float worldDiameter);
    }
}
