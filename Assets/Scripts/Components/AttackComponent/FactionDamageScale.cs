using System;
using EmpireAtWar.Models.Factions;
using UnityEngine;

namespace EmpireAtWar.Components.AttackComponent
{
    /// <summary>Scales every hit of one damage type fired by one faction's units, shields and hull alike.</summary>
    [Serializable]
    public sealed class FactionDamageScale
    {
        [SerializeField] private FactionType faction;
        [SerializeField] private DamageType damageType;
        [SerializeField, Min(0f)] private float multiplier = 1f;

        public FactionType Faction => faction;
        public DamageType DamageType => damageType;
        public float Multiplier => multiplier;
    }
}
