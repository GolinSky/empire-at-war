using EmpireAtWar.Entities.Game;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.Services.BattleNotifications
{
    public sealed class BattleOutcomeNotifier : IInitializable, ILateDisposable, IObserver<BattleResult>
    {
        private readonly IBattleNotifications _notifications;
        private readonly INotifier<BattleResult> _results;

        public BattleOutcomeNotifier(IBattleNotifications notifications, INotifier<BattleResult> results)
        {
            _notifications = notifications;
            _results = results;
        }

        public void Initialize() => _results.AddObserver(this);

        public void LateDispose() => _results.RemoveObserver(this);

        public void UpdateState(BattleResult result)
        {
            string text = Text(result.Outcome);
            if (text != null) _notifications.Post(BattleNotificationPriority.Outcome, text);
        }

        private static string Text(BattleOutcome outcome)
        {
            switch (outcome)
            {
                case BattleOutcome.PlayerVictory: return "Victory";
                case BattleOutcome.EnemyVictory: return "Defeat";
                case BattleOutcome.Draw: return "Draw";
                default: return null;
            }
        }
    }
}
