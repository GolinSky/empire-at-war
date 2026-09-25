using EmpireAtWar.Components.AttackComponent;
using UnityEngine;

namespace EmpireAtWar.Components.Ship.Health
{
    public interface IShieldTarget
    {
        // Keep the hardpoint as the hull aim point; the incoming origin determines the near shield surface.
        Vector3 GetImpactPosition(Vector3 origin, Vector3 target, DamageType damageType);
        bool ShowShieldImpact(Vector3 position);
    }
}
