using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityCommands;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.InputService;
using EmpireAtWar.Services.Layer;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Ui.Base;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Entities.UnitOrderFeedback
{
    public sealed class UnitOrderFeedbackUiController : IUnitOrderFeedbackPresenter,
        IInitializable, ILateTickable, ILateDisposable, IObserver<ISelectionSubject>
    {
        private readonly IUiService _uiService;
        private readonly ICameraService _cameraService;
        private readonly IEntityLocator _entityLocator;
        private readonly IInputService _inputService;
        private readonly ISelectionService _selectionService;
        private readonly ISelectionQuery _selectionQuery;
        private readonly ILayerService _layerService;
        private readonly ShipAbilityService _abilityService;
        private IUnitOrderFeedbackUi _ui;
        private IEntity _attackTarget;
        private ISelectionContext _selection;

        public UnitOrderFeedbackUiController(IUiService uiService, ICameraService cameraService,
            IEntityLocator entityLocator, IInputService inputService, ISelectionService selectionService,
            ISelectionQuery selectionQuery, ILayerService layerService, ShipAbilityService abilityService)
        {
            _uiService = uiService;
            _cameraService = cameraService;
            _entityLocator = entityLocator;
            _inputService = inputService;
            _selectionService = selectionService;
            _selectionQuery = selectionQuery;
            _layerService = layerService;
            _abilityService = abilityService;
        }

        public void Initialize()
        {
            _ui = (IUnitOrderFeedbackUi)_uiService.CreateUi(
                UiType.UnitOrderFeedback, _uiService.DefaultCanvasTransform);
            _ui.SetPresenter(this);
            _ui.Initialize();
            _inputService.OnInput += HandleInput;
            _selectionService.AddObserver(this);
            _entityLocator.EntityRemoved += HandleEntityRemoved;
        }

        public void LateTick()
        {
            if (_attackTarget != null)
            {
                if (_attackTarget.HealthModel.IsDestroyed)
                {
                    _ui.StopAttack();
                    _attackTarget = null;
                }
                else
                {
                    _ui.SetAttackPosition(_cameraService.WorldToScreenPoint(
                        _attackTarget.HealthModel.Transform.position));
                }
            }

        }

        public void AttackFeedbackCompleted() => _attackTarget = null;

        public void LateDispose()
        {
            _inputService.OnInput -= HandleInput;
            _selectionService.RemoveObserver(this);
            _entityLocator.EntityRemoved -= HandleEntityRemoved;
            _ui.Dispose();
            _attackTarget = null;
        }

        public void UpdateState(ISelectionSubject subject)
        {
            _selection = subject.PlayerSelectionContext;
            // StationCombatPresenter already responds to enemy-selection changes.
            if (subject.UpdatedType == PlayerType.Opponent &&
                subject.EnemySelectionContext.HasSelectable && HasSelectedStationaryWeapon())
            {
                PlayAttack(subject.EnemySelectionContext.Entity);
            }
        }

        private void HandleInput(InputType inputType, TouchPhase touchPhase, Vector2 screenPosition)
        {
            if (inputType != InputType.ShipInput || _abilityService.IsWaitingForTarget) return;

            if (_selectionQuery.TryFindAt(screenPosition, out SelectionEntry target))
            {
                if (target.Entity.PlayerType == PlayerType.Opponent && HasSelectedCommand<IAttackCommand>())
                    PlayAttack(target.Entity);
                return;
            }

            if (!HasSelectedCommand<IMoveCommand>()) return;
            RaycastHit hit = _cameraService.ScreenPointToRay(screenPosition);
            if (hit.collider != null && _layerService.IsInLayer(hit.collider.gameObject, LayerKey.Obstacle)) return;
            _ui.PlayMovement(screenPosition);
        }

        private void PlayAttack(IEntity target)
        {
            if (target.HealthModel.IsDestroyed || !target.HealthModel.HasUnits) return;
            _attackTarget = target;
            _ui.PlayAttack(_cameraService.WorldToScreenPoint(target.HealthModel.Transform.position));
        }

        private bool HasSelectedCommand<TCommand>() where TCommand : IEntityCommand
        {
            if (_selection == null) return false;
            foreach (IEntity entity in _selection.Entities)
            {
                if (!entity.HealthModel.IsDestroyed && entity.TryGetCommand(out TCommand _)) return true;
            }
            return false;
        }

        private bool HasSelectedStationaryWeapon()
        {
            if (_selection == null) return false;
            foreach (IEntity entity in _selection.Entities)
            {
                if (entity.HealthModel.IsDestroyed || entity.TryGetCommand(out IMoveCommand _)) continue;
                foreach (HardPointModel hardPoint in entity.HealthModel.HardPointModels)
                {
                    if (hardPoint.HardPointType == HardPointType.Weapon && !hardPoint.IsDestroyed) return true;
                }
            }
            return false;
        }

        private void HandleEntityRemoved(IEntity entity)
        {
            if (entity != _attackTarget) return;
            _ui.StopAttack();
            _attackTarget = null;
        }
    }
}
