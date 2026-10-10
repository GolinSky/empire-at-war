using System.Collections.Generic;

namespace EmpireAtWar.Models.Health
{
    /// <summary>Every hardpoint of one unit, indexed by hardpoint id, plus the unit's shared shield capacity.</summary>
    public interface IHardPointsSource
    {
        IReadOnlyList<IHardPointStatus> HardPoints { get; }
        float MaxShields { get; }
    }
}
