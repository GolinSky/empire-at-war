using System;
using System.Collections;
using UnityEngine;

namespace EmpireAtWar.Services.CoroutineService
{
    public class CoroutineService: MonoBehaviour
    {
        public string Id => nameof(CoroutineService);

        
        public Coroutine StartCustomCoroutine(IEnumerator enumerator)
        {
            return StartCoroutine(enumerator);
        }

        public Coroutine WaitUntil(Func<bool> condition, Action callback)
        {
            return StartCoroutine(WaitUntilCoroutine(condition, callback));
        }
      

        public Coroutine InvokeWithDelay(Action action, float delay)
        {
            return StartCoroutine(InvokeWithDelayIEnumerator(action, delay));
        }

        private IEnumerator InvokeWithDelayIEnumerator(Action action, float delay)
        {
            yield return new WaitForSeconds(delay);
            action?.Invoke();
        }
        
        private IEnumerator WaitUntilCoroutine(Func<bool> condition, Action action)
        {
            yield return new WaitUntil(condition);
            action?.Invoke();
        }
    }
}