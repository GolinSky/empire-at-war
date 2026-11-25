using EmpireAtWar.Models.Players;
using EmpireAtWar.Presenters.ReinforcementZones;
using UnityEngine;

namespace EmpireAtWar.Services.ReinforcementZones
{
    public readonly struct RelayOwnershipChange
    {
        public ReinforcementZone Zone { get; }
        public Vector3 Position { get; }
        public PlayerId PreviousOwner { get; }
        public PlayerId Owner { get; }

        public RelayOwnershipChange(ReinforcementZone zone, PlayerId previousOwner)
        {
            Zone = zone;
            Position = zone.Center;
            PreviousOwner = previousOwner;
            Owner = zone.Owner;
        }
    }
}
