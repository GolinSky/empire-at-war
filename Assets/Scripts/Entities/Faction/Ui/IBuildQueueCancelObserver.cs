using System;

namespace EmpireAtWar.Entities.Faction.Ui
{
    /// <summary>Reports that the player cancelled an item in the build queue; read-only.</summary>
    public interface IBuildQueueCancelObserver
    {
        event Action Canceled;
    }
}
