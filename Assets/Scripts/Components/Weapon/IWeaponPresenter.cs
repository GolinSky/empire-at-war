using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Models.Health;

namespace EmpireAtWar.Components.Weapon
{
    public interface IWeaponPresenter
    {
        /// <summary>Rolls accuracy for one shot. False means the shot is a visual-only miss.</summary>
        bool RollHit(AttackData attackData, WeaponProfile profile);
        void ApplyDamage(AttackData attackData, IHardPointModel hardPointModel, WeaponProfile profile, float attackDelay);
        bool CommitImpact(AttackData attackData, IHardPointModel hardPointModel, float damage, DamageType damageType,
            int targetId);
    }
}
