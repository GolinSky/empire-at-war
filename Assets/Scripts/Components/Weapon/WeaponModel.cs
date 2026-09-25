using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Components.Weapon
{
    public class WeaponModel : PureModel
    {
        private readonly WeaponsData _weaponsData;
        private readonly DamageMatrixData _damageMatrix;

        public float OptimalAttackRange { get; private set; } = 100f;
        public float MissSpread => _damageMatrix.MissSpread;

        public WeaponModel(WeaponsData weaponsData, DamageMatrixData damageMatrix)
        {
            _weaponsData = weaponsData;
            _damageMatrix = damageMatrix;
        }

        public WeaponProfile GetProfile(WeaponType weaponType) => _weaponsData.GetProfile(weaponType);

        public bool RollHit(DamageType damageType, ShipClass targetClass) =>
            Random.value < _damageMatrix.GetAccuracy(damageType, targetClass);

        public void SetOptimalAttackRange(IEnumerable<WeaponType> weaponTypes)
        {
            float maxAttackDistance = 0f;
            foreach (WeaponType weaponType in weaponTypes)
            {
                maxAttackDistance = Mathf.Max(maxAttackDistance, GetProfile(weaponType).Range);
            }

            OptimalAttackRange = maxAttackDistance * 0.5f;
        }
    }
}
