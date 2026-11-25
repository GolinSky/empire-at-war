using EmpireAtWar.Models.Players;
using System.Collections.Generic;
using EmpireAtWar.Presenters.ReinforcementZones;

namespace EmpireAtWar.Services.ReinforcementZones
{
    /// <summary>Read access to the live reinforcement zones.</summary>
    public interface IReinforcementZoneSource
    {
        IReadOnlyList<ReinforcementZone> Zones { get; }
        float SquadronCaptureWeight { get; }
        (float Friendly, float Hostile) GetTeamStrengths(ReinforcementZone zone, PlayerId owner);
    }
}
