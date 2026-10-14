using System;
using EmpireAtWar.Entities.Units;

namespace EmpireAtWar.Entities.Game
{
    public sealed class SimulationFleetEntry
    {
        public UnitTypeId Unit { get; }
        public int Quantity { get; }
        public SimulationFleetEntry(UnitTypeId unit, int quantity)
        {
            if (quantity < 1 || quantity > 100) throw new ArgumentOutOfRangeException(nameof(quantity));
            Unit = unit;
            Quantity = quantity;
        }
    }
}
