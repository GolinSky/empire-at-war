using System.Collections.Generic;
using EmpireAtWar.Commands.Menu;
using EmpireAtWar.Models.Menu;
using LightWeightFramework.Controller;
using UnityEngine;

namespace EmpireAtWar.Controllers.Menu
{
    public interface IUserStateNotifier:INotifier<UserNotifierState> {}
    
    public class MenuController : Controller<MenuModel>, IMenuCommand, IUserStateNotifier
    {
        private List<IObserver<UserNotifierState>> _observers = new List<IObserver<UserNotifierState>>();
        
        public MenuController(MenuModel model) : base(model)
        {
        }

        public void ExitSkirmish()
        {
            UpdateState(UserNotifierState.ExitGame);
        }

        public void ResumeGame()
        {
            UpdateState(UserNotifierState.InGame);
        }

        public void OpenMenu()
        {
            UpdateState(UserNotifierState.InMenu);
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