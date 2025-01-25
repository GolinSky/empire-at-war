using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Services.Initialiaze;
using LightWeightFramework.Controller;
using Zenject;

namespace EmpireAtWar.Ship
{
    public interface IShipEntity
    {
        IShipModelObserver ModelObserver { get; }
    }

    public class ShipController : Controller<ShipModel>, IInitializable, ILateDisposable, IShipEntity, ILateIInitializable
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
           

        }
        
        public void LateInitialize()
        {
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