using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Components.Weapon
{
    public class WeaponModel: PureModel, IWeaponContext
    {
        private IProjectileModel _projectileModel;

        public float DelayBetweenAttack { get; }

        
        public WeaponModel(IWeaponContext weaponContext, IProjectileModel projectileModel)
        {
            _projectileModel = projectileModel;
            DelayBetweenAttack = weaponContext.DelayBetweenAttack;
        }
    }
}