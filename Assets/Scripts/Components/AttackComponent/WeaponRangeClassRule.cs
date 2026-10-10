using System;
using UnityEngine;

namespace EmpireAtWar.Components.AttackComponent
{
    [Serializable]
    public sealed class WeaponRangeClassRule
    {
        [SerializeField] private WeaponRangeClass rangeClass;
        [SerializeField, Min(0f)] private float rangeMultiplier = 1f;
        [SerializeField, Min(0f)] private float damageMultiplier = 1f;
        [SerializeField, Min(0.01f)] private float reloadMultiplier = 1f;
        [SerializeField, Min(0f)] private float accuracyMultiplier = 1f;

        public WeaponRangeClass RangeClass => rangeClass;
        public float RangeMultiplier => rangeMultiplier;
        public float DamageMultiplier => damageMultiplier;
        public float ReloadMultiplier => reloadMultiplier;
        public float AccuracyMultiplier => accuracyMultiplier;
    }
}
