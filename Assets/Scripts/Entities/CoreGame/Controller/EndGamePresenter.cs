using System;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Views.Game;

namespace EmpireAtWar.Controllers.Game
{
    public sealed class EndGamePresenter : IDisposable
    {
        private readonly IBattleVictoryService _battleVictoryService;
        private readonly IEndGameView _view;
        private readonly Action _returnToMenu;
        private bool _isLeaving;

        public EndGamePresenter(
            IBattleVictoryService battleVictoryService,
            IEndGameView view,
            Action returnToMenu)
        {
            _battleVictoryService = battleVictoryService ?? throw new ArgumentNullException(nameof(battleVictoryService));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _returnToMenu = returnToMenu ?? throw new ArgumentNullException(nameof(returnToMenu));
            _view.Hide();
            _view.ReturnToMenuRequested += ReturnToMenu;
            _battleVictoryService.OutcomeChanged += ShowResult;

            if (_battleVictoryService.CurrentOutcome != BattleOutcome.None)
            {
                ShowResult(_battleVictoryService.CurrentOutcome);
            }
        }

        public void Dispose()
        {
            _view.ReturnToMenuRequested -= ReturnToMenu;
            _battleVictoryService.OutcomeChanged -= ShowResult;
        }

        private void ShowResult(BattleOutcome outcome)
        {
            _view.ShowResult(_battleVictoryService.FinalResult);
        }

        private void ReturnToMenu()
        {
            if (_isLeaving)
            {
                return;
            }

            _isLeaving = true;
            _returnToMenu();
        }
    }
}
