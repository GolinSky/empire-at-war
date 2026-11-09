using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Services.BattleNotifications;
using EmpireAtWar.Ui.Base;
using UnityEngine;

namespace EmpireAtWar.Entities.BattleNotifications
{
    public sealed class BattleNotificationUiController : UiController, IBattleNotificationPresenter, IInitializable,
        ITickable, ILateDisposable, IObserver<BattleState>
    {
        private readonly BattleNotificationFeed _feed;
        private readonly SkirmishStateModel _state;
        private readonly BattleDefinition _battle;
        private BattleNotificationUi _ui;

        public BattleNotificationUiController(IUiService uiService, BattleNotificationFeed feed,
            SkirmishStateModel state, BattleDefinition battle) : base(uiService)
        {
            _feed = feed;
            _state = state;
            _battle = battle;
        }

        public void Initialize()
        {
            _ui = CreateUi<BattleNotificationUi>();
            _ui.AssignPresenter(this);
            _ui.SetFeed(_feed);
            _ui.SetSurvivalLayout(_battle.Mode == GameMode.Survival);
            _ui.Initialize();
            _ui.Show();
            _state.AddObserver(this);
        }

        public void UpdateState(BattleState state)
        {
            if (state == BattleState.Ended) _feed.EndBattle();
            else _feed.SetAccepting(state == BattleState.Running || state == BattleState.Paused);
        }

        // Real time, so cards still expire while the battle is paused or sped up.
        public void Tick() => _feed.Advance(Time.unscaledDeltaTime);

        public void Dismiss(int notificationId) => _feed.Dismiss(notificationId);

        public void LateDispose()
        {
            _state.RemoveObserver(this);
            if (!ReferenceEquals(_ui, null)) _ui.Dispose();
        }
    }
}
