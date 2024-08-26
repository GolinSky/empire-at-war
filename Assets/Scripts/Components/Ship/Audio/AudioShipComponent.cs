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
        
        
        private readonly ITimerPoolWrapperService timerPoolWrapperService;
        private readonly IAudioService audioService;
        private readonly IShipMoveModelObserver shipMoveModelObserver;
        private readonly IRadarModelObserver radarModelObserver;
        private readonly ITimer alarmTimer;
        
        public AudioShipComponent(IModel model, ITimerPoolWrapperService timerPoolWrapperService, IAudioService audioService) : base(model)
        {
            this.timerPoolWrapperService = timerPoolWrapperService;
            this.audioService = audioService;
            shipMoveModelObserver = model.GetModelObserver<IShipMoveModelObserver>();
            radarModelObserver = model.GetModelObserver<IRadarModelObserver>();
            alarmTimer = TimerFactory.ConstructTimer(Model.AlarmDelay.Random);
        }

        public void Initialize()
        {
            radarModelObserver.OnHitDetected += PlayAlarm;
            PlayHyperSpaceClip();
        }

        public void LateDispose()
        {
            radarModelObserver.OnHitDetected -= PlayAlarm;
        }
        
        private void PlayAlarm(RaycastHit[] raycastHits)
        {
            if (alarmTimer.IsComplete)
            {
                if (audioService.CanPlayAlarm())
                {
                    alarmTimer.StartTimer();
                    Model.PlayAlarm();
                    audioService.RegisterAlarmPlaying();
                }
            }
        }
        
        private void PlayHyperSpaceClip()
        {
            timerPoolWrapperService.Invoke(() => { Model.PlayHyperSpace(); },
                shipMoveModelObserver.HyperSpaceSpeed * HYPER_SPACE_TIME_PERCENTAGE);
        }
    }
}