using System;
using UnityEngine;

namespace EmpireAtWar.Components.AttackComponent
{
    /// <summary>How many hardpoints of one weapon type a unit carries; baked from its view prefab.</summary>
    [Serializable]
    public struct WeaponLoadoutEntry
    {
        [SerializeField] private WeaponType weaponType;
        [SerializeField, Min(0)] private int count;

        public WeaponType WeaponType => weaponType;
        public int Count => count;

        public WeaponLoadoutEntry(WeaponType weaponType, int count)
        {
            this.weaponType = weaponType;
            this.count = count;
        }
    }
}
