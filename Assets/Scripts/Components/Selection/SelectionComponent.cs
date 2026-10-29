using System;
using DG.Tweening;
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

        void PlayAttackFeedback(Transform target);
        void StopAttackFeedback();
    }

    public class SelectionComponent : MonoComponent<SelectionModel>, ISelectionComponent,
        IInitializable, ILateDisposable, IRespawnable, EmpireAtWar.IObserver<ISelectionSubject>
    {
        private const float SINGLE_SELECTION_SCALE = 0.8f;
        private const float MULTISELECT_SCALE = 0.6f;
        private const float ATTACK_HOLD = 0.1f;
        private const float ATTACK_LOCK = 0.55f;
        private const float ATTACK_FADE = 0.2f;
        private static readonly Quaternion _worldRotation = Quaternion.Euler(90f, 0f, 0f);

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
        private Transform _markerTransform;
        private bool _isMarkerVisible;
        private Sequence _attackSequence;
        private Transform _attackTarget;
        private Vector3 _markerLocalPosition;
        private Vector3 _attackStartScale;
        private Color _markerColor;
        private float _attackProgress;

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
            _markerTransform = selectedRenderer.transform;
            _markerTransform.rotation = _worldRotation;
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
            StopAttackFeedback();
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
            _isMarkerVisible = isActive;
            if (isActive)
            {
                _markerTransform.rotation = _worldRotation;
            }
            selectedRenderer.enabled = isActive;
        }

        private void LateUpdate()
        {
            if (_attackSequence != null)
            {
                _markerTransform.position = Vector3.Lerp(
                    _markerTransform.parent.TransformPoint(_markerLocalPosition),
                    _attackTarget.position, _attackProgress);
                _markerTransform.rotation = _worldRotation *
                    Quaternion.Euler(0f, 0f, 180f * _attackProgress);
                _markerTransform.localScale = Vector3.Lerp(
                    _attackStartScale, _attackStartScale * 0.2f, _attackProgress);
                return;
            }
            // Keep batching and renderer bounds intact; only visible, turning markers need a write.
            if (_isMarkerVisible && _markerTransform.rotation != _worldRotation)
            {
                _markerTransform.rotation = _worldRotation;
            }
        }

        public void UpdateState(ISelectionSubject subject)
        {
            UpdateMarkerScale(subject.PlayerSelectionContext.Count);
        }

        private void UpdateMarkerScale(int selectedCount)
        {
            if (_attackSequence != null) return;
            float scale = selectedCount > 1 ? MULTISELECT_SCALE : SINGLE_SELECTION_SCALE;
            _markerTransform.localScale = new Vector3(
                _singleSelectionScale.x * scale, _singleSelectionScale.y * scale, 1f);
        }

        public void PlayAttackFeedback(Transform target)
        {
            StopAttackFeedback();
            _attackTarget = target;
            _markerLocalPosition = _markerTransform.localPosition;
            _attackStartScale = _markerTransform.localScale;
            _markerColor = selectedRenderer.color;
            _attackProgress = 0f;
            _markerTransform.rotation = _worldRotation;
            selectedRenderer.color = Color.red;
            selectedRenderer.enabled = true;
            _attackSequence = DOTween.Sequence()
                .AppendInterval(ATTACK_HOLD)
                .Append(DOTween.To(() => _attackProgress, value => _attackProgress = value,
                    1f, ATTACK_LOCK).SetEase(Ease.InOutCubic))
                .AppendInterval(0.12f)
                .Append(selectedRenderer.DOFade(0f, ATTACK_FADE))
                .SetUpdate(true)
                .OnComplete(StopAttackFeedback);
        }

        public void StopAttackFeedback()
        {
            if (_attackSequence == null) return;
            _attackSequence.Kill();
            _attackSequence = null;
            _attackTarget = null;
            _markerTransform.localPosition = _markerLocalPosition;
            _markerTransform.rotation = _worldRotation;
            selectedRenderer.color = _markerColor;
            UpdateMarkerScale(_selectionService.PlayerSelectionContext.Count);
            HandleSelection(Model.IsSelected);
        }

        private void OnDisable() => StopAttackFeedback();
    }
}
