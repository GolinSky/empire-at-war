using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Ship;
using EmpireAtWar.Services.Ship;
using LightWeightFramework.Controller;
using Zenject;

namespace EmpireAtWar.Controllers.Ship
{
    public interface IShipEntity
    {
        IShipModelObserver ModelObserver { get; }
    }

    public class ShipController : Controller<ShipModel>, IInitializable, ILateDisposable, IShipEntity
    {
        private readonly IShipService shipService;
        private ShipUnitModel enginesUnitModel;
        
        public IShipModelObserver ModelObserver => Model;

        public ShipController(ShipModel model, IShipService shipService) : base(model)
        {
            this.shipService = shipService;
        }

        public void Initialize()
        {
            shipService.Add(this);
            foreach (ShipUnitModel shipUnitModel in Model.HealthModel.ShipUnitModels)
            {
                if (shipUnitModel.ShipUnitType == ShipUnitType.Engines)
                {
                    enginesUnitModel = shipUnitModel;
                }
            }

            enginesUnitModel.OnShipUnitChanged += HandleEnginesData;
        }
        
        public void LateDispose()
        {
            shipService.Remove(this);
            enginesUnitModel.OnShipUnitChanged -= HandleEnginesData;
        }
        
        private void HandleEnginesData()
        {
            if (enginesUnitModel.Health <= 0f)
            {
                Model.ShipMoveModel.ApplyMoveCoefficient(Model.MinMoveCoefficient);
            }
        }
    }
}