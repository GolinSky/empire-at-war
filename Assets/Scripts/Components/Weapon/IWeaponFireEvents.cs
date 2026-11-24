using System;
using EmpireAtWar.Components.AttackComponent;
using UnityEngine;

namespace EmpireAtWar.Components.Weapon
{
    public interface IWeaponFireEvents
    {
        event Action<WeaponProfile, Transform, float> ShotEmitted;

        /// <summary>A main-battery mount fired the first shot of a salvo: profile, muzzle, aim point.</summary>
        event Action<WeaponProfile, Transform, Vector3> MainBatterySalvoStarted;
    }
}
