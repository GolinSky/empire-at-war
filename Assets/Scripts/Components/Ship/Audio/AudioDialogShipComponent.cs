using EmpireAtWar.Components.Radar;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Models.Audio;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Movement;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.Services.Audio;
using LightWeightFramework.Model;
using UnityEngine;
using UnityEngine.Rendering;
using Utilities.ScriptUtils.Time;
using Zenject;
using AudioType = EmpireAtWar.Services.Audio.AudioType;

namespace EmpireAtWar.Components.Audio
{
    public class AudioDialogShipComponent: BaseComponent<AudioShipDialogModel>, IInitializable,
        ILateDisposable
    {
        private const float MIN_ALARM_DELAY = 30;
        private const float MAX_ALARM_DELAY = 60f;
        
        private readonly IAudioService _audioService;
        private readonly PlayerType _playerType;
        private readonly IShipMoveModelObserver _shipMoveModelObserver;
        private readonly ISelectionModelObserver _selectionModelObserver;
        private readonly IRadarModelObserver _radarModelObserver;
        private readonly ITimer _alarmRadarTimer;
        private bool _isSelected;

        public AudioDialogShipComponent(IModel model, IAudioService audioService, PlayerType playerType) : base(model)
        {
            _audioService = audioService;
            _playerType = playerType;
            _shipMoveModelObserver = model.GetModelObserver<IShipMoveModelObserver>();
            _selectionModelObserver = model.GetModelObserver<ISelectionModelObserver>();
            _radarModelObserver = model.GetModelObserver<IRadarModelObserver>();
            _alarmRadarTimer = TimerFactory.ConstructTimer(Random.Range(MIN_ALARM_DELAY, MAX_ALARM_DELAY));
            _alarmRadarTimer.StartTimer();
        }
        
        public void Initialize()
        {
            _selectionModelObserver.OnSelected += PlaySelectionClip;
            _shipMoveModelObserver.OnLookAt += PlayAttackClip;
            _shipMoveModelObserver.OnTargetPositionChanged += PlayMoveClip;
            _shipMoveModelObserver.OnStop += PlayDamageClip;
            _radarModelObserver.Enemies.ItemAdded += PlayAlarmSights;
        }

        public void LateDispose()
        {
            _selectionModelObserver.OnSelected -= PlaySelectionClip;
            _shipMoveModelObserver.OnLookAt -= PlayAttackClip;
            _shipMoveModelObserver.OnTargetPositionChanged -= PlayMoveClip;
            _shipMoveModelObserver.OnStop -= PlayDamageClip;
            _radarModelObserver.Enemies.ItemAdded -= PlayAlarmSights;
        }

        private void PlayAlarmSights(ObservableList<IEntity> sender, ListChangedEventArgs<IEntity> listChangedEventArgs)
        {
            if (_isSelected)
            {
                if (_alarmRadarTimer.IsComplete)
                {
                    _alarmRadarTimer.StartTimer();
                    Play(Model.GetAlarmSightsClip(_playerType));
                }
            }
        }

        private void PlayDamageClip()
        {
            if (_isSelected)
            {
                Play(Model.GetDamageClip(_playerType));
            }
        }

        private void PlayMoveClip(Vector3 obj)
        {
            if (_isSelected)
            {
                Play(Model.GetMoveClip(_playerType));
            }
        }
        
        private void PlayAttackClip(Vector3 vector3)
        {
            if (_isSelected)
            {
                Play(Model.GetAttackClip(_playerType));
            }
        }
        
        private void PlaySelectionClip(bool isSelected)
        {
            _isSelected = isSelected;
            if (isSelected)
            {
                Play(Model.GetDialogClip(_playerType));
            }
        }

        private void Play(AudioClip audioClip)
        {
            _audioService.PlayOneShot(audioClip, AudioType.Dialog);
        }
    }
}