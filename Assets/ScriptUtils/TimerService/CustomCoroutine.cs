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
            timer = TimerFactory.ConstructTimer(delay);
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