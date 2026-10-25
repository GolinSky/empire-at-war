using System;
using EmpireAtWar.ViewComponents.Weapon;
using UnityEngine;

namespace EmpireAtWar.Components.AttackComponent
{
    /// <summary>Base stats of one family: a Light, Single weapon. Every other weapon of the family scales from these.</summary>
    [Serializable]
    public sealed class WeaponFamilyRule
    {
        [SerializeField] private WeaponFamily family;
        [SerializeField] private WeaponPlatform platform;
        [SerializeField] private DamageType damageType;

        [Header("Base Damage")]
        [Tooltip("Damage of one shot from one barrel, before caliber and mount.")]
        [SerializeField, Min(0f)] private float damage;
        [SerializeField, Min(1)] private int shotsPerSalvo = 1;
        [Tooltip("Seconds between shots of a salvo.")]
        [SerializeField, Min(0f)] private float shotInterval;
        [Tooltip("Seconds from the start of a salvo until the hardpoint can fire again.")]
        [SerializeField, Min(0.01f)] private float reload = 1f;
        [Tooltip("Firing distance of a Light, Standard weapon in world units.")]
        [SerializeField, Min(0f)] private float range;

        [Header("Targeting")]
        [Tooltip("Point defense: fires only at fighters, bombers and interceptors.")]
        [SerializeField] private bool strikecraftOnly;
        [Tooltip("Targets a ship hardpoint picks first; the others only when none of these is in reach.")]
        [SerializeField] private TargetPreference preferredTarget;
        [Tooltip("Missile defense can shoot this projectile down before it lands.")]
        [SerializeField] private bool interceptable;
        [Tooltip("Caliber shifts damage and accuracy towards bigger targets. Off for strikecraft weapons.")]
        [SerializeField] private bool targetSizeBias = true;

        [Header("Turret")]
        [Tooltip("Degrees per second a Light mount turns towards a new target before its salvo. 0 fires without turning; squadron families never turn.")]
        [SerializeField, Min(0f)] private float turnRate;

        [Header("Visual")]
        [SerializeField] private ShotEffect shotPrefab;
        [Tooltip("Used by Dual and Triple mounts, and larger mounts without a dedicated prefab. Leave empty to use the normal shot.")]
        [SerializeField] private ShotEffect multiBarrelShotPrefab;
        [SerializeField] private ShotEffect quadShotPrefab;
        [SerializeField] private ShotEffect octupleShotPrefab;
        [Tooltip("Units per second. Ignored by beams.")]
        [SerializeField, Min(0f)] private float projectileSpeed;
        [Tooltip("Bolts take the owner's faction color instead of the family color.")]
        [SerializeField] private bool factionColored;
        [SerializeField] private Color color = Color.white;
        [SerializeField] private Vector3 size = Vector3.one;

        public WeaponFamily Family => family;
        public WeaponPlatform Platform => platform;
        public DamageType DamageType => damageType;
        public float Damage => damage;
        public int ShotsPerSalvo => shotsPerSalvo;
        public float ShotInterval => shotInterval;
        public float Reload => reload;
        public float Range => range;
        public bool StrikecraftOnly => strikecraftOnly;
        public TargetPreference PreferredTarget => preferredTarget;
        public bool Interceptable => interceptable;
        public bool TargetSizeBias => targetSizeBias;
        public float TurnRate => turnRate;
        public ShotEffect ShotPrefab => shotPrefab;
        public ShotEffect MultiBarrelShotPrefab => multiBarrelShotPrefab;
        public ShotEffect QuadShotPrefab => quadShotPrefab;
        public ShotEffect OctupleShotPrefab => octupleShotPrefab;
        public float ProjectileSpeed => projectileSpeed;
        public bool FactionColored => factionColored;
        public Color Color => color;
        public Vector3 Size => size;
    }
}
