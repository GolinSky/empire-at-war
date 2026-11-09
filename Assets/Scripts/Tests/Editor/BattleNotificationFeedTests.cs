using System.Collections.Generic;
using EmpireAtWar.Services.BattleNotifications;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Ui")]
    public sealed class BattleNotificationFeedTests
    {
        private BattleNotificationFeed _feed;
        private List<string> _shown;
        private List<string> _hidden;

        [SetUp]
        public void SetUp()
        {
            _feed = new BattleNotificationFeed();
            _feed.SetAccepting(true);
            _shown = new List<string>();
            _hidden = new List<string>();
            _feed.Shown += notification => _shown.Add(notification.Text);
            _feed.Hidden += notification => _hidden.Add(notification.Text);
        }

        [Test]
        public void Post_BeforeBattleRuns_IgnoresGameplayButKeepsOutcome()
        {
            _feed.SetAccepting(false);
            _feed.Post(BattleNotificationPriority.Urgent, "Station under attack");
            _feed.Post(BattleNotificationPriority.Outcome, "Victory");

            Assert.That(_shown, Is.EqualTo(new[] { "Victory" }));
        }

        [Test]
        public void Post_FullStack_UrgentReplacesProgressAndOthersWaitByPriority()
        {
            _feed.Post(BattleNotificationPriority.Tactical, "A");
            _feed.Post(BattleNotificationPriority.Progress, "B");
            _feed.Post(BattleNotificationPriority.Tactical, "C");
            _feed.Post(BattleNotificationPriority.Urgent, "D");
            _feed.Post(BattleNotificationPriority.Progress, "E");
            _feed.Post(BattleNotificationPriority.Tactical, "F");

            Assert.That(_hidden, Is.EqualTo(new[] { "B" }));
            Assert.That(_feed.Visible, Has.Count.EqualTo(BattleNotificationFeed.MAX_VISIBLE));
            Assert.That(_feed.Pending[0].Text, Is.EqualTo("F"));
            Assert.That(_feed.Pending[1].Text, Is.EqualTo("E"));
        }

        [Test]
        public void Post_SameGroupWithinWindow_MergesIntoCountedCard()
        {
            var updated = new List<string>();
            _feed.Updated += notification => updated.Add(notification.Text);

            _feed.Post(BattleNotificationPriority.Tactical, "X-wing squadron lost", "loss", "{0} squadrons lost");
            _feed.Post(BattleNotificationPriority.Tactical, "Y-wing squadron lost", "loss", "{0} squadrons lost");
            _feed.Advance(BattleNotificationFeed.GROUP_WINDOW + 0.1f);
            _feed.Post(BattleNotificationPriority.Tactical, "Z-95 squadron lost", "loss", "{0} squadrons lost");

            Assert.That(updated, Is.EqualTo(new[] { "2 squadrons lost" }));
            Assert.That(_shown, Is.EqualTo(new[] { "X-wing squadron lost", "Z-95 squadron lost" }));
        }

        [Test]
        public void Advance_ExpiredCard_HidesItAndShowsWaitingCard()
        {
            _feed.Post(BattleNotificationPriority.Urgent, "A");
            _feed.Post(BattleNotificationPriority.Urgent, "B");
            _feed.Post(BattleNotificationPriority.Tactical, "C");
            _feed.Post(BattleNotificationPriority.Tactical, "D");

            _feed.Advance(6.5f);

            Assert.That(_hidden, Is.EqualTo(new[] { "C" }));
            Assert.That(_shown, Is.EqualTo(new[] { "A", "B", "C", "D" }));
        }

        [Test]
        public void Dismiss_VisibleCard_HidesItAndPromotesNext()
        {
            _feed.Post(BattleNotificationPriority.Urgent, "A");
            _feed.Post(BattleNotificationPriority.Urgent, "B");
            _feed.Post(BattleNotificationPriority.Urgent, "C");
            _feed.Post(BattleNotificationPriority.Tactical, "D");

            _feed.Dismiss(_feed.Visible[0].Id);

            Assert.That(_hidden, Is.EqualTo(new[] { "A" }));
            Assert.That(_shown[^1], Is.EqualTo("D"));
        }

        [Test]
        public void EndBattle_KeepsOutcomeAndBlocksGameplay()
        {
            _feed.Post(BattleNotificationPriority.Urgent, "Station under attack");
            _feed.Post(BattleNotificationPriority.Outcome, "Defeat");

            _feed.EndBattle();
            _feed.Post(BattleNotificationPriority.Progress, "Unit deployed");

            Assert.That(_hidden, Is.EqualTo(new[] { "Station under attack" }));
            Assert.That(_feed.Visible, Has.Count.EqualTo(1));
            Assert.That(_feed.Visible[0].Text, Is.EqualTo("Defeat"));
        }
    }
}
