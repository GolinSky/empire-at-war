using EmpireAtWar.Services.Saves;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class AutosaveScheduleTests
    {
        [Test]
        public void QuickSaveAndLoad_AreRebindableBattleActions()
        {
            using (var provider = new EmpireAtWar.Services.Input.InputActionsProvider())
            {
                var bindings = new EmpireAtWar.Services.Input.InputBindingService(provider);
                foreach (var pair in new[] { ("QuickSave", "<Keyboard>/f5"), ("QuickLoad", "<Keyboard>/f9") })
                {
                    var action = provider.Actions.asset.FindAction("Battle/" + pair.Item1, true);
                    Assert.That(action.bindings[0].path, Is.EqualTo(pair.Item2));
                    Assert.That(System.Linq.Enumerable.Any(bindings.RebindableSlots, slot => slot.Action == action), Is.True);
                }
                bindings.Dispose();
            }
        }

        [Test]
        public void LoadedClock_StartsFullInterval_AndDoesNotRepeatAtSameTime()
        {
            var schedule = new AutosaveSchedule();
            Assert.That(schedule.TakeDueSave(720f, true), Is.False);
            Assert.That(schedule.TakeDueSave(1019f, true), Is.False);
            Assert.That(schedule.TakeDueSave(1020f, true), Is.True);
            Assert.That(schedule.TakeDueSave(1020f, true), Is.False);
            Assert.That(schedule.TakeDueSave(1320f, true), Is.True);
        }

        [Test]
        public void BusySave_DefersWaveSave_AndResetsInterval()
        {
            var schedule = new AutosaveSchedule();
            schedule.TakeDueSave(0f, true);
            schedule.RequestWaveSave();
            Assert.That(schedule.TakeDueSave(100f, false), Is.False);
            Assert.That(schedule.TakeDueSave(101f, true), Is.True);
            Assert.That(schedule.TakeDueSave(300f, true), Is.False);
            Assert.That(schedule.TakeDueSave(401f, true), Is.True);
        }
    }
}
