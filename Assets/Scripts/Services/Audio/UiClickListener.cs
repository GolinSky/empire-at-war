using System.Collections.Generic;
using EmpireAtWar.Components.Ui;
using EmpireAtWar.Lifecycle;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace EmpireAtWar.Services.Audio
{
    public sealed class UiClickListener : IInitializable, ILateTickable, ILateDisposable
    {
        private readonly IUiSfxService _sfx;
        private readonly HashSet<Vector2Control> _pointers = new HashSet<Vector2Control>();
        private readonly HashSet<Button> _buttons = new HashSet<Button>();
        private readonly List<RaycastResult> _hits = new List<RaycastResult>();
        private EventSystem _eventSystem;
        private PointerEventData _pointerEvent;
        private InputAction _click;
        private InputAction _submit;
        private bool _submitPending;

        public UiClickListener(IUiSfxService sfx) => _sfx = sfx;

        public void Initialize()
        {
            InputSystem.onBeforeUpdate += BindActions;
            InputSystem.onAfterUpdate += ObserveTargets;
        }

        private void BindActions()
        {
            EventSystem current = EventSystem.current;
            var module = current != null ? current.currentInputModule as InputSystemUIInputModule : null;
            InputAction click = module != null ? module.leftClick?.action : null;
            InputAction submit = module != null ? module.submit?.action : null;
            if (_eventSystem == current && _click == click && _submit == submit) return;

            UnbindActions();
            _eventSystem = current;
            _pointerEvent = current != null ? new PointerEventData(current) : null;
            _click = click;
            _submit = submit;
            if (_click != null) _click.performed += HandlePointer;
            if (_submit != null) _submit.performed += HandleSubmit;
        }

        private void HandlePointer(InputAction.CallbackContext context)
        {
            if (context.control.parent is TouchControl touch)
                _pointers.Add(touch.position);
            else if (context.control.device is Pointer pointer)
                _pointers.Add(pointer.position);
        }

        private void HandleSubmit(InputAction.CallbackContext context) => _submitPending = true;

        private void ObserveTargets()
        {
            foreach (Vector2Control pointer in _pointers)
            {
                _pointerEvent.position = pointer.ReadValue();
                _eventSystem.RaycastAll(_pointerEvent, _hits);
                if (_hits.Count > 0)
                    ObserveButton(ExecuteEvents.GetEventHandler<IPointerClickHandler>(_hits[0].gameObject));
                _hits.Clear();
            }
            _pointers.Clear();

            if (_submitPending)
            {
                ObserveButton(_eventSystem.currentSelectedGameObject);
                _submitPending = false;
            }
        }

        private void ObserveButton(GameObject target)
        {
            if (target == null || !target.TryGetComponent(out Button button) ||
                target.TryGetComponent<UiSfxMute>(out _) || !_buttons.Add(button))
                return;

            // Let uGUI confirm the click, including interactability and press/release matching.
            button.onClick.AddListener(_sfx.RequestClick);
        }

        public void LateTick()
        {
            foreach (Button button in _buttons)
                if (button != null)
                    button.onClick.RemoveListener(_sfx.RequestClick);
            _buttons.Clear();
        }

        public void LateDispose()
        {
            InputSystem.onBeforeUpdate -= BindActions;
            InputSystem.onAfterUpdate -= ObserveTargets;
            UnbindActions();
            LateTick();
        }

        private void UnbindActions()
        {
            if (_click != null) _click.performed -= HandlePointer;
            if (_submit != null) _submit.performed -= HandleSubmit;
            _pointers.Clear();
            _submitPending = false;
        }
    }
}
