using System;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Services.Saves;

namespace EmpireAtWar.Entities.EnemyFaction.Models.Combat
{
    /// <summary>
    /// A group of units reduced to what decides a fight: how much hull and shield sits in each
    /// <see cref="ShipClass"/>, and how fast the group damages each class.
    /// </summary>
    public sealed class ForceComposition
    {
        public static readonly IReadOnlyList<ShipClass> Classes =
            Enum.GetValues(typeof(ShipClass)).Cast<ShipClass>().ToArray();

        /// <summary>Array length that fits every <see cref="ShipClass"/> value as an index.</summary>
        public static readonly int ClassSlots = Classes.Max(shipClass => (int)shipClass) + 1;

        private readonly float[] _hull = new float[ClassSlots];
        private readonly float[] _shields = new float[ClassSlots];
        private readonly float[] _hullDps = new float[ClassSlots];
        private readonly float[] _shieldDps = new float[ClassSlots];
        private readonly float[] _piercingDps = new float[ClassSlots];
        private readonly float[] _units = new float[ClassSlots];
        private readonly float[] _maxHull = new float[ClassSlots];
        private readonly float[] _shieldRegen = new float[ClassSlots];
        private float _siegeBonusHullDps;
        private float _siegeBonusShieldDps;
        private float _siegeBonusPiercingDps;
        private int _repairSources;
        private float _repairFractionPerSource;
        private int _repairHealersPerTarget;

        public int UnitCount { get; private set; }
        public bool IsEmpty => UnitCount == 0;

        /// <summary>Adds one unit at its current health.</summary>
        /// <param name="offenseScale">Share of the unit's weapons still firing (0..1).</param>
        public void Add(UnitCombatProfile profile, float hull, float shields, float offenseScale) =>
            Add(profile, hull, shields, offenseScale, profile.ShieldRegen);

        /// <summary>Adds one unit at its current health with its live shield regeneration (0 without a shield generator).</summary>
        public void Add(UnitCombatProfile profile, float hull, float shields, float offenseScale, float shieldRegen)
        {
            int unitClass = (int)profile.ShipClass;
            _hull[unitClass] += hull;
            _shields[unitClass] += shields;
            _units[unitClass] += 1f;
            _maxHull[unitClass] += profile.Hull;
            _shieldRegen[unitClass] += shieldRegen;
            // Indexed: foreach over the IReadOnlyList boxes an enumerator, and the AI adds hundreds of units per frame.
            for (int i = 0; i < Classes.Count; i++)
            {
                ShipClass target = Classes[i];
                int index = (int)target;
                _hullDps[index] += profile.HullDps(target) * offenseScale;
                _shieldDps[index] += profile.ShieldDps(target) * offenseScale;
                _piercingDps[index] += profile.PiercingDps(target) * offenseScale;
            }

            float siegeBonus = SiegeDamage.GetMultiplier(profile.ShipClass, StructureKind.SpaceStation) - 1f;
            if (siegeBonus > 0f)
            {
                _siegeBonusHullDps += siegeBonus * profile.HullDps(ShipClass.Structure) * offenseScale;
                _siegeBonusShieldDps += siegeBonus * profile.ShieldDps(ShipClass.Structure) * offenseScale;
                _siegeBonusPiercingDps += siegeBonus * profile.PiercingDps(ShipClass.Structure) * offenseScale;
            }

            UnitCount++;
        }

        /// <summary>Adds one unit at full health, optionally with the squadrons its hangar fields.</summary>
        public void AddNew(UnitCombatProfile profile, bool includeHangar)
        {
            Add(profile, profile.Hull, profile.Shields, 1f);
            if (!includeHangar)
            {
                return;
            }

            foreach (UnitCombatProfile squadron in profile.Hangar)
            {
                AddNew(squadron, false);
            }
        }

        /// <summary>
        /// Adds a repairer that stands within its radius of this force: it restores
        /// <paramref name="fractionPerSecond"/> of a unit's maximum hull per second, and at most
        /// <paramref name="healersPerTarget"/> repairers work on one unit.
        /// </summary>
        public void AddRepairSource(float fractionPerSecond, int healersPerTarget)
        {
            _repairSources++;
            _repairFractionPerSource = Math.Max(_repairFractionPerSource, fractionPerSecond);
            _repairHealersPerTarget = Math.Max(_repairHealersPerTarget, healersPerTarget);
        }

        public int RepairSources => _repairSources;

        /// <summary>Share of one focused unit's maximum hull restored per second by the repairers in range.</summary>
        public float RepairFractionPerSecond => _repairFractionPerSource * Math.Min(_repairSources, _repairHealersPerTarget);

        public void Add(ForceComposition other)
        {
            for (int i = 0; i < ClassSlots; i++)
            {
                _hull[i] += other._hull[i];
                _shields[i] += other._shields[i];
                _units[i] += other._units[i];
                _maxHull[i] += other._maxHull[i];
                _shieldRegen[i] += other._shieldRegen[i];
                _hullDps[i] += other._hullDps[i];
                _shieldDps[i] += other._shieldDps[i];
                _piercingDps[i] += other._piercingDps[i];
            }
            _siegeBonusHullDps += other._siegeBonusHullDps;
            _siegeBonusShieldDps += other._siegeBonusShieldDps;
            _siegeBonusPiercingDps += other._siegeBonusPiercingDps;
            AddRepair(other);
            UnitCount += other.UnitCount;
        }

