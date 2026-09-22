using System;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Views.Game;

namespace EmpireAtWar.Controllers.Game
{
    public sealed class EndGamePresenter : IObserver<BattleResult>, IDisposable
    {
        private readonly INotifier<BattleResult> _battleVictoryNotifier;
        private readonly IEndGameView _view;
        private readonly Action _returnToMenu;
        private bool _isLeaving;

        public EndGamePresenter(
            INotifier<BattleResult> battleVictoryNotifier,
            IEndGameView view,
            Action returnToMenu)
        {
            _battleVictoryNotifier = battleVictoryNotifier;
            _view = view;
            _returnToMenu = returnToMenu;
            _view.Hide();
            _view.ReturnToMenuRequested += ReturnToMenu;
            _battleVictoryNotifier.AddObserver(this);
        }

        public void Dispose()
        {
            _view.ReturnToMenuRequested -= ReturnToMenu;
            _battleVictoryNotifier.RemoveObserver(this);
        }

        public void UpdateState(BattleResult result)
        {
            _view.ShowResult(result);
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
