using System;
using System.Collections.Generic;
using EmpireAtWar.Controllers.MiniMap;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Models.Menu;
using EmpireAtWar.Ui.Base;
using EmpireAtWar.Mvc;
using UnityEngine;
using EmpireAtWar.Views.Menu;
using Zenject;

namespace EmpireAtWar.Controllers.Menu
{
    public interface IUserStateNotifier:INotifier<UserNotifierState> {}
    
    public class MenuController : UiController, IPauseMenuPresenter, IUserStateNotifier, IObserver<BattleResult>, IInitializable, ILateDisposable
    {
        private readonly INotifier<BattleResult> _battleVictoryNotifier;
        private readonly IUiCancelRouter _cancelRouter;
        private List<IObserver<UserNotifierState>> _observers = new List<IObserver<UserNotifierState>>();
        private IPauseMenuUiView _ui;
        private bool _hasBattleEnded;

        public MenuController(
            IUiService uiService,
            IUiCancelRouter cancelRouter,
            INotifier<BattleResult> battleVictoryNotifier) : base(uiService, cancelRouter)
        {
            _cancelRouter = cancelRouter;
            _battleVictoryNotifier = battleVictoryNotifier;
        }
        
        public void Initialize()
        {
            BaseUi ui = UiService.CreateUi(UiType.PauseMenu);
            _ui = ui as IPauseMenuUiView
                ?? throw new InvalidOperationException(
                    "The skirmish pause menu prefab does not implement IPauseMenuUiView.");
            _ui.SetPresenter(this);
            _ui.Initialize();
            _ui.SetMenuVisible(false);
            _cancelRouter.CancelUnhandled += OpenMenu;
            _battleVictoryNotifier.AddObserver(this);
        }

        public void LateDispose()
        {
            _cancelRouter.CancelUnhandled -= OpenMenu;
            _battleVictoryNotifier.RemoveObserver(this);
            if (_ui != null)
            {
                _ui.Dispose();
            }
        }

        public void ExitSkirmish()
        {
            _ui.SetMenuVisible(false);
            Unfocus();
            UpdateState(UserNotifierState.ExitGame);
        }

        public void ResumeGame()
        {
            SetMenuOpen(false);
        }

        public void OpenMenu()
        {
            SetMenuOpen(true);
        }

        // Unconsumed Escape opens the menu; the open menu is focused, so the next Escape closes it.
        protected override bool HandleCancel()
        {
            SetMenuOpen(false);
            return true;
        }

        private void SetMenuOpen(bool isOpen)
        {
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
