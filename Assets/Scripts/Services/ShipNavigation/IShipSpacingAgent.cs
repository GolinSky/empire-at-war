using UnityEngine;

namespace EmpireAtWar.Services.ShipNavigation
{
    public interface IShipSpacingAgent : IShipNavigationAgent
    {
        int SpacingRevision { get; }
        bool CanSettle { get; }
        bool IsMoving { get; }
        Vector3 SpacingTargetPosition { get; }
        Vector3 SpacingForward { get; }
        bool TrySettle(Vector3 destination, int revision);
    }
}
