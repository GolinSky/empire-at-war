using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using VContainer.Internal;

namespace EmpireAtWar.Lifecycle
{
    public sealed class OrderedLifecycleCoordinator : global::VContainer.Unity.IInitializable,
        global::VContainer.Unity.ITickable, global::VContainer.Unity.IFixedTickable,
        global::VContainer.Unity.ILateTickable, IDisposable
    {
        private readonly IInitializable[] _initializables;
        private readonly ITickable[] _tickables;
        private readonly IFixedTickable[] _fixedTickables;
        private readonly ILateTickable[] _lateTickables;
        private readonly IOrderedDisposable[] _disposables;
        private readonly ILateDisposable[] _lateDisposables;
        private readonly LifecycleTickGate _tickGate;
        private bool _isDisposed;

        public OrderedLifecycleCoordinator(
            ContainerLocal<IReadOnlyList<IInitializable>> initializables,
            ContainerLocal<IReadOnlyList<ITickable>> tickables,
            ContainerLocal<IReadOnlyList<IFixedTickable>> fixedTickables,
            ContainerLocal<IReadOnlyList<ILateTickable>> lateTickables,
            ContainerLocal<IReadOnlyList<IOrderedDisposable>> disposables,
            ContainerLocal<IReadOnlyList<ILateDisposable>> lateDisposables,
            LifecycleTickGate tickGate)
        {
            _initializables = Order(initializables.Value, order => order.Initialize);
            _tickables = Order(tickables.Value, order => order.Tick);
            _fixedTickables = Order(fixedTickables.Value, order => order.FixedTick);
            _lateTickables = Order(lateTickables.Value, order => order.LateTick);
            _disposables = Order(disposables.Value, order => order.Dispose);
            _lateDisposables = Order(lateDisposables.Value, order => order.LateDispose);
            _tickGate = tickGate;
        }

        public void Initialize()
        {
            foreach (IInitializable initializable in _initializables)
                initializable.Initialize();
        }

        public void Tick()
        {
            if (!_tickGate.IsTicking || _isDisposed) return;
            foreach (ITickable tickable in _tickables)
                tickable.Tick();
        }

        public void LateTick()
        {
            if (!_tickGate.IsTicking || _isDisposed) return;
            foreach (ILateTickable lateTickable in _lateTickables)
                lateTickable.LateTick();
        }

        public void FixedTick()
        {
            if (!_tickGate.IsTicking || _isDisposed) return;
            foreach (IFixedTickable fixedTickable in _fixedTickables)
                fixedTickable.FixedTick();
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            for (int i = _disposables.Length - 1; i >= 0; i--)
                _disposables[i].Dispose();
            for (int i = _lateDisposables.Length - 1; i >= 0; i--)
                _lateDisposables[i].LateDispose();
        }

        private static T[] Order<T>(IReadOnlyList<T> callbacks, Func<LifecycleOrderAttribute, int> selectOrder)
        {
            return callbacks.OrderBy(callback =>
            {
                LifecycleOrderAttribute order = callback.GetType().GetCustomAttribute<LifecycleOrderAttribute>();
                return order == null ? 0 : selectOrder(order);
            }).ToArray();
        }
    }
}
