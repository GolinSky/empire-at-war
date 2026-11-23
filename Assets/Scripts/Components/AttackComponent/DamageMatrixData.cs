using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Components.AttackComponent
{
    [CreateAssetMenu(fileName = nameof(DamageMatrixData), menuName = "Data/Weapon/DamageMatrixData")]
    public class DamageMatrixData : Data
    {
        [SerializeField] private List<DamageTypeProfile> damageTypes = new List<DamageTypeProfile>();
        private Dictionary<DamageType, DamageTypeProfile> _profiles;

        [Tooltip("Radius around the target that missed shots fly to.")]
        [SerializeField] private float missSpread = 6f;

        [Tooltip("Per-faction scale on one damage type, applied to every hit that faction's units fire.")]
        [SerializeField] private List<FactionDamageScale> factionDamageScales = new List<FactionDamageScale>();

        public float MissSpread => missSpread;

        public IReadOnlyDictionary<(DamageType, ShipClass), float> CopyAccuracy()
        {
            var accuracy = new Dictionary<(DamageType, ShipClass), float>();
            foreach (DamageTypeProfile profile in damageTypes)
                foreach (ShipClass shipClass in Enum.GetValues(typeof(ShipClass)))
                    accuracy.Add((profile.DamageType, shipClass), profile.Accuracy[shipClass]);
            return accuracy;
        }

        public float GetDamageMultiplier(DamageType damageType, ShipClass shipClass) =>
            GetProfile(damageType).Damage[shipClass];

        public float GetAccuracy(DamageType damageType, ShipClass shipClass) =>
            GetProfile(damageType).Accuracy[shipClass];

        public float GetShieldMultiplier(DamageType damageType) => GetProfile(damageType).VsShield;

        public float GetShipShieldMultiplier(DamageType damageType)
        {
            DamageTypeProfile profile = GetProfile(damageType);
            return profile.VsShield * profile.ShipShieldMultiplier;
        }

        public float GetSquadronShieldMultiplier(DamageType damageType)
        {
            DamageTypeProfile profile = GetProfile(damageType);
            return profile.VsShield * profile.SquadronShieldMultiplier;
        }

        public bool IsShieldPiercing(DamageType damageType) => GetProfile(damageType).ShieldPiercing;

        /// <summary>Scale on hits of this damage type fired by this faction's units; 1 when the faction has none.</summary>
        public float GetFactionDamageMultiplier(FactionType faction, DamageType damageType)
        {
            foreach (FactionDamageScale scale in factionDamageScales)
                if (scale.Faction == faction && scale.DamageType == damageType) return scale.Multiplier;
            return 1f;
        }

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
