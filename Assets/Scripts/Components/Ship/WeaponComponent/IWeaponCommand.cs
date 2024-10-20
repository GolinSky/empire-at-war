using EmpireAtWar.Models.Weapon;
using EmpireAtWar.ViewComponents.Health;
using LightWeightFramework.Command;

namespace EmpireAtWar.Components.Ship.WeaponComponent
{
    public interface IWeaponCommand:ICommand
    {
        void ApplyDamage(IHardPointView unitView, WeaponType weaponType);
    }
}