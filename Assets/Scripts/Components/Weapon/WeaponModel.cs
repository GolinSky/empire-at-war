using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Components.Weapon
{
    public class WeaponModel : Model
    {
        private readonly IReadOnlyDictionary<(DamageType, ShipClass), float> _accuracy;

        public float OptimalAttackRange { get; private set; } = 100f;

        public WeaponModel(IReadOnlyDictionary<(DamageType, ShipClass), float> accuracy)
        {
            _accuracy = accuracy;
        }

        public bool RollHit(DamageType damageType, ShipClass targetClass, float roll) =>
            roll < _accuracy[(damageType, targetClass)];

        public void SetAttackRange(float range)
        {
            OptimalAttackRange = range;
        }
    }
}
