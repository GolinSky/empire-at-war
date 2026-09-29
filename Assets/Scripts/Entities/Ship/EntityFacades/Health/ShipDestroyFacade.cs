using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Models.Health;

namespace EmpireAtWar.Entities.Ship.EntityFacades.Health
{
    public sealed class ShipDestroyFacade : IEntityDestroyFacade
    {
        private readonly HealthModel _healthModel;

        public ShipDestroyFacade(HealthModel healthModel)
        {
            _healthModel = healthModel;
        }

        public void Destroy()
        {
            _healthModel.Kill();
        }
    }
}
