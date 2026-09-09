using System;
using EmpireAtWar.Mvc;
using Zenject;

namespace EmpireAtWar.Components.Ship.Audio
{
    public class AudioShipModel : PureModel, IAudioShipModelObserver
    {
        public enum OneShot
        {
            HyperSpace,
            Alarm
        }

        private readonly AudioShipData _data;

        public event Action<OneShot> OnOneShotRequested;

        public float AlarmDelay => _data.AlarmDelay.Random;

        [Inject]
        public AudioShipModel(AudioShipData data)
        {
            _data = data;
        }

        public void PlayHyperSpace()
        {
            RequestOneShot(OneShot.HyperSpace);
        }

        public void PlayAlarm()
        {
            RequestOneShot(OneShot.Alarm);
        }

        private void RequestOneShot(OneShot oneShot)
        {
            OnOneShotRequested?.Invoke(oneShot);
        }
    }
}
