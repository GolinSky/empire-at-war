using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Mvc;
using System.Collections.Generic;

namespace EmpireAtWar.Components.Weapon
{
    public class WeaponModel: PureModel, IWeaponContext
    {

        public float DelayBetweenAttack { get; }
        public float OptimalAttackRange { get; private set; } = 100f;

        public WeaponDamageData WeaponDamageModel { get; }
        public IProjectileModel ProjectileModel{ get; }


        public WeaponModel(IWeaponContext weaponContext, IProjectileModel projectileModel, WeaponDamageData weaponDamageModel)
        {
            ProjectileModel = projectileModel;
            WeaponDamageModel = weaponDamageModel;
            DelayBetweenAttack = weaponContext.DelayBetweenAttack;
        }

        public void SetOptimalAttackRange(IEnumerable<WeaponType> weaponTypes)
        {
            float maxAttackDistance = 0f;

            foreach (WeaponType weaponType in weaponTypes)
            {
                float attackDistance = GetAttackDistance(weaponType);
                if (attackDistance > maxAttackDistance)
                {
                    maxAttackDistance = attackDistance;
                }
            }

            OptimalAttackRange = maxAttackDistance * 0.5f;
        }
        
        public float GetAttackDistance(WeaponType weaponType)
        {
            return WeaponDamageModel.GetDamageModel(weaponType).Distance;
        }
        
        public float GetDamage(WeaponType weaponType, float distance)
        {
            return WeaponDamageModel.GetDamageModel(weaponType).GetDamage(distance);
        }
    }
}
