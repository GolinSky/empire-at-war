using UnityEngine;

namespace EmpireAtWar.Components.Weapon
{
    public interface IWeaponFacing
    {
        float GetFiringTurnAngle(Vector3 targetPosition);
    }
}
