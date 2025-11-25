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
            float distance = Vector3.Distance(hardPointView.Position, transform.position);
            float duration = distance / turretView.Speed;

            turretView.Attack(hardPointView, duration);
            turretView.ResetParent();
            WeaponPresenter.ApplyDamage(attackData, hardPointView, WeaponType, duration);
        }
    }
}