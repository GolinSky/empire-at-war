using System;
using System.Collections.Generic;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Ship
{
    public interface IShipService : IService
    {
        event Action<IShipEntity> ShipAdded;
        event Action<IShipEntity> ShipRemoved;

        IReadOnlyList<IShipEntity> Ships { get; }

        void Add(IShipEntity entity);
        void Remove(IShipEntity entity);

        /// <summary>Adds every ship inside the area to the capture tally; a ship weighs 1.</summary>
        void AddShipStrength(Func<Vector3, bool> contains, CaptureStrengthBuilder tally);
    }

    public class ShipService : Service, IShipService
    {
        private const float SHIP_CAPTURE_STRENGTH = 1f;

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

        public void AddShipStrength(Func<Vector3, bool> contains, CaptureStrengthBuilder tally)
        {
            foreach (IShipEntity ship in _shipEntities)
            {
                if (contains(ship.WorldPosition))
                {
                    tally.Add(ship.Owner, SHIP_CAPTURE_STRENGTH);
                }
            }
        }
    }
}
