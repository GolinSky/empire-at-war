using System.Collections.Generic;
using EmpireAtWar.Controllers.Economy;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Models.Economy;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Player;
using UnityEngine;
using Utilities.ScriptUtils.Time;
using Zenject;

namespace EmpireAtWar.Services.Economy
{
    public class EconomyService : Service, IWallet, ITickable, IEconomyProvider, IIncomeProvider, IInitializable,
        ILateDisposable, IObserver<BattleState>
    {
        private const float DEFAULT_INCOME = 1f;

        private readonly ITimer _incomeTimer;

        private readonly EconomyModel _model;
        private readonly INotifier<BattleState> _battleState;
        private readonly IPlayerRegistry _playerRegistry;
        private readonly PlayerId _owner;
        private readonly List<IIncomeProvider> _incomeProviders = new();

        private float _commonIncome;
        // Income is paid only while the battle runs, never during loading.
        private bool _isRunning;

        public float Income => DEFAULT_INCOME;
        public float TotalIncome => _commonIncome;

        public EconomyService(
            EconomyModel model,
            EconomyData data,
            INotifier<BattleState> battleState,
            IPlayerRegistry playerRegistry,
            PlayerSlot owner)
        {
            _model = model;
            _battleState = battleState;
            _playerRegistry = playerRegistry;
            _owner = owner.Id;
            _incomeTimer = TimerFactory.ConstructTimer(data.IncomeDelay);
        }

        public void Initialize()
        {
            AddProvider(this);
            _battleState.AddObserver(this);
            // Registered so allied mining facilities can pay into this economy too.
            _playerRegistry.RegisterEconomy(_owner, this);
        }

        public void LateDispose()
        {
            _battleState.RemoveObserver(this);
            _playerRegistry.UnregisterEconomy(_owner);
        }

        public void UpdateState(BattleState state)
        {
            _isRunning = state == BattleState.Running;
        }

        public void Tick()
        {
            if (_isRunning && _incomeTimer.IsComplete)
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
