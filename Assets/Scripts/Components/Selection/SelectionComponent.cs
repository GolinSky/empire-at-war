using System;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Selection;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace EmpireAtWar.Components.Ship.Selection
{
    public interface ISelectionPositionProvider
    {
        Vector3 WorldPosition { get; }
    }

    public interface ISelectionComponent : IComponent
    {
        Vector3 WorldPosition { get; }

        void SetActive(bool isActive);
    }

    public class SelectionComponent : MonoComponent<SelectionModel>, ISelectionComponent,
        IInitializable, ILateDisposable
    {
        [SerializeField] private Canvas selectedCanvas;
        [SerializeField] private Image selectedImage;
        private SharedSelectionData _sharedSelectionData;

        [SerializeField] private SelectionType selectionType;

        private bool _canBeSelected = true;

        [Inject] private PlayerId Owner { get; }
        [Inject] private ILocalPlayer LocalPlayer { get; }
        public Vector3 WorldPosition => selectedCanvas.transform.position;

        [Inject]
        private void Construct(SelectionModel model, SharedSelectionData sharedSelectionData)
        {
            _sharedSelectionData = sharedSelectionData;
            SetModel(model);
        }

        public void Initialize()
        {
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
            // Only the local player's own units show the selection ring.
            if (!LocalPlayer.IsLocal(Owner))
            {
                return;
            }

            Model.IsSelected = isActive;
        }

        private void HandleSelection(bool isActive)
        {
            selectedCanvas.enabled = isActive;
        }
    }
}
