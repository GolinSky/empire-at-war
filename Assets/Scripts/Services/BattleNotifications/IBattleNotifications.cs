namespace EmpireAtWar.Services.BattleNotifications
{
    /// <summary>Entry point for gameplay sources that want a text card shown to the local player.</summary>
    public interface IBattleNotifications
    {
        /// <param name="groupKey">Cards with the same key posted close together merge into one counted card.</param>
        /// <param name="groupFormat">Text for a merged card, with {0} as the count; null appends " ×N".</param>
        void Post(BattleNotificationPriority priority, string text, string groupKey = null, string groupFormat = null);
    }
}
