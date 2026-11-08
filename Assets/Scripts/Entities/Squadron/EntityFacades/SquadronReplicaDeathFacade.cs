using EmpireAtWar.Components.Squadrons.Health;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;

namespace EmpireAtWar.Entities.Squadrons.EntityFacades
{
    public sealed class SquadronReplicaDeathFacade : IReplicaDeathFacade
    {
        private readonly SquadronHealthModel _health;

        public SquadronReplicaDeathFacade(SquadronHealthModel health)
        {
            _health = health;
        }

        public void Die() => _health.Kill();
    }
}
