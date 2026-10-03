using EmpireAtWar.Controllers.Economy;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Services.Stations;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.Reinforcement;
using EmpireAtWar.Services.Selection;
using EmpireAtWar.Mvc;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Services.Factions
{
    public interface IFactionService
    {
        void ChangeSelection();
        void CloseSelection();
        void TryPurchaseUnit(UnitRequest unitRequest);
        void CancelBuilding(string id);
    }

    public class FactionService : Service, IFactionService, IInitializable, ILateDisposable,
        IIncomeProvider, IObserver<ISelectionSubject>, ITickable
    {
        private const float DEFAULT_INCOME = 5f;

        private readonly ISelectionService _selectionService;
        private readonly PlayerSlot _owner;
        private readonly IWallet _wallet;
        private readonly IReinforcementPool _reinforcementPool;
        private readonly IEconomyProvider _economyProvider;
        private readonly IStationRegistry _stationRegistry;
        private readonly PlayerFactionModel _model;
        private readonly FactionResearchModel _research;
        private readonly SuperWeaponModel _superWeapons;
        private ISelectionContext _selectionContext;
        private bool _isInitialized;
        
        public float Income { get; private set; }

        public FactionService(
            PlayerFactionModel model,
            FactionResearchModel research,
            SuperWeaponModel superWeapons,
            ISelectionService selectionService,
            IWallet wallet,
            IReinforcementPool reinforcementPool,
            IEconomyProvider economyProvider,
            IStationRegistry stationRegistry,
            PlayerSlot owner)
        {
            _owner = owner;
            _model = model;
            _research = research;
            _superWeapons = superWeapons;
            Income = DEFAULT_INCOME;
            _selectionService = selectionService;
            _wallet = wallet;
            _reinforcementPool = reinforcementPool;
            _economyProvider = economyProvider;
            _stationRegistry = stationRegistry;
        }

        public void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            _selectionService.AddObserver(this);
            _economyProvider.AddProvider(this);
            _model.OnUnitCompleted += BuildUnit;
            _isInitialized = true;
        }

        public void LateDispose()
        {
            if (!_isInitialized)
            {
                return;
            }

            _selectionService.RemoveObserver(this);
            _economyProvider.RemoveProvider(this);
            _model.OnUnitCompleted -= BuildUnit;
            _isInitialized = false;
        }

        public void ChangeSelection()
        {
            _model.SelectionType = _model.SelectionType == SelectionType.Base ? SelectionType.None : SelectionType.Base;
        }

        public void CloseSelection()
        {
            if(_selectionContext != null)
            {
                _selectionService.RemoveSelectable(_selectionContext);
            }
        }

        private void BuildUnit(UnitRequest unitRequest)
        {
            if (!_stationRegistry.IsStationOperational(_owner.Id))
            {
                if (unitRequest is MiningFacilityUnitRequest ||
                    unitRequest is DefendPlatformUnitRequest)
                {
                    _model.ReleaseStructure(unitRequest);
                }
                if (unitRequest is SuperWeaponUnitRequest revertedSuperWeapon)
                {
                    _superWeapons.CancelCharging(revertedSuperWeapon.Key);
                }
                _wallet.Refund(unitRequest);
                return;
            }

            switch (unitRequest)
            {
                case LevelUnitRequest levelUnitRequest:
                    _model.CurrentLevel++;
                    Income = DEFAULT_INCOME * _model.CurrentLevel;
                    _economyProvider.RecalculateIncome(this);
                    return;
                case ResearchUnitRequest researchUnitRequest:
                    _research.Complete(researchUnitRequest.Key);
                    return;
                case SuperWeaponUnitRequest superWeaponUnitRequest:
                    _superWeapons.CompleteCharging(superWeaponUnitRequest.Key);
                    return;
            }

            _reinforcementPool.Add(unitRequest);
        }

        public void TryPurchaseUnit(UnitRequest unitRequest)
        {
            if (!_stationRegistry.IsStationOperational(_owner.Id) ||
                !_model.CanQueueUnit(unitRequest) ||
                unitRequest is SuperWeaponUnitRequest superWeapon && !_superWeapons.CanPurchase(superWeapon.Key))
            {
                return;
            }

            if (!_wallet.TrySpend(unitRequest))
            {
                return;
            }

            _model.QueueUnit(unitRequest);
            if (unitRequest is SuperWeaponUnitRequest purchasedSuperWeapon)
            {
                _superWeapons.StartCharging(purchasedSuperWeapon.Key);
            }
        }

        public void CancelBuilding(string id)
        {
            if (_model.TryCancelCurrentUnit(id, out UnitRequest unitRequest))
            {
                if (unitRequest is SuperWeaponUnitRequest superWeapon)
                {
                    _superWeapons.CancelCharging(superWeapon.Key);
                }
                _wallet.Refund(unitRequest);
            }
        }

        public void Tick()
        {
            if (_isInitialized)
            {
                _model.Advance(Time.deltaTime);
            }
        }

        public void UpdateState(ISelectionSubject selectionSubject)
        {
            if (selectionSubject.UpdatedScope == SelectionScope.Local)
            {
                _selectionContext = selectionSubject.PlayerSelectionContext;
                _model.SelectionType = _selectionContext.SelectionType;// move it to selection component and reuse it 
            }
        }
    }
}
