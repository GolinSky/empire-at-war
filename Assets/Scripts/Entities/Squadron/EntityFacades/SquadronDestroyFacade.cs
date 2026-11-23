using EmpireAtWar.Components.Squadrons.Health;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;

namespace EmpireAtWar.Entities.Squadrons.EntityFacades
{
    public sealed class SquadronDestroyFacade : IEntityDestroyFacade
    {
        private readonly SquadronHealthModel _health;

        public SquadronDestroyFacade(SquadronHealthModel health)
        {
            _health = health;
        }

        public void Destroy() => _health.Kill();
    }
}
