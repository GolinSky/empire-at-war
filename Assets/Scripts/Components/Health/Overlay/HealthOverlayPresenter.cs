using System;
using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityCommands;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.InputService;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Components.Ship.Health.Overlay
{
    public sealed class HealthOverlayPresenter : IInitializable, ILateDisposable, ITickable
    {
        private readonly IHealthOverlayView _view;
        private readonly ISelectionQuery _selectionQuery;
        private readonly IInputService _inputService;
        private readonly ICameraService _cameraService;

        private IEntity _target;
        private ISelectionPositionProvider _targetPositionProvider;

        public HealthOverlayPresenter(
            IHealthOverlayView view,
            ISelectionQuery selectionQuery,
            IInputService inputService,
            ICameraService cameraService)
        {
            _view = view;
            _selectionQuery = selectionQuery;
            _inputService = inputService;
            _cameraService = cameraService;
        }

        public void Initialize()
        {
            if (!_view.IsAvailable)
            {
                throw new InvalidOperationException(
                    "The health overlay view must be initialized before its presenter.");
            }
        }

        public void LateDispose()
        {
            SetTarget(null);
        }

        public void Tick()
        {
            if (!_view.IsAvailable)
            {
                return;
            }

            IEntity desiredTarget = GetHoveredEntity();
            if (!ReferenceEquals(_target, desiredTarget))
            {
                SetTarget(desiredTarget);
            }

            if (_target == null || _target.HealthModel.IsDestroyed)
            {
                _view.Hide();
                return;
            }

            Vector3 worldPosition = _targetPositionProvider.WorldPosition;
            Vector3 viewportPosition = _cameraService.WorldToViewportPoint(worldPosition);
            if (viewportPosition.z <= 0f ||
                viewportPosition.x < 0f ||
                viewportPosition.x > 1f ||
                viewportPosition.y < 0f ||
                viewportPosition.y > 1f)
            {
                _view.Hide();
                return;
            }

            _view.Show(_cameraService.WorldToScreenPoint(worldPosition));
        }

        private IEntity GetHoveredEntity()
        {
            if (!_inputService.SupportsHover ||
                !_selectionQuery.TryFindAt(_inputService.TouchPosition, out SelectionEntry selection))
            {
                return null;
            }

            return IsValid(selection.Entity) ? selection.Entity : null;
        }

        private void SetTarget(IEntity target)
        {
            if (ReferenceEquals(_target, target))
            {
                return;
            }

            ISelectionPositionProvider targetPositionProvider = null;
            if (target != null)
            {
                if (!target.TryGetCommand(out IEntitySelectionCommand selectionCommand) ||
                    !(selectionCommand is ISelectionPositionProvider positionProvider))
                {
                    throw new InvalidOperationException(
                        "A health overlay target requires a selection position provider.");
                }

                targetPositionProvider = positionProvider;
            }

            if (_target != null)
            {
                _target.HealthModel.OnValueChanged -= HandleHealthChanged;
                _target.HealthModel.OnDestroy -= HandleTargetDestroyed;
            }

            _target = target;
            _targetPositionProvider = targetPositionProvider;
            if (_target == null)
            {
                if (_view.IsAvailable)
                {
                    _view.Hide();
                }
                return;
            }

            _target.HealthModel.OnValueChanged += HandleHealthChanged;
            _target.HealthModel.OnDestroy += HandleTargetDestroyed;
            UpdateValues(_target.HealthModel);
        }

        private void HandleHealthChanged()
        {
            if (_target == null)
            {
                throw new InvalidOperationException("A health update requires an active target.");
            }

            if (!_view.IsAvailable)
            {
                return;
            }

            UpdateValues(_target.HealthModel);
        }

        private void HandleTargetDestroyed()
        {
            SetTarget(null);
        }

        private void UpdateValues(IHealthModelObserver model)
        {
            _view.SetValues(model.ArmorPercentage, model.ShieldPercentage);
        }

        private static bool IsValid(IEntity entity)
        {
            return entity != null && !entity.HealthModel.IsDestroyed;
        }
    }
}
