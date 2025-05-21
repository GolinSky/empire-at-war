using EmpireAtWar.Components.Radar;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Models.Movement;
using EmpireAtWar.Services.Audio;
using EmpireAtWar.Services.TimerPoolWrapperService;
using LightWeightFramework.Command;
using LightWeightFramework.Model;
using UnityEngine.Rendering;
using Utilities.ScriptUtils.Time;
using Zenject;

namespace EmpireAtWar.Components.Ship.Audio
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
            // _radarModelObserver.OnHitDetected += PlayAlarm;
            _radarModelObserver.Enemies.ItemAdded += PlayAlarm;
            PlayHyperSpaceClip();
        }

        public void LateDispose()
        {
            _radarModelObserver.Enemies.ItemAdded -= PlayAlarm;

            // _radarModelObserver.OnHitDetected -= PlayAlarm;
        }
        
        private void PlayAlarm(ObservableList<IEntity> sender, ListChangedEventArgs<IEntity> listChangedEventArgs)
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