using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;

namespace EmpireAtWar.Models.Health
{
    public interface IHealthData
    {
        ShipClass ShipClass { get; }
        bool UsesHullHardPointBudget { get; }
        float Hull { get; }
        float Shields { get; }
        float ShieldRegenerateValue { get; }
        float ShieldRegenerateDelay { get; }
        IReadOnlyList<HardPointHealth> HardPointHealth { get; }
    }
}
