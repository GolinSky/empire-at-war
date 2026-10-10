using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Models.Health
{
    /// <summary>
    /// Decorates base health data with the owning faction's researched shield capacity.
    /// Values are read live, so research completed mid-battle applies to units already on the field.
    /// </summary>
    public sealed class ResearchHealthData : IHealthData
    {
        private readonly IHealthData _healthData;
        private readonly IFactionResearchModelObserver _research;

        public ShipClass ShipClass => _healthData.ShipClass;
        public float Hull => _healthData.Hull;
        public float Shields => _healthData.Shields * _research.GetMultiplier(ResearchStat.ShieldCapacity, ShipClass);
        public float ShieldRegenerateValue => _healthData.ShieldRegenerateValue;
        public float ShieldRegenerateDelay => _healthData.ShieldRegenerateDelay;
        public IReadOnlyList<HardPointHealth> HardPointHealth => _healthData.HardPointHealth;

        public ResearchHealthData(IHealthData healthData, IFactionResearchModelObserver research)
        {
            _healthData = healthData;
            _research = research;
        }
    }
}
