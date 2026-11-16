using System.IO;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Saves;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    [Category("GameFlow")]
    public sealed class ControlGroupSaveTests
    {
        [Test]
        public void RoundTripPreservesZeroSlotOwnerAndLongEntityIdentities()
        {
            var save = new BattleSave
            {
                ControlGroups = new[] { ControlGroupSave.Create(0, new PlayerId(2), new[] { 9007199254740993L }) }
            };
            BattleSave restored = BattleSave.FromJson(save.ToJson());
            Assert.That(restored.ControlGroups, Has.Length.EqualTo(1));
            Assert.That(restored.ControlGroups[0].Slot, Is.Zero);
            Assert.That(restored.ControlGroups[0].Owner, Is.EqualTo(new PlayerId(2)));
            Assert.That(restored.ControlGroups[0].Members, Is.EqualTo(new[] { 9007199254740993L }));
        }

        [Test]
        public void LegacySaveWithoutGroupsStartsEmpty()
        {
            BattleSave restored = BattleSave.FromJson("{}");
            Assert.That(restored.ControlGroups, Is.Empty);
        }

        [TestCase(-1)]
        [TestCase(10)]
        public void InvalidSlotIsRejected(int slot)
        {
            Assert.Throws<InvalidDataException>(() => BattleSaveValidator.ValidateControlGroups(new[]
            {
                ControlGroupSave.Create(slot, new PlayerId(0), new long[] { 99 })
            }));
        }

        [Test]
        public void DuplicateSlotPerOwnerIsRejectedButDifferentOwnersCanShareNumber()
        {
            var group = ControlGroupSave.Create(0, new PlayerId(0), new long[] { 99 });
            Assert.Throws<InvalidDataException>(() => BattleSaveValidator.ValidateControlGroups(new[] { group, group }));
            Assert.DoesNotThrow(() => BattleSaveValidator.ValidateControlGroups(new[]
            {
                group, ControlGroupSave.Create(0, new PlayerId(1), new long[] { 100 })
            }));
        }
    }
}
