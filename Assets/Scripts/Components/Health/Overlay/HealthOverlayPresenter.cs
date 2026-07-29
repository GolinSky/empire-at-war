using System;
using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityCommands;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.InputService;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Components.Ship.Health.Overlay
{
    public sealed class HealthOverlayPresenter : IInitializable, ILateDisposable, ITickable,
        IObserver<ISelectionSubject>
    {
        private readonly IHealthOverlayView _view;
        private readonly ISelectionService _selectionService;
        private readonly ISelectionSubject _selectionSubject;
        private readonly ISelectionQuery _selectionQuery;
        private readonly IInputService _inputService;
        private readonly ICameraService _cameraService;

        private IEntity _target;
        private ISelectionPositionProvider _targetPositionProvider;

        public HealthOverlayPresenter(
            IHealthOverlayView view,
            ISelectionService selectionService,
            ISelectionSubject selectionSubject,
            ISelectionQuery selectionQuery,
            IInputService inputService,
            ICameraService cameraService)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _selectionService = selectionService ??
                throw new ArgumentNullException(nameof(selectionService));
            _selectionSubject = selectionSubject ??
                throw new ArgumentNullException(nameof(selectionSubject));
            _selectionQuery = selectionQuery ??
                throw new ArgumentNullException(nameof(selectionQuery));
            _inputService = inputService ?? throw new ArgumentNullException(nameof(inputService));
            _cameraService = cameraService ?? throw new ArgumentNullException(nameof(cameraService));
        }

        public void Initialize()
        {
            _selectionService.AddObserver(this);
            SetTarget(GetDesiredTarget());
        }

        public void LateDispose()
        {
            _selectionService.RemoveObserver(this);
            SetTarget(null);
        }

        public void UpdateState(ISelectionSubject value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            SetTarget(GetDesiredTarget());
        }

        public void Tick()
        {
            IEntity desiredTarget = GetDesiredTarget();
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

        private IEntity GetDesiredTarget()
        {
            return GetHoveredEntity() ?? GetSelectedEntity();
        }

        private IEntity GetSelectedEntity()
        {
            ISelectionContext latestContext = GetContext(_selectionSubject.UpdatedType);
            if (IsValid(latestContext?.Entity))
            {
                return latestContext.Entity;
            }

            if (IsValid(_selectionSubject.PlayerSelectionContext.Entity))
            {
                return _selectionSubject.PlayerSelectionContext.Entity;
            }

            return IsValid(_selectionSubject.EnemySelectionContext.Entity)
                ? _selectionSubject.EnemySelectionContext.Entity
                : null;
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

        private ISelectionContext GetContext(PlayerType playerType)
        {
            switch (playerType)
            {
                case PlayerType.Player:
                    return _selectionSubject.PlayerSelectionContext;
                case PlayerType.Opponent:
                    return _selectionSubject.EnemySelectionContext;
                default:
                    return null;
            }
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
                _view.Hide();
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
