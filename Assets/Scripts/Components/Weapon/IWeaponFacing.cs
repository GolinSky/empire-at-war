using EmpireAtWar.Entities.BaseEntity;
using UnityEngine;

namespace EmpireAtWar.Components.Weapon
{
    public interface IWeaponFacing
    {
        float GetFiringTurnAngle(IEntity target, Vector3 pivot);
    }
}
