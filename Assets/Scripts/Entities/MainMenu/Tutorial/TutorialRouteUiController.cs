using EmpireAtWar.Commands.Game;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Ui.Base;

namespace EmpireAtWar.Entities.MainMenu.Tutorial
{
    public sealed class TutorialRouteUiController : UiController, ITutorialRoute, ITutorialPresenter, ILateDisposable
    {
        private readonly IGameCommand _gameCommand;
        private TutorialFactionUi _ui;

        public TutorialRouteUiController(IUiService uiService, IUiCancelRouter cancelRouter, IGameCommand gameCommand)
            : base(uiService, cancelRouter)
        {
            _gameCommand = gameCommand;
        }

        public void Open()
        {
            if (_ui == null)
            {
                _ui = CreateUi<TutorialFactionUi>();
                _ui.AssignPresenter(this);
                _ui.Initialize();
            }
            _ui.Show();
            Focus();
        }

        public void Close()
        {
            _ui.Hide();
            Unfocus();
        }

        public void StartTutorial(FactionType faction) => _gameCommand.StartBattle(TutorialPreset.Create(faction));

        protected override bool HandleCancel()
        {
            Close();
            return true;
        }

        public void LateDispose()
        {
            if (_ui != null) _ui.Dispose();
            _ui = null;
            Unfocus();
        }
    }
}
