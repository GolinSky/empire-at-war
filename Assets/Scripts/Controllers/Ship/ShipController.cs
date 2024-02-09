using EmpireAtWar.Models.Ship;
using EmpireAtWar.Services.NavigationService;
using EmpireAtWar.Services.Ship;
using LightWeightFramework.Controller;
using UnityEngine;
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

        public IShipModelObserver ModelObserver => Model;

        public ShipController(ShipModel model, IShipService shipService) : base(model)
        {
            this.shipService = shipService;
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