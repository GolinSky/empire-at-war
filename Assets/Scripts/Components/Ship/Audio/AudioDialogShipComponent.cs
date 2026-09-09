using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Audio;
using EmpireAtWar.Mvc;
using UnityEngine;
using Utilities.ScriptUtils.Time;
using Zenject;
using AudioType = EmpireAtWar.Services.Audio.AudioType;

namespace EmpireAtWar.Components.Ship.Audio
{
    public interface IAudioDialogShipComponent : IComponent
    {
        void HandleEnemyDetected();
        void HandleStopped();
        void HandleMove(Vector3 position);
        void HandleAttack(Vector3 target);
        void HandleSelection(bool isSelected);
    }

    public class AudioDialogShipComponent: MonoComponent<AudioShipDialogModel>, IAudioDialogShipComponent, IInitializable,
        ILateDisposable
    {
        private const float MIN_ALARM_DELAY = 30f;
        private const float MAX_ALARM_DELAY = 60f;
        
        private AudioShipDialogData _data;
        private IAudioService _audioService;
        private PlayerType _playerType;
        private ITimer _alarmRadarTimer;
        private bool _isSelected;

        [Inject]
        private void Construct(
            [InjectOptional] AudioShipDialogModel model,
            [InjectOptional] AudioShipDialogData data,
            IAudioService audioService,
            PlayerType playerType)
        {
            if (model == null)
            {
                enabled = false;
                return;
            }

            if (data == null)
            {
                throw new System.InvalidOperationException($"{nameof(AudioShipDialogData)} is required when {nameof(AudioShipDialogModel)} is bound.");
            }

            SetModel(model);
            _data = data;
            _audioService = audioService;
            _playerType = playerType;
            _alarmRadarTimer = TimerFactory.ConstructTimer(Random.Range(MIN_ALARM_DELAY, MAX_ALARM_DELAY));
            _alarmRadarTimer.StartTimer();
        }
        
        public void Initialize()
        {
        }

        public void LateDispose()
        {
        }

        public void HandleEnemyDetected()
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

        public void HandleStopped()
        {
            if (_isSelected)
            {
                Play(Model.GetDamageClip(_playerType));
            }
        }

        public void HandleMove(Vector3 position)
        {
            if (_isSelected)
            {
                Play(Model.GetMoveClip(_playerType));
            }
        }
        
        public void HandleAttack(Vector3 target)
        {
            if (_isSelected)
            {
                Play(Model.GetAttackClip(_playerType));
            }
        }
        
        public void HandleSelection(bool isSelected)
        {
            _isSelected = isSelected;
            if (isSelected)
            {
                Play(Model.GetDialogClip(_playerType));
            }
        }

        private void Play((FactionType FactionType, AudioShipDialogData.ClipType ClipType, int Index) request)
        {
            _audioService.PlayOneShot(_data.GetClip(request), AudioType.Dialog);
        }
    }
}
