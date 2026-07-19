using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Components.Weapon
{
    public class WeaponModel: PureModel, IWeaponContext
    {

        public float DelayBetweenAttack { get; }
        public float OptimalAttackRange { get; set; } = 100f;// temp value

        public WeaponDamageModel WeaponDamageModel { get; }
        public IProjectileModel ProjectileModel{ get; }


        public WeaponModel(IWeaponContext weaponContext, IProjectileModel projectileModel, WeaponDamageModel weaponDamageModel)
        {
            ProjectileModel = projectileModel;
            WeaponDamageModel = weaponDamageModel;
            DelayBetweenAttack = weaponContext.DelayBetweenAttack;
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