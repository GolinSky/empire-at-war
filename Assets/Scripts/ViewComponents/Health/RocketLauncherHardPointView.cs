using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Models.Health;
using EmpireAtWar.ViewComponents.Weapon;
using UnityEngine;

namespace EmpireAtWar.ViewComponents.Health
{
    public class RocketLauncherHardPointView:WeaponHardPointView
    {
        public override void Attack(AttackData attackData, IHardPointModel hardPointView)
        {
            BaseTurretView turretView = GetTurret();
            turretView.SetParent(transform);
 

            turretView.Attack(hardPointView, out var duration);
            turretView.ResetParent();
            WeaponPresenter.ApplyDamage(attackData, hardPointView, WeaponType, duration);
        }
    }
}