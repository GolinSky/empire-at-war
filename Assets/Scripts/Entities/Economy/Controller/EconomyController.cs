using System.Collections.Generic;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Models.Economy;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Patterns.ChainOfResponsibility;
using EmpireAtWar.Ui.Base;
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
        private const float DEFAULT_INCOME = 1f;

        private readonly ITimer _incomeTimer;
        private IChainHandler<UnitRequest> _nextChain;
        private List<IIncomeProvider> _incomeProviders = new List<IIncomeProvider>();
        
        private float _commonIncome;
        public float Income => DEFAULT_INCOME;
        
        public EconomyController(EconomyModel model) : base(model)
        {
            _incomeTimer = TimerFactory.ConstructTimer(model.IncomeDelay);
            Model.Money = Model.StartMoneyAmount;
        }
        public void Initialize()
        {
            AddProvider(this);
        }
        
        public void Tick()
        {
            if (_incomeTimer.IsComplete)
            {
                _incomeTimer.StartTimer();
                Model.Money += _commonIncome;
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
            _nextChain = chainHandler;
            return _nextChain;
        }

        public void Handle(UnitRequest unitRequest)
        {
            if (TryBuyUnit(unitRequest.FactionData.Price))
            {
                if (_nextChain != null)
                {
                    _nextChain.Handle(unitRequest);
                }
            }
        }

        public void Revert(UnitRequest unitRequest)
        {
            Model.Money += unitRequest.FactionData.Price;
        }

        public void AddProvider(IIncomeProvider incomeProvider)
        {
            if (_incomeProviders.Contains(incomeProvider))
            {
                Debug.LogError("Income already in collection");
            }
            _incomeProviders.Add(incomeProvider);
            CalculateBaseIncome();
        }

        public void RemoveProvider(IIncomeProvider incomeProvider)
        {
            if (!_incomeProviders.Contains(incomeProvider))
            {
                Debug.LogError("Income not contains in collection");
            }
            _incomeProviders.Remove(incomeProvider);
            CalculateBaseIncome();
        }

        public void RecalculateIncome(IIncomeProvider incomeProvider)
        {
            CalculateBaseIncome();
        }

        private void CalculateBaseIncome()
        {
            _commonIncome = 0;
            foreach (IIncomeProvider provider in _incomeProviders)
            {
                _commonIncome += provider.Income;
            }
        }
    }
}