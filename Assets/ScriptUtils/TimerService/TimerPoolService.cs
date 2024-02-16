using System;
using System.Collections.Generic;

namespace Utils.TimerService
{
    public class TimerPoolService
    {
        private List<CustomCoroutine> customCoroutines = new List<CustomCoroutine>();

        private Queue<CustomCoroutine> pool = new Queue<CustomCoroutine>();

        public void Invoke(Action action, float delay)
        {
            customCoroutines.Add(Construct(action, delay));
        }

        public void Update()
        {
            for (var i = 0; i < customCoroutines.Count; i++)
            {
                customCoroutines[i].Update();
            }
        }

        private CustomCoroutine Construct(Action action, float delay)
        {
            if (pool.Count != 0)
            {
                CustomCoroutine customCoroutine = pool.Dequeue();
                customCoroutine.Init(action, delay);
                return customCoroutine;
            }

            CustomCoroutine newCustomCoroutine = new CustomCoroutine(action, delay);
            newCustomCoroutine.OnFinished += UpdatePool; 
            return newCustomCoroutine;
        }

        private void UpdatePool(CustomCoroutine customCoroutine)
        {
            customCoroutines.Remove(customCoroutine);
            pool.Enqueue(customCoroutine);
        }
    }
}