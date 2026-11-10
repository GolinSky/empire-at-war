using System.Collections.Generic;
using EmpireAtWar.Entities.MiniMap;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.Input;
using EmpireAtWar.Services.Settings;
using EmpireAtWar.Services.Tutorial;
using EmpireAtWar.Ui.Base;
using UnityEngine;

namespace EmpireAtWar.Entities.TutorialUi
{
    public sealed class HintUiController : UiController, IHintPresenter, IInitializable, ILateTickable, ILateDisposable
    {
        private readonly IHintService _hints;
        private readonly ISettingsService _settings;
        private readonly ICameraService _camera;
        private readonly InputActionsProvider _input;
        private readonly IInputBindings _bindings;
        private readonly IMiniMapTargetMarker _miniMap;
        private readonly Vector3[] _corners = new Vector3[4];
        private readonly List<RectTransform> _anchors = new List<RectTransform>();
        private HintUi _ui;
        private bool _isPreviewShown;
        private string _body;

        public HintUiController(IUiService uiService, IHintService hints, ISettingsService settings,
            ICameraService camera, InputActionsProvider input, IInputBindings bindings,
            IMiniMapTargetMarker miniMap) : base(uiService)
        {
            _miniMap = miniMap;
            _hints = hints;
            _settings = settings;
            _camera = camera;
            _input = input;
            _bindings = bindings;
        }

        public void Initialize()
        {
            _ui = CreateUi<HintUi>();
            _ui.AssignPresenter(this);
            _ui.Initialize();
            _ui.Show();
            _hints.Shown += HandleShown;
            _hints.Closed += HandleClosed;
            _bindings.BindingsChanged += RefreshInputs;
        }

        public void LateTick()
        {
            HintDefinition hint = _hints.Current;
            if (hint == null) return;
            // Progress tokens ([count]) change while the card is open.
            if (hint.Body.IndexOf('[') >= 0) RefreshBody(hint);
            bool hasAnchor = TutorialAnchor.TryFindAll(hint.Anchor, _anchors) ||
                             TutorialAnchor.TryFindAll(hint.AnchorFallback, _anchors);
            _ui.SetAnchorFrame(hasAnchor, hasAnchor ? ScreenRect(_anchors) : default);
            if (hint.PreviewAnchor && hasAnchor && !_isPreviewShown)
            {
                _ui.SetPreview(_anchors[0]);
                _isPreviewShown = true;
            }
            bool hasTarget = _hints.TryGetWorldTarget(out Vector3 position);
            _ui.SetWorldMarker(hasTarget, hasTarget ? _camera.SceneCamera.WorldToScreenPoint(position) : default);
            if (hasTarget) _miniMap.ShowTarget(position);
            else _miniMap.HideTarget();
        }

        public void Dismiss() => _hints.Dismiss();

        public void TurnOffHints()
        {
            if (_settings.SetShowHints(false) == SettingsApplyResult.SaveFailed)
                Debug.LogWarning("Could not save the show-hints setting.");
        }

        private void HandleShown(HintDefinition hint)
        {
            _body = Body(hint);
            _ui.OpenHint(hint.Title, _body, _hints.CurrentProgress);
            _ui.SetDemo(hint.Demo);
            _ui.SetPreview(null);
            _isPreviewShown = false;
            SetInputPrompts(hint);
        }

        private void HandleClosed(HintDefinition hint, bool completed)
        {
            _ui.CloseHint(completed);
            _miniMap.HideTarget();
        }

        private string Body(HintDefinition hint) =>
            _hints.ResolveText(HintInputFormatter.FormatBody(hint, _input.Actions.asset));

        // Settings edits update the open card without restarting its animation.
        private void RefreshInputs()
        {
            HintDefinition hint = _hints.Current;
            if (hint == null) return;
            _body = null;
            RefreshBody(hint);
            SetInputPrompts(hint);
        }

        private void RefreshBody(HintDefinition hint)
        {
            string body = Body(hint);
            if (body == _body) return;
            _body = body;
            _ui.SetBody(body);
        }

        private void SetInputPrompts(HintDefinition hint) =>
            _ui.SetInputs(HintInputFormatter.CreatePrompts(hint, _input.Actions.asset));

        private Rect ScreenRect(List<RectTransform> anchors)
        {
            // HUD canvases are Screen Space Overlay, so world corners are screen pixels.
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            foreach (RectTransform anchor in anchors)
            {
                anchor.GetWorldCorners(_corners);
                min = Vector2.Min(min, _corners[0]);
                max = Vector2.Max(max, _corners[2]);
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        public void LateDispose()
        {
            _hints.Shown -= HandleShown;
            _hints.Closed -= HandleClosed;
            _bindings.BindingsChanged -= RefreshInputs;
            if (!ReferenceEquals(_ui, null)) _ui.Dispose();
        }
    }
}
