using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Models.Health;

namespace EmpireAtWar.Components.Weapon
{
    public interface IWeaponPresenter
    {
        void ApplyDamage(AttackData attackData, IHardPointModel unitView, WeaponType weaponType, float attackDelay);
        bool CommitImpact(AttackData attackData, IHardPointModel hardPointModel, WeaponType weaponType, int targetId);
    }
}