        private void AddRepair(ForceComposition other)
        {
            _repairSources += other._repairSources;
            _repairFractionPerSource = Math.Max(_repairFractionPerSource, other._repairFractionPerSource);
            _repairHealersPerTarget = Math.Max(_repairHealersPerTarget, other._repairHealersPerTarget);
        }

        /// <summary>Adds <paramref name="share"/> (0..1) of <paramref name="other"/>: an estimate of a part of a force.</summary>
        public void AddShare(ForceComposition other, float share)
        {
            for (int i = 0; i < ClassSlots; i++)
            {
                _hull[i] += other._hull[i] * share;
                _shields[i] += other._shields[i] * share;
                _units[i] += other._units[i] * share;
                _maxHull[i] += other._maxHull[i] * share;
                _shieldRegen[i] += other._shieldRegen[i] * share;
                _hullDps[i] += other._hullDps[i] * share;
                _shieldDps[i] += other._shieldDps[i] * share;
                _piercingDps[i] += other._piercingDps[i] * share;
            }
            _siegeBonusHullDps += other._siegeBonusHullDps * share;
            _siegeBonusShieldDps += other._siegeBonusShieldDps * share;
            _siegeBonusPiercingDps += other._siegeBonusPiercingDps * share;
            AddRepair(other);
            UnitCount += other.UnitCount;
        }

        public void CopyFrom(ForceComposition other)
        {
            Array.Copy(other._hull, _hull, ClassSlots);
            Array.Copy(other._shields, _shields, ClassSlots);
            Array.Copy(other._hullDps, _hullDps, ClassSlots);
            Array.Copy(other._shieldDps, _shieldDps, ClassSlots);
            Array.Copy(other._piercingDps, _piercingDps, ClassSlots);
            Array.Copy(other._units, _units, ClassSlots);
            Array.Copy(other._maxHull, _maxHull, ClassSlots);
            Array.Copy(other._shieldRegen, _shieldRegen, ClassSlots);
            _siegeBonusHullDps = other._siegeBonusHullDps;
            _siegeBonusShieldDps = other._siegeBonusShieldDps;
            _siegeBonusPiercingDps = other._siegeBonusPiercingDps;
            _repairSources = other._repairSources;
            _repairFractionPerSource = other._repairFractionPerSource;
            _repairHealersPerTarget = other._repairHealersPerTarget;
            UnitCount = other.UnitCount;
        }

        public void Clear()
        {
            Array.Clear(_hull, 0, ClassSlots);
            Array.Clear(_shields, 0, ClassSlots);
            Array.Clear(_hullDps, 0, ClassSlots);
            Array.Clear(_shieldDps, 0, ClassSlots);
            Array.Clear(_piercingDps, 0, ClassSlots);
            Array.Clear(_units, 0, ClassSlots);
            Array.Clear(_maxHull, 0, ClassSlots);
            Array.Clear(_shieldRegen, 0, ClassSlots);
            _siegeBonusHullDps = 0f;
            _siegeBonusShieldDps = 0f;
            _siegeBonusPiercingDps = 0f;
            _repairSources = 0;
            _repairFractionPerSource = 0f;
            _repairHealersPerTarget = 0;
            UnitCount = 0;
        }

        public float Hull(ShipClass shipClass) => _hull[(int)shipClass];
        public float Shields(ShipClass shipClass) => _shields[(int)shipClass];
        public float HullDps(ShipClass target) => _hullDps[(int)target];
        public float ShieldDps(ShipClass target) => _shieldDps[(int)target];
        public float PiercingDps(ShipClass target) => _piercingDps[(int)target];

        /// <summary>Average shield regeneration per second of one unit of the class.</summary>
        public float ShieldRegenPerUnit(ShipClass shipClass)
        {
            float units = _units[(int)shipClass];
            return units > 0f ? _shieldRegen[(int)shipClass] / units : 0f;
        }

        /// <summary>Hull per second the repairers in range restore on one focused unit of the class (never strikecraft).</summary>
        public float HullRepairPerUnit(ShipClass shipClass)
        {
            float units = _units[(int)shipClass];
            return units > 0f && !shipClass.IsStrikecraft()
                ? RepairFractionPerSecond * _maxHull[(int)shipClass] / units
                : 0f;
        }

        internal (float Hull, float Shields, float Piercing) DamageRates(ShipClass target, StructureKind? kind)
        {
            bool applySiege = target == ShipClass.Structure && SiegeDamage.IsEligibleTarget(kind);
            return (HullDps(target) + (applySiege ? _siegeBonusHullDps : 0f),
                ShieldDps(target) + (applySiege ? _siegeBonusShieldDps : 0f),
                PiercingDps(target) + (applySiege ? _siegeBonusPiercingDps : 0f));
        }
    }
}
