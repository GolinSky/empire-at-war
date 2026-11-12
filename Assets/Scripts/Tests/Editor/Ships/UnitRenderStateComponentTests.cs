using System.Reflection;
using EmpireAtWar.Components.TeamColor;
using EmpireAtWar.Models.Players;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Ships")]
    public sealed class UnitRenderStateComponentTests
    {
        private GameObject _root;
        private MeshRenderer _renderer;
        private UnitRenderStateComponent _component;
        private IPlayerRoster _roster;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Unit render state test");
            _renderer = _root.AddComponent<MeshRenderer>();
            _component = _root.AddComponent<UnitRenderStateComponent>();
            var serialized = new SerializedObject(_component);
            SerializedProperty renderers = serialized.FindProperty("meshRenderers");
            renderers.arraySize = 1;
            renderers.GetArrayElementAtIndex(0).objectReferenceValue = _renderer;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            _roster = TestPlayers.CreateDuel();
            typeof(UnitRenderStateComponent).GetMethod("Construct", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_component, new object[] { _roster, TestPlayers.Enemy });
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
        }

        [Test]
        public void HighlightTransitions_PreserveTeamAndSetAndClearShaderBits()
        {
            Assert.That(_renderer.GetShaderUserValue(), Is.EqualTo(2u));
            _component.SetHighlight(UnitHighlight.Hovered);
            Assert.That(_renderer.GetShaderUserValue(), Is.EqualTo(2u | (1u << 8)));
            _component.SetHighlight(UnitHighlight.Hovered | UnitHighlight.Selected);
            Assert.That(_renderer.GetShaderUserValue(), Is.EqualTo(2u | (1u << 8) | (1u << 9)));
            _component.SetHighlight(UnitHighlight.Selected);
            Assert.That(_renderer.GetShaderUserValue(), Is.EqualTo(2u | (1u << 9)));
            _component.SetHighlight(UnitHighlight.None);
            Assert.That(_renderer.GetShaderUserValue(), Is.EqualTo(2u));
        }

        [Test]
        public void UnchangedHighlight_DoesNotRewriteRenderer()
        {
            _component.SetHighlight(UnitHighlight.Hovered);
            // A sentinel makes a redundant native renderer write observable.
            _renderer.SetShaderUserValue(123u);
            _renderer.renderingLayerMask = 4u;
            _component.SetHighlight(UnitHighlight.Hovered);
            Assert.That(_renderer.GetShaderUserValue(), Is.EqualTo(123u));
            Assert.That(_renderer.renderingLayerMask, Is.EqualTo(4u));
            _component.SetHighlight(UnitHighlight.None);
            Assert.That(_renderer.GetShaderUserValue(), Is.EqualTo(2u));
            Assert.That(_renderer.renderingLayerMask, Is.EqualTo(4u));
        }

        [TestCase(UnitHighlight.Hovered)]
        [TestCase(UnitHighlight.Selected)]
        [TestCase(UnitHighlight.AttackTargeted)]
        public void Highlight_EnablesOutlineLayerAndPreservesOtherLayerAndTeamBits(UnitHighlight highlight)
        {
            const uint OTHER_LAYERS = 1u | (1u << 5) | (1u << 23);
            _renderer.renderingLayerMask = OTHER_LAYERS;

            _component.SetHighlight(highlight);

            Assert.That(_renderer.renderingLayerMask,
                Is.EqualTo(OTHER_LAYERS | UnitOutlineRendererFeature.OUTLINE_RENDERING_LAYER));
            Assert.That(_renderer.GetShaderUserValue(), Is.EqualTo(2u | ((uint)highlight << 8)));
            _component.SetHighlight(UnitHighlight.None);
            Assert.That(_renderer.renderingLayerMask, Is.EqualTo(OTHER_LAYERS));
            Assert.That(_renderer.GetShaderUserValue(), Is.EqualTo(2u));
        }

        [Test]
        public void ClearingIndividualFlags_KeepsOutlineUntilLastFlagClears()
        {
            const uint OTHER_LAYERS = 1u | (1u << 11);
            _renderer.renderingLayerMask = OTHER_LAYERS;
            _component.SetHighlight(UnitHighlight.Hovered | UnitHighlight.Selected | UnitHighlight.AttackTargeted);
            Assert.That(_renderer.GetShaderUserValue(), Is.EqualTo(2u | (7u << 8)));

            _component.SetHighlight(UnitHighlight.Hovered | UnitHighlight.Selected);
            Assert.That(_renderer.renderingLayerMask,
                Is.EqualTo(OTHER_LAYERS | UnitOutlineRendererFeature.OUTLINE_RENDERING_LAYER));
            Assert.That(_renderer.GetShaderUserValue(), Is.EqualTo(2u | (3u << 8)));

            _component.SetHighlight(UnitHighlight.Hovered);
            Assert.That(_renderer.renderingLayerMask,
                Is.EqualTo(OTHER_LAYERS | UnitOutlineRendererFeature.OUTLINE_RENDERING_LAYER));
            Assert.That(_renderer.GetShaderUserValue(), Is.EqualTo(2u | (1u << 8)));

            _component.SetHighlight(UnitHighlight.None);
            Assert.That(_renderer.renderingLayerMask, Is.EqualTo(OTHER_LAYERS));
            Assert.That(_renderer.GetShaderUserValue(), Is.EqualTo(2u));
        }

        [Test]
        public void WreckTeamValue_DoesNotCarryLiveUnitHighlight()
        {
            _component.SetHighlight(UnitHighlight.Hovered | UnitHighlight.Selected);
            uint wreckValue = UnitRenderStateComponent.GetTeamUserValue(TestPlayers.Enemy, _roster);
            Assert.That(wreckValue, Is.EqualTo(2u));
            Assert.That(wreckValue & ~0xFFu, Is.Zero);
            Assert.That(_renderer.GetShaderUserValue() & ~0xFFu, Is.Not.Zero);
        }
    }
}
