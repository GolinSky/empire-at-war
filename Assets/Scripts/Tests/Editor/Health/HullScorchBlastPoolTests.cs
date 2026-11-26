using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Services.UnitExplosion;
using EmpireAtWar.ViewComponents.Health;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Health")]
    public sealed class HullScorchBlastPoolTests
    {
        private const int MAX_ACTIVE = 2;
        private const float UNIT_INTERVAL = 1f;

        private readonly List<Object> _objects = new List<Object>();
        private ExplosionVfx _prefab;
        private UnityEngine.Camera _camera;
        private HullScorchBlastPool _pool;
        private Transform _anchor;

        [SetUp]
        public void SetUp()
        {
            GameObject prefab = Track(new GameObject("HullBlastVfx"));
            ParticleSystem particles = prefab.AddComponent<ParticleSystem>();
            _prefab = prefab.AddComponent<ExplosionVfx>();
            SerializedObject serialized = new SerializedObject(_prefab);
            serialized.FindProperty("particles").objectReferenceValue = particles;
            SerializedProperty renderers = serialized.FindProperty("renderers");
            renderers.arraySize = 1;
            renderers.GetArrayElementAtIndex(0).objectReferenceValue = prefab.GetComponent<ParticleSystemRenderer>();
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // Camera at the origin looking along +z, 1080 pixels high, so screen sizes do not depend on the Editor.
            _camera = Track(new GameObject("Battle Camera")).AddComponent<UnityEngine.Camera>();
            _camera.farClipPlane = 1_000_000f;
            _camera.targetTexture = Track(new RenderTexture(1920, 1080, 0));
            _anchor = Track(new GameObject("Spot")).transform;
            _anchor.position = Vector3.forward * 50f;

            _pool = CreatePool($"{{\"maxActive\":{MAX_ACTIVE},\"unitInterval\":{UNIT_INTERVAL}}}");
        }

        [TearDown]
        public void TearDown()
        {
            _camera.targetTexture = null;
            foreach (Object tracked in _objects)
                if (tracked != null) Object.DestroyImmediate(tracked);
            _objects.Clear();
        }

        [Test]
        public void TryPlay_VisibleSpot_PlaysAtOffset()
        {
            Assert.That(_pool.TryPlay(_anchor, Vector3.up, null, 1f), Is.True);

            Assert.That(Playing(), Has.Count.EqualTo(1));
            Assert.That(Playing()[0].transform.position, Is.EqualTo(_anchor.position + Vector3.up));
        }

        [Test]
        public void TryPlay_HiddenByFog_Skips()
        {
            Assert.That(_pool.TryPlay(_anchor, Vector3.zero, new Fog { IsHiddenByFog = true }, 1f), Is.False);
            Assert.That(Playing(), Is.Empty);
        }

        [Test]
        public void TryPlay_BehindCamera_Skips()
        {
            _anchor.position = Vector3.back * 50f;

            Assert.That(_pool.TryPlay(_anchor, Vector3.zero, null, 1f), Is.False);
        }

        [Test]
        public void TryPlay_TooSmallOnScreen_Skips()
        {
            _anchor.position = Vector3.forward * 100_000f;

            Assert.That(_pool.TryPlay(_anchor, Vector3.zero, null, 1f), Is.False);
        }

        [Test]
        public void TryPlay_AtMaxActive_SkipsWithoutGrowingThePool()
        {
            for (int i = 0; i < MAX_ACTIVE; i++) Assert.That(_pool.TryPlay(_anchor, Vector3.zero, null, 1f), Is.True);

            Assert.That(_pool.TryPlay(_anchor, Vector3.zero, null, 1f), Is.False);
            Assert.That(Root().childCount, Is.EqualTo(MAX_ACTIVE));
        }

        [Test]
        public void TryPlay_PerFrameCap_Skips()
        {
            _pool.LateDispose();
            _pool = CreatePool("{\"maxActive\":10,\"maxStartsPerFrame\":1}");

            Assert.That(_pool.TryPlay(_anchor, Vector3.zero, null, 1f), Is.True);
            Assert.That(_pool.TryPlay(_anchor, Vector3.zero, null, 1f), Is.False);
        }

        [Test]
        public void UnitInterval_StretchesAsTheBattleFills()
        {
            Assert.That(_pool.UnitInterval, Is.EqualTo(UNIT_INTERVAL));

            for (int i = 0; i < MAX_ACTIVE; i++) _pool.TryPlay(_anchor, Vector3.zero, null, 1f);

            Assert.That(_pool.UnitInterval, Is.GreaterThan(UNIT_INTERVAL));
        }

        private HullScorchBlastPool CreatePool(string limitsJson)
        {
            HullBlastLimits limits = new HullBlastLimits();
            JsonUtility.FromJsonOverwrite(limitsJson, limits);
            HullScorchBlastPool pool = new HullScorchBlastPool(_prefab, _camera, limits);
            pool.Initialize();
            Track(Root().gameObject);
            return pool;
        }

        // The newest pool root; LateDispose removes an edit-mode root.
        private Transform Root() => GameObject.Find("HullBlastVfxPool").transform;

        private List<ExplosionVfx> Playing()
        {
            List<ExplosionVfx> playing = new List<ExplosionVfx>();
            foreach (Transform child in Root())
            {
                ExplosionVfx blast = child.GetComponent<ExplosionVfx>();
                if (blast.IsPlaying) playing.Add(blast);
            }
            return playing;
        }

        private T Track<T>(T tracked) where T : Object
        {
            _objects.Add(tracked);
            return tracked;
        }

        private sealed class Fog : IFogVisibilityFacade
        {
            public event System.Action<bool> VisibilityChanged { add { } remove { } }
            public bool IsHiddenByFog { get; set; }
        }
    }
}
