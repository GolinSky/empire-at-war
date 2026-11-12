using EmpireAtWar.Services.BattleNotifications;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Ui")]
    public sealed class MarkerNotificationTests
    {
        [Test]
        public void PendingMarkerRetainsItsFocusActionWhenPromoted()
        {
            var feed = new BattleNotificationFeed(); feed.SetAccepting(true);
            for (int i = 0; i < BattleNotificationFeed.MAX_VISIBLE; i++)
                feed.Post(BattleNotificationPriority.Tactical, "Existing");
            bool focused = false;
            feed.Post(BattleNotificationPriority.Progress, "Attack here", iconKey: "MarkerAttack", onActivate: () => focused = true);
            var marker = feed.Pending[0];
            feed.Dismiss(feed.Visible[0].Id);
            Assert.That(feed.Visible, Does.Contain(marker));
            marker.OnActivate();
            Assert.That(focused, Is.True);
            Assert.That(marker.IconKey, Is.EqualTo("MarkerAttack"));
        }
    }
}
