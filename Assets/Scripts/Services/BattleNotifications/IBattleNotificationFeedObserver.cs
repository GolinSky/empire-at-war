using System;

namespace EmpireAtWar.Services.BattleNotifications
{
    public interface IBattleNotificationFeedObserver
    {
        event Action<BattleNotification> Shown;
        event Action<BattleNotification> Updated;
        event Action<BattleNotification> Hidden;
    }
}
