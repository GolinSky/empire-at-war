using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Models.Health;

namespace EmpireAtWar.Entities.Ship.EntityFacades.Health
{
    public sealed class HealthReplicaDeathFacade : IReplicaDeathFacade
    {
        private readonly HealthModel _healthModel;

        public HealthReplicaDeathFacade(HealthModel healthModel)
        {
            _healthModel = healthModel;
        }

        public void Die() => _healthModel.Kill();
    }
}
