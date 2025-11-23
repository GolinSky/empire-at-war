using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Components.Weapon
{
    public class WeaponModel: PureModel, IWeaponContext
    {
        private IProjectileModel _projectileModel;
        private readonly WeaponDamageModel _weaponDamageModel;

        public float DelayBetweenAttack { get; }
        public float OptimalAttackRange { get; set; } = 100f;// temp value


        public WeaponModel(IWeaponContext weaponContext, IProjectileModel projectileModel, WeaponDamageModel weaponDamageModel)
        {
            _projectileModel = projectileModel;
            _weaponDamageModel = weaponDamageModel;
            DelayBetweenAttack = weaponContext.DelayBetweenAttack;
        }
        
        public float GetAttackDistance(WeaponType weaponType)
        {
            return _weaponDamageModel.GetDamageModel(weaponType).Distance;
        }
    }
}