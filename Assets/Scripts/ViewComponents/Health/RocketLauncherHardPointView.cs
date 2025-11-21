using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Models.Health;
using EmpireAtWar.ViewComponents.Weapon;
using UnityEngine;

namespace EmpireAtWar.ViewComponents.Health
{
    public class RocketLauncherHardPointView:WeaponHardPointView
    {
        public override void Attack(IHardPointModel hardPointView, WeaponType weaponType)
        {
            BaseTurretView turretView = GetTurret();
            turretView.SetParent(transform);
            float distance = Vector3.Distance(hardPointView.Position, transform.position);
            float duration = distance / turretView.Speed;

            turretView.Attack(hardPointView, duration);
            turretView.ResetParent();
            AttackCommand.ApplyDamage(hardPointView, weaponType, duration);
        }
    }
}