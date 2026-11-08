using Mirror;
using UnityEngine;

namespace EmpireAtWar.Services.Networking.Messages
{
    /// <summary>Host → owning client, reliable: the route a ship shows while selected; empty clears it.</summary>
    public struct ShipRouteMessage : NetworkMessage
    {
        public long EntityId;
        public Vector3[] Route;
    }
}
