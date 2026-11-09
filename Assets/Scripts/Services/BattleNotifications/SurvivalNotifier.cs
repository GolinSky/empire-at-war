using EmpireAtWar.Entities.Game;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.Services.BattleNotifications
{
    /// <summary>Survival phase cards: the next wave's 10 s warning and each cleared wave.</summary>
    public sealed class SurvivalNotifier : IInitializable, ILateDisposable
    {
        private const float WARNING_SECONDS = 10f;

        private readonly IBattleNotifications _notifications;
        private readonly SurvivalModel _survival;
        private SurvivalPhase _phase;
        private int _wave;
        private float _timeLeft;

        public SurvivalNotifier(IBattleNotifications notifications, SurvivalModel survival)
        {
            _notifications = notifications;
            _survival = survival;
        }

        public void Initialize()
        {
            Remember();
            _survival.Changed += HandleChanged;
        }

        public void LateDispose() => _survival.Changed -= HandleChanged;

        private void HandleChanged()
        {
            SurvivalPhase phase = _survival.Phase;
            if (_phase == SurvivalPhase.WaveActive && (phase == SurvivalPhase.Preparation || phase == SurvivalPhase.Won))
                _notifications.Post(BattleNotificationPriority.Progress, $"Wave {_wave} cleared");
            if (phase == SurvivalPhase.Preparation && _survival.PreparationTimeLeft <= WARNING_SECONDS &&
                (_phase != SurvivalPhase.Preparation || _wave != _survival.WaveIndex || _timeLeft > WARNING_SECONDS) &&
                _survival.PreparationTimeLeft > 0f)
                _notifications.Post(BattleNotificationPriority.Urgent,
                    $"Wave {_survival.WaveIndex} incoming in {WARNING_SECONDS:0} s");
            Remember();
        }

        private void Remember()
        {
            _phase = _survival.Phase;
            _wave = _survival.WaveIndex;
            _timeLeft = _survival.PreparationTimeLeft;
        }
    }
}
