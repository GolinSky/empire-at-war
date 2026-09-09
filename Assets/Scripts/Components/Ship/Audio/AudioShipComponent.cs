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

        private AudioShipData _data;
        private ITimerPoolWrapperService _timerPoolWrapperService;
        private IAudioService _audioService;
        private ITimer _alarmTimer;
        
        [Inject]
        private void Construct(
            AudioShipModel model,
            AudioShipData data,
            ITimerPoolWrapperService timerPoolWrapperService,
            IAudioService audioService)
        {
            SetModel(model);
            _data = data;
            _timerPoolWrapperService = timerPoolWrapperService;
            _audioService = audioService;
            _alarmTimer = TimerFactory.ConstructTimer(Model.AlarmDelay);
        }

        public void Initialize()
        {
            Model.OnOneShotRequested += PlayOneShot;
            PlayLoop(_data.GetAmbientClip());
        }

        public void LateDispose()
        {
            Release();
        }

        public override void Release()
        {
            Model.OnOneShotRequested -= PlayOneShot;
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

        private void PlayOneShot(AudioShipModel.OneShot oneShot)
        {
            switch (oneShot)
            {
                case AudioShipModel.OneShot.HyperSpace:
                    PlayOneShot(_data.GetHyperSpaceClip());
                    break;
                case AudioShipModel.OneShot.Alarm:
                    PlayOneShot(_data.GetAlarmClip());
                    break;
            }
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
