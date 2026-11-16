using System;

namespace EmpireAtWar.Services.ReinforcementZones
{
    public interface IRelayStatusEvents : IReinforcementZoneSource
    {
        event Action<RelayOwnershipChange> RelayOwnershipChanged;
        event Action RelaySourcesChanged;
    }
}
