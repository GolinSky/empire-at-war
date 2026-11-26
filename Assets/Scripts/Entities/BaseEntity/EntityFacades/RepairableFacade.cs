using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Models.Health;

namespace EmpireAtWar.Entities.BaseEntity.EntityFacades
{
    public sealed class RepairableFacade : IRepairableFacade
    {
        private readonly HealthModel _health;

        public RepairableFacade(HealthModel health)
        {
            _health = health;
        }

        public bool NeedsRepair => _health.NeedsRepair;
        public float HullPercentage => _health.HullPercentage;

        public void Repair(float fraction) => _health.Repair(fraction);
    }
}
