using System.Reflection;
using EmpireAtWar.Components.Ship.EngineGlow;
using EmpireAtWar.Editor.Ship;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Ships")]
    public sealed class ShipEngineGlowComponentTests
    {
        private const int STATE_SHIFT = 8;
        private const uint STATE_MASK = 0x3u;

        private GameObject _root;
        private Material _material;
        private MeshRenderer[] _renderers;
        private ShipEngineGlowComponent _component;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Engine glow test");
            _material = new Material(AssetDatabase.LoadAssetAtPath<Shader>(ShipEngineGlowSetup.SHADER_PATH));
            // Glows 0 and 1 belong to engine hardpoint 4, glow 2 to engine 7, glow 3 to no engine.
            int[] engines = { 4, 4, 7, EngineGlowRenderer.NO_ENGINE };
            _renderers = new MeshRenderer[engines.Length];
            for (int i = 0; i < engines.Length; i++)
            {
                var glow = new GameObject("Glow " + i);
                glow.transform.SetParent(_root.transform);
                _renderers[i] = glow.AddComponent<MeshRenderer>();
                _renderers[i].sharedMaterial = _material;
            }

            _component = _root.AddComponent<ShipEngineGlowComponent>();
            var serialized = new SerializedObject(_component);
            SerializedProperty glows = serialized.FindProperty(ShipEngineGlowSetup.GLOWS_PROPERTY);
            glows.arraySize = engines.Length;
            for (int i = 0; i < engines.Length; i++)
            {
                glows.GetArrayElementAtIndex(i).FindPropertyRelative(ShipEngineGlowSetup.GLOW_RENDERER_PROPERTY).objectReferenceValue = _renderers[i];
                glows.GetArrayElementAtIndex(i).FindPropertyRelative(ShipEngineGlowSetup.GLOW_ENGINE_PROPERTY).intValue = engines[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            typeof(ShipEngineGlowComponent).GetMethod("Construct", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_component, null);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
            Object.DestroyImmediate(_material);
        }

        [Test]
        public void DestroyedEngine_TurnsOffOnlyItsGlows()
        {
            _component.SetState(EngineGlowState.Thrust);

            _component.SetEngineDestroyed(4, true);

            Assert.That(TargetOf(0), Is.EqualTo(EngineGlowState.Off));
            Assert.That(TargetOf(1), Is.EqualTo(EngineGlowState.Off));
            Assert.That(TargetOf(2), Is.EqualTo(EngineGlowState.Thrust));
            Assert.That(TargetOf(3), Is.EqualTo(EngineGlowState.Thrust));
        }

        [Test]
        public void DestroyedEngineGlows_StayOffWhenTheShipChangesState()
        {
            _component.SetEngineDestroyed(7, true);

            _component.SetState(EngineGlowState.Idle);

            Assert.That(TargetOf(2), Is.EqualTo(EngineGlowState.Off));
            Assert.That(TargetOf(0), Is.EqualTo(EngineGlowState.Idle));
            Assert.That(TargetOf(3), Is.EqualTo(EngineGlowState.Idle));
        }

        [Test]
        public void RestoredEngine_RejoinsTheShipState()
        {
            _component.SetState(EngineGlowState.Idle);
            _component.SetEngineDestroyed(4, true);

            _component.SetEngineDestroyed(4, false);

            Assert.That(TargetOf(0), Is.EqualTo(EngineGlowState.Idle));
            Assert.That(TargetOf(1), Is.EqualTo(EngineGlowState.Idle));
        }

        [Test]
        public void UnknownEngine_ChangesNothing()
        {
            _component.SetState(EngineGlowState.Thrust);
            uint before = _renderers[0].GetShaderUserValue();

            _component.SetEngineDestroyed(99, true);

            Assert.That(_renderers[0].GetShaderUserValue(), Is.EqualTo(before));
        }

        private EngineGlowState TargetOf(int glow) =>
            (EngineGlowState)((_renderers[glow].GetShaderUserValue() >> STATE_SHIFT) & STATE_MASK);
    }
}
