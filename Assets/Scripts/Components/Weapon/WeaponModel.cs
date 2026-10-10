using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Components.Weapon
{
    public class WeaponModel : Model
    {
        private readonly IReadOnlyDictionary<(DamageType, ShipClass), float> _accuracy;
        private readonly Dictionary<ShipClass, float> _engagementRanges = new Dictionary<ShipClass, float>();

        /// <summary>Longest firing distance of any hardpoint.</summary>
        public float MaxWeaponRange { get; private set; }

        public WeaponModel(IReadOnlyDictionary<(DamageType, ShipClass), float> accuracy)
        {
            _accuracy = accuracy;
        }

        public bool RollHit(DamageType damageType, ShipClass targetClass, float accuracyMultiplier, float roll) =>
            roll < _accuracy[(damageType, targetClass)] * accuracyMultiplier;

        public void SetRanges(float maxWeaponRange, IReadOnlyDictionary<ShipClass, float> engagementRanges)
        {
            MaxWeaponRange = maxWeaponRange;
            _engagementRanges.Clear();
            foreach (KeyValuePair<ShipClass, float> range in engagementRanges)
                _engagementRanges.Add(range.Key, range.Value);
        }

        /// <summary>Distance at which most of this unit's effective damage against the target class can fire.</summary>
        public float GetEngagementRange(ShipClass targetClass) => _engagementRanges[targetClass];
    }
}
