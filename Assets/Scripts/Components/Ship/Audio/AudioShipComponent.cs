using EmpireAtWar.Services.Audio;
using EmpireAtWar.Services.TimerPoolWrapperService;
using EmpireAtWar.Mvc;
using Utilities.ScriptUtils.Time;
using Zenject;

namespace EmpireAtWar.Components.Ship.Audio
{
    public interface IAudioShipComponent : ICommand
    {
        void PlayHyperSpace(float hyperSpaceDuration);
        void HandleEnemyDetected();
    }

    public class AudioShipComponent : BaseComponent<AudioShipModel>, IAudioShipComponent, IInitializable,
        ILateDisposable
    {
        private const float HYPER_SPACE_TIME_PERCENTAGE = 0.8f;
        
        
        private readonly ITimerPoolWrapperService _timerPoolWrapperService;
        private readonly IAudioService _audioService;
        private readonly ITimer _alarmTimer;
        
        public AudioShipComponent(IModel model, ITimerPoolWrapperService timerPoolWrapperService, IAudioService audioService) : base(model)
        {
            _timerPoolWrapperService = timerPoolWrapperService;
            _audioService = audioService;
            _alarmTimer = TimerFactory.ConstructTimer(Model.AlarmDelay.Random);
        }

        public void Initialize()
        {
        }

        public void LateDispose()
        {
        }
        
        public void HandleEnemyDetected()
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
        
        public void PlayHyperSpace(float hyperSpaceDuration)
        {
            _timerPoolWrapperService.Invoke(() => { Model.PlayHyperSpace(); },
                hyperSpaceDuration * HYPER_SPACE_TIME_PERCENTAGE);
        }
    }
}
