using System.Collections.Generic;
using EmpireAtWar.Components.Movement.Formation;
using EmpireAtWar.Commands.ShipUi;
using EmpireAtWar.Entities.BaseEntity.EntityCommands;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.ShipUi;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.InputService;
using EmpireAtWar.Services.NavigationService;
using EmpireAtWar.Ship;
using EmpireAtWar.Ui.Base;
using EmpireAtWar.Mvc;
using UnityEngine;
using Utilities.ScriptUtils.Time;
using Zenject;

namespace EmpireAtWar.Controllers.ShipUi
{
    public class ShipUiController: Controller<ShipUiModel>, IInitializable, ILateDisposable, IShipUiCommand, IObserver<ISelectionSubject>
    {
        private const float START_DELAY = 0.1f;
        
        private readonly ISelectionService _selectionService;
        private readonly IUiService _uiService;
        private readonly IInputService _inputService;
        private readonly ICameraService _cameraService;
        private readonly ITimer _startTimer;
        private readonly List<IMoveCommand> _moveCommands = new List<IMoveCommand>();
        private readonly List<FormationPoint> _formationPositions = new List<FormationPoint>();

        private ISelectionContext _playerSelectionContext;

        public ShipUiController(
            ShipUiModel model,
            ISelectionService selectionService,
            IUiService uiService,
            IInputService inputService,
            ICameraService cameraService) : base(model)
        {
            _selectionService = selectionService;
            _uiService = uiService;
            _inputService = inputService;
            _cameraService = cameraService;
            _startTimer = TimerFactory.ConstructTimer(START_DELAY);
        }

        public void Initialize()
        {
            _selectionService.AddObserver(this);
            _uiService.CreateUi(UiType.Ship);
            _inputService.OnInput += HandleInput;
            _inputService.OnSwipe += CloseMoveToPositionUi;
            _inputService.OnCameraMove += CloseMoveToPositionUi;
            _inputService.OnZoom += CloseMoveToPositionUi;
        }

        public void LateDispose()
        {
            _selectionService.RemoveObserver(this);
            _inputService.OnInput -= HandleInput;
            _inputService.OnSwipe -= CloseMoveToPositionUi;
            _inputService.OnCameraMove -= CloseMoveToPositionUi;
            _inputService.OnZoom -= CloseMoveToPositionUi;
        }

        private void HandleInput(InputType inputType, TouchPhase touchPhase, Vector2 touchPosition)
        {
            if (inputType == InputType.ShipInput && _playerSelectionContext is { SelectionType: SelectionType.Ship } && _startTimer.IsComplete)
            {
                Model.TapPosition = touchPosition;
                //todo: create plane map entity - use layers or add it as selectable
            }
            else
            {
                CloseMoveToPositionUi();
            }
        }

        private void CloseMoveToPositionUi()
        {
            Model.SkipGoToPositionUi();
        }
        
        private void CloseMoveToPositionUi(float obj)
        {
            CloseMoveToPositionUi();
        }

        private void CloseMoveToPositionUi(Vector2 obj)
        {
            CloseMoveToPositionUi();
        }

        public void CloseSelection()
        {
            if (_playerSelectionContext != null)
            {
                _selectionService.RemoveSelectable(_playerSelectionContext);
            }
        }

        public void MoveToPosition()
        {
            if (_playerSelectionContext == null)
            {
                return;
            }

            _moveCommands.Clear();
            _formationPositions.Clear();
            for (int i = 0; i < _playerSelectionContext.Entities.Count; i++)
            {
                if (_playerSelectionContext.Entities[i].TryGetCommand(out IMoveCommand moveCommand))
                {
                    _moveCommands.Add(moveCommand);
                    _formationPositions.Add(new FormationPoint(
                        moveCommand.WorldPosition.x,
                        moveCommand.WorldPosition.z));
                }
            }

            if (_moveCommands.Count == 0)
            {
                return;
            }

            if (_moveCommands.Count == 1)
            {
                _moveCommands[0].MoveTo(Model.TapPosition);
                return;
            }

            FormationPoint center = FormationModel.CalculateCenter(_formationPositions);
            Vector3 targetWorldPosition = _cameraService.GetWorldPoint(
                Model.TapPosition,
                _moveCommands[0].WorldPosition);
            FormationPoint targetCenter = new FormationPoint(targetWorldPosition.x, targetWorldPosition.z);

            for (int i = 0; i < _moveCommands.Count; i++)
            {
                FormationPoint destination = FormationModel.CalculateDestination(
                    _formationPositions[i],
                    center,
                    targetCenter);
                _moveCommands[i].MoveTo(new Vector3(
                    destination.X,
                    _moveCommands[i].WorldPosition.y,
                    destination.Z));
            }
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

                    Model.UpdateSelection(_playerSelectionContext.SelectionType);
                    break;
                case PlayerType.Opponent:
                    CloseMoveToPositionUi();
                    break;
                case PlayerType.None:
                    CloseMoveToPositionUi();
                    break;

            }
            _startTimer.StartTimer();

        }
    }
}
