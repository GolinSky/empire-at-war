using System.Collections.Generic;
using EmpireAtWar.Controllers.Economy;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Models.Economy;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Mvc;
using UnityEngine;
using Utilities.ScriptUtils.Time;
using Zenject;

namespace EmpireAtWar.Services.Economy
{
    public class EconomyService : Service, IWallet, ITickable, IEconomyProvider, IIncomeProvider, IInitializable
    {
        private const float DEFAULT_INCOME = 1f;

        private readonly ITimer _incomeTimer;

        private readonly EconomyModel _model;
        private readonly List<IIncomeProvider> _incomeProviders = new();

        private float _commonIncome;

        public float Income => DEFAULT_INCOME;
        public float TotalIncome => _commonIncome;

        public EconomyService(EconomyModel model, EconomyData data)
        {
            _model = model;
            _incomeTimer = TimerFactory.ConstructTimer(data.IncomeDelay);
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
                _model.AddMoney(_commonIncome);
            }
        }

        public bool TrySpend(UnitRequest unitRequest)
        {
            return _model.TrySpend(unitRequest.FactionData.Price);
        }

        public void Refund(UnitRequest unitRequest)
        {
            _model.AddMoney(unitRequest.FactionData.Price);
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
