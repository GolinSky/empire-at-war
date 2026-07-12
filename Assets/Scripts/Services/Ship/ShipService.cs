using System;
using System.Collections.Generic;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Ship
{
    public interface IShipService : IService
    {
        event Action<IShipEntity> ShipAdded;
        event Action<IShipEntity> ShipRemoved;

        IReadOnlyList<IShipEntity> Ships { get; }

        void Add(IShipEntity entity);
        void Remove(IShipEntity entity);
    }

    public class ShipService : Service, IShipService
    {
        private readonly List<IShipEntity> _shipEntities = new List<IShipEntity>();

        public event Action<IShipEntity> ShipAdded;
        public event Action<IShipEntity> ShipRemoved;

        public IReadOnlyList<IShipEntity> Ships => _shipEntities;

        public void Add(IShipEntity entity)
        {
            _shipEntities.Add(entity);
            ShipAdded?.Invoke(entity);
        }

        public void Remove(IShipEntity entity)
        {
            if (_shipEntities.Remove(entity))
            {
                ShipRemoved?.Invoke(entity);
            }
        }
    }
}
