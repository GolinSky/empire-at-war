using EmpireAtWar.Lifecycle;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.Input;
using EmpireAtWar.Services.Settings;
using EmpireAtWar.Services.Tutorial;
using EmpireAtWar.Ui.Base;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EmpireAtWar.Entities.TutorialUi
{
    public sealed class HintUiController : UiController, IHintPresenter, IInitializable, ILateTickable, ILateDisposable
    {
        private readonly IHintService _hints;
        private readonly ISettingsService _settings;
        private readonly ICameraService _camera;
        private readonly InputActionsProvider _input;
        private readonly Vector3[] _corners = new Vector3[4];
        private HintUi _ui;

        public HintUiController(IUiService uiService, IHintService hints, ISettingsService settings,
            ICameraService camera, InputActionsProvider input) : base(uiService)
        {
            _hints = hints;
            _settings = settings;
            _camera = camera;
            _input = input;
        }

        public void Initialize()
        {
            _ui = CreateUi<HintUi>();
            _ui.AssignPresenter(this);
            _ui.Initialize();
            _ui.Show();
            _hints.Shown += HandleShown;
            _hints.Closed += HandleClosed;
        }

        public void LateTick()
        {
            HintDefinition hint = _hints.Current;
            if (hint == null) return;
            bool hasAnchor = TutorialAnchor.TryFind(hint.Anchor, out RectTransform anchor) &&
                             anchor.gameObject.activeInHierarchy;
            _ui.SetAnchorFrame(hasAnchor, hasAnchor ? ScreenRect(anchor) : default);
            bool hasTarget = _hints.TryGetWorldTarget(out Vector3 position);
            _ui.SetWorldMarker(hasTarget, hasTarget ? _camera.SceneCamera.WorldToScreenPoint(position) : default);
        }

        public void Dismiss() => _hints.Dismiss();

        public void TurnOffHints()
        {
            if (_settings.SetShowHints(false) == SettingsApplyResult.SaveFailed)
                Debug.LogWarning("Could not save the show-hints setting.");
        }

        private void HandleShown(HintDefinition hint)
        {
            int number = _hints.CurrentBasicNumber;
            string progress = number > 0 ? $"{number}/{_hints.BasicCount}" : string.Empty;
            _ui.OpenHint(hint.Title, FormatBody(hint), progress);
        }

        private void HandleClosed(HintDefinition hint, bool completed) => _ui.CloseHint(completed);

        // Body placeholders take the live binding text, so rebound keys show correctly.
        private string FormatBody(HintDefinition hint)
        {
            if (hint.InputActions.Count == 0) return hint.Body;
            object[] keys = new object[hint.InputActions.Count];
            for (int i = 0; i < keys.Length; i++)
            {
                InputAction action = _input.Actions.asset.FindAction(hint.InputActions[i], throwIfNotFound: true);
                keys[i] = $"<b>{KeyName(action)}</b>";
            }
            return string.Format(hint.Body, keys);
        }

        // First binding only (W/S/A/D, not every alternative); overrides apply.
        private static string KeyName(InputAction action)
        {
            InputBinding first = action.bindings[0];
            if (!first.isComposite) return BindingPathNames.Readable(first.effectivePath);
            string name = string.Empty;
            for (int i = 1; i < action.bindings.Count && action.bindings[i].isPartOfComposite; i++)
                name += (name.Length > 0 ? "/" : string.Empty) + BindingPathNames.Readable(action.bindings[i].effectivePath);
            return name;
        }

        private Rect ScreenRect(RectTransform anchor)
        {
            // HUD canvases are Screen Space Overlay, so world corners are screen pixels.
            anchor.GetWorldCorners(_corners);
            return Rect.MinMaxRect(_corners[0].x, _corners[0].y, _corners[2].x, _corners[2].y);
        }

        public void LateDispose()
        {
            _hints.Shown -= HandleShown;
            _hints.Closed -= HandleClosed;
            if (!ReferenceEquals(_ui, null)) _ui.Dispose();
        }
    }
}
