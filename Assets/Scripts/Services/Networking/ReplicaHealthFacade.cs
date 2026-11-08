using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;

namespace EmpireAtWar.Services.Networking
{
    /// <summary>Online client: weapons still fire for the visuals, but damage is the host's; health arrives replicated.</summary>
    public sealed class ReplicaHealthFacade : IHealthFacade
    {
        public static readonly ReplicaHealthFacade Instance = new ReplicaHealthFacade();

        public void ApplyDamage(float damage, DamageType damageType, int id)
        {
        }
    }
}
