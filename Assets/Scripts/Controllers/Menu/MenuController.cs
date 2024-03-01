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
        private List<IObserver<UserNotifierState>> observers = new List<IObserver<UserNotifierState>>();
        
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
            foreach (IObserver<UserNotifierState> observer in observers)
            {
                observer.UpdateState(state);
            }
        }

        public void AddObserver(IObserver<UserNotifierState> observer)
        {
            if (observers.Contains(observer))
            {
                Debug.LogError($"{observer} is already in collection");
                return;
            }
            observers.Add(observer);
        }

        public void RemoveObserver(IObserver<UserNotifierState> observer)
        {
            observers.Remove(observer);
        }
    }
}