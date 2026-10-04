using UnityEngine;

namespace EmpireAtWar.Components.Ship.Health
{
    public interface IShieldView
    {
        void SetActive(bool active);

        Vector3 GetSurfacePosition(Vector3 origin, Vector3 target);

        // Damage of the hit sets the impact size in world units, independent of the shield's own size.
        void ShowImpact(Vector3 position, float damage);
    }
}
