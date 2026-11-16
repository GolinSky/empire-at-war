using EmpireAtWar.Models.Players;
using System.Collections.Generic;
using EmpireAtWar.Entities.Ship.EntityFacades;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Services.Vision;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Vision")]
    public sealed class VisionServiceTests
    {
        private GameObject _root;
        private VisionService _vision;
        private float _now;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject(nameof(VisionServiceTests));
            _now = 0f;
            _vision = new VisionService(TestPlayers.CreateTeamGame(), true, () => _now);
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
        public void Register_AfterQueryInSameFrame_IsSeen()
        {
            Assert.That(_vision.IsVisible(TestPlayers.Human, Vector3.zero), Is.False);

            _vision.Register(TestPlayers.Human, CreateSource(Vector3.zero), 100f);

            Assert.That(_vision.IsVisible(TestPlayers.Human, Vector3.zero), Is.True);
            Assert.That(_vision.IsVisible(TestPlayers.Enemy, Vector3.zero), Is.False);
        }

        [Test]
        public void Unregister_AfterQueryInSameFrame_RemovesVision()
        {
            Transform source = CreateSource(Vector3.zero);
            _vision.Register(TestPlayers.Human, source, 100f);
            Assert.That(_vision.IsVisible(TestPlayers.Human, Vector3.zero), Is.True);

            _vision.Unregister(source);

            Assert.That(_vision.IsVisible(TestPlayers.Human, Vector3.zero), Is.False);
        }

        [Test]
        public void Unregister_RemovesVision()
        {
            Transform source = CreateSource(Vector3.zero);
            _vision.Register(TestPlayers.Human, source, 100f);

            _vision.Unregister(source);

            Assert.That(_vision.IsVisible(TestPlayers.Human, Vector3.zero), Is.False);
        }

        [Test]
        public void IsVisible_HullEdgeOutsideCenter_UsesShapeAndAlliedSources()
        {
            _vision.Register(TestPlayers.Ally, CreateSource(Vector3.zero), 2f);
            Transform target = CreateSource(new Vector3(0f, -500f, 10f));
            var footprint = new ShipVisionFootprint(target, 42, new[]
            {
                new Vector2(-4f, 5f), new Vector2(4f, 5f), new Vector2(0f, -9f)
            });
            Assert.That(_vision.IsVisible(TestPlayers.Human, target.position), Is.False);
            Assert.That(_vision.IsVisible(TestPlayers.Human, footprint), Is.True);
            Assert.That(_vision.IsVisible(TestPlayers.Enemy, footprint), Is.False);

            target.rotation = Quaternion.Euler(0f, 90f, 0f);
            Assert.That(_vision.IsVisible(TestPlayers.Human, footprint), Is.False);
        }

        [Test]
        public void FiringReveal_IsSharedOnlyWithVictimTeam_ExpiresAfterLastShot()
        {
            var footprint = new ShipVisionFootprint(CreateSource(Vector3.zero), 42, new[]
            {
                new Vector2(-1f, -1f), new Vector2(1f, -1f), new Vector2(0f, 1f)
            });
            var transitions = new List<bool>();
            _vision.RevealChanged += (_, _, active) => transitions.Add(active);
            _vision.Reveal(TestPlayers.Human, 42, 5f);
            Assert.That(_vision.IsVisible(TestPlayers.Human, footprint), Is.True);
            Assert.That(_vision.IsVisible(TestPlayers.Ally, footprint), Is.True);
            Assert.That(_vision.IsVisible(TestPlayers.Enemy, footprint), Is.False);
            Assert.That(_vision.IsVisible(TestPlayers.Human, Vector3.zero), Is.False,
                "Firing reveals the ship, not nearby space or other entities.");

            _now = 4f;
            _vision.Reveal(TestPlayers.Human, 42, 5f);
            _now = 5f;
            _vision.Tick();
            Assert.That(_vision.IsVisible(TestPlayers.Human, footprint), Is.True);
            Assert.That(transitions, Is.EqualTo(new[] { true }), "Shots refresh without extra transitions.");
            _now = 9f;
            _vision.Tick();
            Assert.That(_vision.IsVisible(TestPlayers.Human, footprint), Is.False);
            Assert.That(transitions, Is.EqualTo(new[] { true, false }));
        }

        [Test]
        public void FiringReveal_ExpiringOneVictimDoesNotRemoveAnotherTeamsReveal()
        {
            var footprint = new ShipVisionFootprint(CreateSource(Vector3.zero), 42, new[]
            {
                new Vector2(-1f, -1f), new Vector2(1f, -1f), new Vector2(0f, 1f)
            });
            _vision.Reveal(TestPlayers.Human, 42, 5f);
            _now = 2f;
            _vision.Reveal(TestPlayers.Enemy, 42, 5f);
            _now = 5f;
            _vision.Tick();
            Assert.That(_vision.IsVisible(TestPlayers.Human, footprint), Is.False);
            Assert.That(_vision.IsVisible(TestPlayers.Enemy, footprint), Is.True);
            _vision.ForgetReveals(42);
            Assert.That(_vision.IsVisible(TestPlayers.Enemy, footprint), Is.False);
        }

        [Test]
        public void EntityWithoutHull_RevealSharesWithAlliesAndExpiresWithoutRevealingNearbySpace()
        {
            var target = new Entity(null, null,
                new IEntityFacade[] { new EntityTransformFacade(_root.transform) }, TestPlayers.Enemy, 42, null, null);
            var neighbor = new Entity(null, null,
                new IEntityFacade[] { new EntityTransformFacade(_root.transform) }, TestPlayers.Enemy, 43, null, null);
            Assert.That(_vision.IsVisible(TestPlayers.Human, target), Is.False);
            _vision.Reveal(TestPlayers.Human, target.Id, 5f);
            Assert.That(_vision.IsVisible(TestPlayers.Human, target), Is.True);
            Assert.That(_vision.IsVisible(TestPlayers.Ally, target), Is.True);
            Assert.That(_vision.IsVisible(TestPlayers.Enemy, target), Is.False);
            Assert.That(_vision.IsVisible(TestPlayers.Human, neighbor), Is.False);
            Assert.That(_vision.IsVisible(TestPlayers.Human, _root.transform.position), Is.False);
            _now = 5f;
            _vision.Tick();
            Assert.That(_vision.IsVisible(TestPlayers.Human, target), Is.False);
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
