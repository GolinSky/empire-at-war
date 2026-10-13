using System;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Extensions;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using NUnit.Framework;
using UnityEngine;
using VContainer;
using VContainer.Internal;
using VContainer.Unity;
using Object = UnityEngine.Object;
using IInitializable = EmpireAtWar.Lifecycle.IInitializable;
using ITickable = EmpireAtWar.Lifecycle.ITickable;
using ILateTickable = EmpireAtWar.Lifecycle.ILateTickable;
using IFixedTickable = EmpireAtWar.Lifecycle.IFixedTickable;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class VContainerCompositionTests
    {
        [Test]
        public void Lifecycle_PreservesPhaseOrderStableTiesAndReverseCleanup()
        {
            List<string> calls = new List<string>();
            RecordingCallbacks[] callbacks =
            {
                new NavigationCallbacks(calls), new SfxCallbacks(calls), new FlightCallbacks(calls),
                new PoseCallbacks(calls), new HealthCallbacks(calls), new CombatCallbacks(calls),
                new RecordingCallbacks(calls, "first"), new RecordingCallbacks(calls, "second")
            };
            OrderedLifecycleCoordinator coordinator = CreateCoordinator(callbacks, new LifecycleTickGate());

            coordinator.Initialize();
            Assert.That(calls, Is.EqualTo(new[]
            {
                "initialize:health", "initialize:flight", "initialize:navigation", "initialize:sfx",
                "initialize:pose", "initialize:combat", "initialize:first", "initialize:second"
            }));
            calls.Clear();
            coordinator.Tick();
            Assert.That(calls, Is.EqualTo(new[]
            {
                "tick:navigation", "tick:flight", "tick:pose", "tick:health", "tick:combat",
                "tick:first", "tick:second", "tick:sfx"
            }));
            calls.Clear();
            coordinator.LateTick();
            Assert.That(calls, Is.EqualTo(new[]
            {
                "late:pose", "late:combat", "late:flight", "late:health", "late:first", "late:second",
                "late:sfx", "late:navigation"
            }));
            calls.Clear();
            coordinator.Dispose();
            coordinator.Dispose();
            coordinator.Tick();
            coordinator.LateTick();
            Assert.That(calls, Is.EqualTo(new[]
            {
                "dispose:second", "dispose:first", "dispose:combat", "dispose:health", "dispose:pose",
                "dispose:flight", "dispose:navigation", "dispose:sfx", "release:second", "release:first",
                "release:combat", "release:health", "release:pose", "release:flight", "release:sfx",
                "release:navigation"
            }));
        }

        [Test]
        public void Lifecycle_PoolReturnSuppressesTicksAndRespawnKeepsTheSameCallbacks()
        {
            List<string> calls = new List<string>();
            LifecycleTickGate gate = new LifecycleTickGate();
            OrderedLifecycleCoordinator coordinator = CreateCoordinator(
                new[] { new RecordingCallbacks(calls, "entity") }, gate);
            coordinator.Initialize();
            gate.SetTicking(false);
            coordinator.Tick();
            coordinator.LateTick();
            coordinator.FixedTick();
            Assert.That(calls, Is.EqualTo(new[] { "initialize:entity" }));

            gate.SetTicking(true);
            coordinator.Tick();
            coordinator.LateTick();
            coordinator.FixedTick();
            coordinator.Dispose();
            Assert.That(calls, Is.EqualTo(new[]
            {
                "initialize:entity", "tick:entity", "late:entity", "fixed:entity", "dispose:entity", "release:entity"
            }));
        }

        [Test]
        public void ChildScopes_InjectExplicitReferencesAndKeysWithoutSharingEntityStateOrLifecycle()
        {
            using PopulatedVContainerSettings settings = new PopulatedVContainerSettings();
            GameObject rootObject = new GameObject("VContainer fixture parent");
            rootObject.SetActive(false);
            ProbeScope parent = rootObject.AddComponent<ProbeScope>();
            List<ProbeScope> scopes = new List<ProbeScope> { parent };
            SharedCatalog catalog = new SharedCatalog();
            List<string> parentCalls = new List<string>();
            FixtureHandle parentHandle = new FixtureHandle();
            try
            {
                using (LifetimeScope.EnqueueParent(null))
                using (LifetimeScope.Enqueue(builder =>
                {
                    builder.RegisterLifecycle();
                    builder.RegisterInstance(catalog);
                    builder.RegisterInstance(new RecordingCallbacks(parentCalls, "parent"))
                        .AsImplementedInterfaces();
                    builder.RegisterComponent(parentHandle);
                })) parent.Build();

                FixtureHandle firstHandle = new FixtureHandle();
                FixtureHandle secondHandle = new FixtureHandle();
                ProbeView firstView = CreateView("first", new Vector3(1f, 2f, 3f));
                ProbeView secondView = CreateView("second", new Vector3(4f, 5f, 6f));
                IProbeComponent[] components = { new FirstProbeComponent(), new SecondProbeComponent() };
                ProbeScope first = CreateChild(parent, firstView, firstHandle, 10, components);
                scopes.Add(first);
                ProbeScope second = CreateChild(parent, secondView, secondHandle, 20, components);
                scopes.Add(second);
                Assert.That(parent.Parent, Is.Null);
                Assert.That(first.Parent, Is.SameAs(parent));
                Assert.That(second.Parent, Is.SameAs(parent));
                settings.AssertProductionRootUntouched();

                Assert.That(firstView.InitializationCount, Is.EqualTo(1));
                Assert.That(secondView.InitializationCount, Is.EqualTo(1));
                Assert.That(firstView.WasConfiguredWhenInitialized, Is.True);
                Assert.That(secondView.WasConfiguredWhenInitialized, Is.True);
                Assert.That(firstView.ViewTransform, Is.SameAs(firstView.transform));
                Assert.That(secondView.ViewTransform, Is.SameAs(secondView.transform));
                Assert.That(firstView.OwnerScope, Is.SameAs(first));
                Assert.That(secondView.OwnerScope, Is.SameAs(second));
                Assert.That(firstView.Catalog, Is.SameAs(catalog));
                Assert.That(secondView.Catalog, Is.SameAs(catalog));
                Assert.That(firstView.Components, Is.SameAs(components));
                Assert.That(firstView.State, Is.Not.SameAs(secondView.State));
                firstView.State.Value = 99;
                Assert.That(secondView.State.Value, Is.EqualTo(20));
                Assert.That(parentCalls, Is.EqualTo(new[] { "initialize:parent" }));

                firstHandle.Coordinator.Tick();
                secondHandle.Coordinator.Tick();
                Assert.That(parentCalls, Is.EqualTo(new[] { "initialize:parent" }));
                first.DisposeCore();
                Assert.That(firstView.ReleaseCount, Is.EqualTo(1));
                Assert.That(secondView.ReleaseCount, Is.Zero);
                Assert.That(parentCalls, Is.EqualTo(new[] { "initialize:parent" }));
                secondHandle.Coordinator.Tick();
                Assert.That(secondView.TickCount, Is.EqualTo(2));
                second.DisposeCore();
                parent.DisposeCore();
                Assert.That(parentCalls, Is.EqualTo(new[]
                {
                    "initialize:parent", "dispose:parent", "release:parent"
                }));
            }
            finally
            {
                for (int i = scopes.Count - 1; i >= 0; i--)
                {
                    scopes[i].DisposeCore();
                    Object.DestroyImmediate(scopes[i].gameObject);
                }
            }
        }

        private static OrderedLifecycleCoordinator CreateCoordinator(
            IReadOnlyList<RecordingCallbacks> callbacks, LifecycleTickGate gate) =>
            new OrderedLifecycleCoordinator(
                new ContainerLocal<IReadOnlyList<IInitializable>>(callbacks.Cast<IInitializable>().ToArray()),
                new ContainerLocal<IReadOnlyList<ITickable>>(callbacks.Cast<ITickable>().ToArray()),
                new ContainerLocal<IReadOnlyList<IFixedTickable>>(callbacks.Cast<IFixedTickable>().ToArray()),
                new ContainerLocal<IReadOnlyList<ILateTickable>>(callbacks.Cast<ILateTickable>().ToArray()),
                new ContainerLocal<IReadOnlyList<IOrderedDisposable>>(callbacks.Cast<IOrderedDisposable>().ToArray()),
                new ContainerLocal<IReadOnlyList<ILateDisposable>>(callbacks.Cast<ILateDisposable>().ToArray()), gate);

        [Test]
        public void HealthDecoration_UsesAnExplicitBaseAndKeepsResearchValuesLive()
        {
            BaseHealthData baseData = new BaseHealthData();
            ProbeResearch research = new ProbeResearch { Multiplier = 1.5f };
            HealthProbe probe = new HealthProbe();
            ContainerBuilder builder = new ContainerBuilder();
            builder.RegisterInstance<IFactionResearchModelObserver>(research);
            builder.RegisterInstance<IHealthData>(baseData).Keyed(EntityBindType.HealthData);
            builder.Register<ResearchHealthData>(Lifetime.Singleton)
                .As<IHealthData>();
            builder.RegisterComponent(probe);

            using (builder.Build())
            {
                Assert.That(probe.Health, Is.TypeOf<ResearchHealthData>());
                Assert.That(probe.Health.Hull, Is.EqualTo(baseData.Hull));
                Assert.That(probe.Health.Shields, Is.EqualTo(30f));
                research.Multiplier = 2f;
                Assert.That(probe.Health.Shields, Is.EqualTo(40f));
                Assert.That(baseData.Shields, Is.EqualTo(20f));
            }
        }

        private static ProbeView CreateView(string name, Vector3 position)
        {
            GameObject viewObject = new GameObject(name);
            viewObject.SetActive(false);
            viewObject.transform.position = position;
            return viewObject.AddComponent<ProbeView>();
        }

        private static ProbeScope CreateChild(ProbeScope parent, ProbeView view, FixtureHandle handle,
            int value, IReadOnlyList<IProbeComponent> components)
        {
            ProbeScope child = parent.CreateChild<ProbeScope>(builder =>
            {
                builder.RegisterLifecycle();
                builder.RegisterInstance(new RuntimeArguments(value));
                builder.Register<MutableState>(Lifetime.Singleton);
                builder.RegisterInstance(view.transform).Keyed(EntityBindType.ViewTransform);
                builder.RegisterInstance(components);
                builder.RegisterComponent(view).AsImplementedInterfaces().AsSelf();
                builder.RegisterComponent(handle);
            });
            view.transform.SetParent(child.transform, true);
            child.Build();
            return child;
        }

        private class RecordingCallbacks : IInitializable, ITickable, IFixedTickable, ILateTickable,
            IOrderedDisposable, ILateDisposable
        {
            private readonly List<string> _calls;
            private readonly string _name;

            public RecordingCallbacks(List<string> calls, string name) { _calls = calls; _name = name; }
            public void Initialize() => _calls.Add("initialize:" + _name);
            public void Tick() => _calls.Add("tick:" + _name);
            public void FixedTick() => _calls.Add("fixed:" + _name);
            public void LateTick() => _calls.Add("late:" + _name);
            public void Dispose() => _calls.Add("dispose:" + _name);
            public void LateDispose() => _calls.Add("release:" + _name);
        }

        [LifecycleOrder(Initialize = -100)]
        private sealed class HealthCallbacks : RecordingCallbacks
        { public HealthCallbacks(List<string> calls) : base(calls, "health") { } }
        [LifecycleOrder(Initialize = -90)]
        private sealed class FlightCallbacks : RecordingCallbacks
        { public FlightCallbacks(List<string> calls) : base(calls, "flight") { } }
        [LifecycleOrder(LateTick = -2000)]
        private sealed class PoseCallbacks : RecordingCallbacks
        { public PoseCallbacks(List<string> calls) : base(calls, "pose") { } }
        [LifecycleOrder(LateTick = -1000)]
        private sealed class CombatCallbacks : RecordingCallbacks
        { public CombatCallbacks(List<string> calls) : base(calls, "combat") { } }
        [LifecycleOrder(Tick = 1000, LateTick = 1000, Dispose = -1000)]
        private sealed class SfxCallbacks : RecordingCallbacks
        { public SfxCallbacks(List<string> calls) : base(calls, "sfx") { } }
        [LifecycleOrder(LateTick = 2000)]
        private sealed class NavigationCallbacks : RecordingCallbacks
        { public NavigationCallbacks(List<string> calls) : base(calls, "navigation") { } }

        private sealed class FixtureHandle
        {
            public OrderedLifecycleCoordinator Coordinator { get; private set; }
            [Inject]
            public void Construct(OrderedLifecycleCoordinator coordinator) => Coordinator = coordinator;
        }

        private sealed class ProbeScope : LifetimeScope
        { protected override void Awake() { } }

        private sealed class ProbeView : MonoBehaviour, IInitializable, ITickable, ILateDisposable
        {
            public Transform ViewTransform { get; private set; }
            public LifetimeScope OwnerScope { get; private set; }
            public MutableState State { get; private set; }
            public SharedCatalog Catalog { get; private set; }
            public IReadOnlyList<IProbeComponent> Components { get; private set; }
            public int InitializationCount { get; private set; }
            public int TickCount { get; private set; }
            public int ReleaseCount { get; private set; }
            public bool WasConfiguredWhenInitialized { get; private set; }

            [Inject]
            public void Construct([Key(EntityBindType.ViewTransform)] Transform viewTransform,
                LifetimeScope ownerScope, MutableState state, SharedCatalog catalog,
                IReadOnlyList<IProbeComponent> components)
            {
                ViewTransform = viewTransform;
                OwnerScope = ownerScope;
                State = state;
                Catalog = catalog;
                Components = components;
            }

            public void Initialize()
            {
                WasConfiguredWhenInitialized = ViewTransform == transform && State != null && Catalog != null;
                InitializationCount++;
            }
            public void Tick() => TickCount++;
            public void LateDispose() => ReleaseCount++;
        }

        private sealed class MutableState
        {
            public int Value { get; set; }
            public MutableState(RuntimeArguments arguments) { Value = arguments.Value; }
        }
        private sealed class RuntimeArguments
        {
            public int Value { get; }
            public RuntimeArguments(int value) { Value = value; }
        }
        private sealed class SharedCatalog { }
        private interface IProbeComponent { }
        private sealed class FirstProbeComponent : IProbeComponent { }
        private sealed class SecondProbeComponent : IProbeComponent { }

        private sealed class HealthProbe
        {
            public IHealthData Health { get; private set; }
            [Inject]
            public void Construct(IHealthData health) => Health = health;
        }

        private sealed class BaseHealthData : IHealthData
        {
            public ShipClass ShipClass => ShipClass.Frigate;
            public float Hull => 100f;
            public float Shields => 20f;
            public float ShieldRegenerateValue => 2f;
            public float ShieldRegenerateDelay => 5f;
            public IReadOnlyList<HardPointHealth> HardPointHealth => Array.Empty<HardPointHealth>();
        }

        private sealed class ProbeResearch : IFactionResearchModelObserver
        {
            public event Action<ResearchType> OnResearchCompleted { add { } remove { } }
            public IReadOnlyCollection<ResearchType> ResearchTypes => Array.Empty<ResearchType>();
            public float IncomeMultiplier => 1f;
            public float Multiplier { get; set; }
            public bool TryGetNextTier(ResearchType researchType, out ResearchTierData tier)
            { tier = null; return false; }
            public int GetCompletedTiers(ResearchType researchType) => 0;
            public float GetMultiplier(ResearchStat stat, ShipClass shipClass) => Multiplier;
        }
    }
}
