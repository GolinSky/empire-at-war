using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.InputService;
using EmpireAtWar.Ui.Base;
using EmpireAtWar.ViewComponents.Selection;
using LightWeightFramework.Controller;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Entities.Interaction
{
    public class InteractionController: Controller<InteractionModel>, IInitializable, ILateDisposable
    {
        private readonly IUiService _uiService;
        private readonly IInputService _inputService;
        private readonly ICameraService _cameraService;

        public InteractionController(
            InteractionModel model,
            IUiService uiService,
            IInputService inputService,
            ICameraService cameraService) : base(model)
        {
            _uiService = uiService;
            _inputService = inputService;
            _cameraService = cameraService;
        }

        public void Initialize()
        {
            _uiService.CreateUi(UiType.Interaction);   
            _inputService.OnInput += HandleInput;
        }

        public void LateDispose()
        {
            _inputService.OnInput -= HandleInput;
        }
        
        private void HandleInput(InputType inputType, TouchPhase touchPhase, Vector2 touchPosition)
        {
            if(inputType != InputType.Selection) return;
            
            RaycastHit raycastHit = _cameraService.ScreenPointToRay(touchPosition);

            if(raycastHit.collider == null) return;
            
            ISelectableView selectableView = raycastHit.collider.GetComponent<ISelectableView>();

            if (selectableView != null)
            {
                selectableView.OnSelected();
            }
        }
    }
}