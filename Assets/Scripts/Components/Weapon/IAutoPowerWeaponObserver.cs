using EmpireAtWar.Components.Ship.Health;
using UnityEngine;

namespace EmpireAtWar.Components.Weapon
{
    public interface IAutoPowerWeaponObserver
    {
        bool CanEngageMainWeapon(ShipClass targetClass, Vector3 position);
    }
}
