using System.Collections.Generic;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Squadrons;
using EmpireAtWar.Services.Vision;
using EmpireAtWar.ViewComponents.Health;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class FighterExplosionPoolTests
    {
        private const float VISION_RADIUS = 50f;
        private const int PREWARM_COUNT = 16;

        private readonly List<GameObject> _gameObjects = new List<GameObject>();
        private FighterExplosionPool _pool;

        [SetUp]
        public void SetUp()
        {
            PlayerRoster roster = TestPlayers.CreateDuel();
            VisionService vision = new VisionService(roster);
            GameObject visionSource = Track(new GameObject("VisionSource"));
            vision.Register(TestPlayers.Human, visionSource.transform, VISION_RADIUS);

            GameObject prefab = Track(new GameObject("FighterExplosionVfx"));
            ParticleSystem particles = prefab.AddComponent<ParticleSystem>();
            ParticleSystemRenderer prefabRenderer = prefab.GetComponent<ParticleSystemRenderer>();
            ExplosionVfx explosion = prefab.AddComponent<ExplosionVfx>();
            SerializedObject serialized = new SerializedObject(explosion);
            serialized.FindProperty("particles").objectReferenceValue = particles;
            SerializedProperty renderers = serialized.FindProperty("renderers");
            renderers.arraySize = 1;
            renderers.GetArrayElementAtIndex(0).objectReferenceValue = prefabRenderer;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            _pool = new FighterExplosionPool(explosion, vision, TestPlayers.CreateLocalPlayer(roster));
            _pool.Initialize();
            Track(GameObject.Find(nameof(FighterExplosionPool)));
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
        public void Play_InsideVision_IsDrawn()
        {
            Play(Vector3.right * 10f);

            Assert.That(PlayingRenderers(), Has.Count.EqualTo(1));
            Assert.That(PlayingRenderers()[0].forceRenderingOff, Is.False);
        }

        [Test]
        public void Play_InFog_IsHidden()
        {
            Play(Vector3.right * 500f);

            Assert.That(PlayingRenderers()[0].forceRenderingOff, Is.True);
        }

        [Test]
        public void Play_WhileEveryExplosionPlays_AddsOne()
        {
            for (int i = 0; i <= PREWARM_COUNT; i++)
            {
                Play(Vector3.zero);
            }

            Assert.That(Root().childCount, Is.EqualTo(PREWARM_COUNT + 1));
            Assert.That(PlayingRenderers(), Has.Count.EqualTo(PREWARM_COUNT + 1));
        }

        private void Play(Vector3 position) => _pool.Play(position);

        private Transform Root() => GameObject.Find(nameof(FighterExplosionPool)).transform;

        private List<ParticleSystemRenderer> PlayingRenderers()
        {
            List<ParticleSystemRenderer> playing = new List<ParticleSystemRenderer>();
            foreach (Transform explosion in Root())
            {
                if (explosion.GetComponent<ExplosionVfx>().IsPlaying)
                {
                    playing.Add(explosion.GetComponent<ParticleSystemRenderer>());
                }
            }

            return playing;
        }

        private GameObject Track(GameObject gameObject)
        {
            _gameObjects.Add(gameObject);
            return gameObject;
        }
    }
}
