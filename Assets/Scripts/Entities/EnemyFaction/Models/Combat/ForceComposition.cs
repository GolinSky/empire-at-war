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
        private float _siegeBonusHullDps;
        private float _siegeBonusShieldDps;
        private float _siegeBonusPiercingDps;

        public int UnitCount { get; private set; }
        public bool IsEmpty => UnitCount == 0;

        /// <summary>Adds one unit at its current health.</summary>
        /// <param name="offenseScale">Share of the unit's weapons still firing (0..1).</param>
        public void Add(UnitCombatProfile profile, float hull, float shields, float offenseScale)
        {
            int unitClass = (int)profile.ShipClass;
            _hull[unitClass] += hull;
            _shields[unitClass] += shields;
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

        public void Add(ForceComposition other)
        {
            for (int i = 0; i < ClassSlots; i++)
            {
                _hull[i] += other._hull[i];
                _shields[i] += other._shields[i];
                _hullDps[i] += other._hullDps[i];
                _shieldDps[i] += other._shieldDps[i];
                _piercingDps[i] += other._piercingDps[i];
            }
            _siegeBonusHullDps += other._siegeBonusHullDps;
            _siegeBonusShieldDps += other._siegeBonusShieldDps;
            _siegeBonusPiercingDps += other._siegeBonusPiercingDps;
            UnitCount += other.UnitCount;
        }

        /// <summary>Adds <paramref name="share"/> (0..1) of <paramref name="other"/>: an estimate of a part of a force.</summary>
        public void AddShare(ForceComposition other, float share)
        {
            for (int i = 0; i < ClassSlots; i++)
            {
                _hull[i] += other._hull[i] * share;
                _shields[i] += other._shields[i] * share;
                _hullDps[i] += other._hullDps[i] * share;
                _shieldDps[i] += other._shieldDps[i] * share;
                _piercingDps[i] += other._piercingDps[i] * share;
            }
            _siegeBonusHullDps += other._siegeBonusHullDps * share;
            _siegeBonusShieldDps += other._siegeBonusShieldDps * share;
            _siegeBonusPiercingDps += other._siegeBonusPiercingDps * share;
            UnitCount += other.UnitCount;
        }

        public void CopyFrom(ForceComposition other)
        {
            Array.Copy(other._hull, _hull, ClassSlots);
            Array.Copy(other._shields, _shields, ClassSlots);
            Array.Copy(other._hullDps, _hullDps, ClassSlots);
            Array.Copy(other._shieldDps, _shieldDps, ClassSlots);
            Array.Copy(other._piercingDps, _piercingDps, ClassSlots);
            _siegeBonusHullDps = other._siegeBonusHullDps;
            _siegeBonusShieldDps = other._siegeBonusShieldDps;
            _siegeBonusPiercingDps = other._siegeBonusPiercingDps;
            UnitCount = other.UnitCount;
        }

        public void Clear()
        {
            Array.Clear(_hull, 0, ClassSlots);
            Array.Clear(_shields, 0, ClassSlots);
            Array.Clear(_hullDps, 0, ClassSlots);
            Array.Clear(_shieldDps, 0, ClassSlots);
            Array.Clear(_piercingDps, 0, ClassSlots);
            _siegeBonusHullDps = 0f;
            _siegeBonusShieldDps = 0f;
            _siegeBonusPiercingDps = 0f;
            UnitCount = 0;
        }

        public float Hull(ShipClass shipClass) => _hull[(int)shipClass];
        public float Shields(ShipClass shipClass) => _shields[(int)shipClass];
        public float HullDps(ShipClass target) => _hullDps[(int)target];
        public float ShieldDps(ShipClass target) => _shieldDps[(int)target];
        public float PiercingDps(ShipClass target) => _piercingDps[(int)target];

        internal (float Hull, float Shields, float Piercing) DamageRates(ShipClass target, StructureKind? kind)
        {
            bool applySiege = target == ShipClass.Structure && SiegeDamage.IsEligibleTarget(kind);
            return (HullDps(target) + (applySiege ? _siegeBonusHullDps : 0f),
                ShieldDps(target) + (applySiege ? _siegeBonusShieldDps : 0f),
                PiercingDps(target) + (applySiege ? _siegeBonusPiercingDps : 0f));
        }
    }
}
