using EmpireAtWar.Models.Health;
using LightWeightFramework.Command;

namespace EmpireAtWar.Components.AttackComponent
{
    public interface IAttackCommand:ICommand
    {
        void ApplyDamage(IHardPointModel unitView, WeaponType weaponType, float duration);
    }
}