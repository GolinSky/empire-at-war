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

        public float HullDps(ShipClass target) => _hullDps[(int)target];
        public float ShieldDps(ShipClass target) => _shieldDps[(int)target];
        public float PiercingDps(ShipClass target) => _piercingDps[(int)target];
    }
}
