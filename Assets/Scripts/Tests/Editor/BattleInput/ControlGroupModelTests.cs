using EmpireAtWar.Services.ControlGroups;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    [Category("BattleInput")]
    public sealed class ControlGroupModelTests
    {
        [Test]
        public void AssignmentDeduplicatesAndSnapshotsWithoutRemovingOverlappingGroups()
        {
            var model = new ControlGroupModel();
            var input = new long[] { 7, 8, 7 };
            model.Replace(1, input);
            model.Replace(0, new long[] { 7 });
            input[0] = 99;
            CollectionAssert.AreEqual(new long[] { 7, 8 }, model.GetMembers(1));
            CollectionAssert.AreEqual(new long[] { 7 }, model.GetMembers(0));
        }

        [Test]
        public void IdenticalMembershipIgnoresOrderAndDoesNotPublishChange()
        {
            var model = new ControlGroupModel();
            model.Replace(3, new long[] { 1, 2 });
            int changes = 0;
            model.Changed += () => changes++;
            model.Replace(3, new long[] { 2, 1, 1 });
            Assert.That(model.HasSameMembers(3, new long[] { 2, 1 }), Is.True);
            Assert.That(changes, Is.Zero);
        }

        [Test]
        public void RemovalPrunesEverySquadAndClearingLeavesAllTenSlotsEmpty()
        {
            var model = new ControlGroupModel();
            for (int slot = 0; slot < ControlGroupModel.SLOT_COUNT; slot++)
                model.Replace(slot, new long[] { 1, 2 });
            model.Remove(1);
            for (int slot = 0; slot < ControlGroupModel.SLOT_COUNT; slot++)
                CollectionAssert.AreEqual(new long[] { 2 }, model.GetMembers(slot));
            model.Clear();
            for (int slot = 0; slot < ControlGroupModel.SLOT_COUNT; slot++)
                Assert.That(model.GetMembers(slot), Is.Empty);
        }

        [Test]
        public void PruneCommitsBeforeNotifyingObservers()
        {
            var model = new ControlGroupModel();
            model.Replace(1, new long[] { 1, 2, 3 });
            int changes = 0;
            model.Changed += () =>
            {
                changes++;
                model.Prune(1, id => id != 3);
            };
            model.Prune(1, id => id != 3);
            CollectionAssert.AreEqual(new long[] { 3 }, model.GetMembers(1));
            Assert.That(changes, Is.EqualTo(1));
        }
    }
}
