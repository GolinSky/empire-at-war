using System;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.Ship.Health;

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

        public int UnitCount { get; private set; }
        public bool IsEmpty => UnitCount == 0;

        /// <summary>Adds one unit at its current health.</summary>
        /// <param name="offenseScale">Share of the unit's weapons still firing (0..1).</param>
        public void Add(UnitCombatProfile profile, float hull, float shields, float offenseScale)
        {
            int unitClass = (int)profile.ShipClass;
            _hull[unitClass] += hull;
            _shields[unitClass] += shields;
            foreach (ShipClass target in Classes)
            {
                int index = (int)target;
                _hullDps[index] += profile.HullDps(target) * offenseScale;
                _shieldDps[index] += profile.ShieldDps(target) * offenseScale;
                _piercingDps[index] += profile.PiercingDps(target) * offenseScale;
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

        public void CopyFrom(ForceComposition other)
        {
            Array.Copy(other._hull, _hull, ClassSlots);
            Array.Copy(other._shields, _shields, ClassSlots);
            Array.Copy(other._hullDps, _hullDps, ClassSlots);
            Array.Copy(other._shieldDps, _shieldDps, ClassSlots);
            Array.Copy(other._piercingDps, _piercingDps, ClassSlots);
            UnitCount = other.UnitCount;
        }

        public void Clear()
        {
            Array.Clear(_hull, 0, ClassSlots);
            Array.Clear(_shields, 0, ClassSlots);
            Array.Clear(_hullDps, 0, ClassSlots);
            Array.Clear(_shieldDps, 0, ClassSlots);
            Array.Clear(_piercingDps, 0, ClassSlots);
            UnitCount = 0;
        }

        public float Hull(ShipClass shipClass) => _hull[(int)shipClass];
        public float Shields(ShipClass shipClass) => _shields[(int)shipClass];
        public float HullDps(ShipClass target) => _hullDps[(int)target];
        public float ShieldDps(ShipClass target) => _shieldDps[(int)target];
        public float PiercingDps(ShipClass target) => _piercingDps[(int)target];
    }
}
