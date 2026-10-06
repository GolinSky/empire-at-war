using EmpireAtWar.Components.Ui.Tooltip;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class TooltipTriggerTests
    {
        private GameObject _target;
        private TooltipTrigger _trigger;
        private object _lastKey;

        private int _starts;
        private int _ends;

        [SetUp]
        public void SetUp()
        {
            // NUnit reuses the fixture instance, so per-test counters must start from zero.
            _starts = 0;
            _ends = 0;
            _lastKey = null;
            _target = new GameObject("Tooltip target", typeof(RectTransform), typeof(TooltipTrigger));
            _trigger = _target.GetComponent<TooltipTrigger>();
            var serialized = new SerializedObject(_trigger);
            serialized.FindProperty("anchorRect").objectReferenceValue = _target.transform;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            _trigger.HoverStarted += (key, anchor, source) => { _starts++; _lastKey = key; };
            _trigger.HoverEnded += source => _ends++;
            _trigger.SetKey("first");
            _trigger.OnPointerEnter(new PointerEventData(null));
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_target);

        [Test]
        public void RefreshingSameCardKeyPreservesHover()
        {
            _trigger.SetKey("first");
            Assert.That(_starts, Is.EqualTo(1));
            Assert.That(_ends, Is.Zero);
        }

        [Test]
        public void RecycledCardStartsHoverForNewKeyWithoutPointerMovement()
        {
            _trigger.SetKey("second");
            Assert.That(_starts, Is.EqualTo(2));
            Assert.That(_ends, Is.EqualTo(1));
            Assert.That(_lastKey, Is.EqualTo("second"));
        }

        [Test]
        public void DisabledSourceEndsHover()
        {
            // Edit Mode skips OnDisable for components without [ExecuteAlways], so raise it directly.
            typeof(TooltipTrigger).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_trigger, null);
            Assert.That(_ends, Is.EqualTo(1));
        }
    }
}
