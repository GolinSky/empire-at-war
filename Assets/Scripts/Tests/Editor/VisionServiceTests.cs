using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Vision;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class VisionServiceTests
    {
        private GameObject _root;
        private VisionService _vision;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject(nameof(VisionServiceTests));
            _vision = new VisionService(TestPlayers.CreateTeamGame());
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_root);

        [Test]
        public void IsVisible_InsideOwnSource_True()
        {
            _vision.Register(TestPlayers.Human, CreateSource(Vector3.zero), 100f);

            Assert.That(_vision.IsVisible(TestPlayers.Human, new Vector3(99f, 0f, 0f)), Is.True);
            Assert.That(_vision.IsVisible(TestPlayers.Human, new Vector3(101f, 0f, 0f)), Is.False);
        }

        [Test]
        public void IsVisible_IgnoresHeight()
        {
            _vision.Register(TestPlayers.Human, CreateSource(new Vector3(0f, -300f, 0f)), 100f);

            Assert.That(_vision.IsVisible(TestPlayers.Human, new Vector3(50f, 200f, 50f)), Is.True);
        }

        [Test]
        public void IsVisible_AllySharesVision_EnemyDoesNot()
        {
            _vision.Register(TestPlayers.Ally, CreateSource(Vector3.zero), 100f);

            Assert.That(_vision.IsVisible(TestPlayers.Human, Vector3.zero), Is.True);
            Assert.That(_vision.IsVisible(TestPlayers.Enemy, Vector3.zero), Is.False);
        }

        [Test]
        public void IsAreaVisible_SourceReachesFootprintEdge_True()
        {
            _vision.Register(TestPlayers.Human, CreateSource(Vector3.zero), 100f);

            Assert.That(_vision.IsVisible(TestPlayers.Human, new Vector3(350f, 0f, 0f)), Is.False);
            Assert.That(_vision.IsAreaVisible(TestPlayers.Human, new Vector3(350f, 0f, 0f), 260f), Is.True);
            Assert.That(_vision.IsAreaVisible(TestPlayers.Human, new Vector3(350f, 0f, 0f), 240f), Is.False);
            Assert.That(_vision.IsAreaVisible(TestPlayers.Enemy, new Vector3(350f, 0f, 0f), 260f), Is.False);
        }

        [Test]
        public void Register_SameTransformAgain_UpdatesRadius()
        {
            Transform source = CreateSource(Vector3.zero);
            _vision.Register(TestPlayers.Human, source, 10f);

            _vision.Register(TestPlayers.Human, source, 100f);

            Assert.That(_vision.Sources.Count, Is.EqualTo(1));
            Assert.That(_vision.IsVisible(TestPlayers.Human, new Vector3(50f, 0f, 0f)), Is.True);
        }

        [Test]
        public void Unregister_RemovesVision()
        {
            Transform source = CreateSource(Vector3.zero);
            _vision.Register(TestPlayers.Human, source, 100f);

            _vision.Unregister(source);

            Assert.That(_vision.IsVisible(TestPlayers.Human, Vector3.zero), Is.False);
        }

        private Transform CreateSource(Vector3 position)
        {
            Transform source = new GameObject("Source").transform;
            source.SetParent(_root.transform);
            source.position = position;
            return source;
        }
    }
}
