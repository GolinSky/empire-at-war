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
        private readonly IShipService _shipService;
        private HardPointModel _enginesUnitModel;
        
        public IShipModelObserver ModelObserver => Model;

        public ShipController(ShipModel model, IShipService shipService) : base(model)
        {
            _shipService = shipService;
        }

        public void Initialize()
        {
            _shipService.Add(this);
           

        }
        
        public void LateInitialize()
        {
            foreach (HardPointModel shipUnitModel in Model.HealthModel.HardPointModels)
            {
                if (shipUnitModel.HardPointType == HardPointType.Engines)
                {
                    _enginesUnitModel = shipUnitModel;
                }
            }
            _enginesUnitModel.OnShipUnitChanged += HandleEnginesData;

        }
        
        public void LateDispose()
        {
            _shipService.Remove(this);
            _enginesUnitModel.OnShipUnitChanged -= HandleEnginesData;
        }
        
        private void HandleEnginesData()
        {
            if (_enginesUnitModel.IsDestroyed)
            {
                Model.ShipMoveModel.ApplyMoveCoefficient(Model.MinMoveCoefficient);
            }
        }

 
    }
}