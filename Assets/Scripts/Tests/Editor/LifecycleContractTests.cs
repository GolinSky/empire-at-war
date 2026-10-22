using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.VContainer;
using NUnit.Framework;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class LifecycleContractTests
    {
        // Everything EntryPointDispatcher drives itself, outside OrderedLifecycleCoordinator.
        private static readonly Type[] VContainerEntryPoints =
        {
            typeof(VContainer.Unity.IInitializable), typeof(IPostInitializable), typeof(IStartable),
            typeof(IPostStartable), typeof(IAsyncStartable), typeof(VContainer.Unity.ITickable),
            typeof(VContainer.Unity.IPostTickable), typeof(VContainer.Unity.IFixedTickable), typeof(IPostFixedTickable),
            typeof(VContainer.Unity.ILateTickable), typeof(IPostLateTickable)
        };

        private readonly List<GameObject> _objects = new List<GameObject>();
        private PopulatedVContainerSettings _settings;

        [SetUp]
        public void SetUp() => _settings = new PopulatedVContainerSettings();

        [TearDown]
        public void TearDown()
        {
            try
            {
                for (int i = _objects.Count - 1; i >= 0; i--)
                    if (_objects[i] != null) Object.DestroyImmediate(_objects[i]);
                _objects.Clear();
            }
            finally
            {
                _settings.Dispose();
            }
        }

        [Test]
        public void ProjectTypes_ImplementOnlyOrderedLifecycleCallbacks()
        {
            string[] violations = typeof(OrderedLifecycleCoordinator).Assembly.GetTypes()
                .Where(type => type != typeof(OrderedLifecycleCoordinator))
                .SelectMany(type => VContainerEntryPoints
                    .Where(entryPoint => entryPoint.IsAssignableFrom(type))
                    .Select(entryPoint => $"{type.FullName} : {entryPoint.FullName}"))
                .ToArray();

            Assert.That(violations, Is.Empty,
                "Use the EmpireAtWar.Lifecycle interfaces. VContainer entry points bypass LifecycleOrder, " +
                "the tick gate and ordered disposal, and a type with both runs its callback twice.");
        }

        [Test]
        public void EntryPointExceptionHandler_PreservesOriginalExceptionAndThrowSite()
        {
            var builder = new ContainerBuilder();
            builder.RegisterLifecycle();
            using (IObjectResolver container = builder.Build())
            {
                Type handlerType = typeof(LifetimeScope).Assembly.GetType(
                    "VContainer.Unity.EntryPointExceptionHandler", true);
                object handler = container.Resolve(handlerType);
                InvalidOperationException original = Assert.Throws<InvalidOperationException>(ThrowLifecycleFailure);

                TargetInvocationException published = Assert.Throws<TargetInvocationException>(() =>
                    handlerType.GetMethod("Publish").Invoke(handler, new object[] { original }));

                Assert.That(published.InnerException, Is.SameAs(original));
                Assert.That(published.InnerException.StackTrace, Does.Contain(nameof(ThrowLifecycleFailure)));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowLifecycleFailure() => throw new InvalidOperationException("Lifecycle failure");

        [Test]
        public void ChildScope_RegistersExactlyOneCoordinator()
        {
            GameObject root = new GameObject("Lifecycle Contract Root");
            root.SetActive(false);
            _objects.Add(root);
            RootScope parent = root.AddComponent<RootScope>();
            parent.autoRun = false;
            using (LifetimeScope.EnqueueParent(null)) parent.Build();
            root.SetActive(true);
            _settings.AssertProductionRootUntouched();

            CallbackProbe probe = new CallbackProbe();
            AssetLifetimeScope child = parent.CreateChild<AssetLifetimeScope>(
                builder => builder.RegisterInstance(probe).AsImplementedInterfaces(), "Lifecycle Contract Child");
            child.Build();
            child.Container.Resolve<OrderedLifecycleCoordinator>().Tick();

            Assert.That(probe.InitializeCount, Is.EqualTo(1));
            Assert.That(probe.TickCount, Is.EqualTo(1));
        }

        private sealed class RootScope : AssetLifetimeScope
        {
            protected override void Configure(IContainerBuilder builder)
            {
                Assets = new UnusedAssets();
                builder.RegisterInstance(Assets);
                builder.RegisterLifecycle();
            }
        }

        private sealed class CallbackProbe : EmpireAtWar.Lifecycle.IInitializable, EmpireAtWar.Lifecycle.ITickable
        {
            public int InitializeCount { get; private set; }
            public int TickCount { get; private set; }
            public void Initialize() => InitializeCount++;
            public void Tick() => TickCount++;
        }

        private sealed class UnusedAssets : IAssetService
        {
            public TSource Load<TSource>(string key) where TSource : Object => throw new NotSupportedException();
            public TComponent LoadComponent<TComponent>(string key) where TComponent : Component =>
                throw new NotSupportedException();
            public GameObject LoadPrefab(string key) => throw new NotSupportedException();
        }
    }
}
