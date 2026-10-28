using System.Linq;
using System.Threading;
using EmpireAtWar.Services.Input;
using NUnit.Framework;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace EmpireAtWar.Tests.Editor
{
    [Category("BattleInput")]
    public sealed class InputBindingServiceTests
    {
        private const int MATCH_WAIT_MILLISECONDS = 150;

        private InputActionsProvider _provider;
        private InputBindingService _service;
        private Keyboard _keyboard;

        [SetUp]
        public void SetUp()
        {
            _provider = new InputActionsProvider();
            _service = new InputBindingService(_provider);
        }

        [TearDown]
        public void TearDown()
        {
            _service.Dispose();
            _provider.Dispose();
            if (_keyboard != null)
            {
                InputSystem.RemoveDevice(_keyboard);
                _keyboard = null;
            }
        }

        [Test]
        public void ModifierShortcuts_AreOneRowShowingTheWholeChord()
        {
            Assert.That(Display("Select Visible"), Is.EqualTo("Ctrl+A"));
            Assert.That(Display("Select All"), Is.EqualTo("Ctrl+Shift+A"));
            Assert.That(_service.RebindableSlots.Count(slot => slot.Action.name == "SelectAll"), Is.EqualTo(1));
        }

        [Test]
        public void Labels_AreUniqueAndNeverNameDefaultKeys()
        {
            string[] labels = _service.RebindableSlots.Select(slot => slot.Label).ToArray();

            Assert.That(labels, Is.Unique);
            Assert.That(labels, Has.None.Contains("WASD").And.None.Contains("Ctrl").And.None.Contains("Modifier"));
            Assert.That(labels, Does.Contain("Move Up").And.Contain("Move Up (Alt)").And.Contain("Move Left (Alt 2)"));
        }

        [Test]
        public void Chords_ConflictOnlyWithTheExactSameKeys()
        {
            // Ctrl+A, Ctrl+Shift+A and Move Left (A) share keys but none needs exactly the same set.
            Assert.That(_service.HasConflicts(), Is.False);

            BindingSlot selectAll = Slot("Select All");
            BindingSlot selectVisible = Slot("Select Visible");
            selectVisible.Action.ApplyBindingOverride(selectVisible.FirstPartIndex, "<Keyboard>/shift");
            Assert.That(_service.HasConflicts(), Is.False, "Shift+A differs from Ctrl+Shift+A.");

            selectAll.Action.ApplyBindingOverride(selectAll.FirstPartIndex, "<Keyboard>/shift");
            Assert.That(Display("Select All"), Is.EqualTo("Shift+Shift+A"));
            Assert.That(_service.HasConflicts(), Is.False, "Shift+Shift+A has three parts, Shift+A two.");
        }

        [Test]
        public void ResetBinding_RestoresEveryChordPart()
        {
            BindingSlot selectAll = Slot("Select All");
            selectAll.Action.ApplyBindingOverride(selectAll.FirstPartIndex, "<Keyboard>/alt");
            selectAll.Action.ApplyBindingOverride(selectAll.KeyPartIndex, "<Keyboard>/b");
            Assert.That(Display("Select All"), Is.EqualTo("Alt+Shift+B"));

            _service.ResetBinding(selectAll);

            Assert.That(Display("Select All"), Is.EqualTo("Ctrl+Shift+A"));
        }

        [Test]
        public void ChordRebind_CapturesHeldModifiersWithTheKey()
        {
            RebindResult result = Rebind("Select Visible", Key.LeftShift, Key.B);

            Assert.That(result, Is.EqualTo(RebindResult.Completed));
            Assert.That(Display("Select Visible"), Is.EqualTo("Shift+B"));

            Assert.That(Rebind("Select All", Key.RightAlt, Key.LeftCtrl, Key.G), Is.EqualTo(RebindResult.Completed));
            Assert.That(Display("Select All"), Is.EqualTo("Ctrl+Alt+G"));
            Assert.That(_service.HasConflicts(), Is.False);
        }

        [Test]
        public void ChordRebind_WithWrongModifierCount_LeavesTheChordUnchanged()
        {
            RebindResult result = Rebind("Select All", Key.LeftCtrl, Key.B);

            Assert.That(result, Is.EqualTo(RebindResult.WrongModifierCount));
            Assert.That(Display("Select All"), Is.EqualTo("Ctrl+Shift+A"));
        }

        [Test]
        public void SingleKeyRebind_ConflictCancel_RestoresPreviousKey()
        {
            Assert.That(Rebind("Queue Waypoint", Key.W), Is.EqualTo(RebindResult.Conflict));
            Assert.That(_service.PendingConflicts.Single().Label, Is.EqualTo("Move Up"));

            Assert.That(_service.ResolveConflict(ConflictResolution.Cancel), Is.True);

            Assert.That(Display("Queue Waypoint"), Is.EqualTo("Alt"));
            Assert.That(_service.HasConflicts(), Is.False);
        }

        [Test]
        public void SingleKeyRebind_ConflictSwap_GivesTheOtherSlotThePreviousKey()
        {
            Assert.That(Rebind("Queue Waypoint", Key.W), Is.EqualTo(RebindResult.Conflict));

            Assert.That(_service.ResolveConflict(ConflictResolution.Swap), Is.True);

            Assert.That(Display("Queue Waypoint"), Is.EqualTo("W"));
            Assert.That(Display("Move Up"), Is.EqualTo("Alt"));
            Assert.That(_service.HasConflicts(), Is.False);
        }

        [Test]
        public void SingleKeyRebind_ConflictReplace_UnbindsTheOtherSlot()
        {
            Assert.That(Rebind("Queue Waypoint", Key.W), Is.EqualTo(RebindResult.Conflict));

            Assert.That(_service.ResolveConflict(ConflictResolution.Replace), Is.True);

            Assert.That(Display("Queue Waypoint"), Is.EqualTo("W"));
            Assert.That(Display("Move Up"), Is.EqualTo("Unbound"));
        }

        [Test]
        public void Chord_WithAnUnboundPart_IsShownUnboundAndNeverConflicts()
        {
            BindingSlot selectAll = Slot("Select All");
            selectAll.Action.ApplyBindingOverride(selectAll.KeyPartIndex, string.Empty);
            BindingSlot selectVisible = Slot("Select Visible");
            selectVisible.Action.ApplyBindingOverride(selectVisible.KeyPartIndex, string.Empty);

            Assert.That(Display("Select All"), Is.EqualTo("Unbound"));
            Assert.That(_service.HasConflicts(), Is.False);
        }

        /// <summary>Presses the keys in order, holding each, then releases all once the capture completes.</summary>
        private RebindResult Rebind(string label, params Key[] keys)
        {
            if (_keyboard == null)
            {
                _keyboard = InputSystem.AddDevice<Keyboard>();
            }

            // Chord capture reads modifiers from Keyboard.current, which is the Editor's real keyboard otherwise.
            _keyboard.MakeCurrent();
            RebindResult? result = null;
            _service.StartRebind(Slot(label), completed => result = completed);
            for (int i = 1; i <= keys.Length; i++)
            {
                InputSystem.QueueStateEvent(_keyboard, new KeyboardState(keys.Take(i).ToArray()));
                InputSystem.Update();
            }

            // Interactive rebinding completes only after its short wait for another match has passed.
            Thread.Sleep(MATCH_WAIT_MILLISECONDS);
            InputSystem.Update();
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            InputSystem.Update();
            Assert.That(result, Is.Not.Null, $"Rebinding {label} did not complete.");
            return result.Value;
        }

        // Without a live device the layout names Ctrl "Control"; the game shows the device name "Ctrl".
        private string Display(string label)
        {
            return _service.GetBindingDisplayString(Slot(label)).Replace("Control", "Ctrl");
        }

        private BindingSlot Slot(string label)
        {
            return _service.RebindableSlots.Single(slot => slot.Label == label);
        }
    }
}
