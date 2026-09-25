using System;
using EmpireAtWar.Components.AttackComponent;
using UnityEngine;

namespace EmpireAtWar.Components.Weapon
{
    public interface IWeaponFireEvents
    {
        event Action<WeaponProfile, Transform> ShotEmitted;
    }
}
