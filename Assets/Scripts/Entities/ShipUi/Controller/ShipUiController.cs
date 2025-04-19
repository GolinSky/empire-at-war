using EmpireAtWar.Commands.ShipUi;
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
    public class ShipUiController: Controller<ShipUiModel>, IInitializable, ILateDisposable, IShipUiCommand, IObserver<ISelectionContext>
    {
        private const float START_DELAY = 0.5f;
        
        private readonly ISelectionService _selectionService;
        private readonly IUiService _uiService;
        private readonly IInputService _inputService;
        private ISelectionContext _context;
        private ITimer _startTimer;


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
            _inputService.OnSwipe += CloseGoToPositionUi;
            _inputService.OnZoom += CloseGoToPositionUi;
        }

        public void LateDispose()
        {
            _selectionService.RemoveObserver(this);
            _inputService.OnInput -= HandleInput;
            _inputService.OnSwipe -= CloseGoToPositionUi;
            _inputService.OnZoom -= CloseGoToPositionUi;
        }

        private void HandleInput(InputType inputType, TouchPhase touchPhase, Vector2 touchPosition)
        {
            if (inputType == InputType.ShipInput && _context is { SelectionType: SelectionType.Ship } && _startTimer.IsComplete)
            {
                Model.TapPosition = touchPosition;
            }
            else
            {
                CloseGoToPositionUi();
            }
        }

        private void CloseGoToPositionUi()
        {
            Model.SkipGoToPositionUi();
        }
        
        private void CloseGoToPositionUi(float obj)
        {
            CloseGoToPositionUi();
        }

        private void CloseGoToPositionUi(Vector2 obj)
        {
            CloseGoToPositionUi();
        }

        public void CloseSelection()
        {
            _selectionService.RemoveSelectable();
        }

        public void MoveToPosition()
        {
            _context.Selectable?.Movable?.MoveToPosition(Model.TapPosition);
        }

        public void UpdateState(ISelectionContext context)
        {
            _context = context;
            if (context.SelectionType == SelectionType.Ship)
            {
                _startTimer.StartTimer();
                IShipModelObserver shipModelObserver = context.Selectable.ModelObserver.GetModelObserver<IShipModelObserver>();
                if (shipModelObserver != null)
                {
                    Model.ShipIcon = Model.GetShipIcon(shipModelObserver.ShipType);
                }
            }
            Model.UpdateSelection(context.SelectionType);
        }
    }
}