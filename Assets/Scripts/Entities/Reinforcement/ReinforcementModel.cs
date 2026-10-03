using System;
using System.Collections.Generic;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Mvc;
using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Models.Reinforcement
{
    public interface IReinforcementModelObserver : IModelObserver
    {
        event Action<int> OnCapacityChanged;

        event Action<bool> OnSpawnUnit;

        event Action<UnitRequest> OnReinforcementAdded;

        bool IsTrySpawning { get; }
        int MaxUnitCapacity { get; }
        int CurrentUnitCapacity { get; }
        int CapacityLeft { get; }

        bool CanSpawnUnit(ShipType shipType);

        bool CanSpawnUnit(SquadronType squadronType);
    }

    public class ReinforcementModel : PureModel, IReinforcementModelObserver
    {
        private readonly ReinforcementData _data;
        private readonly Dictionary<ShipType, FactionData> _shipFactionData = new();
        private readonly Dictionary<SquadronType, FactionData> _squadronFactionData = new();
        private readonly Dictionary<UnitLimitKey, int> _reserveCounts = new();

        private int _currentUnitCapacity;

        public event Action<int> OnCapacityChanged;

        public event Action<bool> OnSpawnUnit;

        public event Action<UnitRequest> OnReinforcementAdded;

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

        public ReinforcementModel(ReinforcementData data)
        {
            _data = data;
        }

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

        public bool CanSpawnUnit(SquadronType squadronType)
        {
            return _squadronFactionData[squadronType].UnitCapacity <= CapacityLeft;
        }

        public void AddUnitCapacity(SquadronType squadronType)
        {
            CurrentUnitCapacity += _squadronFactionData[squadronType].UnitCapacity;
        }

        public void RemoveUnitCapacity(SquadronType squadronType)
        {
            CurrentUnitCapacity -= _squadronFactionData[squadronType].UnitCapacity;
        }

        public void UpdateSquadronData(SquadronUnitRequest squadronUnitRequest)
        {
            if (!_squadronFactionData.ContainsKey(squadronUnitRequest.Key))
            {
                _squadronFactionData.Add(squadronUnitRequest.Key, squadronUnitRequest.FactionData);
            }
        }

        public void AddReinforcement(UnitRequest unitRequest)
        {
            UnitLimitKey key = UnitLimitKey.From(unitRequest);
            _reserveCounts[key] = GetReserveCount(unitRequest) + 1;
            OnReinforcementAdded?.Invoke(unitRequest);
        }

        public int GetReserveCount(UnitRequest request) =>
            _reserveCounts.TryGetValue(UnitLimitKey.From(request), out int count) ? count : 0;

        public void ConsumeReinforcement(UnitRequest request)
        {
            UnitLimitKey key = UnitLimitKey.From(request);
            int remaining = _reserveCounts[key] - 1;
            if (remaining == 0) _reserveCounts.Remove(key);
            else _reserveCounts[key] = remaining;
        }
    }
}
