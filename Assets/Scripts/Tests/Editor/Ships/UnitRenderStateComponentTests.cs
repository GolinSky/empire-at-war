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
        // Bit layout shared with ShipLitInput.hlsl.
        private const uint TEAM_MASK = 0xFFu;
        private const uint HIGHLIGHT_MASK = 0x7u << 8;
        private const uint DAMAGE_MASK = 0x1Fu << 11;
        private const uint SEED_MASK = 0x1Fu << 16;

        private GameObject _root;
        private MeshRenderer _renderer;
        private UnitRenderStateComponent _component;
        private IPlayerRoster _roster;
        // Team 2 plus this unit's pattern seed, as written by Construct.
        private uint _base;

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
            _base = _renderer.GetShaderUserValue();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
        }

        [Test]
        public void HighlightTransitions_PreserveTeamAndSetAndClearShaderBits()
        {
            Assert.That(_renderer.GetShaderUserValue(), Is.EqualTo(_base));
            _component.SetHighlight(UnitHighlight.Hovered);
            Assert.That(_renderer.GetShaderUserValue(), Is.EqualTo(_base | (1u << 8)));
            _component.SetHighlight(UnitHighlight.Hovered | UnitHighlight.Selected);
            Assert.That(_renderer.GetShaderUserValue(), Is.EqualTo(_base | (1u << 8) | (1u << 9)));
            _component.SetHighlight(UnitHighlight.Selected);
            Assert.That(_renderer.GetShaderUserValue(), Is.EqualTo(_base | (1u << 9)));
            _component.SetHighlight(UnitHighlight.None);
            Assert.That(_renderer.GetShaderUserValue(), Is.EqualTo(_base));
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
            Assert.That(_renderer.GetShaderUserValue(), Is.EqualTo(_base));
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
            Assert.That(_renderer.GetShaderUserValue(), Is.EqualTo(_base | ((uint)highlight << 8)));
            _component.SetHighlight(UnitHighlight.None);
            Assert.That(_renderer.renderingLayerMask, Is.EqualTo(OTHER_LAYERS));
            Assert.That(_renderer.GetShaderUserValue(), Is.EqualTo(_base));
        }

        [Test]
        public void ClearingIndividualFlags_KeepsOutlineUntilLastFlagClears()
        {
            const uint OTHER_LAYERS = 1u | (1u << 11);
            _renderer.renderingLayerMask = OTHER_LAYERS;
            _component.SetHighlight(UnitHighlight.Hovered | UnitHighlight.Selected | UnitHighlight.AttackTargeted);
            Assert.That(_renderer.GetShaderUserValue(), Is.EqualTo(_base | (7u << 8)));

            _component.SetHighlight(UnitHighlight.Hovered | UnitHighlight.Selected);
            Assert.That(_renderer.renderingLayerMask,
                Is.EqualTo(OTHER_LAYERS | UnitOutlineRendererFeature.OUTLINE_RENDERING_LAYER));
            Assert.That(_renderer.GetShaderUserValue(), Is.EqualTo(_base | (3u << 8)));

            _component.SetHighlight(UnitHighlight.Hovered);
            Assert.That(_renderer.renderingLayerMask,
                Is.EqualTo(OTHER_LAYERS | UnitOutlineRendererFeature.OUTLINE_RENDERING_LAYER));
            Assert.That(_renderer.GetShaderUserValue(), Is.EqualTo(_base | (1u << 8)));

            _component.SetHighlight(UnitHighlight.None);
            Assert.That(_renderer.renderingLayerMask, Is.EqualTo(OTHER_LAYERS));
            Assert.That(_renderer.GetShaderUserValue(), Is.EqualTo(_base));
        }

        [Test]
        public void Construct_WritesTeamAndOnlyTeamAndSeedBits()
        {
            Assert.That(_base & TEAM_MASK, Is.EqualTo(2u));
            Assert.That(_base & (HIGHLIGHT_MASK | DAMAGE_MASK), Is.Zero);
            Assert.That(_base & ~(TEAM_MASK | SEED_MASK), Is.Zero);
        }

        [TestCase(1f, 0u)]
        [TestCase(0.999f, 1u)]
        [TestCase(0.5f, 16u)]
        [TestCase(0f, 31u)]
        public void HullDamage_QuantizesHullIntoLevelBits(float hull, uint level)
        {
            _component.SetHullDamage(hull);

            Assert.That(_renderer.GetShaderUserValue(), Is.EqualTo(_base | (level << 11)));
        }

        [Test]
        public void HullDamage_RepairLowersLevel()
        {
            _component.SetHullDamage(0.2f);
            _component.SetHullDamage(1f);

            Assert.That(_renderer.GetShaderUserValue(), Is.EqualTo(_base));
        }

        [Test]
        public void HullDamageAndHighlight_PreserveEachOther()
        {
            const uint OTHER_LAYERS = 1u | (1u << 5);
            _renderer.renderingLayerMask = OTHER_LAYERS;
            _component.SetHullDamage(0f);
            Assert.That(_renderer.renderingLayerMask, Is.EqualTo(OTHER_LAYERS),
                "Damage bits alone must not turn the outline on.");

            _component.SetHighlight(UnitHighlight.Selected);
            Assert.That(_renderer.GetShaderUserValue(), Is.EqualTo(_base | (31u << 11) | (2u << 8)));
            Assert.That(_renderer.renderingLayerMask,
                Is.EqualTo(OTHER_LAYERS | UnitOutlineRendererFeature.OUTLINE_RENDERING_LAYER));

            _component.SetHighlight(UnitHighlight.None);
            Assert.That(_renderer.GetShaderUserValue(), Is.EqualTo(_base | (31u << 11)));
            Assert.That(_renderer.renderingLayerMask, Is.EqualTo(OTHER_LAYERS));
        }

        [Test]
        public void WreckUserValue_CarriesTeamDamageAndSeedButNotHighlight()
        {
            _component.SetHullDamage(0f);
            _component.SetHighlight(UnitHighlight.Hovered | UnitHighlight.Selected);

            Assert.That(_component.WreckUserValue, Is.EqualTo(_base | (31u << 11)));
            Assert.That(_renderer.GetShaderUserValue() & HIGHLIGHT_MASK, Is.Not.Zero);
        }
    }
}
