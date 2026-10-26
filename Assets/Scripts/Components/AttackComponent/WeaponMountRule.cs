using System;
using UnityEngine;

namespace EmpireAtWar.Components.AttackComponent
{
    [Serializable]
    public sealed class WeaponMountRule
    {
        [SerializeField] private WeaponMount mount;
        [Tooltip("Ship weapon shot damage multiplier against a single barrel. Below the barrel count: extra barrels add less.")]
        [SerializeField, Min(0f)] private float damageMultiplier = 1f;

        public WeaponMount Mount => mount;
        public float DamageMultiplier => damageMultiplier;
    }
}
