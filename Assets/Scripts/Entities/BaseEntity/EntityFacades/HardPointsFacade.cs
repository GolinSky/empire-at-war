using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Models.Health;

namespace EmpireAtWar.Entities.BaseEntity.EntityFacades
{
    public sealed class HardPointsFacade : IHardPointsFacade
    {
        private readonly IHardPointsSource _source;

        public IReadOnlyList<IHardPointStatus> HardPoints => _source.HardPoints;

        public HardPointsFacade(IHardPointsSource source)
        {
            _source = source;
        }
    }
}
