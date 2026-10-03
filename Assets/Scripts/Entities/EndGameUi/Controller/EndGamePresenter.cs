using System;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Views.Game;
using EmpireAtWar.Components.Ui.Tooltip;
using EmpireAtWar.Entities.Tooltip;
using EmpireAtWar.Services.Tooltip;

namespace EmpireAtWar.Controllers.Game
{
    public sealed class EndGamePresenter : IObserver<BattleResult>, IDisposable
    {
        private readonly INotifier<BattleResult> _battleVictoryNotifier;
        private readonly IEndGameView _view;

        private readonly Action _returnToMenu;
        private BattleResult _result;
        private readonly TooltipRequests _tooltips;
        private readonly TooltipHoverSubscription _tooltipHover;

        private bool _isLeaving;
        private bool _isShown;

        public EndGamePresenter(
            INotifier<BattleResult> battleVictoryNotifier,
            IEndGameView view,
            ITooltipService tooltips, Action returnToMenu)
        {
            _battleVictoryNotifier = battleVictoryNotifier;
            _view = view;
            _returnToMenu = returnToMenu;
            _tooltips = new TooltipRequests(tooltips);
            _tooltipHover = new TooltipHoverSubscription(((ITooltipHoverView)view).TooltipHover,
                HandleTooltipHover, _tooltips);
            _view.Hide();
            _view.ReturnToMenuRequested += ReturnToMenu;
            _battleVictoryNotifier.AddObserver(this);
        }

        public void Dispose()
        {
            _view.ReturnToMenuRequested -= ReturnToMenu;
            _battleVictoryNotifier.RemoveObserver(this);
            _tooltipHover.Dispose();
        }

        public void UpdateState(BattleResult result)
        {
            _result = result;
            _isShown = true;
            _view.ShowResult(result);
        }

        private void ReturnToMenu()
        {
            if (_isLeaving)
            {
                return;
            }

            _isLeaving = true;
            _tooltips.HideAll();
            _returnToMenu();
        }

        private void HandleTooltipHover(object key, TooltipAnchor anchor, object source) =>
            _tooltips.Show(source, key, anchor, () => _isShown && !_isLeaving, () =>
                new TooltipContent(title: (string)key, description: (string)key == "Objective"
                    ? _result.VictoryCondition == BattleVictoryCondition.DestroyEnemyFleet
                        ? "Destroy the opposing fleet to win the battle." : "Destroy the opposing station to win the battle."
                    : "Return to the main menu to set up another battle."));
    }
}
