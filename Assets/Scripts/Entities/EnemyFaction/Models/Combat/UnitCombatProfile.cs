using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;

namespace EmpireAtWar.Entities.EnemyFaction.Models.Combat
{
    /// <summary>
    /// Full-health durability and damage output of one unit type, per target <see cref="ShipClass"/>.
    /// Damage rates already include the damage matrix multipliers and accuracy.
    /// </summary>
    public sealed class UnitCombatProfile
    {
        private static readonly ShipClass[] _warshipClasses =
            { ShipClass.Corvette, ShipClass.Frigate, ShipClass.Cruiser, ShipClass.Capital, ShipClass.HeavyCapital };

        private readonly float[] _hullDps;
        private readonly float[] _shieldDps;
        private readonly float[] _piercingDps;

        public ShipClass ShipClass { get; }
        public float Hull { get; }
        public float Shields { get; }

        /// <summary>Squadrons the unit fields at the same time from its hangar.</summary>
        public IReadOnlyList<UnitCombatProfile> Hangar { get; }

        /// <param name="hullDps">Hull damage per second of shield-blocked weapons, indexed by <see cref="ShipClass"/>.</param>
        /// <param name="shieldDps">Shield damage per second of the same weapons, indexed by <see cref="ShipClass"/>.</param>
        /// <param name="piercingDps">Hull damage per second of shield-piercing weapons, indexed by <see cref="ShipClass"/>.</param>
        public UnitCombatProfile(
            ShipClass shipClass,
            float hull,
            float shields,
            float[] hullDps,
            float[] shieldDps,
            float[] piercingDps,
            IReadOnlyList<UnitCombatProfile> hangar)
        {
            ShipClass = shipClass;
            Hull = hull;
            Shields = shields;
            _hullDps = hullDps;
            _shieldDps = shieldDps;
            _piercingDps = piercingDps;
            Hangar = hangar;
        }

        /// <summary>
        /// Standalone fighting value against warships: the square root of durability times average hull damage
        /// (Lanchester), hangar squadrons included. Divided by price it ranks units by value per credit.
        /// </summary>
        public float GetCombatValue()
        {
            float durability = 0f;
            float dps = 0f;
            AddCombatValue(ref durability, ref dps);
            return (float)Math.Sqrt(durability * dps / _warshipClasses.Length);
        }

        private void AddCombatValue(ref float durability, ref float dps)
        {
            durability += Hull + Shields;
            foreach (ShipClass target in _warshipClasses) dps += HullDps(target) + PiercingDps(target);
            foreach (UnitCombatProfile squadron in Hangar) squadron.AddCombatValue(ref durability, ref dps);
        }

        public float HullDps(ShipClass target) => _hullDps[(int)target];
        public float ShieldDps(ShipClass target) => _shieldDps[(int)target];
        public float PiercingDps(ShipClass target) => _piercingDps[(int)target];
    }
}
