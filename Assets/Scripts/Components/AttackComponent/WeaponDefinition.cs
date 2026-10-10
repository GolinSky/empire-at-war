using System;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.ViewComponents.Weapon;
using UnityEngine;

namespace EmpireAtWar.Components.AttackComponent
{
    /// <summary>
    /// One weapon a hardpoint can mount: its place in the taxonomy plus an optional variant.
    /// Stats are never stored here; <see cref="WeaponProfileFactory"/> derives them from the rules.
    /// </summary>
    [Serializable]
    public sealed class WeaponDefinition
    {
        [SerializeField] private WeaponType weaponType;
        [Tooltip("Player-facing weapon name, e.g. in the hardpoint tooltip.")]
        [SerializeField] private string displayName;

        [Header("Taxonomy")]
        [SerializeField] private WeaponFamily family;
        [SerializeField] private WeaponCaliber caliber;
        [SerializeField] private WeaponMount mount = WeaponMount.Single;
        [SerializeField] private WeaponFirePattern pattern;
        [Tooltip("Shots per burst. Used only by the Burst pattern.")]
        [SerializeField, Min(2)] private int burstShots = 2;
        [SerializeField] private WeaponRangeClass rangeClass;

        [Header("Variant")]
        [SerializeField] private WeaponScope scope;
        [Tooltip("Owning faction when the scope is Faction.")]
        [SerializeField] private FactionType faction;
        [Tooltip("Owning unit data name (for example ISDII, XWing) when the scope is Unit.")]
        [SerializeField] private string unit;
        [SerializeField, Min(0f)] private float damageMultiplier = 1f;
        [SerializeField, Min(0.01f)] private float reloadMultiplier = 1f;
        [SerializeField, Min(0f)] private float rangeMultiplier = 1f;
        [Tooltip("Why this weapon differs from its archetype. Required when a multiplier is not 1.")]
        [SerializeField, TextArea] private string reason;

        [Header("Visual Override")]
        [Tooltip("Use this shot prefab, speed and size instead of the family's.")]
        [SerializeField] private bool overrideVisual;
        [SerializeField] private ShotEffect shotPrefab;
        [SerializeField, Min(0f)] private float projectileSpeed;
        [SerializeField] private Vector3 size = Vector3.one;
        [Tooltip("Use this bolt color instead of the family or faction color.")]
        [SerializeField] private bool overrideColor;
        [SerializeField] private Color color = Color.white;

        public WeaponType WeaponType => weaponType;
        public string DisplayName => displayName;
        public WeaponFamily Family => family;
        public WeaponCaliber Caliber => caliber;
        public WeaponMount Mount => mount;
        public WeaponFirePattern Pattern => pattern;
        public int BurstShots => burstShots;
        public WeaponRangeClass RangeClass => rangeClass;
        public WeaponScope Scope => scope;
        public FactionType Faction => faction;
        public string Unit => unit;
        public float DamageMultiplier => damageMultiplier;
        public float ReloadMultiplier => reloadMultiplier;
        public float RangeMultiplier => rangeMultiplier;
        public string Reason => reason;
        public bool OverrideVisual => overrideVisual;
        public ShotEffect ShotPrefab => shotPrefab;
        public float ProjectileSpeed => projectileSpeed;
        public Vector3 Size => size;
        public bool OverrideColor => overrideColor;
        public Color Color => color;
    }
}
