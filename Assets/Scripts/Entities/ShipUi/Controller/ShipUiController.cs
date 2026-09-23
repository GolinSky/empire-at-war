using EmpireAtWar.Components.Movement.Formation;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Entities.BaseEntity.EntityCommands;
using System.Collections.Generic;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.ShipUi;
using EmpireAtWar.Presenters.ShipUi;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.InputService;
using EmpireAtWar.Services.Layer;
using EmpireAtWar.Services.NavigationService;
using EmpireAtWar.Services.UiRouting;
using EmpireAtWar.Ship;
using EmpireAtWar.Ui.Base;
using EmpireAtWar.Views;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Controllers.ShipUi
{
    public class ShipUiController : IShipUiPresenter, IInitializable,
        ILateDisposable, ISkirmishUiRoute, EmpireAtWar.IObserver<ISelectionSubject>
    {
        private readonly IUiService _uiService;
        private readonly ISelectionService _selectionService;
        private readonly IInputService _inputService;
        private readonly ICameraService _cameraService;
        private readonly ILayerService _layerService;
        private readonly ISelectionQuery _selectionQuery;
        private readonly ShipUiModel _model;
        private readonly ShipAbilityService _abilityService;
        private readonly ISkirmishRouteNavigation _routeNavigation;
        private readonly List<IMoveCommand> _moveCommands = new List<IMoveCommand>();
        private readonly List<ShipAbilitySlot> _abilitySlots = new List<ShipAbilitySlot>();
        private readonly List<FormationPoint> _formationPositions = new List<FormationPoint>();
        private readonly List<float> _formationRadii = new List<float>();
        private readonly List<FormationPoint> _formationDestinations = new List<FormationPoint>();

        private ISelectionContext _playerSelectionContext;
        private IShipUi _shipUi;
        private IShipGroupUi _shipGroupUi;
        private bool _isRouteActive;

        public ShipUiController(
            IUiService uiService,
            ISelectionService selectionService,
            IInputService inputService,
            ICameraService cameraService,
            ILayerService layerService,
            ISelectionQuery selectionQuery,
            ShipUiModel model,
            ISkirmishRouteNavigation routeNavigation,
            ShipAbilityService abilityService)
        {
            _uiService = uiService;
            _selectionService = selectionService;
            _inputService = inputService;
            _cameraService = cameraService;
            _layerService = layerService;
            _selectionQuery = selectionQuery;
            _model = model;
            _routeNavigation = routeNavigation;
            _abilityService = abilityService;
        }

        public void Initialize()
        {
            _selectionService.AddObserver(this);
            _inputService.OnInput += HandleInput;
            _routeNavigation.RegisterRoute(SkirmishUiRoutePosition.Content, this);
            _abilityService.TargetingChanged += UpdateTargeting;
        }

        public void LateDispose()
        {
            _selectionService.RemoveObserver(this);
            _inputService.OnInput -= HandleInput;
            _routeNavigation.UnregisterRoute(SkirmishUiRoutePosition.Content, this);
            _abilityService.TargetingChanged -= UpdateTargeting;
            if (_shipUi != null)
            {
                _shipUi.Dispose();
                _shipGroupUi.Dispose();
            }
        }

        public void Activate(bool isActive, Transform parentTransform)
        {
            if (_shipUi == null)
            {
                _shipUi = (IShipUi)_uiService.CreateUi(UiType.Ship, parentTransform);
                _shipGroupUi = (IShipGroupUi)_uiService.CreateUi(UiType.ShipGroup, parentTransform);

                _shipUi.SetModel(_model);
                _shipUi.SetPresenter(this);
                _shipUi.Initialize();

                _shipGroupUi.SetModel(_model);
                _shipGroupUi.SetPresenter(this);
                _shipGroupUi.Initialize();
            }
            else
            {
                _shipUi.SetParent(parentTransform);
                _shipGroupUi.SetParent(parentTransform);
            }

            _isRouteActive = isActive;
            RefreshSelection();
        }

        public void CloseSelection()
        {
            if (_playerSelectionContext != null)
            {
                _selectionService.RemoveSelectable(_playerSelectionContext);
            }
        }

        public void SelectShipGroup(ShipType shipType)
        {
            _selectionService.SelectCurrentShipsByType(shipType);
        }

        public void PressAbility(ShipAbilityId id) =>
            _abilityService.Press(_playerSelectionContext.Entities, id);

        private void UpdateTargeting() => _model.SetPendingAbility(
            _abilityService.IsWaitingForTarget ? _abilityService.PendingAbilityId : (ShipAbilityId?)null);

        public void UpdateState(ISelectionSubject subject)
        {
            if (subject.UpdatedType != PlayerType.Player)
            {
                return;
            }

            _playerSelectionContext = subject.PlayerSelectionContext;
            _abilityService.CancelTargeting();
            UpdateSelection();
        }

        private void UpdateSelection()
        {
            bool hasShips = HasMovableSelection() &&
                            _playerSelectionContext.SelectionType == SelectionType.Ship;
            ShipType? selectedShipType = null;
            if (hasShips && _playerSelectionContext.Count == 1 &&
                _playerSelectionContext.Entity.Model is IShipModelObserver ship)
            {
                selectedShipType = ship.ShipType;
            }

            _model.UpdateSelection(hasShips, selectedShipType);
            RefreshSelection();
        }

        private void RefreshSelection()
        {
            if (_shipUi == null)
            {
                return;
            }

            bool hasGroup = _model.HasShips && _playerSelectionContext.Count > 1;

            _abilitySlots.Clear();
            if (_model.HasShips)
            {
                foreach (var entity in _playerSelectionContext.Entities)
                {
                    if (entity.HealthModel.IsDestroyed ||
                        !entity.TryGetCommand(out IShipAbilityCommand abilityCommand)) continue;
                    for (int i = 0; i < abilityCommand.Slots.Count; i++)
                        _abilitySlots.Add(abilityCommand.Slots[i]);
                }
            }
            _shipUi.SetAbilitySlots(_abilitySlots);
            _shipGroupUi.SetAbilitySlots(_abilitySlots);

            _shipGroupUi.ClearGroups();
            if (hasGroup)
            {
                SortedDictionary<ShipType, int> groupCounts = new SortedDictionary<ShipType, int>();
                foreach (var entity in _playerSelectionContext.Entities)
                {
                    if (entity.HealthModel.IsDestroyed || !(entity.Model is IShipModelObserver ship))
                    {
                        continue;
                    }

                    groupCounts.TryGetValue(ship.ShipType, out int count);
                    groupCounts[ship.ShipType] = count + 1;
                }

                foreach (KeyValuePair<ShipType, int> group in groupCounts)
                {
                    int visibleEntries = group.Value <= 4 ? group.Value : 1;
                    Sprite icon = _model.GetShipIcon(group.Key);
                    _shipGroupUi.AddGroup(group.Key, icon, group.Value, visibleEntries);
                }
            }

            if (_isRouteActive && _model.HasShips && !hasGroup)
            {
                _shipUi.Show();
                _shipGroupUi.Hide();
            }
            else if (_isRouteActive && hasGroup)
            {
                _shipUi.Hide();
                _shipGroupUi.Show();
            }
            else
            {
                _shipUi.Hide();
                _shipGroupUi.Hide();
            }
        }

        private void HandleInput(InputType inputType, TouchPhase touchPhase, Vector2 touchPosition)
        {
            if (inputType == InputType.ShipInput && _abilityService.IsWaitingForTarget &&
                !_selectionQuery.TryFindAt(touchPosition, out SelectionEntry _))
            {
                _abilityService.CancelTargeting();
                return;
            }
            if (inputType == InputType.ShipInput &&
                HasMovableSelection() &&
                !IsMapObstacleTap(touchPosition) &&
                !_selectionQuery.TryFindAt(touchPosition, out SelectionEntry _))
            {
                MoveToPosition(touchPosition);
            }
        }

        private void MoveToPosition(Vector2 touchPosition)
        {
            if (_playerSelectionContext == null)
            {
                return;
            }

            _moveCommands.Clear();
            _formationPositions.Clear();
            _formationRadii.Clear();
            for (int i = 0; i < _playerSelectionContext.Entities.Count; i++)
            {
                if (!_playerSelectionContext.Entities[i].HealthModel.IsDestroyed &&
                    _playerSelectionContext.Entities[i].TryGetCommand(out IMoveCommand moveCommand))
                {
                    _moveCommands.Add(moveCommand);
                    _formationPositions.Add(new FormationPoint(
                        moveCommand.WorldPosition.x,
                        moveCommand.WorldPosition.z));
                    _formationRadii.Add(moveCommand.NavigationRadius);
                }
            }

            if (_moveCommands.Count == 0)
            {
                return;
            }

            if (_moveCommands.Count == 1)
            {
                _moveCommands[0].MoveTo(touchPosition);
                return;
            }

            Vector3 targetWorldPosition = _cameraService.GetWorldPoint(
                touchPosition,
                _moveCommands[0].WorldPosition);
            FormationPoint targetCenter = new FormationPoint(targetWorldPosition.x, targetWorldPosition.z);
            FormationModel.CalculateCompactDestinations(
                _formationPositions,
                _formationRadii,
                targetCenter,
                _formationDestinations);

            for (int i = 0; i < _moveCommands.Count; i++)
            {
                FormationPoint destination = _formationDestinations[i];
                _moveCommands[i].MoveTo(new Vector3(
                    destination.X,
                    _moveCommands[i].WorldPosition.y,
                    destination.Z));
            }
        }

        private bool IsMapObstacleTap(Vector2 screenPosition)
        {
            RaycastHit hit = _cameraService.ScreenPointToRay(screenPosition);
            return hit.collider != null &&
                   _layerService.IsInLayer(hit.collider.gameObject, LayerKey.Obstacle);
        }

        private bool HasMovableSelection()
        {
            if (_playerSelectionContext == null)
            {
                return false;
            }

            for (int i = 0; i < _playerSelectionContext.Entities.Count; i++)
            {
                if (!_playerSelectionContext.Entities[i].HealthModel.IsDestroyed &&
                    _playerSelectionContext.Entities[i].TryGetCommand(out IMoveCommand _))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
