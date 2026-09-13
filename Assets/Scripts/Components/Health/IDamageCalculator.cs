using EmpireAtWar.Components.AttackComponent;

namespace EmpireAtWar.Models.Health
{
    public interface IDamageCalculator
    {
        DamageData GetDamage(
            WeaponType weaponType,
            IHealthState healthState,
            bool isMoving,
            float damage);
    }
}
