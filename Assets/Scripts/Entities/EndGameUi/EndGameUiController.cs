using EmpireAtWar.Lifecycle;
using System;
using EmpireAtWar.Ui.Base;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Components.Ui.Tooltip;
using EmpireAtWar.Entities.Tooltip;
using EmpireAtWar.Services.Tooltip;

namespace EmpireAtWar.Entities.EndGameUi
{
    public sealed class EndGameUiController : UiController, IEndGamePresenter, IObserver<BattleResult>, IOrderedDisposable
    {
        private readonly INotifier<BattleResult> _battleVictoryNotifier;
        private readonly EndGameUi _view;
        private readonly Action _returnToMenu;
        private BattleResult _result;
        private readonly TooltipRequests _tooltips;
        private readonly TooltipHoverSubscription _tooltipHover;
        private bool _isLeaving;
        private bool _isShown;

        public EndGameUiController(IUiService uiService, IUiCancelRouter cancelRouter,
            INotifier<BattleResult> battleVictoryNotifier,
            ITooltipService tooltipService, Action returnToMenu) : base(uiService, cancelRouter)
        {
            _battleVictoryNotifier = battleVictoryNotifier;
            _view = CreateUi<EndGameUi>();
            _returnToMenu = returnToMenu;
            _tooltips = new TooltipRequests(tooltipService);
            _view.AssignPresenter(this);
            _view.Initialize();
            _tooltipHover = new TooltipHoverSubscription(_view.TooltipHover,
                HandleTooltipHover, _tooltips);
            _view.Hide();
            _battleVictoryNotifier.AddObserver(this);
        }

        public void Dispose()
        {
            _battleVictoryNotifier.RemoveObserver(this);
            _tooltipHover.Dispose();
            if (_view != null)
            {
                _view.Hide();
                _view.Dispose();
            }
        }

        public void UpdateState(BattleResult result)
        {
            if (result.Mode == GameMode.BattleSimulation) return;
            _result = result;
            _isShown = true;
            _view.ShowResult(result);
        }

        public void ReturnToMenu()
        {
            if (_isLeaving) return;
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
