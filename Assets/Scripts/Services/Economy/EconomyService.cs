using System.Collections.Generic;
using EmpireAtWar.Controllers.Economy;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.EnemyFaction.Controllers;
using EmpireAtWar.Models.Economy;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Factions;
using EmpireAtWar.Services.Player;
using UnityEngine;
using VContainer;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.Services.Economy
{
    public class EconomyService : Service, IWallet, ITickable, IEconomyProvider, IIncomeProvider, IInitializable,
        ILateDisposable, IObserver<BattleState>
    {
        private const float DEFAULT_INCOME = 3f;
        /// <summary>One <c>economy</c> telemetry row per player this often: money, income and its sources.</summary>
        private const float TELEMETRY_SECONDS = 15f;

        private readonly EconomyModel _model;
        private readonly EconomyData _data;
        private readonly INotifier<BattleState> _battleState;
        private readonly IPlayerRegistry _playerRegistry;
        private readonly IFactionResearchModelObserver _research;
        private readonly PlayerId _owner;
        private readonly PlayerSlot _slot;
        private float _telemetryIn;
        private readonly List<IIncomeProvider> _incomeProviders = new();

        private float _commonIncome;
        private float _replicatedIncome;
        private (float Facilities, float StationResearchBonus, float FacilityResearchBonus) _incomeBreakdown;
        private (float Facilities, float StationResearchBonus, float FacilityResearchBonus) _replicatedBreakdown;
        // Income is paid only while the battle runs, never during loading.
        private bool _isRunning;
        private bool _followsHost;

        public float Income => DEFAULT_INCOME * _research.StationIncomeMultiplier;
        public float TotalIncome => _followsHost ? _replicatedIncome : _commonIncome;
        public (float Facilities, float StationResearchBonus, float FacilityResearchBonus) IncomeBreakdown =>
            _followsHost ? _replicatedBreakdown : _incomeBreakdown;

        public EconomyService(
            EconomyModel model,
            EconomyData data,
            INotifier<BattleState> battleState,
            IPlayerRegistry playerRegistry,
            PlayerSlot owner,
            IFactionResearchModelObserver research)
        {
            _model = model;
            _data = data;
            _battleState = battleState;
            _playerRegistry = playerRegistry;
            _research = research;
            _slot = owner;
            _owner = owner.Id;
        }

        public void Initialize()
        {
            AddProvider(this);
            _research.OnResearchCompleted += HandleResearchCompleted;
            _battleState.AddObserver(this);
            // Registered so allied mining facilities can pay into this economy too.
            _playerRegistry.RegisterEconomy(_owner, this);
        }

        public void LateDispose()
        {
            _research.OnResearchCompleted -= HandleResearchCompleted;
            _battleState.RemoveObserver(this);
            _playerRegistry.UnregisterEconomy(_owner);
        }

        public void UpdateState(BattleState state)
        {
            _isRunning = state == BattleState.Running;
        }

        /// <summary>Online client: income is paid on the host; credits are replicated.</summary>
        public void FollowHost() => _followsHost = true;

        public void ApplyReplicatedIncome(float income, float facilities, float stationResearchBonus, float facilityResearchBonus)
        {
            _replicatedIncome = income;
            _replicatedBreakdown = (facilities, stationResearchBonus, facilityResearchBonus);
        }

        public void Tick()
        {
            if (_isRunning && !_followsHost && _model.AdvanceIncomeTimer(Time.deltaTime, _data.IncomeDelay))
            {
                _model.AddMoney(_commonIncome);
            }
            if (!_isRunning || _followsHost) return;
            _telemetryIn -= Time.deltaTime;
            if (_telemetryIn > 0f) return;
            _telemetryIn = TELEMETRY_SECONDS;
            RecordEconomy();
        }

        private void RecordEconomy()
        {
            var sources = new SortedDictionary<string, (int Count, float Income)>();
            foreach (IIncomeProvider provider in _incomeProviders)
            {
                string name = provider.GetType().Name;
                sources.TryGetValue(name, out var entry);
                sources[name] = (entry.Count + 1, entry.Income + provider.Income);
            }
            var text = new System.Text.StringBuilder(System.FormattableString.Invariant(
                $"money={_model.Money:F0};income={_commonIncome:F1};sources="));
            foreach (KeyValuePair<string, (int Count, float Income)> source in sources)
                text.Append(System.FormattableString.Invariant($"{source.Key}x{source.Value.Count}:{source.Value.Income:F1} "));
            EmpireAtWar.Services.Enemy.EnemyAiTelemetry.RecordEvent(_owner, _slot.Faction, "economy", text.ToString());
        }

        public bool TrySpend(UnitRequest unitRequest)
        {
            return _model.TrySpend(unitRequest.FactionData.Price);
        }

        public void Refund(UnitRequest unitRequest)
        {
            _model.Refund(unitRequest.FactionData.Price);
        }

        public void AddMoney(float amount)
        {
            _model.AddMoney(amount);
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

        private void HandleResearchCompleted(ResearchType researchType) => CalculateBaseIncome();

        private void CalculateBaseIncome()
        {
            _commonIncome = 0;
            _incomeBreakdown = default;
            float stationMultiplier = _research.StationIncomeMultiplier;
            foreach (IIncomeProvider provider in _incomeProviders)
            {
                float income = provider.Income;
                _commonIncome += income;
                if (provider is TeamIncomeSource facility)
                {
                    _incomeBreakdown.Facilities += income;
                    _incomeBreakdown.FacilityResearchBonus += income - facility.BaseIncomeShare;
                }
                else if (stationMultiplier != 1f && (provider == this || provider is FactionService or EnemyFactionController))
                {
                    _incomeBreakdown.StationResearchBonus += income - income / stationMultiplier;
                }
            }
        }
    }
}
