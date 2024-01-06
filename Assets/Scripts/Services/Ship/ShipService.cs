using System.Collections.Generic;
using EmpireAtWar.Controllers.Ship;
using EmpireAtWar.Services.NavigationService;
using WorkShop.LightWeightFramework.Game;
using WorkShop.LightWeightFramework.Service;

namespace EmpireAtWar.Services.Ship
{
    public interface IShipService : IService
    {
        void Add(IShipEntity entity);
        void Remove(IShipEntity entity);

        IShipEntity GetShipEntity(ISelectable selectable);
    }

    public class ShipService : Service, IShipService
    {
        private List<IShipEntity> shipEntities = new List<IShipEntity>(); 
        protected override void OnInit(IGameObserver gameObserver)
        {
        }

        protected override void Release()
        {
        }

        //fast fall
        public void Add(IShipEntity entity)
        {
            shipEntities.Add(entity);
        }

        //fast fall
        public void Remove(IShipEntity entity)
        {
            shipEntities.Remove(entity);
        }

        public IShipEntity GetShipEntity(ISelectable selectable)
        {
            foreach (var shipController in shipEntities)
            {
                if (selectable == shipController)
                {
                    return shipController;
                }
            }

            return null;
        }
    }
}