namespace EmpireAtWar.Services.BattleNotifications
{
    /// <summary>Lower values are more important and win when the visible stack is full.</summary>
    public enum BattleNotificationPriority
    {
        Outcome = 0,
        Urgent = 1,
        Tactical = 2,
        Feedback = 3,
        Progress = 4
    }
}
