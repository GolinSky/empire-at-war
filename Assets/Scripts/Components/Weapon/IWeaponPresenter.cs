using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Models.Health;

namespace EmpireAtWar.Components.Weapon
{
    public interface IWeaponPresenter
    {
        /// <summary>Chance that one shot of this profile hits the target; a miss is visual only.</summary>
        float GetHitChance(AttackData attackData, WeaponProfile profile);

        /// <param name="damageDuration">Seconds after arrival to spread the damage over in ticks; 0 lands it at once.</param>
        /// <param name="missile">In-flight record when the shot can be intercepted; null otherwise.</param>
        void ApplyDamage(AttackData attackData, IHardPointModel hardPointModel, WeaponProfile profile, float attackDelay,
            float damageDuration, IncomingMissile missile);

        bool CommitImpact(AttackData attackData, IHardPointModel hardPointModel, float damage, DamageType damageType,
            int targetId);
    }
}
