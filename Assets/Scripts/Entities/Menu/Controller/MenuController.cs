using System;
using System.Collections.Generic;
using EmpireAtWar.Commands.Menu;
using EmpireAtWar.Controllers.MiniMap;
using EmpireAtWar.Models.Menu;
using EmpireAtWar.Services.InputService;
using EmpireAtWar.Ui.Base;
using EmpireAtWar.Mvc;
using UnityEngine;
using EmpireAtWar.Views.Menu;
using Zenject;

namespace EmpireAtWar.Controllers.Menu
{
    public interface IUserStateNotifier:INotifier<UserNotifierState> {}
    
    public class MenuController : Controller<MenuModel>, IMenuCommand, IUserStateNotifier, IInitializable, ILateDisposable
    {
        private readonly IUiService _uiService;
        private readonly IInputService _inputService;
        private List<IObserver<UserNotifierState>> _observers = new List<IObserver<UserNotifierState>>();
        private IMenuUiView _ui;
        private bool _isMenuOpen;

        public MenuController(
            MenuModel model,
            IUiService uiService,
            IInputService inputService) : base(model)
        {
            _uiService = uiService;
            _inputService = inputService;
        }
        
        public void Initialize()
        {
            BaseUi ui = _uiService.CreateUi(UiType.Menu);
            _ui = ui as IMenuUiView
                ?? throw new InvalidOperationException(
                    "The skirmish menu prefab does not implement IMenuUiView.");
            _ui.SetMenuVisible(false);
            _inputService.OnEscapePressed += ToggleMenu;
        }

        public void LateDispose()
        {
            _inputService.OnEscapePressed -= ToggleMenu;
        }

        public void ExitSkirmish()
        {
            _isMenuOpen = false;
            _ui.SetMenuVisible(false);
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

        private void ToggleMenu()
        {
            SetMenuOpen(!_isMenuOpen);
        }

        private void SetMenuOpen(bool isOpen)
        {
            _isMenuOpen = isOpen;
            _ui.SetMenuVisible(isOpen);
            UpdateState(isOpen
                ? UserNotifierState.InMenu
                : UserNotifierState.InGame);
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
