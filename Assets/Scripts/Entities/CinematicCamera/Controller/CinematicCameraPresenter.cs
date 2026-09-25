using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.CinematicCamera.Model;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.SkirmishGame;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Ui.Base;
using EmpireAtWar.Utils;
using UnityEngine;
using ViewComponents;
using Zenject;
using InputServiceImpl = EmpireAtWar.Services.InputService.InputService;
using Random = System.Random;

namespace EmpireAtWar.Entities.CinematicCamera.Controller
{
    public class CinematicCameraPresenter : ICinematicCameraController, ILateTickable, ILateDisposable
    {
        private const long NO_TARGET = -1;

        private readonly CinematicCameraModel _model;
        private readonly CinematicCameraSettings _settings;
        private readonly ICameraService _cameraService;
        private readonly InputServiceImpl _inputService;
        private readonly IUiService _uiService;
        private readonly IEntityLocator _entityLocator;
        private readonly FogOfWarSystem _fogOfWarSystem;
        private readonly ISkirmishSessionModelObserver _sessionModel;
        private readonly CinematicActivityTracker _activityTracker = new();
        private readonly CinematicInterestScorer _scorer;
        private readonly CinematicShotSequencer _sequencer;
        private readonly List<CinematicCandidate> _candidates = new();

        private Vector3 _savedPosition;
        private Quaternion _savedRotation;
        private CinematicShot _shot;
        private float _shotDuration;
        private float _shotElapsed;
        private float _sampleTimer;
        private long _targetId = NO_TARGET;
        private float _framingDistance;
        private Vector3 _anchorPosition;
        private Quaternion _anchorRotation;
        private Vector3 _focusOffset;
        private bool _isTargetLost;
        private bool _isCutPending;
        private bool _isExitRequested;
        private int _enterFrame;

        public CinematicCameraPresenter(
            CinematicCameraModel model,
            CameraData cameraData,
            ICameraService cameraService,
            InputServiceImpl inputService,
            IUiService uiService,
            IEntityLocator entityLocator,
            FogOfWarSystem fogOfWarSystem,
            ISkirmishSessionModelObserver sessionModel)
        {
            _model = model;
            _settings = cameraData.Cinematic;
            _cameraService = cameraService;
            _inputService = inputService;
            _uiService = uiService;
            _entityLocator = entityLocator;
            _fogOfWarSystem = fogOfWarSystem;
            _sessionModel = sessionModel;

            Random random = new Random();
            _scorer = new CinematicInterestScorer(_settings, random);
            _sequencer = new CinematicShotSequencer(random, _settings.MinShotDuration, _settings.MaxShotDuration);
        }

        public void Enter()
        {
            if (_model.IsActive || _sessionModel.IsBattleEnded)
            {
                return;
            }

            _savedPosition = _cameraService.CameraPosition;
            _savedRotation = _cameraService.CameraTransform.rotation;
            _inputService.Block(true);
            _uiService.SetHudVisible(false);
            _inputService.OnEscapePressed += RequestExit;
            _inputService.OnEndDrag += OnPointerReleased;
            _model.SetActive(true);
            _enterFrame = Time.frameCount;

            _activityTracker.Clear();
            SampleActivity();
            _targetId = NO_TARGET;
            StartShot(_sequencer.First());
        }

        public void LateTick()
        {
            if (!_model.IsActive)
            {
                return;
            }

            if (_isExitRequested || _sessionModel.IsBattleEnded)
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
                Mathf.Clamp01(_shotElapsed / _shotDuration));

            if (_isCutPending)
            {
                _isCutPending = false;
                _cameraService.SetPose(desired.position, desired.rotation);
                return;
            }

            float blend = 1f - Mathf.Exp(-_settings.FollowSharpness * deltaTime);
            Transform cameraTransform = _cameraService.CameraTransform;
            _cameraService.SetPose(
                Vector3.Lerp(cameraTransform.position, desired.position, blend),
                Quaternion.Slerp(cameraTransform.rotation, desired.rotation, blend));
        }

        public void LateDispose()
        {
            if (_model.IsActive)
            {
                Unsubscribe();
            }
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

            Transform target = entity.HealthModel.Transform;
            _anchorPosition = target.position;
            _anchorRotation = target.rotation;
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

                Vector3 position = entity.HealthModel.Transform.position;
                if (entity.PlayerType != PlayerType.Player && _fogOfWarSystem.IsHidden(position))
                {
                    continue;
                }

                _candidates.Add(new CinematicCandidate(
                    entity.Id,
                    position.ToNumerics(),
                    entity.HealthModel.ShipClass,
                    entity.PlayerType,
                    _activityTracker.GetSecondsSinceDamaged(entity.Id, time)));
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
            _uiService.SetHudVisible(true);
            _inputService.Block(false);
            _model.SetActive(false);
        }

        private void Unsubscribe()
        {
            _inputService.OnEscapePressed -= RequestExit;
            _inputService.OnEndDrag -= OnPointerReleased;
        }
    }
}
