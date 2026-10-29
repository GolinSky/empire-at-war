using System;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.ViewComponents.Weapon;
using UnityEngine;

namespace EmpireAtWar.Components.AttackComponent
{
    [Serializable]
    public sealed class WeaponProfile
    {
        [SerializeField] private WeaponType weaponType;
        [Tooltip("Player-facing weapon name, e.g. in the hardpoint tooltip.")]
        [SerializeField] private string displayName;
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

        [Header("Targeting")]
        [Tooltip("Point defense: fires only at fighters, bombers and interceptors.")]
        [SerializeField] private bool strikecraftOnly;
        [Tooltip("Missile defense can shoot this projectile down before it lands.")]
        [SerializeField] private bool interceptable;

        [Header("Visual")]
        [SerializeField] private ShotEffect shotPrefab;
        [Tooltip("Units per second. Ignored by beams.")]
        [SerializeField] private float projectileSpeed;
        [SerializeField] private Color color = Color.white;
        [SerializeField] private Vector3 size = Vector3.one;

        // Set only on profiles built from the weapon rules; serialized ability and super weapon profiles keep the defaults.
        [NonSerialized] private WeaponFamily? _family;
        [NonSerialized] private bool _usesFactionColor;
        [NonSerialized] private bool _hasTargetSizeBias;
        [NonSerialized] private ShipClassValues _damageBias;
        [NonSerialized] private ShipClassValues _accuracyBias;
        [NonSerialized] private float _turnRate;
        [NonSerialized] private TargetPreference _preferredTarget;

        public WeaponProfile()
        {
        }

        public WeaponProfile(WeaponType weaponType, string displayName, WeaponFamily family, DamageType damageType,
            float damage, int shotsPerSalvo, float shotInterval, float reload, float range, bool strikecraftOnly,
            bool interceptable, ShotEffect shotPrefab, float projectileSpeed, Color color, Vector3 size,
            bool usesFactionColor, bool hasTargetSizeBias, ShipClassValues damageBias, ShipClassValues accuracyBias,
            float turnRate, TargetPreference preferredTarget = TargetPreference.Any)
        {
            this.weaponType = weaponType;
            this.displayName = displayName;
            this.damageType = damageType;
            this.damage = damage;
            this.shotsPerSalvo = shotsPerSalvo;
            this.shotInterval = shotInterval;
            this.reload = reload;
            this.range = range;
            this.strikecraftOnly = strikecraftOnly;
            this.interceptable = interceptable;
            this.shotPrefab = shotPrefab;
            this.projectileSpeed = projectileSpeed;
            this.color = color;
            this.size = size;
            _family = family;
            _usesFactionColor = usesFactionColor;
            _hasTargetSizeBias = hasTargetSizeBias;
            _damageBias = damageBias;
            _accuracyBias = accuracyBias;
            _turnRate = turnRate;
            _preferredTarget = preferredTarget;
        }

        public WeaponType WeaponType => weaponType;
        public string DisplayName => displayName;
        public DamageType DamageType => damageType;
        public float Damage => damage;
        public int ShotsPerSalvo => shotsPerSalvo;
        public float ShotInterval => shotInterval;
        public float Reload => reload;
        public float Range => range;
        public bool StrikecraftOnly => strikecraftOnly;
        public bool Interceptable => interceptable;
        public ShotEffect ShotPrefab => shotPrefab;
        public float ProjectileSpeed => projectileSpeed;
        public Color Color => color;
        public Vector3 Size => size;

        public WeaponFamily Family => _family ?? throw new InvalidOperationException(
            $"{weaponType} was not built from the weapon rules and has no family.");
        public bool UsesFactionColor => _usesFactionColor;
        /// <summary>Degrees per second the mount turns before a salvo; 0 fires without turning.</summary>
        public float TurnRate => _turnRate;
        public TargetPreference PreferredTarget => _preferredTarget;

        public WeaponProfile WithColor(Color boltColor) => new WeaponProfile(weaponType, displayName, Family, damageType,
            damage, shotsPerSalvo, shotInterval, reload, range, strikecraftOnly, interceptable, shotPrefab,
            projectileSpeed, boltColor, size, false, _hasTargetSizeBias, _damageBias, _accuracyBias, _turnRate,
            _preferredTarget);

        public WeaponProfile WithRangeMultiplier(float multiplier)
        {
            // Point defense guards its own hull, so a ship's longer reach never extends it.
            if (multiplier == 1f || Family == WeaponFamily.PointDefense)
                return this;

            return new WeaponProfile(weaponType, displayName, Family, damageType,
                damage, shotsPerSalvo, shotInterval, reload, range * multiplier,
                strikecraftOnly, interceptable, shotPrefab, projectileSpeed, color, size,
                _usesFactionColor, _hasTargetSizeBias, _damageBias, _accuracyBias, _turnRate,
                _preferredTarget);
        }

        public WeaponProfile WithWarheadMultipliers(float damageMultiplier, float reloadMultiplier)
        {
            if (Family != WeaponFamily.FighterTorpedo && Family != WeaponFamily.FighterMissile)
                return this;

            return new WeaponProfile(weaponType, displayName, Family, damageType,
                damage * damageMultiplier, shotsPerSalvo, shotInterval, reload * reloadMultiplier,
                range, strikecraftOnly, interceptable, shotPrefab, projectileSpeed, color, size,
                _usesFactionColor, _hasTargetSizeBias, _damageBias, _accuracyBias, _turnRate,
                _preferredTarget);
        }

        public float GetDamageBias(ShipClass targetClass) => _hasTargetSizeBias ? _damageBias[targetClass] : 1f;

        public float GetAccuracyBias(ShipClass targetClass) => _hasTargetSizeBias ? _accuracyBias[targetClass] : 1f;

        // Firing cycle: the reload starts with the first shot, but a long salvo can outlast it.
        public float CycleSeconds => Mathf.Max(reload, (shotsPerSalvo - 1) * shotInterval);

        public float DamagePerSecond => damage * shotsPerSalvo / CycleSeconds;
    }
}
