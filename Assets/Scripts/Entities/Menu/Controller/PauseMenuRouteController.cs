using EmpireAtWar.Components.Ui.Tooltip;
using EmpireAtWar.Entities.Tooltip;
using EmpireAtWar.Services.Tooltip;
using System;
using System.Collections.Generic;
using EmpireAtWar.Controllers.MiniMap;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.MainMenu.Settings;
using EmpireAtWar.Ui.Base;
using EmpireAtWar.Mvc;
using UnityEngine;
using EmpireAtWar.Views.Menu;
using Zenject;

namespace EmpireAtWar.Controllers.Menu
{
    public interface IUserStateNotifier:INotifier<UserNotifierState> {}

    public class PauseMenuRouteController : UiController, IPauseMenuRoute, IPauseMenuRouteNavigation, IUserStateNotifier, IObserver<BattleResult>, IInitializable, ILateDisposable
    {
        private readonly INotifier<BattleResult> _battleVictoryNotifier;
        private readonly IUiCancelRouter _cancelRouter;
        private readonly ISettingsRoute _settingsRoute;
        private List<IObserver<UserNotifierState>> _observers = new List<IObserver<UserNotifierState>>();
        private IPauseMenuUiView _ui;
        private bool _hasBattleEnded;
        private readonly TooltipRequests _tooltips;
        private TooltipHoverSubscription _tooltipHover;

        public PauseMenuRouteController(
            IUiService uiService,
            IUiCancelRouter cancelRouter,
            ISettingsRoute settingsRoute,
            INotifier<BattleResult> battleVictoryNotifier,
            ITooltipService tooltips) : base(uiService, cancelRouter)
        {
            _cancelRouter = cancelRouter;
            _settingsRoute = settingsRoute;
            _battleVictoryNotifier = battleVictoryNotifier;
            _tooltips = new TooltipRequests(tooltips);
        }

        public void Initialize()
        {
            BaseUi ui = UiService.CreateUi(UiType.PauseMenu);
            _ui = ui as IPauseMenuUiView
                ?? throw new InvalidOperationException(
                    "The skirmish pause menu prefab does not implement IPauseMenuUiView.");
            _ui.SetNavigation(this);
            _ui.Initialize();
            _tooltipHover = new TooltipHoverSubscription(
                ((ITooltipHoverView)_ui).TooltipHover, HandleTooltipHover, _tooltips);
            _ui.SetMenuVisible(false);
            _cancelRouter.CancelUnhandled += Open;
            _battleVictoryNotifier.AddObserver(this);
        }

        public void LateDispose()
        {
            _cancelRouter.CancelUnhandled -= Open;
            _battleVictoryNotifier.RemoveObserver(this);
            if (_ui != null)
            {
                _tooltipHover.Dispose();
                _ui.Dispose();
            }
        }

        public void Open()
        {
            SetMenuOpen(true);
        }

        public void Close()
        {
            SetMenuOpen(false);
        }

        // Settings opens above the pause menu and takes cancel focus; closing it returns here.
        public void OpenSettings()
        {
            _tooltips.HideAll();
            _settingsRoute.Open();
        }

        public void ExitSkirmish()
        {
            _ui.SetMenuVisible(false);
            Unfocus();
            UpdateState(UserNotifierState.ExitGame);
        }

        // Unconsumed Escape opens the menu; the open menu is focused, so the next Escape closes it.
        protected override bool HandleCancel()
        {
            Close();
            return true;
        }

        private void SetMenuOpen(bool isOpen)
        {
            _tooltips.HideAll();
            if (_hasBattleEnded)
            {
                return;
            }

            _ui.SetMenuVisible(isOpen);
            if (isOpen)
            {
                Focus();
            }
            else
            {
                Unfocus();
            }

            UpdateState(isOpen
                ? UserNotifierState.InMenu
                : UserNotifierState.InGame);
        }

        private void HandleTooltipHover(object key, TooltipAnchor anchor, object source) =>
            _tooltips.Show(source, key, anchor, () => !_hasBattleEnded, () =>
                new TooltipContent((string)key, (string)key switch
                {
                    "Resume" => "Close the pause menu and resume the battle.",
                    "Options" => "Edit display, camera and key-binding settings.",
                    "Exit" => "Exit this battle and return to the main menu.",
                    _ => throw new ArgumentOutOfRangeException(nameof(key))
                }));

        public void UpdateState(BattleResult result)
        {
            _hasBattleEnded = true;
        }

        private void UpdateState(UserNotifierState state)
        {
            foreach (IObserver<UserNotifierState> observer in _observers)
            {
                observer.UpdateState(state);
            }
        }

        public void AddObserver(IObserver<UserNotifierState> observer)
        {
            if (_observers.Contains(observer))
            {
                Debug.LogError($"{observer} is already in collection");
                return;
            }
            _observers.Add(observer);
        }

        public void RemoveObserver(IObserver<UserNotifierState> observer)
        {
            _observers.Remove(observer);
        }
    }
}
