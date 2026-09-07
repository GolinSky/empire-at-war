using System;
using EmpireAtWar.Commands;
using EmpireAtWar.Entities.Ship.Mediator;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.NavigationService;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace EmpireAtWar.Components.Ship.Selection
{
    public interface ISelectionPositionProvider
    {
        Vector3 WorldPosition { get; }
    }

    public interface ISelectionComponent : IComponent, IUnitComponent
    {
        Vector3 WorldPosition { get; }
        void SetActive(bool isActive);
    }

    public class SelectionComponent : MonoComponent<SelectionModel>, ISelectionComponent, ISelectionCommand,
        IInitializable, ILateDisposable
    {
        [SerializeField] private SelectionType selectionType;
        [SerializeField] private Canvas selectedCanvas;
        [SerializeField] private Image selectedImage;

        private IUnitMediator _mediator;
        private bool _canBeSelected = true;
        private SharedSelectionData _sharedSelectionData;

        [Inject] private PlayerType PlayerType { get; }
        public Vector3 WorldPosition => selectedCanvas.transform.position;
        [Inject]
        private void Construct(SelectionModel model, SharedSelectionData sharedSelectionData)
        {
            _sharedSelectionData = sharedSelectionData;
            SetModel(model);
        }

        public void Initialize()
        {
            if (selectedCanvas == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(SelectionComponent)} requires an explicitly assigned selection canvas.");
            }

            if (selectedImage == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(SelectionComponent)} requires an explicitly assigned selection image.");
            }

            Model.OnSelected += HandleSelection;
            selectedImage.sprite = _sharedSelectionData.SelectionSprite;
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
