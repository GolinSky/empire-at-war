using EmpireAtWar.Components.Ui.Tooltip;
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
        private int _starts;
        private int _ends;
        private object _lastKey;

        [SetUp]
        public void SetUp()
        {
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
            _target.SetActive(false);
            Assert.That(_ends, Is.EqualTo(1));
        }
    }
}
