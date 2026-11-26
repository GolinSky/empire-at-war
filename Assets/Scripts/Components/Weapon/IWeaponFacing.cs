using EmpireAtWar.Entities.BaseEntity;
using UnityEngine;
using EmpireAtWar.Entities.Units;

namespace EmpireAtWar.Components.Weapon
{
    public interface IWeaponFacing
    {
        float GetFiringTurnAngle(IEntity target, Vector3 pivot, float? targetBearing = null);

        /// <summary>True while the guns reach a target of this class at this centre distance.</summary>
        bool IsWithinReach(float distance, float contactDistance, ShipClass targetClass);
    }
}
