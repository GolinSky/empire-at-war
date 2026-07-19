using EmpireAtWar.Controllers.Economy;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Patterns.ChainOfResponsibility;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.NavigationService;
using EmpireAtWar.Mvc;
using Zenject;

namespace EmpireAtWar.Services.Factions
{
    public interface IFactionService
    {
        void ChangeSelection();
        void CloseSelection();
        void BuildUnit(UnitRequest unitRequest);
        void TryPurchaseUnit(UnitRequest unitRequest);
        void RevertBuilding(UnitRequest unitRequest);
    }

    public class FactionService : Service, IFactionService, IInitializable, ILateDisposable,
        IBuildShipChain, IIncomeProvider, IObserver<ISelectionSubject>
    {
        private const float DEFAULT_INCOME = 5f;

        private readonly ISelectionService _selectionService;
        private readonly LazyInject<IPurchaseProcessor> _purchaseMediator;
        private readonly IEconomyProvider _economyProvider;
        private readonly PlayerFactionModel _model;
        private IChainHandler<UnitRequest> _nextChain;
        private ISelectionContext _selectionContext;
        
        public float Income { get; private set; }

        public FactionService(
            PlayerFactionModel model,
            ISelectionService selectionService,
            LazyInject<IPurchaseProcessor> purchaseMediator,
            IEconomyProvider economyProvider)
        {
            _model = model;
            Income = DEFAULT_INCOME;
            _selectionService = selectionService;
            _purchaseMediator = purchaseMediator;
            _economyProvider = economyProvider;
        }

        public void Initialize()
        {
            _purchaseMediator.Value.Add(this);
            _selectionService.AddObserver(this);
            _economyProvider.AddProvider(this);
        }

        public void LateDispose()
        {
            _selectionService.RemoveObserver(this);
            _economyProvider.RemoveProvider(this);
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

        public void BuildUnit(UnitRequest unitRequest)
        {
            _model.CompleteUnit(unitRequest);

            switch (unitRequest)
            {
                case LevelUnitRequest levelUnitRequest:
                    _model.CurrentLevel++;
                    Income = DEFAULT_INCOME * _model.CurrentLevel;
                    _economyProvider.RecalculateIncome(this);
                    return;
            }

            if (_nextChain != null)
            {
                _nextChain.Handle(unitRequest);
            }
        }

        public void TryPurchaseUnit(UnitRequest unitRequest)
        {
            if (!_model.CanQueueUnit(unitRequest))
            {
                return;
            }

            _purchaseMediator.Value.Handle(unitRequest);
        }

        public void RevertBuilding(UnitRequest unitRequest)
        {
            _purchaseMediator.Value.RevertFlow(unitRequest);
            _model.CompleteUnit(unitRequest);
        }

        public IChainHandler<UnitRequest> SetNext(IChainHandler<UnitRequest> chainHandler)
        {
            _nextChain = chainHandler;
            return _nextChain;
        }

        public void Handle(UnitRequest unitRequest)
        {
            _model.QueueUnit(unitRequest);
        }

        public void UpdateState(ISelectionSubject selectionSubject)
        {
            if (selectionSubject.UpdatedType == PlayerType.Player)
            {
                _selectionContext = selectionSubject.PlayerSelectionContext;
                _model.SelectionType = _selectionContext.SelectionType;// move it to selection component and reuse it 
            }
        }
    }
}
