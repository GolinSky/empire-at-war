using EmpireAtWar.Entities.BaseEntity;

namespace EmpireAtWar.Services.UnitOrders
{
    /// <summary>
    /// Focus fire holders of the local player's team, for display. Offline and host read the slot registry; an online
    /// client reads the counts the host replicates.
    /// </summary>
    public interface IFocusFireCounts
    {
        int CountHolders(IEntity target, bool isSquadron);
    }
}
