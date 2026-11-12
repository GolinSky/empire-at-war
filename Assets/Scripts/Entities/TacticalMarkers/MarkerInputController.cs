using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.CinematicCamera.Model;
using EmpireAtWar.Entities.MiniMap;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.CaptureSites;
using EmpireAtWar.Services.Input;
using EmpireAtWar.Services.PlayerCommands;
using EmpireAtWar.Services.TacticalMarkers;
using EmpireAtWar.Services.Vision;
using EmpireAtWar.Ui.Base;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EmpireAtWar.Entities.TacticalMarkers
{
    public sealed class MarkerInputController : UiController, IInitializable, ITickable, ILateTickable, ILateDisposable
    {
        private const float HOLD_SECONDS = 0.2f;
        private readonly InputAction _place;
        private readonly InputAction _cancelCommand;
        private readonly IPointerInput _pointer;
        private readonly IInputLock _inputLock;
        private readonly ICinematicCameraModelObserver _cinematic;
        private readonly IMiniMapInputObserver _miniMap;
        private readonly IUiHitTest _hitTest;
        private readonly ICameraService _camera;
        private readonly ISelectionQuery _selection;
        private readonly IReadOnlyList<ICaptureSiteDirectory> _sites;
        private readonly ILocalPlayer _local;
        private readonly IPlayerCommandSink _commands;
        private readonly MarkerRadialModel _model;
        private readonly TacticalMarkerSettings _settings;
        private readonly MarkerWorldPresenter _world;
        private readonly IEntityLocator _entities;
        private readonly IVisionService _vision;
        private readonly TacticalMarkerTargets _targets;
        private MarkerRadialUi _ui;
        private bool _held;
        private bool _closePending;
        private float _pressedAt;
        private Vector3 _point;
        private long _target;

        public MarkerInputController(IUiService ui, IUiCancelRouter cancel, InputActionsProvider input,
            IPointerInput pointer, IInputLock inputLock, ICinematicCameraModelObserver cinematic,
            IMiniMapInputObserver miniMap, IUiHitTest hitTest, ICameraService camera, ISelectionQuery selection,
            IReadOnlyList<ICaptureSiteDirectory> sites, ILocalPlayer local, IPlayerCommandSink commands,
            MarkerRadialModel model, TacticalMarkerSettings settings, MarkerWorldPresenter world,
            IEntityLocator entities, IVisionService vision, TacticalMarkerTargets targets) : base(ui, cancel)
        {
            _place = input.Actions.asset.FindAction("Battle/PlaceMarker", true);
            _cancelCommand = input.Actions.Battle.Command;
            _pointer = pointer; _inputLock = inputLock; _cinematic = cinematic; _miniMap = miniMap;
            _hitTest = hitTest; _camera = camera; _selection = selection; _sites = sites;
            _local = local; _commands = commands; _model = model; _settings = settings; _world = world;
            _entities = entities; _vision = vision; _targets = targets;
        }

        public void Initialize()
        {
            _ui = CreateUi<MarkerRadialUi>();
            _ui.Initialize(_model, _settings);
            _ui.Show();
            _world.Initialize(_ui.WorldMarkerPrefab, _ui.AudioSource);
        }

        public void Tick()
        {
            if (_closePending) return;
            if (_inputLock.IsLocked || _cinematic.IsActive) { Cancel(); return; }
            if (!_held && _place.WasPressedThisFrame() && _pointer.HasPosition && TryPick(_pointer.Position))
            {
                _held = true; _pressedAt = Time.unscaledTime;
                Focus();
            }
            if (!_held) return;
            if (_cancelCommand.WasPressedThisFrame()) { Cancel(); return; }
            if (!_model.IsOpen && _place.IsPressed() && Time.unscaledTime - _pressedAt >= HOLD_SECONDS)
            {
                if (!TryPick(_pointer.Position)) { Cancel(); return; }
                _ui.SetOrigin(_pointer.Position, _place.GetBindingDisplayString());
                Vector2 start = _ui.GetPointerOffset(_pointer.Position);
                _model.Open(start.x, start.y);
            }
            if (_model.IsOpen)
            {
                Vector2 offset = _ui.GetPointerOffset(_pointer.Position);
                _model.Select(offset.x, offset.y);
            }
            if (_place.IsPressed()) return;
            int index = _model.IsOpen ? _model.SelectedIndex : 0;
            Cancel();
            if (index >= 0) _commands.Submit(new PlaceMarkerCommand(_local.Id, (TacticalMarkerType)index, _point, _target));
        }

        private bool TryPick(Vector2 screen)
        {
            _target = -1;
            if (_miniMap.TryGetWorldPoint(screen, out _point)) { SnapMiniMap(screen); return true; }
            if (_hitTest.IsOverUi(screen)) return false;
            if (_selection.TryFindAt(screen, out SelectionEntry selected))
            {
                _point = selected.Entity.GetFacade<IEntityTransformFacade>().Transform.position;
                _target = selected.Entity.Id;
                return true;
            }
            Ray ray = _camera.SceneCamera.ScreenPointToRay(screen);
            foreach (ICaptureSiteDirectory directory in _sites)
            {
                ICaptureSite site = CaptureSitePointerQuery.Find(directory.Sites, ray);
                if (site == null) continue;
                _point = site.Center; _target = _targets.GetSiteId(site);
                return true;
            }
            _point = _camera.GetWorldPoint(screen, Vector3.zero);
            return true;
        }

        private void SnapMiniMap(Vector2 screen)
        {
            float nearest = 12f * 12f;
            foreach (IEntity entity in _entities.Entities)
            {
                if (entity.HealthModel.IsDestroyed || !entity.TryGetFacade(out IEntityTransformFacade view)) continue;
                Vector3 point = view.Transform.position;
                if (!_vision.IsVisible(_local.Id, point)) continue;
                float distance = (_miniMap.WorldToScreenPoint(point) - screen).sqrMagnitude;
                if (distance >= nearest) continue;
                nearest = distance; _point = point; _target = entity.Id;
            }
            foreach (ICaptureSiteDirectory directory in _sites)
            foreach (ICaptureSite site in directory.Sites)
            {
                if (!site.IsRevealed || !_vision.IsVisible(_local.Id, site.Center)) continue;
                float distance = (_miniMap.WorldToScreenPoint(site.Center) - screen).sqrMagnitude;
                if (distance >= nearest) continue;
                nearest = distance; _point = site.Center; _target = _targets.GetSiteId(site);
            }
        }

        protected override bool HandleCancel() { Cancel(); return true; }
        public void Cancel()
        {
            _held = false;
            // Keep the raycast blocker through this frame's pointer processing so RMB cancel cannot
            // become a new world command when PointerGestures observes the same press.
            _closePending = _model.IsOpen;
            Unfocus();
        }

        public void LateTick()
        {
            if (!_closePending) return;
            _model.Close();
            _closePending = false;
        }

        public void LateDispose() { Cancel(); LateTick(); _world.LateDispose(); if (_ui != null) _ui.Dispose(); }
    }
}
