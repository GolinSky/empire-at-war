using System;
using EmpireAtWar.Models.Factions;
using WorkShop.LightWeightFramework.Service;

namespace EmpireAtWar.Services.Reinforcement
{
    public interface IReinforcementService:IService
    {
        event Action<ShipType> OnReinforcementAdded;
        void AddReinforcement(ShipType shipType);
    }
    public class ReinforcementService:Service, IReinforcementService
    {
        public event Action<ShipType> OnReinforcementAdded;

        public void AddReinforcement(ShipType shipType)
        {
            OnReinforcementAdded?.Invoke(shipType);
        }
    }
}