using EmpireAtWar.Models.Audio;
using EmpireAtWar.Models.Movement;
using EmpireAtWar.Models.Radar;
using EmpireAtWar.Services.Audio;
using EmpireAtWar.Services.TimerPoolWrapperService;
using LightWeightFramework.Command;
using LightWeightFramework.Model;
using UnityEngine;
using Utilities.ScriptUtils.Time;
using Zenject;

namespace EmpireAtWar.Components.Audio
{
    public interface IAudioShipComponent : ICommand
    {
    }

    public class AudioShipComponent : BaseComponent<AudioShipModel>, IAudioShipComponent, IInitializable,
        ILateDisposable
    {
        private const float HYPER_SPACE_TIME_PERCENTAGE = 0.8f;
        
        
        private readonly ITimerPoolWrapperService _timerPoolWrapperService;
        private readonly IAudioService _audioService;
        private readonly IShipMoveModelObserver _shipMoveModelObserver;
        private readonly IRadarModelObserver _radarModelObserver;
        private readonly ITimer _alarmTimer;
        
        public AudioShipComponent(IModel model, ITimerPoolWrapperService timerPoolWrapperService, IAudioService audioService) : base(model)
        {
            _timerPoolWrapperService = timerPoolWrapperService;
            _audioService = audioService;
            _shipMoveModelObserver = model.GetModelObserver<IShipMoveModelObserver>();
            _radarModelObserver = model.GetModelObserver<IRadarModelObserver>();
            _alarmTimer = TimerFactory.ConstructTimer(Model.AlarmDelay.Random);
        }

        public void Initialize()
        {
            _radarModelObserver.OnHitDetected += PlayAlarm;
            PlayHyperSpaceClip();
        }

        public void LateDispose()
        {
            _radarModelObserver.OnHitDetected -= PlayAlarm;
        }
        
        private void PlayAlarm(RaycastHit[] raycastHits)
        {
            if (_alarmTimer.IsComplete)
            {
                if (_audioService.CanPlayAlarm())
                {
                    _alarmTimer.StartTimer();
                    Model.PlayAlarm();
                    _audioService.RegisterAlarmPlaying();
                }
            }
        }
        
        private void PlayHyperSpaceClip()
        {
            _timerPoolWrapperService.Invoke(() => { Model.PlayHyperSpace(); },
                _shipMoveModelObserver.HyperSpaceSpeed * HYPER_SPACE_TIME_PERCENTAGE);
        }
    }
}