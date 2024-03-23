using EmpireAtWar.Models.Audio;
using EmpireAtWar.Models.Movement;
using EmpireAtWar.Models.Radar;
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
        private const float HyperSpaceTimePercentage = 0.8f;
        
        private readonly ITimerPoolWrapperService timerPoolWrapperService;
        private readonly IShipMoveModelObserver shipMoveModelObserver;
        private readonly IRadarModelObserver radarModelObserver;
        private readonly ITimer alarmTimer;
        
        public AudioShipComponent(IModel model, ITimerPoolWrapperService timerPoolWrapperService) : base(model)
        {
            this.timerPoolWrapperService = timerPoolWrapperService;
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
                alarmTimer.StartTimer();
                Model.PlayAlarm();
            }
        }
        
        private void PlayHyperSpaceClip()
        {
            timerPoolWrapperService.Invoke(() => { Model.PlayHyperSpace(); },
                shipMoveModelObserver.HyperSpaceSpeed * HyperSpaceTimePercentage);
        }
    }
}