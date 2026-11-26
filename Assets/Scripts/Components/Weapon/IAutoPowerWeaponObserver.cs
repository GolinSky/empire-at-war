using UnityEngine;
using EmpireAtWar.Entities.Units;

namespace EmpireAtWar.Components.Weapon
{
    public interface IAutoPowerWeaponObserver
    {
        bool CanEngageMainWeapon(ShipClass targetClass, Vector3 position);
    }
}
