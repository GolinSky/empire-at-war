using System.Collections.Generic;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Models.Economy;
using EmpireAtWar.Patterns.ChainOfResponsibility;
using LightWeightFramework.Controller;
using UnityEngine;
using Utilities.ScriptUtils.Time;
using Zenject;

namespace EmpireAtWar.Controllers.Economy
{
    public interface IEconomyProvider
    {
        void AddProvider(IIncomeProvider incomeProvider);
        void RemoveProvider(IIncomeProvider incomeProvider);
        void RecalculateIncome(IIncomeProvider incomeProvider);
    }

    public class EconomyController : Controller<EconomyModel>, IPurchaseChain, ITickable, IEconomyProvider, IIncomeProvider, IInitializable
    {
        private const float DefaultIncome = 1f;

        private readonly ITimer incomeTimer;
        private IChainHandler<UnitRequest> nextChain;
        private List<IIncomeProvider> incomeProviders = new List<IIncomeProvider>();
        
        private float commonIncome;
        public float Income => DefaultIncome;

        public EconomyController(EconomyModel model) : base(model)
        {
            incomeTimer = TimerFactory.ConstructTimer(model.IncomeDelay);
            Model.Money = Model.StartMoneyAmount;
        }
        public void Initialize()
        {
            AddProvider(this);
        }
        
        public void Tick()
        {
            if (incomeTimer.IsComplete)
            {
                incomeTimer.StartTimer();
                Model.Money += commonIncome;
            }
        }
        
        private bool TryBuyUnit(float price)
        {
            if (Model.Money > price)
            {
                Model.Money -= price;
                return true;
            }
            return false;
        }
        
        public IChainHandler<UnitRequest> SetNext(IChainHandler<UnitRequest> chainHandler)
        {
            nextChain = chainHandler;
            return nextChain;
        }

        public void Handle(UnitRequest unitRequest)
        {
            if (TryBuyUnit(unitRequest.FactionData.Price))
            {
                if (nextChain != null)
                {
                    nextChain.Handle(unitRequest);
                }
            }
        }

        public void Revert(UnitRequest unitRequest)
        {
            Model.Money += unitRequest.FactionData.Price;
        }

        public void AddProvider(IIncomeProvider incomeProvider)
        {
            if (incomeProviders.Contains(incomeProvider))
            {
                Debug.LogError("Income already in collection");
            }
            incomeProviders.Add(incomeProvider);
            CalculateBaseIncome();
        }

        public void RemoveProvider(IIncomeProvider incomeProvider)
        {
            if (!incomeProviders.Contains(incomeProvider))
            {
                Debug.LogError("Income not contains in collection");
            }
            incomeProviders.Remove(incomeProvider);
            CalculateBaseIncome();
        }

        public void RecalculateIncome(IIncomeProvider incomeProvider)
        {
            CalculateBaseIncome();
        }

        private void CalculateBaseIncome()
        {
            commonIncome = 0;
            foreach (IIncomeProvider provider in incomeProviders)
            {
                commonIncome += provider.Income;
            }
        }
    }
}