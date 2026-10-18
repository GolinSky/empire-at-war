using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Profiling;
using VContainer.Internal;

namespace EmpireAtWar.Lifecycle
{
    public sealed class OrderedLifecycleCoordinator : global::VContainer.Unity.IInitializable,
        global::VContainer.Unity.ITickable, global::VContainer.Unity.IFixedTickable,
        global::VContainer.Unity.ILateTickable, IDisposable
    {
        // Per-callback markers named like Zenject's, e.g. "EmpireAtWar.Services.Foo.Tick()".
        private static readonly Dictionary<(Type, string), ProfilerMarker> CallbackMarkers =
            new Dictionary<(Type, string), ProfilerMarker>();
        // Entity scopes rebuild coordinators on every spawn; the attribute is read once per type.
        private static readonly Dictionary<Type, LifecycleOrderAttribute> CallbackOrders =
            new Dictionary<Type, LifecycleOrderAttribute>();

        private readonly IInitializable[] _initializables;
        private readonly ITickable[] _tickables;
        private readonly IFixedTickable[] _fixedTickables;
        private readonly ILateTickable[] _lateTickables;
        private readonly ProfilerMarker[] _tickMarkers;
        private readonly ProfilerMarker[] _fixedTickMarkers;
        private readonly ProfilerMarker[] _lateTickMarkers;
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
            _tickMarkers = Markers(_tickables, "Tick()");
            _fixedTickMarkers = Markers(_fixedTickables, "FixedTick()");
            _lateTickMarkers = Markers(_lateTickables, "LateTick()");
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
            for (int i = 0; i < _tickables.Length; i++)
            {
                using (_tickMarkers[i].Auto())
                    _tickables[i].Tick();
            }
        }

        public void LateTick()
        {
            if (!_tickGate.IsTicking || _isDisposed) return;
            for (int i = 0; i < _lateTickables.Length; i++)
            {
                using (_lateTickMarkers[i].Auto())
                    _lateTickables[i].LateTick();
            }
        }

        public void FixedTick()
        {
            if (!_tickGate.IsTicking || _isDisposed) return;
            for (int i = 0; i < _fixedTickables.Length; i++)
            {
                using (_fixedTickMarkers[i].Auto())
                    _fixedTickables[i].FixedTick();
            }
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

        private static ProfilerMarker[] Markers<T>(T[] callbacks, string method)
        {
            var markers = new ProfilerMarker[callbacks.Length];
            for (int i = 0; i < callbacks.Length; i++)
            {
                Type type = callbacks[i].GetType();
                if (!CallbackMarkers.TryGetValue((type, method), out ProfilerMarker marker))
                {
                    marker = new ProfilerMarker($"{type.FullName}.{method}");
                    CallbackMarkers.Add((type, method), marker);
                }

                markers[i] = marker;
            }

            return markers;
        }

        private static T[] Order<T>(IReadOnlyList<T> callbacks, Func<LifecycleOrderAttribute, int> selectOrder)
        {
            return callbacks.OrderBy(callback =>
            {
                LifecycleOrderAttribute order = OrderOf(callback.GetType());
                return order == null ? 0 : selectOrder(order);
            }).ToArray();
        }

        private static LifecycleOrderAttribute OrderOf(Type type)
        {
            if (!CallbackOrders.TryGetValue(type, out LifecycleOrderAttribute order))
            {
                order = type.GetCustomAttribute<LifecycleOrderAttribute>();
                CallbackOrders.Add(type, order);
            }

            return order;
        }
    }
}
