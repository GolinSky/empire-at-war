using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Models.Health;

namespace EmpireAtWar.Entities.Ship.EntityFacades.Health
{
    public sealed class HardPointsFacade : IHardPointsFacade
    {
        private readonly IHardPointsSource _source;

        public HardPointsFacade(IHardPointsSource source)
        {
            _source = source;
        }

        public IReadOnlyList<IHardPointStatus> HardPoints => _source.HardPoints;
        public float MaxShields => _source.MaxShields;
    }
}
