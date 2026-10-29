using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Extensions;
using VContainer;

namespace EmpireAtWar.Models.Health
{
    /// <summary>
    /// Decorates base health data with the owning faction's researched shield capacity.
    /// Values are read live, so research completed mid-battle applies to units already on the field.
    /// </summary>
    public sealed class ResearchHealthData : IHealthData
    {
        private readonly IHealthData _healthData;
        private readonly IReadOnlyList<IFactionResearchModelObserver> _research;

        public ShipClass ShipClass => _healthData.ShipClass;
        public bool UsesHullHardPointBudget => _healthData.UsesHullHardPointBudget;
        public float Hull => _healthData.Hull;
        public float Shields
        {
            get
            {
                float shields = _healthData.Shields;
                foreach (IFactionResearchModelObserver research in _research)
                    shields *= research.GetMultiplier(ResearchStat.ShieldCapacity, ShipClass);
                return shields;
            }
        }
        public float ShieldRegenerateValue => _healthData.ShieldRegenerateValue;
        public float ShieldRegenerateDelay => _healthData.ShieldRegenerateDelay;
        public IReadOnlyList<HardPointHealth> HardPointHealth => _healthData.HardPointHealth;

        public ResearchHealthData([Key(EntityBindType.HealthData)] IHealthData healthData,
            IReadOnlyList<IFactionResearchModelObserver> research)
        {
            _healthData = healthData;
            _research = research;
        }
    }
}
