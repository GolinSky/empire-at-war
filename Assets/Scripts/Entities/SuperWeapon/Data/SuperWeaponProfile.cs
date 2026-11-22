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
        [Tooltip("Damage multiplier against targets disabled by a superweapon, so disable-then-strike combos stay weak.")]
        [SerializeField, Range(0f, 1f)] private float stunnedTargetDamageMultiplier = 1f;

        [Header("Research")]
        [Tooltip("Damage per shot after Orbital Weapons research tier 1..N. Element 0 is tier 1; tier 0 uses the weapon damage.")]
        [SerializeField] private float[] researchDamage = Array.Empty<float>();
        [Tooltip("Area damage after Orbital Weapons research tier 1..N. Element 0 is tier 1; tier 0 uses Area Damage.")]
        [SerializeField] private float[] researchAreaDamage = Array.Empty<float>();

        [Header("Area")]
        [Tooltip("Damage dealt around the impact to the target's side. Zero disables the effect.")]
        [SerializeField] private float areaDamage;
        [SerializeField] private float areaRadius;

        public WeaponProfile Weapon => weapon;
        public float ImpactSize => impactSize;
        public float FiringDelay => firingDelay;
        public float StunDuration => stunDuration;
        public CombatStatModifier StunModifier => stunModifier;
        public float StunnedTargetDamageMultiplier => stunnedTargetDamageMultiplier;
        public float AreaDamage => areaDamage;
        public float AreaRadius => areaRadius;

        public float GetDamage(int researchTier) => researchTier == 0 ? weapon.Damage : researchDamage[researchTier - 1];

        public float GetAreaDamage(int researchTier) =>
            researchTier == 0 || areaDamage <= 0f ? areaDamage : researchAreaDamage[researchTier - 1];
    }
}
