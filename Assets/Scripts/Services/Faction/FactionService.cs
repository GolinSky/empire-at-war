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
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Units;
using UnityEngine;
using VContainer;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.Services.Factions
{
    public interface IFactionService
    {
        bool ForceUpgradeStation();

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
        private readonly IWallet _wallet;
        private readonly IReinforcementPool _reinforcementPool;
        private readonly IEconomyProvider _economyProvider;
        private readonly IProductionAccess _productionAccess;
        private readonly IEntityLocator _entities;
        private ISelectionContext _selectionContext;

        private readonly PlayerSlot _owner;
        private readonly PlayerFactionModel _model;
        private readonly FactionResearchModel _research;
        private readonly SuperWeaponModel _superWeapons;

        private bool _isInitialized;

        public float Income => DEFAULT_INCOME * _model.CurrentLevel;

        public FactionService(
            ISelectionService selectionService,
            IWallet wallet,
            IReinforcementPool reinforcementPool,
            IEconomyProvider economyProvider,
            IProductionAccess productionAccess,
            PlayerFactionModel model,
            FactionResearchModel research,
            SuperWeaponModel superWeapons,
            PlayerSlot owner,
            IEntityLocator entities)
        {
            _owner = owner;
            _model = model;
            _research = research;
            _superWeapons = superWeapons;
            _selectionService = selectionService;
            _wallet = wallet;
            _reinforcementPool = reinforcementPool;
            _economyProvider = economyProvider;
            _productionAccess = productionAccess;
            _entities = entities;
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
            _entities.EntityAdded += HandleEntityAdded;
            _entities.EntityRemoved += HandleEntityRemoved;
            foreach (IEntity entity in _entities.Entities) HandleEntityAdded(entity);
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
            _entities.EntityAdded -= HandleEntityAdded;
            _entities.EntityRemoved -= HandleEntityRemoved;
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
            if (!_productionAccess.CanProduce)
            {
                if (unitRequest is ShipUnitRequest ship && ship.Key == ShipType.Resolute)
                    _model.SetResoluteReserved(false);
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
            if (!_productionAccess.CanProduce ||
                unitRequest is LevelUnitRequest && _productionAccess.ProductionHost != _owner.Id ||
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

        private bool IsOwnedResolute(IEntity entity) => entity.Owner == _owner.Id &&
            entity.TryGetFacade(out IUnitTypeFacade type) &&
            type.UnitTypeId == UnitTypeId.Ship(ShipType.Resolute);

        private void HandleEntityAdded(IEntity entity)
        {
            if (IsOwnedResolute(entity)) _model.SetResoluteReserved(true);
        }

        private void HandleEntityRemoved(IEntity entity)
        {
            if (IsOwnedResolute(entity) && entity.HealthModel.IsDestroyed)
                _model.SetResoluteReserved(false);
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

        public bool ForceUpgradeStation()
        {
            FactionData upgrade = _model.GetCurrentLevelFactionData();
            if (_productionAccess.ProductionHost != _owner.Id ||
                upgrade == null || !_productionAccess.CanProduce) return false;

            // Refund queued upgrades so their later completion cannot exceed the level limit.
            foreach (ProductionQueueSnapshot queued in _model.GetProductionQueueSnapshots())
            {
                if (queued.UnitRequest is LevelUnitRequest)
                {
                    for (int index = 0; index < queued.Count; index++)
                        CancelBuilding(queued.UnitRequest.Id);
                }
            }

            BuildUnit(new LevelUnitRequest(upgrade, _model.CurrentLevel));
            return true;
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
