using UnityEngine;

namespace Utils.TimerService
{
    public class Timer : ITimer
    {
        private float _delay;
        private float _timer;
        private bool _isComplete;
        
        private float CurrentTime => Time.time;
        public float TimeLeft => IsComplete ? default : Mathf.Abs(CurrentTime - _timer);
        public bool IsComplete => _timer < CurrentTime;

            
        public Timer(float delay)
        {
            _delay = delay;
        }
      
        public void StartTimer()
        {
            _timer = CurrentTime + _delay;
        }

        public void ChangeDelay(float newDelay)
        {
            _delay = newDelay;
        }

        public void ForceFinish()
        {
            _timer = default;
        }

        public void AppendTime(float appendDelay)
        {
            _timer += appendDelay;
        }
    }
}