using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Models.Health;
using LightWeightFramework.Controller;
using Zenject;

namespace EmpireAtWar.Ship
{
    public interface IShipEntity
    {
        IShipModelObserver ModelObserver { get; }
    }

    public class ShipController : Controller<ShipModel>, IInitializable, ILateDisposable, IShipEntity
    {
        private readonly IShipService shipService;
        private HardPointModel enginesUnitModel;
        
        public IShipModelObserver ModelObserver => Model;

        public ShipController(ShipModel model, IShipService shipService) : base(model)
        {
            this.shipService = shipService;
        }

        public void Initialize()
        {
            shipService.Add(this);
            foreach (HardPointModel shipUnitModel in Model.HealthModel.HardPointModels)
            {
                if (shipUnitModel.HardPointType == HardPointType.Engines)
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
            if (enginesUnitModel.IsDestroyed)
            {
                Model.ShipMoveModel.ApplyMoveCoefficient(Model.MinMoveCoefficient);
            }
        }
    }
}