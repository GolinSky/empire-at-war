using System.Collections.Generic;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.CinematicCamera.Model;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.Input;
using EmpireAtWar.Ui.Base;
using EmpireAtWar.Utils;
using UnityEngine;
using Zenject;
using Random = System.Random;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Map;

namespace EmpireAtWar.Entities.CinematicCamera.Controller
{
    public class CinematicCameraPresenter : UiController, ICinematicCameraController, IInitializable, ILateTickable,
        ILateDisposable, IObserver<BattleState>
    {
        private const long NO_TARGET = -1;

        private readonly ICameraService _cameraService;
        private readonly IInputLock _inputLock;
        private readonly IPointerInput _pointerInput;
        private readonly IEntityLocator _entityLocator;
        private readonly INotifier<BattleState> _battleState;
        private System.IDisposable _inputLockHandle;

        private readonly CinematicCameraModel _model;
        private readonly CinematicCameraData _settings;
        private readonly IMapModelObserver _mapModel;
        private readonly CameraData _cameraData;
        private readonly CinematicActivityTracker _activityTracker = new();
        private readonly CinematicInterestScorer _scorer;
        private readonly CinematicShotSequencer _sequencer;
        private readonly List<CinematicCandidate> _candidates = new();

        private Vector3 _savedPosition;
        private Quaternion _savedRotation;
        private CinematicShot _shot;
        private Vector3 _anchorPosition;
        private Quaternion _anchorRotation;
        private Vector3 _focusOffset;

        private float _shotDuration;
        private float _shotElapsed;
        private float _sampleTimer;
        private float _framingDistance;

        private long _targetId = NO_TARGET;

        private int _enterFrame;

        private bool _isTargetLost;
        private bool _isCutPending;
        private bool _isExitRequested;
        private bool _isBattleEnded;

        public CinematicCameraPresenter(
            ICameraService cameraService,
            IInputLock inputLock,
            IPointerInput pointerInput,
            IUiService uiService,
            IUiCancelRouter cancelRouter,
            IEntityLocator entityLocator,
            INotifier<BattleState> battleState,
            IPlayerRoster playerRoster,
            CinematicCameraModel model,
            CinematicCameraData cinematicCameraData,
            IMapModelObserver mapModel,
            CameraData cameraData) : base(uiService, cancelRouter)
        {
            _model = model;
            _settings = cinematicCameraData;
            _mapModel = mapModel;
            _cameraData = cameraData;
            _cameraService = cameraService;
            _inputLock = inputLock;
            _pointerInput = pointerInput;
            _entityLocator = entityLocator;
            _battleState = battleState;

            Random random = new Random();
            _scorer = new CinematicInterestScorer(settings: _settings, random: random, relations: playerRoster);
            _sequencer = new CinematicShotSequencer(random, _settings.MinShotDuration, _settings.MaxShotDuration);
        }

        public void Initialize()
        {
            _battleState.AddObserver(this);
        }

        public void LateDispose()
        {
            _battleState.RemoveObserver(this);
            if (_model.IsActive)
            {
                Unsubscribe();
            }
        }

        public void Enter()
        {
            if (_model.IsActive || _isBattleEnded)
            {
                return;
            }

            _savedPosition = _cameraService.CameraPosition;
            _savedRotation = _cameraService.CameraTransform.rotation;
            _inputLockHandle = _inputLock.Acquire();
            UiService.SetHudVisible(false);
            Focus();
            _pointerInput.PrimaryReleased += OnPointerReleased;
            _model.SetActive(true);
            _enterFrame = Time.frameCount;

            _activityTracker.Clear();
            SampleActivity();
            _targetId = NO_TARGET;
            StartShot(_sequencer.First());
        }

        public void UpdateState(BattleState state)
        {
            _isBattleEnded = state == BattleState.Ended;
        }

        public void LateTick()
        {
            if (!_model.IsActive)
            {
                return;
            }

            if (_isExitRequested || _isBattleEnded)
            {
                Exit();
                return;
            }

            float deltaTime = Time.unscaledDeltaTime;
            _sampleTimer -= deltaTime;
            if (_sampleTimer <= 0f)
            {
                SampleActivity();
            }

            _shotElapsed += deltaTime;
            UpdateAnchor();
            if (_shotElapsed >= _shotDuration)
            {
                StartShot(_sequencer.Next(_shot.Type));
            }

            if (_targetId == NO_TARGET)
            {
                return;
            }

            Pose desired = CinematicShotSolver.Solve(
                _shot,
                _anchorPosition,
                _anchorRotation,
                _focusOffset,
                _framingDistance,
                _settings.WideShotDistanceMultiplier,
                _cameraService.FieldOfView,
                Mathf.Clamp01(_shotElapsed / _shotDuration));

            Vector3 lookPoint = _shot.Type == CinematicShotType.Wide
                ? _anchorPosition + _focusOffset
                : _anchorPosition;
            Vector3 min = new Vector3(_mapModel.SizeRange.Min.x, _cameraData.ZoomRange.Min, _mapModel.SizeRange.Min.y);
            Vector3 max = new Vector3(_mapModel.SizeRange.Max.x, _cameraData.ZoomRange.Max, _mapModel.SizeRange.Max.y);
            desired = CinematicShotSolver.ConstrainPose(desired, lookPoint, min, max, _cameraData.ZoomRange.Max);

            if (_isCutPending)
            {
                _isCutPending = false;
                _cameraService.SetPose(desired.position, desired.rotation);
                return;
            }

            float blend = 1f - Mathf.Exp(-_settings.FollowSharpness * deltaTime);
            Pose blended = CinematicShotSolver.InterpolatePose(
                _cameraService.CameraPosition, desired, lookPoint, blend);
            blended = CinematicShotSolver.ConstrainPose(blended, lookPoint, min, max, _cameraData.ZoomRange.Max);
            _cameraService.SetPose(blended.position, blended.rotation);
        }

        private void StartShot(CinematicShot shot)
        {
            _shot = shot;
            _shotDuration = shot.Duration;
            _shotElapsed = 0f;
            CollectCandidates();

            if (!_scorer.TrySelect(_candidates, _targetId, out CinematicSelection selection))
            {
                _targetId = NO_TARGET;
                return;
            }

            // Cut when switching subjects; glide when re-framing the same subject.
            _isCutPending = selection.Target.Id != _targetId;
            _targetId = selection.Target.Id;
            _isTargetLost = false;
            _framingDistance = _settings.GetClassProfile(selection.Target.ShipClass).FramingDistance;
            _focusOffset = selection.FocusOffset.ToUnity();
            UpdateAnchor();
        }

        private void UpdateAnchor()
        {
            if (_targetId == NO_TARGET || _isTargetLost)
            {
                return;
            }

            if (!_entityLocator.TryGetEntity(_targetId, out IEntity entity) || entity.HealthModel.IsDestroyed)
            {
                // Linger briefly on the wreck before moving on.
                _isTargetLost = true;
                _shotDuration = Mathf.Min(_shotDuration, _shotElapsed + _settings.DestroyedTargetLinger);
                return;
            }

            Transform target = entity.GetFacade<IEntityTransformFacade>().Transform;
            _anchorPosition = target.position;
            _anchorRotation = target.rotation;
            if (entity.TryGetFacade<IMoveFacade>(out IMoveFacade movement))
            {
                _framingDistance = CinematicShotSolver.CalculateFramingDistance(
                    _settings.GetClassProfile(entity.HealthModel.ShipClass).FramingDistance,
                    movement.NavigationRadius, _cameraService.FieldOfView);
            }
        }

        private void CollectCandidates()
        {
            _candidates.Clear();
            float time = Time.unscaledTime;
            foreach (IEntity entity in _entityLocator.Entities)
            {
                if (entity.HealthModel.IsDestroyed)
                {
                    continue;
                }

                Vector3 position = entity.GetFacade<IEntityTransformFacade>().Transform.position;
                if (entity.IsHiddenByFog())
                {
                    continue;
                }

                _candidates.Add(new CinematicCandidate(
                    id: entity.Id,
                    position: position.ToNumerics(),
                    shipClass: entity.HealthModel.ShipClass,
                    owner: entity.Owner,
                    secondsSinceDamaged: _activityTracker.GetSecondsSinceDamaged(entity.Id, time)));
            }
        }

        private void SampleActivity()
        {
            _sampleTimer = _settings.ActivitySampleInterval;
            float time = Time.unscaledTime;
            foreach (IEntity entity in _entityLocator.Entities)
            {
                if (!entity.HealthModel.IsDestroyed)
                {
                    _activityTracker.Sample(
                        entity.Id,
                        entity.HealthModel.Hull + entity.HealthModel.Shields,
                        time);
                }
            }
        }

        private void OnPointerReleased(Vector2 _)
        {
            // The release of the click that opened the mode must not close it.
            if (Time.frameCount != _enterFrame)
            {
                RequestExit();
            }
        }

        protected override bool HandleCancel()
        {
            RequestExit();
            return true;
        }

        private void RequestExit()
        {
            _isExitRequested = true;
        }

        private void Exit()
        {
            Unsubscribe();
            _isExitRequested = false;
            _targetId = NO_TARGET;
            _cameraService.SetPose(_savedPosition, _savedRotation);
            UiService.SetHudVisible(true);
            _inputLockHandle.Dispose();
            _model.SetActive(false);
        }

        private void Unsubscribe()
        {
            Unfocus();
            _pointerInput.PrimaryReleased -= OnPointerReleased;
        }
    }
}
