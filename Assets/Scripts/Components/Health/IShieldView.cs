using UnityEngine;

namespace EmpireAtWar.Components.Ship.Health
{
    public interface IShieldView
    {
        void SetActive(bool active);
        Vector3 GetSurfacePosition(Vector3 origin, Vector3 target);
        void ShowImpact(Vector3 position);
    }
}
