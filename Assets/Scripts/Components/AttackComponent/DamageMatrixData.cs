using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Components.AttackComponent
{
    [CreateAssetMenu(fileName = nameof(DamageMatrixData), menuName = "Data/Weapon/DamageMatrixData")]
    public class DamageMatrixData : Data
    {
        [SerializeField] private List<DamageTypeProfile> damageTypes = new List<DamageTypeProfile>();
        [Tooltip("Radius around the target that missed shots fly to.")]
        [SerializeField] private float missSpread = 6f;

        private Dictionary<DamageType, DamageTypeProfile> _profiles;

        public float MissSpread => missSpread;

        public float GetDamageMultiplier(DamageType damageType, ShipClass shipClass) =>
            GetProfile(damageType).Damage[shipClass];

        public float GetAccuracy(DamageType damageType, ShipClass shipClass) =>
            GetProfile(damageType).Accuracy[shipClass];

        public float GetShieldMultiplier(DamageType damageType) => GetProfile(damageType).VsShield;

        public bool IsShieldPiercing(DamageType damageType) => GetProfile(damageType).ShieldPiercing;

        private DamageTypeProfile GetProfile(DamageType damageType)
        {
            if (_profiles == null)
            {
                _profiles = new Dictionary<DamageType, DamageTypeProfile>();
                foreach (DamageTypeProfile profile in damageTypes)
                    _profiles.Add(profile.DamageType, profile);
            }

            return _profiles[damageType];
        }
    }
}
