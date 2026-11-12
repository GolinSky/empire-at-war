using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.SpawnBlocking;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Navigation")]
    public sealed class BattleAreaServiceTests
    {
        private GameObject _root;
        private BattleAreaService _blockers;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject(nameof(BattleAreaServiceTests));
            _blockers = new BattleAreaService(TestPlayers.CreateTeamGame());
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_root);

        [Test]
        public void IsBlocked_OwnedBlocker_BlocksHostilesOnly()
        {
            _blockers.Register(BattleAreaLayer.SpawnBlock, TestPlayers.Enemy, CreateSource(Vector3.zero), 100f);

            Assert.That(_blockers.IsBlocked(TestPlayers.Human, Vector3.zero), Is.True);
            Assert.That(_blockers.IsBlocked(TestPlayers.Ally, Vector3.zero), Is.True);
            Assert.That(_blockers.IsBlocked(TestPlayers.Enemy, Vector3.zero), Is.False);
            Assert.That(_blockers.IsBlocked(TestPlayers.SecondEnemy, Vector3.zero), Is.False);
        }

        [Test]
        public void IsBlocked_NeutralBlocker_BlocksEveryone()
        {
            _blockers.Register(BattleAreaLayer.SpawnBlock, PlayerId.None, CreateSource(Vector3.zero), 100f);

            Assert.That(_blockers.IsBlocked(TestPlayers.Human, Vector3.zero), Is.True);
            Assert.That(_blockers.IsBlocked(TestPlayers.Enemy, Vector3.zero), Is.True);
        }

        [Test]
        public void IsBlocked_OutsideRadius_False()
        {
            _blockers.Register(BattleAreaLayer.SpawnBlock, PlayerId.None, CreateSource(Vector3.zero), 100f);

            Assert.That(_blockers.IsBlocked(TestPlayers.Human, new Vector3(0f, 0f, 101f)), Is.False);
        }

        [Test]
        public void Register_CapturedRelay_OpensItForNewOwnerTeam()
        {
            Transform relay = CreateSource(Vector3.zero);
            _blockers.Register(BattleAreaLayer.SpawnBlock, PlayerId.None, relay, 100f);

            _blockers.Register(BattleAreaLayer.SpawnBlock, TestPlayers.Human, relay, 100f);

            Assert.That(_blockers.Get(BattleAreaLayer.SpawnBlock).Count, Is.EqualTo(1));
            Assert.That(_blockers.IsBlocked(TestPlayers.Human, Vector3.zero), Is.False);
            Assert.That(_blockers.IsBlocked(TestPlayers.Ally, Vector3.zero), Is.False);
            Assert.That(_blockers.IsBlocked(TestPlayers.Enemy, Vector3.zero), Is.True);
        }

        [Test]
        public void Unregister_RemovesBlock()
        {
            Transform source = CreateSource(Vector3.zero);
            _blockers.Register(BattleAreaLayer.SpawnBlock, PlayerId.None, source, 100f);

            _blockers.Unregister(BattleAreaLayer.SpawnBlock, source);

            Assert.That(_blockers.IsBlocked(TestPlayers.Human, Vector3.zero), Is.False);
        }

        [Test]
        public void OrderSnap_UsesFirstMatchingSourceAndSurvivesOtherLayerRemoval()
        {
            Transform first = CreateSource(new Vector3(5f, 2f, 0f));
            Transform second = CreateSource(new Vector3(-5f, -1f, 0f));
            _blockers.Register(BattleAreaLayer.OrderSnap, PlayerId.None, first, 5f);
            _blockers.Register(BattleAreaLayer.OrderSnap, PlayerId.None, second, 5f);
            _blockers.Register(BattleAreaLayer.StructureExclusion, PlayerId.None, first, 5f);
            _blockers.Unregister(BattleAreaLayer.StructureExclusion, first);

            Assert.That(_blockers.TryGetOrderSnap(Vector3.zero, out Vector3 center), Is.True);
            Assert.That(center, Is.EqualTo(first.position));
            _blockers.Unregister(BattleAreaLayer.OrderSnap, first);
            Assert.That(_blockers.TryGetOrderSnap(Vector3.zero, out center), Is.True);
            Assert.That(center, Is.EqualTo(second.position));
            Assert.That(_blockers.TryGetOrderSnap(Vector3.right * 20f, out _), Is.False);
        }

        private Transform CreateSource(Vector3 position)
        {
            Transform source = new GameObject("Blocker").transform;
            source.SetParent(_root.transform);
            source.position = position;
            return source;
        }
    }
}
