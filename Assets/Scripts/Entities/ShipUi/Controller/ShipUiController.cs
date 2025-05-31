using EmpireAtWar.Commands.ShipUi;
using EmpireAtWar.Entities.BaseEntity.EntityCommands;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.ShipUi;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.InputService;
using EmpireAtWar.Services.NavigationService;
using EmpireAtWar.Ship;
using EmpireAtWar.Ui.Base;
using LightWeightFramework.Controller;
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
        private readonly ITimer _startTimer;

        private ISelectionContext _playerSelectionContext;

        public ShipUiController(
            ShipUiModel model,
            ISelectionService selectionService,
            IUiService uiService,
            IInputService inputService) : base(model)
        {
            _selectionService = selectionService;
            _uiService = uiService;
            _inputService = inputService;
            _startTimer = TimerFactory.ConstructTimer(START_DELAY);
        }

        public void Initialize()
        {
            _selectionService.AddObserver(this);
            _uiService.CreateUi(UiType.Ship);
            _inputService.OnInput += HandleInput;
            _inputService.OnSwipe += CloseMoveToPositionUi;
            _inputService.OnZoom += CloseMoveToPositionUi;
        }

        public void LateDispose()
        {
            _selectionService.RemoveObserver(this);
            _inputService.OnInput -= HandleInput;
            _inputService.OnSwipe -= CloseMoveToPositionUi;
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
            // _playerSelectionContext.Selectable?.Movable?.MoveToPosition(Model.TapPosition);
            if (_playerSelectionContext.Entity.TryGetCommand(out IMoveCommand moveCommand))
            {
                moveCommand.MoveTo(Model.TapPosition);
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
                        IShipModelObserver shipModelObserver = _playerSelectionContext.Entity.Model
                            .GetModelObserver<IShipModelObserver>();
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