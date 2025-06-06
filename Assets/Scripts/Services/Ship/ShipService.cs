﻿using System.Collections.Generic;
using LightWeightFramework.Components.Service;

namespace EmpireAtWar.Ship
{
    public interface IShipService : IService
    {
        void Add(IShipEntity entity);
        void Remove(IShipEntity entity);

       // IShipEntity GetShipEntity(ISelectable selectable);
    }

    public class ShipService : Service, IShipService
    {
        private List<IShipEntity> _shipEntities = new List<IShipEntity>(); 

        public void Add(IShipEntity entity)
        {
            _shipEntities.Add(entity);
        }

        public void Remove(IShipEntity entity)
        {
            _shipEntities.Remove(entity);
        }

        // public IShipEntity GetShipEntity(ISelectable selectable)
        // {
        //     // foreach (var shipController in shipEntities)
        //     // {
        //     //     if (selectable == shipController.Selectable)
        //     //     {
        //     //         return shipController;
        //     //     }
        //     // }
        //
        //     return null;
        // }
    }
}