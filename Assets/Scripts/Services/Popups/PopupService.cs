using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.Popups;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Ui.Base;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Services.Popups
{
    [LifecycleOrder(LateDispose = 1)]
    public sealed class PopupService : IPopupService, IUiCancelHandler, IInitializable, ILateDisposable
    {
        private readonly IUiService _uiService;
        private readonly IUiPopupCancelLayer _cancelRouter;
        private readonly List<(PopupHandle Handle, IPopupController Controller, BaseUi View)> _stack = new();
        private PopupBlockerUi _blocker;
        private bool _isDisposed;

        public PopupService(IUiService uiService, IUiPopupCancelLayer cancelRouter)
        {
            _uiService = uiService;
            _cancelRouter = cancelRouter;
        }

        public void Initialize() => _cancelRouter.SetPopupLayer(this);

        public PopupHandle Open(IPopupController controller)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(PopupService));

            if (_blocker == null)
            {
                _blocker = _uiService.CreateUi<PopupBlockerUi>();
                _blocker.Hide();
            }

            var handle = new PopupHandle();
            BaseUi view = controller.Open(() => Remove(handle, dismissed: false));

            if (_stack.Count > 0)
            {
                var previous = _stack[^1];
                previous.View.SetInteraction(false);
                previous.Controller.SetCovered(true);
            }

            _stack.Add((handle, controller, view));
            PlaceBlocker();
            return handle;
        }

        public void Close(PopupHandle handle) => Remove(handle, dismissed: true);

        public bool IsOpen(PopupHandle handle) => Find(handle) >= 0;

        public bool TryCancel()
        {
            if (_stack.Count == 0)
                return false;

            _stack[^1].Controller.Cancel();
            return true;
        }

        public void LateDispose()
        {
            _isDisposed = true;
            _cancelRouter.SetPopupLayer(null);

            var failures = new CleanupFailures();
            while (_stack.Count > 0)
            {
                PopupHandle handle = _stack[^1].Handle;
                failures.Attempt(() => Close(handle));
            }

            if (_blocker != null)
            {
                DestroyView(_blocker);
                _blocker = null;
            }

            failures.ThrowIfAny();
        }

        private int Find(PopupHandle handle) => _stack.FindIndex(entry => entry.Handle == handle);

        private void Remove(PopupHandle handle, bool dismissed)
        {
            int index = Find(handle);
            if (index < 0)
                return;

            var entry = _stack[index];
            _stack.RemoveAt(index);
            try
            {
                entry.View.Hide();
                if (index == _stack.Count && !_isDisposed)
                {
                    PlaceBlocker();
                    if (_stack.Count > 0)
                    {
                        var top = _stack[^1];
                        top.View.SetInteraction(true);
                        top.Controller.SetCovered(false);
                    }
                }

                entry.Controller.Dispose(dismissed);
            }
            finally
            {
                DestroyView(entry.View);
            }
        }

        private void PlaceBlocker()
        {
            if (_stack.Count == 0)
            {
                _blocker.Hide();
                return;
            }

            _blocker.transform.SetAsLastSibling();
            _stack[^1].View.transform.SetAsLastSibling();
            _blocker.Show();
        }

        private static void DestroyView(BaseUi view)
        {
            if (Application.isPlaying)
                Object.Destroy(view.gameObject);
            else
                Object.DestroyImmediate(view.gameObject);
        }
    }
}
