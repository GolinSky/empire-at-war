using EmpireAtWar.Controllers.Ship;
using EmpireAtWar.Services.NavigationService;
using UnityEngine;
using WorkShop.LightWeightFramework.Command;
using WorkShop.LightWeightFramework.Game;
using Zenject;

namespace EmpireAtWar.Commands.Ship
{
    public interface IShipCommand:ICommand
    {
        ISelectionCommand SelectionCommand { get; }
    }
    public class ShipCommand: Command<ShipController>, IShipCommand, IInitializable
    {
        public ISelectionCommand SelectionCommand { get; }

        public ShipCommand(ShipController controller, IGameObserver gameObserver, INavigationService navigationService) : base(controller, gameObserver)
        {
            SelectionCommand = new SelectionCommand(controller, navigationService, controller);
            Debug.Log("ShipCommand ctor");
        }

        public void Init(IGameObserver gameObserver)
        {
            
        }

        public void Release()
        {
        }


        public void Initialize()
        {
            Debug.Log("ShipCommand Initialize");

        }
    }
}