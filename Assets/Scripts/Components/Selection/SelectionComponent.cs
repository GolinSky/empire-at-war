using EmpireAtWar.Commands;
using EmpireAtWar.Entities.Ship.Mediator;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.NavigationService;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Components.Ship.Selection
{
    public interface ISelectionComponent : IComponent, IUnitComponent
    {
        void SetActive(bool isActive);
    }

    public class SelectionComponent : MonoComponent<SelectionModel>, ISelectionComponent, ISelectionCommand,
        IInitializable, ILateDisposable
    {
        [SerializeField] private SelectionType selectionType;
        [SerializeField] private Canvas selectedCanvas;

        private IUnitMediator _mediator;
        private bool _canBeSelected = true;

        [Inject] private PlayerType PlayerType { get; }
        [Inject]
        private void Construct(IModel model)
        {
            SetModel(model.GetModel<SelectionModel>());
        }

        public void Initialize()
        {
            Model.OnSelected += HandleSelection;
        }

        public void LateDispose()
        {
            Release();
        }

        public override void Release()
        {
            OnSkipSelection(selectionType);
            Model.OnSelected -= HandleSelection;
            HandleSelection(false);
            _canBeSelected = false;
        }

        public void OnSelected()
        {
            if (_canBeSelected)
            {
                OnSelected(selectionType);
            }
        }

        public void OnSelected(SelectionType type)
        {
        }

        public void OnSkipSelection(SelectionType type)
        {
        }

        public void SetActive(bool isActive)
        {
            if (PlayerType != PlayerType.Player)
            {
                return;
            }

            Model.IsSelected = isActive;
            _mediator?.OnSelect(isActive);
        }

        public void SetMediator(IUnitMediator mediator)
        {
            _mediator = mediator;
        }

        private void HandleSelection(bool isActive)
        {
            selectedCanvas.enabled = isActive;
        }
    }
}
