using System;
using UnityEngine;

namespace EmpireAtWar.Components.AttackComponent
{
    /// <summary>One row of the rock-paper-scissors matrix: how a damage type treats each ship class and shields.</summary>
    [Serializable]
    public sealed class DamageTypeProfile
    {
        [SerializeField] private DamageType damageType;
        [Tooltip("Hull and hardpoint damage multiplier per target class.")]
        [SerializeField] private ShipClassValues damage;
        [Tooltip("Chance (0..1) that a shot hits a target of this class.")]
        [SerializeField] private ShipClassValues accuracy;
        [Tooltip("Damage multiplier applied to shields.")]
        [SerializeField] private float vsShield = 1f;
        [Tooltip("Ignores shields and always damages hardpoints and hull.")]
        [SerializeField] private bool shieldPiercing;

        public DamageType DamageType => damageType;
        public ShipClassValues Damage => damage;
        public ShipClassValues Accuracy => accuracy;
        public float VsShield => vsShield;
        public bool ShieldPiercing => shieldPiercing;
    }
}
