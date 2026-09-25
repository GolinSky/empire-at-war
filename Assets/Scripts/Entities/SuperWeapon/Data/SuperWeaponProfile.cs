using System;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Combat;
using UnityEngine;

namespace EmpireAtWar.Entities.SuperWeapons
{
    [Serializable]
    public sealed class SuperWeaponProfile
    {
        [Tooltip("Damage type, damage per shot, shot count, shot interval and shot visual of one firing.")]
        [SerializeField] private WeaponProfile weapon;
        [SerializeField] private float impactSize = 2f;
        [SerializeField, Min(0f)] private float firingDelay;

        [Header("Disable")]
        [Tooltip("Seconds the target stays disabled after a hit. Zero disables the effect.")]
        [SerializeField] private float stunDuration;
        [SerializeField] private CombatStatModifier stunModifier;

        [Header("Area")]
        [Tooltip("Damage dealt around the impact to the target's side. Zero disables the effect.")]
        [SerializeField] private float areaDamage;
        [SerializeField] private float areaRadius;

        public WeaponProfile Weapon => weapon;
        public float ImpactSize => impactSize;
        public float FiringDelay => firingDelay;
        public float StunDuration => stunDuration;
        public CombatStatModifier StunModifier => stunModifier;
        public float AreaDamage => areaDamage;
        public float AreaRadius => areaRadius;
    }
}
