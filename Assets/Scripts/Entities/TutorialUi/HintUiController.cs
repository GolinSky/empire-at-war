using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.MiniMap;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.Selection;
using EmpireAtWar.Services.Stations;
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
        private readonly ISelectionService _selection;
        private readonly IStationRegistry _stations;
        private readonly ILocalPlayer _player;
        private readonly IMiniMapTargetMarker _miniMap;
        private readonly Vector3[] _corners = new Vector3[4];
        private HintUi _ui;
        private bool _isStationSelectionPrompt;
        private bool _isPreviewShown;

        public HintUiController(IUiService uiService, IHintService hints, ISettingsService settings,
            ICameraService camera, InputActionsProvider input, ISelectionService selection,
            IStationRegistry stations, ILocalPlayer player, IInputBindings bindings,
            IMiniMapTargetMarker miniMap) : base(uiService)
        {
            _miniMap = miniMap;
            _hints = hints;
            _settings = settings;
            _camera = camera;
            _input = input;
            _bindings = bindings;
            _selection = selection;
            _stations = stations;
            _player = player;
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
            // Build-panel hints point at the station until it is selected, because the panel only fills then.
            bool markStation = hint.NeedsStationSelected &&
                               _selection.PlayerSelectionContext.SelectionType != SelectionType.Base;
            if (markStation != _isStationSelectionPrompt) SetInputPrompts(hint, markStation);
            RectTransform anchor = null;
            bool hasAnchor = !markStation && TutorialAnchor.TryFind(hint.Anchor, out anchor) &&
                             anchor.gameObject.activeInHierarchy;
            _ui.SetAnchorFrame(hasAnchor, hasAnchor ? ScreenRect(anchor) : default);
            if (hint.PreviewAnchor && hasAnchor && !_isPreviewShown)
            {
                _ui.SetPreview(anchor);
                _isPreviewShown = true;
            }
            bool hasTarget = markStation ? TryGetStation(out Vector3 position) : _hints.TryGetWorldTarget(out position);
            _ui.SetWorldMarker(hasTarget, hasTarget ? _camera.SceneCamera.WorldToScreenPoint(position) : default);
            if (hasTarget) _miniMap.ShowTarget(position);
            else _miniMap.HideTarget();
        }

        private bool TryGetStation(out Vector3 position)
        {
            position = default;
            if (!_stations.TryGetLivingStation(_player.Id, out IEntity station)) return false;
            position = station.GetFacade<IEntityTransformFacade>().Transform.position;
            return true;
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
            _ui.OpenHint(hint.Title, Body(hint), progress);
            _ui.SetDemo(hint.Demo);
            _ui.SetPreview(null);
            _isPreviewShown = false;
            SetInputPrompts(hint, hint.NeedsStationSelected &&
                _selection.PlayerSelectionContext.SelectionType != SelectionType.Base);
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
            _ui.SetBody(Body(hint));
            SetInputPrompts(hint, hint.NeedsStationSelected &&
                _selection.PlayerSelectionContext.SelectionType != SelectionType.Base);
        }

        private void SetInputPrompts(HintDefinition hint, bool selectStation)
        {
            _isStationSelectionPrompt = selectStation;
            _ui.SetInputs(selectStation
                ? HintInputFormatter.CreateActionPrompts(_input.Actions.Battle.Select)
                : HintInputFormatter.CreatePrompts(hint, _input.Actions.asset));
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
            _bindings.BindingsChanged -= RefreshInputs;
            if (!ReferenceEquals(_ui, null)) _ui.Dispose();
        }
    }
}
