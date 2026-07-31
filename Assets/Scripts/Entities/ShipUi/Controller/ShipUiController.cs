using System.Collections.Generic;
using EmpireAtWar.Components.Movement.Formation;
using EmpireAtWar.Commands.ShipUi;
using EmpireAtWar.Entities.BaseEntity.EntityCommands;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.ShipUi;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.InputService;
using EmpireAtWar.Services.Layer;
using EmpireAtWar.Services.NavigationService;
using EmpireAtWar.Services.UiRouting;
using EmpireAtWar.Ship;
using EmpireAtWar.Ui.Base;
using EmpireAtWar.Mvc;
using ShipUiView = EmpireAtWar.Views.ShipUi;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Controllers.ShipUi
{
    public class ShipUiController : Controller<ShipUiModel>, IInitializable,
        ILateDisposable, IShipUiCommand, IObserver<ISelectionSubject>,
        ISkirmishUiRoute
    {
        private readonly ISelectionService _selectionService;
        private readonly IUiService _uiService;
        private readonly IInputService _inputService;
        private readonly ICameraService _cameraService;
        private readonly ILayerService _layerService;
        private readonly ISelectionQuery _selectionQuery;
        private readonly ISkirmishRouteNavigation _routeNavigation;
        private readonly List<IMoveCommand> _moveCommands = new List<IMoveCommand>();
        private readonly List<FormationPoint> _formationPositions = new List<FormationPoint>();
        private readonly List<float> _formationRadii = new List<float>();
        private readonly List<FormationPoint> _formationDestinations =
            new List<FormationPoint>();

        private ISelectionContext _playerSelectionContext;
        private ShipUiView _shipUi;

        public ShipUiController(
            ShipUiModel model,
            ISelectionService selectionService,
            IUiService uiService,
            IInputService inputService,
            ICameraService cameraService,
            ILayerService layerService,
            ISelectionQuery selectionQuery,
            ISkirmishRouteNavigation routeNavigation) : base(model)
        {
            _selectionService = selectionService;
            _uiService = uiService;
            _inputService = inputService;
            _cameraService = cameraService;
            _layerService = layerService;
            _selectionQuery = selectionQuery ??
                throw new System.ArgumentNullException(nameof(selectionQuery));
            _routeNavigation = routeNavigation ??
                throw new System.ArgumentNullException(nameof(routeNavigation));
        }

        public void Initialize()
        {
            _selectionService.AddObserver(this);
            _inputService.OnInput += HandleInput;
            _routeNavigation.RegisterRoute(
                SkirmishUiRoutePosition.Content,
                this);
        }

        public void LateDispose()
        {
            _selectionService.RemoveObserver(this);
            _inputService.OnInput -= HandleInput;
            _routeNavigation.UnregisterRoute(
                SkirmishUiRoutePosition.Content,
                this);
        }

        public void Activate(bool isActive, Transform parentTransform)
        {
            if (_shipUi == null)
            {
                BaseUi ui = _uiService.CreateUi(UiType.Ship, parentTransform);
                _shipUi = ui as ShipUiView
                    ?? throw new System.InvalidOperationException(
                        "The ship prefab does not contain ShipUi.");
            }
            else
            {
                _shipUi.SetParent(parentTransform);
            }

            if (isActive)
            {
                _shipUi.Show();
            }
            else
            {
                _shipUi.Hide();
            }
        }

        private void HandleInput(InputType inputType, TouchPhase touchPhase, Vector2 touchPosition)
        {
            if (inputType == InputType.ShipInput &&
                HasMovableSelection() &&
                !IsMapObstacleTap(touchPosition) &&
                !_selectionQuery.TryFindAt(touchPosition, out SelectionEntry _))
            {
                MoveToPosition(touchPosition);
            }
        }

        public void CloseSelection()
        {
            if (_playerSelectionContext != null)
            {
                _selectionService.RemoveSelectable(_playerSelectionContext);
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
                   _layerService.IsInLayer(
                       hit.collider.gameObject,
                       LayerKey.Obstacle);
        }

        public void UpdateState(ISelectionSubject subject)
        {
            switch (subject.UpdatedType)
            {
                case PlayerType.Player:
                    _playerSelectionContext = subject.PlayerSelectionContext;
                    if (_playerSelectionContext.SelectionType == SelectionType.Ship)
                    {
                        IShipModelObserver shipModelObserver = _playerSelectionContext.Entity.Model as IShipModelObserver;
                        if (shipModelObserver != null)
                        {
                            Model.ShipIcon = Model.GetShipIcon(shipModelObserver.ShipType);
                        }
                    }

                    Model.UpdateSelection(HasMovableSelection());
                    break;
                case PlayerType.Opponent:
                    break;
                case PlayerType.None:
                    break;

            }
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
