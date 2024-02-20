using System;

namespace Utils.TimerService
{
    public class CustomCoroutine
    {
        public event Action<CustomCoroutine> OnFinished;
        private Action action;
        private ITimer timer;

        private bool isFinished;

        public CustomCoroutine(Action action, float delay)
        {
            Init(action, delay);
        }

        public void Init(Action action, float delay)
        {
            this.action = action;
            if (timer == null)
            {
                timer = TimerFactory.ConstructTimer(delay);
            }
            else
            {
                timer.ChangeDelay(delay);
            }
            timer.StartTimer();
            isFinished = false;
        }

        public void Update()
        {
            if(isFinished) return;
            if (timer.IsComplete)
            {
                action?.Invoke();
                isFinished = true;
                OnFinished?.Invoke(this);
            }
        }
    }
}