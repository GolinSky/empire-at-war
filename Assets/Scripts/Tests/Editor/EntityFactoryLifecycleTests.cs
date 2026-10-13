using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Extensions;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Factory;
using EmpireAtWar.Services.VContainer;
using NUnit.Framework;
using UnityEngine;
using VContainer;
using VContainer.Internal;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class EntityFactoryLifecycleTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (int i = _objects.Count - 1; i >= 0; i--)
                if (_objects[i] != null) Object.DestroyImmediate(_objects[i]);
            _objects.Clear();
        }

        [Test]
        public void Create_InjectsAndInitializesBeforeActivationAndIsolatesChildren()
        {
            TestScope parent = CreateParent();
            ProbeEntity prefab = CreatePrefab();
            ProbeFactory factory = new ProbeFactory(parent.Assets, parent, parent.Ownership);
            ProbeEntity first = factory.Create(prefab, TestPlayers.Human, 11, new Vector3(2f, 0f, 3f));
            ProbeEntity second = factory.Create(prefab, TestPlayers.Enemy, 12, new Vector3(7f, 0f, 8f));

            Assert.That(first.InitializedBeforeActivation, Is.True);
            Assert.That(second.InitializedBeforeActivation, Is.True);
            Assert.That(first.gameObject.activeInHierarchy, Is.True);
            Assert.That(second.gameObject.activeInHierarchy, Is.True);
            Assert.That(first.ConstructCount, Is.EqualTo(1));
            Assert.That(first.InitializeCount, Is.EqualTo(1));
            Assert.That(first.Owner, Is.EqualTo(TestPlayers.Human));
            Assert.That(second.Owner, Is.EqualTo(TestPlayers.Enemy));
            Assert.That(first.EntityId, Is.EqualTo(11));
            Assert.That(second.EntityId, Is.EqualTo(12));
            Assert.That(first.transform.position, Is.EqualTo(new Vector3(2f, 0f, 3f)));
            Assert.That(second.transform.position, Is.EqualTo(new Vector3(7f, 0f, 8f)));
            Assert.That(first.OwnTransform, Is.SameAs(first.transform));
            Assert.That(first.Scope, Is.Not.SameAs(second.Scope));
            Assert.That(first.Scope.Parent, Is.SameAs(parent));
            Assert.That(first.Gate, Is.Not.SameAs(second.Gate));
            Assert.That(parent.Ownership.Count, Is.EqualTo(2));

            OrderedLifecycleCoordinator lifecycle = first.Scope.Container.Resolve<OrderedLifecycleCoordinator>();
            lifecycle.Tick();
            Assert.That(first.TickCount, Is.EqualTo(1));
            first.Gate.SetTicking(false);
            lifecycle.Tick();
            Assert.That(first.TickCount, Is.EqualTo(1));
            first.Gate.SetTicking(true);
            lifecycle.Tick();
            Assert.That(first.TickCount, Is.EqualTo(2));
            first.Scope.DisposeCore();
            Assert.That(first.DisposeCount, Is.EqualTo(1));
            Assert.That(first.Owned.ReleaseCount, Is.EqualTo(1));
            // EditMode does not dispatch OnDestroy for the runtime scope component.
            typeof(EntityLifetimeScope).GetMethod("OnDestroy",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(first.Scope, null);
            Object.DestroyImmediate(first.Scope.gameObject);
            Assert.That(parent.Ownership.Count, Is.EqualTo(1));
            Assert.That(first.DisposeCount, Is.EqualTo(1));
            Assert.That(first.Owned.ReleaseCount, Is.EqualTo(1));
            Assert.That(second.DisposeCount, Is.Zero);
            Assert.That(second.Owned.ReleaseCount, Is.Zero);
        }

        [Test]
        public void Create_FailedInstallationRemovesChildAndDoesNotActivateView()
        {
            TestScope parent = CreateParent();
            ProbeEntity prefab = CreatePrefab();
            ProbeFactory factory = new ProbeFactory(parent.Assets, parent, parent.Ownership);
            Assert.Throws<InvalidOperationException>(() => factory.CreateInvalid(prefab));
            Assert.That(parent.transform.childCount, Is.Zero);
            Assert.That(parent.Ownership.Count, Is.Zero);
            Assert.That(prefab.ConstructCount, Is.Zero);
            Assert.That(prefab.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void DisposeParent_ReleasesActiveAndPooledChildrenBeforeParentResources()
        {
            TestScope parent = CreateParent();
            ProbeEntity prefab = CreatePrefab();
            ProbeFactory factory = new ProbeFactory(parent.Assets, parent, parent.Ownership);
            ProbeEntity active = factory.Create(prefab, TestPlayers.Human, 21, Vector3.zero);
            ProbeEntity pooled = factory.Create(prefab, TestPlayers.Human, 22, Vector3.one);
            pooled.Gate.SetTicking(false);
            LifetimeScope activeScope = active.Scope;
            LifetimeScope pooledScope = pooled.Scope;
            OwnedComponentProbe activeComponent = active.Owned;
            OwnedComponentProbe pooledComponent = pooled.Owned;

            parent.DisposeCore();

            Assert.That(parent.Ownership.Count, Is.Zero);
            Assert.That(activeScope.Container, Is.Null);
            Assert.That(pooledScope.Container, Is.Null);
            Assert.That(active.DisposeCount, Is.EqualTo(1));
            Assert.That(pooled.DisposeCount, Is.EqualTo(1));
            Assert.That(activeComponent.ReleaseCount, Is.EqualTo(1));
            Assert.That(pooledComponent.ReleaseCount, Is.EqualTo(1));
            Assert.That(parent.Resource.DisposeCount, Is.EqualTo(1));
        }

        private TestScope CreateParent()
        {
            GameObject root = new GameObject("Factory Test Parent");
            root.SetActive(false);
            _objects.Add(root);
            TestScope parent = root.AddComponent<TestScope>();
            parent.autoRun = false;
            parent.Build();
            root.SetActive(true);
            return parent;
        }

        private ProbeEntity CreatePrefab()
        {
            GameObject prefab = new GameObject("Factory Test Prefab");
            prefab.SetActive(false);
            _objects.Add(prefab);
            ProbeEntity view = prefab.AddComponent<ProbeEntity>();
            view.SetComponent(prefab.AddComponent<OwnedComponentProbe>());
            return view;
        }

        private sealed class ProbeFactory : BaseFactory
        {
            public ProbeFactory(IAssetService assets, LifetimeScope scope, EntityScopeOwnership ownership)
                : base(assets, scope, ownership) { }

            public ProbeEntity Create(ProbeEntity prefab, PlayerId owner, long id, Vector3 position) =>
                Spawn(prefab, position, Quaternion.identity, (builder, view) =>
                {
                    builder.RegisterInstance(owner);
                    builder.RegisterInstance(id);
                    builder.RegisterInstance(view.transform).Keyed(EntityBindType.ViewTransform);
                    builder.RegisterComponent(view).AsSelf().AsImplementedInterfaces();
                    EntityFeatureBindings.RegisterComponent(builder, view.Owned);
                    builder.RegisterLifecycle();
                });

            public ProbeEntity CreateInvalid(ProbeEntity prefab) =>
                Spawn(prefab, Vector3.zero, Quaternion.identity,
                    (builder, view) => throw new InvalidOperationException("Fixture installation failed."));
        }

        private sealed class TestScope : AssetLifetimeScope
        {
            public EntityScopeOwnership Ownership { get; private set; }
            public ParentResourceProbe Resource { get; private set; }

            protected override void Configure(IContainerBuilder builder)
            {
                Assets = new UnusedAssets();
                Ownership = new EntityScopeOwnership();
                Resource = new ParentResourceProbe();
                builder.RegisterInstance(Assets);
                builder.RegisterInstance(Ownership).AsImplementedInterfaces().AsSelf();
                builder.RegisterInstance(Resource).AsImplementedInterfaces().AsSelf();
                builder.RegisterLifecycle();
            }
        }

        private sealed class ProbeEntity : EntityView, EmpireAtWar.Lifecycle.IInitializable,
            EmpireAtWar.Lifecycle.ITickable, EmpireAtWar.Lifecycle.ILateDisposable
        {
            [SerializeField] private OwnedComponentProbe ownedComponent;
            private EntityComponentLifecycle _components;
            private ParentResourceProbe _parentResource;

            public OwnedComponentProbe Owned => ownedComponent;
            public void SetComponent(OwnedComponentProbe component) => ownedComponent = component;

            public override string Id => nameof(ProbeEntity);
            public int ConstructCount { get; private set; }
            public int InitializeCount { get; private set; }
            public int TickCount { get; private set; }
            public int DisposeCount { get; private set; }
            public bool InitializedBeforeActivation { get; private set; }
            public long EntityId { get; private set; }
            public PlayerId Owner { get; private set; }
            public Transform OwnTransform { get; private set; }
            public LifetimeScope Scope { get; private set; }
            public LifecycleTickGate Gate { get; private set; }

            [Inject]
            private void Construct(long id, PlayerId owner, [Key(EntityBindType.ViewTransform)] Transform ownTransform,
                LifetimeScope scope, LifecycleTickGate gate, ParentResourceProbe parentResource,
                ContainerLocal<IReadOnlyList<IMonoComponent>> components)
            {
                ConstructCount++;
                EntityId = id;
                Owner = owner;
                OwnTransform = ownTransform;
                Scope = scope;
                Gate = gate;
                _parentResource = parentResource;
                _components = new EntityComponentLifecycle(components.Value);
                Assert.That(gameObject.activeInHierarchy, Is.False);
            }

            public void Initialize()
            {
                InitializeCount++;
                InitializedBeforeActivation = !gameObject.activeInHierarchy && ConstructCount == 1;
            }
            public void Tick() => TickCount++;
            public void LateDispose()
            {
                Assert.That(_parentResource.DisposeCount, Is.Zero,
                    "The entity must release before its parent's resources are disposed.");
                DisposeCount++;
                _components.Release();
            }
        }

        private sealed class ParentResourceProbe : IOrderedDisposable
        {
            public int DisposeCount { get; private set; }
            public void Dispose() => DisposeCount++;
        }

        private sealed class OwnedComponentProbe : MonoBehaviour, IMonoComponent, EmpireAtWar.Lifecycle.ILateDisposable
        {
            public int ReleaseCount { get; private set; }
            public void Release() => ReleaseCount++;
            public void LateDispose() => Release();
        }

        private sealed class UnusedAssets : IAssetService
        {
            public TSource Load<TSource>(string key) where TSource : Object => throw new NotSupportedException();
            public TComponent LoadComponent<TComponent>(string key) where TComponent : Component => throw new NotSupportedException();
            public GameObject LoadPrefab(string key) => throw new NotSupportedException();
        }
    }
}
