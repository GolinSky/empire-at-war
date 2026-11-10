using System;

namespace EmpireAtWar.Entities.Reinforcement
{
    /// <summary>Reports that the reinforcement panel opened; read-only.</summary>
    public interface IReinforcementPanelObserver
    {
        event Action Opened;

        bool IsOpen { get; }
    }
}
