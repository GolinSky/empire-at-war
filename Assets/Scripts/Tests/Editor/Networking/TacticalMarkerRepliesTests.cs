using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.TacticalMarkers;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Networking")]
    public sealed class TacticalMarkerRepliesTests
    {
        [Test]
        public void AuthorityRejectsEnemyAcknowledgement()
        {
            var roster = Roster();
            var replies = new TacticalMarkerReplies(new Session(true), roster);
            int received = 0;
            replies.Replied += _ => received++;
            var marker = new TacticalMarker(1, TacticalMarkerType.Attack, new PlayerId(0), new TeamId(0),
                Vector3.zero, -1, 0, 1);
            replies.Reply(marker, new PlayerId(2), TacticalMarkerReplyStatus.Accepted);
            Assert.That(received, Is.Zero);
            replies.Reply(marker, new PlayerId(1), TacticalMarkerReplyStatus.Accepted);
            Assert.That(received, Is.EqualTo(1));
        }

        [Test]
        public void ClientMirrorRejectsOtherTeamReplies()
        {
            var replies = new TacticalMarkerReplies(new Session(false), Roster());
            int received = 0;
            replies.Replied += _ => received++;
            replies.Receive(new TacticalMarkerReply(1, new PlayerId(2), new PlayerId(2), new TeamId(1),
                TacticalMarkerReplyStatus.Captured, Vector3.zero, TacticalMarkerType.Capture));
            Assert.That(received, Is.Zero);
            replies.Receive(new TacticalMarkerReply(1, new PlayerId(0), new PlayerId(1), new TeamId(0),
                TacticalMarkerReplyStatus.Accepted, Vector3.zero, TacticalMarkerType.Attack));
            Assert.That(received, Is.EqualTo(1));
        }

        private static PlayerRoster Roster() => new PlayerRoster(new[]
        {
            new PlayerSlot(new PlayerId(0), new TeamId(0), default, PlayerController.Human, EnemyAiDifficulty.Easy, 0),
            new PlayerSlot(new PlayerId(1), new TeamId(0), default, PlayerController.Ai, EnemyAiDifficulty.Easy, 1),
            new PlayerSlot(new PlayerId(2), new TeamId(1), default, PlayerController.Ai, EnemyAiDifficulty.Easy, 2)
        });

        private sealed class Session : IMatchSession
        {
            public bool IsOnline => true;
            public bool IsAuthority { get; }
            public PlayerId LocalPlayerId => new PlayerId(0);
            public Session(bool authority) { IsAuthority = authority; }
        }
    }
}
