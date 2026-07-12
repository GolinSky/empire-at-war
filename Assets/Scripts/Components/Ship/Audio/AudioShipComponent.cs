using EmpireAtWar.Services.Audio;
using EmpireAtWar.Services.TimerPoolWrapperService;
using EmpireAtWar.Mvc;
using UnityEngine;
using Utilities.ScriptUtils.Time;
using Zenject;

namespace EmpireAtWar.Components.Ship.Audio
{
    public interface IAudioShipComponent : IComponent, ICommand
    {
        void PlayHyperSpace(float hyperSpaceDuration);
        void HandleEnemyDetected();
    }

    public class AudioShipComponent : MonoComponent<AudioShipModel>, IAudioShipComponent, IInitializable,
        ILateDisposable
    {
        private const float HYPER_SPACE_TIME_PERCENTAGE = 0.8f;

        [SerializeField] private AudioSource source;

        private ITimerPoolWrapperService _timerPoolWrapperService;
        private IAudioService _audioService;
        private ITimer _alarmTimer;
        
        [Inject]
        private void Construct(
            IModel model,
            ITimerPoolWrapperService timerPoolWrapperService,
            IAudioService audioService)
        {
            SetModel(model.GetModel<AudioShipModel>());
            _timerPoolWrapperService = timerPoolWrapperService;
            _audioService = audioService;
            _alarmTimer = TimerFactory.ConstructTimer(Model.AlarmDelay.Random);
        }

        public void Initialize()
        {
            Model.OnOneShotPlayed += PlayOneShot;
            PlayLoop(Model.AmbientClip);
        }

        public void LateDispose()
        {
            Release();
        }

        public override void Release()
        {
            Model.OnOneShotPlayed -= PlayOneShot;
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

        private void PlayOneShot(AudioClip clip)
        {
            source.PlayOneShot(clip);
        }

        private void PlayLoop(AudioClip clip)
        {
            source.Stop();
            source.clip = clip;
            source.loop = true;
            source.Play(0);
        }
    }
}
