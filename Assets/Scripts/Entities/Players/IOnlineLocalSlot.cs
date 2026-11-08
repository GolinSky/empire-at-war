namespace EmpireAtWar.Models.Players
{
    /// <summary>
    /// Project-lifetime answer to "which slot plays at this PC" while an online battle loads or runs. Offline
    /// battles have no answer; their single human is the local player.
    /// </summary>
    public interface IOnlineLocalSlot
    {
        bool TryGetLocalSlot(out PlayerId player);
    }
}
