namespace EmpireAtWar.Services.BattleNotifications
{
    /// <summary>Entry point for gameplay sources that want a text card shown to the local player.</summary>
    public interface IBattleNotifications
    {
        /// <param name="groupKey">While a card with this key is on screen or waiting, new posts add to its count.</param>
        /// <param name="groupFormat">Text for a counted card, with {0} as the count; null appends " ×N".</param>
        /// <param name="iconKey">Existing UI icon key; null leaves the card text-only.</param>
        void Post(BattleNotificationPriority priority, string text, string groupKey = null, string groupFormat = null,
            string iconKey = null);

        /// <summary>Shows the card, or keeps the card with this key on screen, without counting repeats.</summary>
        void Hold(BattleNotificationPriority priority, string text, string key, string iconKey = null);
    }
}
