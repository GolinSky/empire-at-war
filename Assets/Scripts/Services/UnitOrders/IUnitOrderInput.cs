using System;

namespace EmpireAtWar.Services.UnitOrders
{
    public interface IUnitOrderInput
    {
        event Action QueueWaypointReleased;

        bool IsQueueWaypointHeld { get; }
    }
}
