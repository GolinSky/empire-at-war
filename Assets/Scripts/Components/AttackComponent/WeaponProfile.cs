using System;
using EmpireAtWar.ViewComponents.Weapon;
using UnityEngine;

namespace EmpireAtWar.Components.AttackComponent
{
    [Serializable]
    public sealed class WeaponProfile
    {
        [SerializeField] private WeaponType weaponType;
        [SerializeField] private DamageType damageType;

        [Header("Damage")]
        [Tooltip("Damage of a single shot before the damage matrix is applied.")]
        [SerializeField] private float damage;
        [SerializeField] private int shotsPerSalvo = 1;
        [Tooltip("Seconds between shots inside one salvo.")]
        [SerializeField] private float shotInterval;
        [Tooltip("Seconds from the start of a salvo until the hardpoint can fire again.")]
        [SerializeField] private float reload;
        [SerializeField] private float range;

        [Header("Visual")]
        [SerializeField] private ShotEffect shotPrefab;
        [Tooltip("Units per second. Ignored by beams.")]
        [SerializeField] private float projectileSpeed;
        [SerializeField] private Color color = Color.white;
        [SerializeField] private Vector3 size = Vector3.one;

        public WeaponType WeaponType => weaponType;
        public DamageType DamageType => damageType;
        public float Damage => damage;
        public int ShotsPerSalvo => shotsPerSalvo;
        public float ShotInterval => shotInterval;
        public float Reload => reload;
        public float Range => range;
        public ShotEffect ShotPrefab => shotPrefab;
        public float ProjectileSpeed => projectileSpeed;
        public Color Color => color;
        public Vector3 Size => size;
    }
}
