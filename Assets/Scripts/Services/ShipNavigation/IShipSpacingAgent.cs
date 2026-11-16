using UnityEngine;

namespace EmpireAtWar.Services.ShipNavigation
{
    public interface IShipSpacingAgent : IShipNavigationAgent
    {
        int SpacingRevision { get; }
        bool CanSettle { get; }
        Vector3 SpacingTargetPosition { get; }
        bool TrySettle(Vector3 destination, int revision);
    }
}
