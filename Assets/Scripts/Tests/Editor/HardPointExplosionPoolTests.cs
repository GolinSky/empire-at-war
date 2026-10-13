using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Services.UnitExplosion;
using EmpireAtWar.ViewComponents.Health;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class HardPointExplosionPoolTests
    {
        private const int PREWARM_COUNT = 16;

        private readonly List<GameObject> _gameObjects = new List<GameObject>();
        private HardPointExplosionPool _pool;
        private Transform _anchor;

        [SetUp]
        public void SetUp()
        {
            GameObject prefab = Track(new GameObject("ExplosionVfx"));
            ParticleSystem particles = prefab.AddComponent<ParticleSystem>();
            ParticleSystemRenderer prefabRenderer = prefab.GetComponent<ParticleSystemRenderer>();
            ExplosionVfx explosion = prefab.AddComponent<ExplosionVfx>();
            SerializedObject serialized = new SerializedObject(explosion);
            serialized.FindProperty("particles").objectReferenceValue = particles;
            SerializedProperty renderers = serialized.FindProperty("renderers");
            renderers.arraySize = 1;
            renderers.GetArrayElementAtIndex(0).objectReferenceValue = prefabRenderer;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            _anchor = Track(new GameObject("HardPoint")).transform;
            _pool = new HardPointExplosionPool(explosion);
            _pool.Initialize();
            Track(Root().gameObject);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject gameObject in _gameObjects)
            {
                Object.DestroyImmediate(gameObject);
            }

            _gameObjects.Clear();
        }

        [Test]
        public void Initialize_PrewarmsExplosions()
        {
            Assert.That(Root().childCount, Is.EqualTo(PREWARM_COUNT));
            Assert.That(PlayingExplosions(), Is.Empty);
        }

        [Test]
        public void Play_StartsAtAnchor()
        {
            _anchor.position = Vector3.right * 10f;

            _pool.Play(_anchor, null);

            Assert.That(PlayingExplosions(), Has.Count.EqualTo(1));
            Assert.That(PlayingExplosions()[0].transform.position, Is.EqualTo(_anchor.position));
            Assert.That(Renderer(PlayingExplosions()[0]).forceRenderingOff, Is.False);
        }

        [Test]
        public void Tick_FollowsMovingAnchor()
        {
            _pool.Play(_anchor, null);
            _anchor.position = Vector3.forward * 25f;

            _pool.Tick();

            Assert.That(PlayingExplosions()[0].transform.position, Is.EqualTo(_anchor.position));
        }

        [Test]
        public void Tick_FollowsOwnerFogVisibility()
        {
            FakeFogVisibility fog = new FakeFogVisibility { IsHiddenByFog = true };
            _pool.Play(_anchor, fog);
            Assert.That(Renderer(PlayingExplosions()[0]).forceRenderingOff, Is.True);

            fog.IsHiddenByFog = false;
            _pool.Tick();

            Assert.That(Renderer(PlayingExplosions()[0]).forceRenderingOff, Is.False);
        }

        [Test]
        public void Tick_AfterAnchorDestroyed_KeepsLastPosition()
        {
            _anchor.position = Vector3.right * 5f;
            _pool.Play(_anchor, null);
            ExplosionVfx explosion = PlayingExplosions()[0];
            Object.DestroyImmediate(_anchor.gameObject);

            _pool.Tick();

            Assert.That(explosion.transform.position, Is.EqualTo(Vector3.right * 5f));
        }

        [Test]
        public void Tick_AfterAnchorDestroyed_StopsLoopingExplosionAndReusesIt()
        {
            _pool.Play(_anchor, null);
            ExplosionVfx looping = PlayingExplosions()[0];
            ParticleSystem particles = looping.GetComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.loop = true;
            Object.DestroyImmediate(_anchor.gameObject);

            _pool.Tick();

            Assert.That(particles.isEmitting, Is.False);

            _pool.Tick();
            for (int i = 0; i < PREWARM_COUNT; i++)
            {
                _pool.Play(Track(new GameObject("HardPoint")).transform, null);
            }

            Assert.That(Root().childCount, Is.EqualTo(PREWARM_COUNT));
        }

        [Test]
        public void Play_WhileEveryExplosionPlays_AddsOne()
        {
            for (int i = 0; i <= PREWARM_COUNT; i++)
            {
                _pool.Play(_anchor, null);
            }

            Assert.That(Root().childCount, Is.EqualTo(PREWARM_COUNT + 1));
            Assert.That(PlayingExplosions(), Has.Count.EqualTo(PREWARM_COUNT + 1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Tick_AfterAnchorOrParentDisabled_StopsLoopingExplosion(bool disableParent)
        {
            GameObject parent = Track(new GameObject("Owner"));
            _anchor.SetParent(parent.transform);
            _pool.Play(_anchor, null);
            ExplosionVfx explosion = PlayingExplosions()[0];
            (disableParent ? parent : _anchor.gameObject).SetActive(false);

            _pool.Tick();

            Assert.That(explosion.GetComponent<ParticleSystem>().isEmitting, Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void HardPoint_RestoredOrReenabledBeforeTick_RetiresOldFire(bool disable)
        {
            HardPoint hardPoint = _anchor.gameObject.AddComponent<HardPoint>();
            ContainerBuilder builder = new ContainerBuilder();
            builder.RegisterInstance<IHardPointExplosionPool>(_pool);
            builder.RegisterInstance<IFogVisibilityFacade>(new VisibleFogFacade());
            builder.RegisterComponent(hardPoint);
            using IObjectResolver container = builder.Build();
            hardPoint.UpdateData(0f);
            ExplosionVfx oldFire = PlayingExplosions()[0];

            if (disable)
            {
                GameObject owner = Track(new GameObject("Owner"));
                _anchor.SetParent(owner.transform);
                owner.SetActive(false);
                // EditMode does not dispatch lifecycle callbacks on ordinary runtime MonoBehaviours.
                typeof(HardPoint).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(hardPoint, null);
                owner.SetActive(true);
            }
            else
            {
                hardPoint.UpdateData(1f);
            }

            Assert.That(oldFire.GetComponent<ParticleSystem>().isEmitting, Is.False);
            hardPoint.UpdateData(0f);
            Assert.That(oldFire.GetComponent<ParticleSystem>().isEmitting, Is.False);
            Assert.That(PlayingExplosions().FindAll(e => e.GetComponent<ParticleSystem>().isEmitting), Has.Count.EqualTo(1));
        }

        [Test]
        public void Play_AfterExplosionFinished_ReusesIt()
        {
            _pool.Play(_anchor, null);
            ExplosionVfx finished = PlayingExplosions()[0];
            finished.GetComponent<ParticleSystem>().Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _pool.Tick();
            for (int i = 0; i < PREWARM_COUNT; i++)
            {
                _pool.Play(_anchor, null);
            }

            Assert.That(Root().childCount, Is.EqualTo(PREWARM_COUNT));
            Assert.That(finished.IsPlaying, Is.True);
        }

        private Transform Root() => GameObject.Find(nameof(HardPointExplosionPool)).transform;

        private static ParticleSystemRenderer Renderer(ExplosionVfx explosion) =>
            explosion.GetComponent<ParticleSystemRenderer>();

        private List<ExplosionVfx> PlayingExplosions()
        {
            List<ExplosionVfx> playing = new List<ExplosionVfx>();
            foreach (Transform child in Root())
            {
                ExplosionVfx explosion = child.GetComponent<ExplosionVfx>();
                if (explosion.IsPlaying)
                {
                    playing.Add(explosion);
                }
            }

            return playing;
        }

        private GameObject Track(GameObject gameObject)
        {
            _gameObjects.Add(gameObject);
            return gameObject;
        }

        private sealed class FakeFogVisibility : IFogVisibilityFacade
        {
            public bool IsHiddenByFog { get; set; }
        }
    }
}
