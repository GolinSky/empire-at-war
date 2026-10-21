using System;
using System.Collections.Generic;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Repository;
using EmpireAtWar.Services.VContainer;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using VContainer;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class CachedAssetServiceTests
    {
        private const string SHIP_KEY = "Ship";

        private readonly List<Object> _objects = new List<Object>();
        private PopulatedVContainerSettings _settings;
        private FakeHandleLoader _loader;

        [SetUp]
        public void SetUp()
        {
            _settings = new PopulatedVContainerSettings();
            GameObject prefab = new GameObject(SHIP_KEY, typeof(BoxCollider));
            Material material = new Material(Shader.Find("Hidden/InternalErrorShader"));
            _objects.Add(prefab);
            _objects.Add(material);
            _loader = new FakeHandleLoader(prefab, material);
        }

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
        public void Load_SameKeyAndType_LoadsOnce()
        {
            CachedAssetService assets = new CachedAssetService(_loader);

            GameObject first = assets.LoadPrefab(SHIP_KEY);
            GameObject second = assets.Load<GameObject>(SHIP_KEY);
            BoxCollider collider = assets.LoadComponent<BoxCollider>(SHIP_KEY);

            Assert.That(_loader.Handles.Count, Is.EqualTo(1));
            Assert.That(second, Is.SameAs(first));
            Assert.That(collider.gameObject, Is.SameAs(first));
        }

        [Test]
        public void Load_SameKeyDifferentType_LoadsEachType()
        {
            CachedAssetService assets = new CachedAssetService(_loader);

            assets.LoadPrefab(SHIP_KEY);
            Material material = assets.Load<Material>(SHIP_KEY);

            Assert.That(_loader.Handles.Count, Is.EqualTo(2));
            Assert.That(material, Is.SameAs(_loader.Material));
        }

        [Test]
        public void Dispose_ReleasesEveryHandle()
        {
            CachedAssetService assets = new CachedAssetService(_loader);
            assets.LoadPrefab(SHIP_KEY);
            assets.Load<Material>(SHIP_KEY);

            assets.Dispose();

            Assert.That(_loader.Handles.TrueForAll(handle => !handle.IsValid()), Is.True);
        }

        [Test]
        public void Load_AfterDispose_Throws()
        {
            CachedAssetService assets = new CachedAssetService(_loader);
            assets.Dispose();

            Assert.Throws<ObjectDisposedException>(() => assets.LoadPrefab(SHIP_KEY));
            Assert.That(_loader.Handles, Is.Empty);
        }

        [Test]
        public void Dispose_KeepsAnotherCacheHandlesForTheSameKey()
        {
            CachedAssetService project = new CachedAssetService(_loader);
            CachedAssetService battle = new CachedAssetService(_loader);
            project.LoadPrefab(SHIP_KEY);
            battle.LoadPrefab(SHIP_KEY);

            battle.Dispose();

            Assert.That(_loader.Handles[0].IsValid(), Is.True, "The project handle must survive the battle release.");
            Assert.That(_loader.Handles[1].IsValid(), Is.False);
            Assert.That(project.LoadPrefab(SHIP_KEY), Is.SameAs(_loader.Prefab));
            project.Dispose();
        }

        [Test]
        public void LoadComponent_PrefabWithoutComponent_Throws()
        {
            CachedAssetService assets = new CachedAssetService(_loader);

            Assert.Throws<MissingComponentException>(() => assets.LoadComponent<Rigidbody>(SHIP_KEY));
        }

        [Test]
        public void OwningScope_ReleasesAssetsAfterContainerDisposal()
        {
            GameObject root = new GameObject("Asset Owner Root");
            root.SetActive(false);
            _objects.Add(root);
            OwnerScope owner = root.AddComponent<OwnerScope>();
            owner.autoRun = false;
            owner.Loader = _loader;
            using (LifetimeScope.EnqueueParent(null)) owner.Build();
            root.SetActive(true);
            AssetLifetimeScope child = owner.CreateChild<AssetLifetimeScope>(builder => { }, "Asset Owner Child");
            child.Build();
            HandleProbe probe = owner.Container.Resolve<HandleProbe>();

            // EditMode does not dispatch OnDestroy for the runtime scope component.
            typeof(AssetLifetimeScope).GetMethod("OnDestroy",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(owner, null);

            Assert.That(child.Assets, Is.SameAs(owner.Assets), "Child scopes use their owner's cache.");
            Assert.That(probe.WasValidOnDispose, Is.True, "Container disposal must run before the release.");
            Assert.That(_loader.Handles[0].IsValid(), Is.False);
        }

        private sealed class OwnerScope : AssetLifetimeScope
        {
            public FakeHandleLoader Loader { get; set; }

            protected override void Configure(IContainerBuilder builder)
            {
                OwnAssets(new CachedAssetService(Loader));
                builder.RegisterInstance(Assets);
                Assets.LoadPrefab(SHIP_KEY);
                builder.RegisterInstance(new HandleProbe(Loader.Handles[0])).AsImplementedInterfaces().AsSelf();
                builder.RegisterLifecycle();
            }
        }

        private sealed class HandleProbe : IOrderedDisposable
        {
            private readonly AsyncOperationHandle _handle;

            public HandleProbe(AsyncOperationHandle handle)
            {
                _handle = handle;
            }

            public bool WasValidOnDispose { get; private set; }

            public void Dispose() => WasValidOnDispose = _handle.IsValid();
        }

        private sealed class FakeHandleLoader : IAssetHandleLoader
        {
            private readonly ResourceManager _resourceManager = new ResourceManager();

            public FakeHandleLoader(GameObject prefab, Material material)
            {
                Prefab = prefab;
                Material = material;
            }

            public GameObject Prefab { get; }
            public Material Material { get; }
            public List<AsyncOperationHandle> Handles { get; } = new List<AsyncOperationHandle>();

            public AsyncOperationHandle<TSource> Load<TSource>(string key) where TSource : Object
            {
                Object asset = typeof(TSource) == typeof(Material) ? (Object)Material : Prefab;
                AsyncOperationHandle<TSource> handle =
                    _resourceManager.CreateCompletedOperation((TSource)asset, string.Empty);
                Handles.Add(handle);
                return handle;
            }
        }
    }
}
