using EmpireAtWar.Models.Ship;
using EmpireAtWar.Services.NavigationService;
using EmpireAtWar.Services.Ship;
using LightWeightFramework.Controller;
using Zenject;

namespace EmpireAtWar.Controllers.Ship
{
    public interface IShipEntity
    {
        IShipModelObserver ModelObserver { get; }
        ISelectable Selectable { get; }
    }
    
    public class ShipController: Controller<ShipModel>, IInitializable, ILateDisposable, IShipEntity
    {
        private readonly IShipService shipService;

        public IShipModelObserver ModelObserver => Model;
        
        public ISelectable Selectable { get; }

        public ShipController(ShipModel model, IShipService shipService, ISelectable selectable) : base(model)
        {
            this.shipService = shipService;
            Selectable = selectable;
        }

        public void Initialize()
        {
            shipService.Add(this);
        }
        
        public void LateDispose()
        {
            shipService.Remove(this);
        }
    }
}