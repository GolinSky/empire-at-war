using EmpireAtWar.Services.Audio;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Weapon;
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
        private TimerPoolService _timerPoolService;
        private IAudioService _audioService;
        private ITimer _alarmTimer;
        private IWeaponFireEvents _weaponFireEvents;
        private WeaponAudioPresenter _weaponAudio;
        
        [Inject]
        private void Construct(
            AudioShipModel model,
            AudioShipData data,
            TimerPoolService timerPoolService,
            IAudioService audioService,
            IWeaponFireEvents weaponFireEvents,
            WeaponAudioPresenter weaponAudio)
        {
            SetModel(model);
            _data = data;
            _timerPoolService = timerPoolService;
            _audioService = audioService;
            _weaponFireEvents = weaponFireEvents;
            _weaponAudio = weaponAudio;
            _alarmTimer = TimerFactory.ConstructTimer(Model.AlarmDelay);
        }

        public void Initialize()
        {
            Model.OnOneShotRequested += PlayOneShot;
            _weaponFireEvents.ShotEmitted += PlayWeaponShot;
        }

        public void LateDispose()
        {
            Release();
        }

        public override void Release()
        {
            Model.OnOneShotRequested -= PlayOneShot;
            _weaponFireEvents.ShotEmitted -= PlayWeaponShot;
            _weaponAudio.Release(this);
        }

        private void PlayWeaponShot(WeaponProfile profile, Transform muzzle)
        {
            if (!isActiveAndEnabled) return;
            _weaponAudio.PlayShot(this, profile, muzzle);
        }

        private void OnDisable()
        {
            if (_weaponAudio != null) _weaponAudio.Release(this);
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
            _timerPoolService.Invoke(() => { Model.PlayHyperSpace(); },
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
    }
}
