using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using UnityEngine;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Lifecycle
{
    internal sealed class CleanupFailures
    {
        private List<Exception> _exceptions;

        public void Add(Exception exception)
        {
            (_exceptions ??= new List<Exception>()).Add(exception);
        }

        public void Attempt(Action cleanup)
        {
            try { cleanup(); }
            catch (Exception exception) { Add(exception); }
        }

        public void DisposeContainer(LifetimeScope scope)
        {
            // VContainer 1.19 pops each disposable before calling it. Resume with the next
            // one on failure so all callbacks run and DisposeCore clears the container.
            do
            {
                try
                {
                    scope.DisposeCore();
                    return;
                }
                catch (Exception exception) { Add(exception); }
            } while (scope.Container != null);
        }

        public void DisposeScope(LifetimeScope scope)
        {
            DisposeContainer(scope);
            Attempt(() =>
            {
                if (Application.isPlaying) Object.Destroy(scope.gameObject);
                else Object.DestroyImmediate(scope.gameObject);
            });
        }

        public void ThrowIfAny()
        {
            if (_exceptions == null) return;
            if (_exceptions.Count == 1) ExceptionDispatchInfo.Capture(_exceptions[0]).Throw();
            throw new AggregateException(_exceptions);
        }
    }
}
