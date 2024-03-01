using System;
using LightWeightFramework.Components.Service;

namespace EmpireAtWar.Services.Economy
{
    public interface IEconomyService: IService
    {
        bool TryBuyUnit(float price);
        void Assign(Func<float, bool> func);
    }
    
    public class EconomyService:Service, IEconomyService
    {
        private Func<float, bool> tryBuyDelegate;
        
        public bool TryBuyUnit(float price)
        {
            return tryBuyDelegate?.Invoke(price) ?? false;
        }

        public void Assign(Func<float, bool> func)
        {
            tryBuyDelegate = func;
        }
    }
}