using System;
using System.Collections.Generic;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Mvc;
using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Models.Reinforcement
{
    public interface IReinforcementModelObserver : IModelObserver
    {
        event Action<int> OnCapacityChanged;
        event Action<bool> OnSpawnUnit;
        event Action<string, FactionData> OnReinforcementAdded;
        bool IsTrySpawning { get; }
        int MaxUnitCapacity { get; }
        int CurrentUnitCapacity { get; }
        int CapacityLeft { get; }
        bool CanSpawnUnit(ShipType shipType);
    }

    public class ReinforcementModel : PureModel, IReinforcementModelObserver
    {
        public event Action<int> OnCapacityChanged;
        public event Action<bool> OnSpawnUnit;
        public event Action<string, FactionData> OnReinforcementAdded;

        private readonly ReinforcementData _data;
        private readonly Dictionary<ShipType, FactionData> _shipFactionData = new();

        private int _currentUnitCapacity;

        public ReinforcementModel(ReinforcementData data)
        {
            _data = data;
        }

        public int CurrentUnitCapacity
        {
            get => _currentUnitCapacity;
            private set
            {
                _currentUnitCapacity = value;
                OnCapacityChanged?.Invoke(_currentUnitCapacity);
            }
        }

        public int MaxUnitCapacity => _data.MaxUnitCapacity;
        public int CapacityLeft => MaxUnitCapacity - CurrentUnitCapacity;
        public bool IsTrySpawning { get; set; }

        public void InvokeSpawnShipEvent(bool success)
        {
            OnSpawnUnit?.Invoke(success);
        }

        public bool CanSpawnUnit(ShipType shipType)
        {
            return _shipFactionData[shipType].UnitCapacity <= CapacityLeft;
        }

        public void AddUnitCapacity(ShipType shipType)
        {
            CurrentUnitCapacity += _shipFactionData[shipType].UnitCapacity;
        }

        public void RemoveUnitCapacity(ShipType shipType)
        {
            CurrentUnitCapacity -= _shipFactionData[shipType].UnitCapacity;
        }

        public void UpdateShipData(ShipUnitRequest shipUnitRequest)
        {
            if (!_shipFactionData.ContainsKey(shipUnitRequest.Key))
            {
                _shipFactionData.Add(shipUnitRequest.Key, shipUnitRequest.FactionData);
            }
        }

        public void AddReinforcement(UnitRequest unitRequest)
        {
            OnReinforcementAdded?.Invoke(unitRequest.Id, unitRequest.FactionData);
        }
    }
}
