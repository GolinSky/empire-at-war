using System;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Selection;
using EmpireAtWar.Services.Battle;
using UnityEngine;
using VContainer;
using EmpireAtWar.Lifecycle;

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
        IInitializable, ILateDisposable, IRespawnable, EmpireAtWar.IObserver<ISelectionSubject>
    {
        private const float MULTISELECT_SCALE = 0.6f;

        // A sprite renderer, not a world canvas: a selected unit moves every frame, and a canvas under it
        // would rebuild its batch every frame (one per selected unit).
        [Tooltip("Flat anchor of the ring; the health overlay is placed from its position.")]
        [SerializeField] private Transform selectionRoot;
        [SerializeField] private SpriteRenderer selectedRenderer;
        [Tooltip("Ring width and height in the ring's local units.")]
        [SerializeField] private Vector2 ringSize;
        private SharedSelectionData _sharedSelectionData;
        private ISelectionService _selectionService;
        private Vector3 _singleSelectionScale;

        [SerializeField] private SelectionType selectionType;

        private bool _canBeSelected = true;

        private PlayerId Owner { get; set; }
        private ILocalPlayer LocalPlayer { get; set; }
        public Vector3 WorldPosition => selectionRoot.position;

        [Inject]
        private void Construct(SelectionModel model, SharedSelectionData sharedSelectionData,
            PlayerId owner, ILocalPlayer localPlayer, ISelectionService selectionService)
        {
            Owner = owner;
            LocalPlayer = localPlayer;
            _sharedSelectionData = sharedSelectionData;
            _selectionService = selectionService;
            SetModel(model);
        }

        public void Initialize()
        {
            Model.OnSelected += HandleSelection;
            Sprite sprite = _sharedSelectionData.SelectionSprite;
            selectedRenderer.sprite = sprite;
            selectedRenderer.sharedMaterial = _sharedSelectionData.SelectionMaterial;
            Vector3 spriteSize = sprite.bounds.size;
            _singleSelectionScale =
                new Vector3(ringSize.x / spriteSize.x, ringSize.y / spriteSize.y, 1f);
            UpdateMarkerScale(_selectionService.PlayerSelectionContext.Count);
            _selectionService.AddObserver(this);
        }

        public void LateDispose()
        {
            Release();
        }

        public void Respawn()
        {
            Model.IsSelected = false;
            _canBeSelected = true;
            Initialize();
        }

        public override void Release()
        {
            OnSkipSelection(selectionType);
            Model.OnSelected -= HandleSelection;
            _selectionService.RemoveObserver(this);
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
            selectedRenderer.enabled = isActive;
        }

        public void UpdateState(ISelectionSubject subject)
        {
            UpdateMarkerScale(subject.PlayerSelectionContext.Count);
        }

        private void UpdateMarkerScale(int selectedCount)
        {
            float scale = selectedCount > 1 ? MULTISELECT_SCALE : 1f;
            selectedRenderer.transform.localScale = new Vector3(
                _singleSelectionScale.x * scale, _singleSelectionScale.y * scale, 1f);
        }
    }
}
