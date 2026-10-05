using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.SpawnBlocking;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class SpawnBlockerServiceTests
    {
        private GameObject _root;
        private SpawnBlockerService _blockers;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject(nameof(SpawnBlockerServiceTests));
            _blockers = new SpawnBlockerService(TestPlayers.CreateTeamGame());
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_root);

        [Test]
        public void IsBlocked_OwnedBlocker_BlocksHostilesOnly()
        {
            _blockers.Register(TestPlayers.Enemy, CreateSource(Vector3.zero), 100f);

            Assert.That(_blockers.IsBlocked(TestPlayers.Human, Vector3.zero), Is.True);
            Assert.That(_blockers.IsBlocked(TestPlayers.Ally, Vector3.zero), Is.True);
            Assert.That(_blockers.IsBlocked(TestPlayers.Enemy, Vector3.zero), Is.False);
            Assert.That(_blockers.IsBlocked(TestPlayers.SecondEnemy, Vector3.zero), Is.False);
        }

        [Test]
        public void IsBlocked_NeutralBlocker_BlocksEveryone()
        {
            _blockers.Register(PlayerId.None, CreateSource(Vector3.zero), 100f);

            Assert.That(_blockers.IsBlocked(TestPlayers.Human, Vector3.zero), Is.True);
            Assert.That(_blockers.IsBlocked(TestPlayers.Enemy, Vector3.zero), Is.True);
        }

        [Test]
        public void IsBlocked_OutsideRadius_False()
        {
            _blockers.Register(PlayerId.None, CreateSource(Vector3.zero), 100f);

            Assert.That(_blockers.IsBlocked(TestPlayers.Human, new Vector3(0f, 0f, 101f)), Is.False);
        }

        [Test]
        public void Register_CapturedRelay_OpensItForNewOwnerTeam()
        {
            Transform relay = CreateSource(Vector3.zero);
            _blockers.Register(PlayerId.None, relay, 100f);

            _blockers.Register(TestPlayers.Human, relay, 100f);

            Assert.That(_blockers.Blockers.Count, Is.EqualTo(1));
            Assert.That(_blockers.IsBlocked(TestPlayers.Human, Vector3.zero), Is.False);
            Assert.That(_blockers.IsBlocked(TestPlayers.Ally, Vector3.zero), Is.False);
            Assert.That(_blockers.IsBlocked(TestPlayers.Enemy, Vector3.zero), Is.True);
        }

        [Test]
        public void Unregister_RemovesBlock()
        {
            Transform source = CreateSource(Vector3.zero);
            _blockers.Register(PlayerId.None, source, 100f);

            _blockers.Unregister(source);

            Assert.That(_blockers.IsBlocked(TestPlayers.Human, Vector3.zero), Is.False);
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
