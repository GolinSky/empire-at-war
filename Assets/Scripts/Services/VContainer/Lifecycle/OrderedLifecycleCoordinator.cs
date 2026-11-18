using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Profiling;
using VContainer.Internal;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using EmpireAtWar.Services.Timing;
#endif

namespace EmpireAtWar.Lifecycle
{
    public sealed class OrderedLifecycleCoordinator : global::VContainer.Unity.IInitializable,
        global::VContainer.Unity.ITickable, global::VContainer.Unity.IPostTickable, global::VContainer.Unity.IFixedTickable,
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
        private readonly IPostTickable[] _postTickables;
        private readonly IFixedTickable[] _fixedTickables;
        private readonly ILateTickable[] _lateTickables;
        private readonly ProfilerMarker[] _tickMarkers;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private readonly int[] _tickAttribution;
#endif
        private readonly ProfilerMarker[] _postTickMarkers;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private readonly int[] _postTickAttribution;
#endif
        private readonly ProfilerMarker[] _fixedTickMarkers;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private readonly int[] _fixedTickAttribution;
#endif
        private readonly ProfilerMarker[] _lateTickMarkers;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private readonly int[] _lateTickAttribution;
#endif
        private readonly IOrderedDisposable[] _disposables;
        private readonly ILateDisposable[] _lateDisposables;
        private readonly LifecycleTickGate _tickGate;
        private bool _isDisposed;

        public OrderedLifecycleCoordinator(
            ContainerLocal<IReadOnlyList<IInitializable>> initializables,
            ContainerLocal<IReadOnlyList<ITickable>> tickables,
            ContainerLocal<IReadOnlyList<IPostTickable>> postTickables,
            ContainerLocal<IReadOnlyList<IFixedTickable>> fixedTickables,
            ContainerLocal<IReadOnlyList<ILateTickable>> lateTickables,
            ContainerLocal<IReadOnlyList<IOrderedDisposable>> disposables,
            ContainerLocal<IReadOnlyList<ILateDisposable>> lateDisposables,
            LifecycleTickGate tickGate)
        {
            _initializables = Order(initializables.Value, order => order.Initialize);
            _tickables = Order(tickables.Value, order => order.Tick);
            _postTickables = Order(postTickables.Value, order => order.PostTick);
            _fixedTickables = Order(fixedTickables.Value, order => order.FixedTick);
            _lateTickables = Order(lateTickables.Value, order => order.LateTick);
            _tickMarkers = Markers(_tickables, "Tick()");
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _tickAttribution = Attribution(_tickables, "Tick()");
#endif
            _postTickMarkers = Markers(_postTickables, "PostTick()");
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _postTickAttribution = Attribution(_postTickables, "PostTick()");
#endif
            _fixedTickMarkers = Markers(_fixedTickables, "FixedTick()");
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _fixedTickAttribution = Attribution(_fixedTickables, "FixedTick()");
#endif
            _lateTickMarkers = Markers(_lateTickables, "LateTick()");
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _lateTickAttribution = Attribution(_lateTickables, "LateTick()");
#endif
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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                using (BattleSystemAttribution.Measure(_tickAttribution[i]))
#endif
                using (_tickMarkers[i].Auto())
                    _tickables[i].Tick();
            }
        }

        public void PostTick()
        {
            if (!_tickGate.IsTicking || _isDisposed) return;
            for (int i = 0; i < _postTickables.Length; i++)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                using (BattleSystemAttribution.Measure(_postTickAttribution[i]))
#endif
                using (_postTickMarkers[i].Auto())
                    _postTickables[i].PostTick();
            }
        }

        public void LateTick()
        {
            if (!_tickGate.IsTicking || _isDisposed) return;
            for (int i = 0; i < _lateTickables.Length; i++)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                using (BattleSystemAttribution.Measure(_lateTickAttribution[i]))
#endif
                using (_lateTickMarkers[i].Auto())
                    _lateTickables[i].LateTick();
            }
        }

        public void FixedTick()
        {
            if (!_tickGate.IsTicking || _isDisposed) return;
            for (int i = 0; i < _fixedTickables.Length; i++)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                using (BattleSystemAttribution.Measure(_fixedTickAttribution[i]))
#endif
                using (_fixedTickMarkers[i].Auto())
                    _fixedTickables[i].FixedTick();
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            var failures = new CleanupFailures();
            for (int i = _disposables.Length - 1; i >= 0; i--)
                failures.Attempt(_disposables[i].Dispose);
            for (int i = _lateDisposables.Length - 1; i >= 0; i--)
                failures.Attempt(_lateDisposables[i].LateDispose);
            failures.ThrowIfAny();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static int[] Attribution<T>(T[] callbacks, string method)
        {
            var ids = new int[callbacks.Length];
            for (int i = 0; i < callbacks.Length; i++)
                ids[i] = BattleSystemAttribution.Register($"{callbacks[i].GetType().FullName}.{method}");
            return ids;
        }
#endif

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
