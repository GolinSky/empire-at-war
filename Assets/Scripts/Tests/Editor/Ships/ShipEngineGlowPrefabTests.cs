using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.FogOfWar;
using EmpireAtWar.Components.Ship.EngineGlow;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.TeamColor;
using EmpireAtWar.Editor.Ship;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.ViewComponents.Health;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    /// <summary>
    /// Engine glows fade through their renderer user value (EngineGlow.shader).
    /// Fix failures with Tools/Empire At War/Units/Ships/Set Up Engine Glow.
    /// </summary>
    [Category("Ships")]
    public sealed class ShipEngineGlowPrefabTests
    {
        private static IEnumerable<string> ShipViewPrefabs() => ShipEngineGlowSetup.FindShipViewPaths();

        [Test]
        public void GlowMaterials_UseEngineGlowShaderWithAFadeAndADimmerIdle()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShipEngineGlowSetup.SHADER_PATH);
            foreach (string path in ShipEngineGlowSetup.GLOW_MATERIAL_PATHS)
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                Assert.That(material.shader, Is.EqualTo(shader), path);
                Assert.That(material.GetFloat("_IdleIntensity"), Is.InRange(0f, 1f).And.LessThan(1f), path);
                Assert.That(material.GetFloat("_FadeSeconds"), Is.GreaterThan(0f), path);
            }
        }

        [Test]
        public void ShipViews_IncludeEveryEngineGlowShip()
        {
            Assert.That(ShipViewPrefabs().Count(path =>
                    GlowRenderers(AssetDatabase.LoadAssetAtPath<GameObject>(path)).Length > 0), Is.GreaterThan(0),
                "No ship view uses the engine glow shader.");
        }

        [TestCaseSource(nameof(ShipViewPrefabs))]
        public void EngineGlow_IsBoundListedFoggedAndHasOneUserValueWriter(string path)
        {
            GameObject view = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            EntityBindings bindings = view.GetComponent<EntityBindings>();
            ShipEngineGlowComponent[] components = bindings.Injectables.OfType<ShipEngineGlowComponent>().ToArray();
            Assert.That(components, Has.Length.EqualTo(1), $"{path}: RegisterEngineGlow needs exactly one engine glow in Injectables.");
            Assert.That(view.GetComponentsInChildren<ShipEngineGlowComponent>(true), Is.EquivalentTo(components),
                $"{path}: an unbound engine glow exists in the hierarchy.");

            SerializedProperty entries = new SerializedObject(components[0]).FindProperty(ShipEngineGlowSetup.GLOWS_PROPERTY);
            Object[] listed = Enumerable.Range(0, entries.arraySize).Select(i => entries.GetArrayElementAtIndex(i)
                .FindPropertyRelative(ShipEngineGlowSetup.GLOW_RENDERER_PROPERTY).objectReferenceValue).ToArray();
            MeshRenderer[] glows = view.GetComponentsInChildren<MeshRenderer>(true)
                .Where(renderer => renderer.enabled && ShipEngineGlowSetup.UsesEngineGlow(renderer)).ToArray();
            Assert.That(listed, Is.All.Not.Null, path);
            Assert.That(listed, Is.EquivalentTo(glows), $"{path}: every enabled engine glow must be listed once.");

            foreach (FogVisibilityComponent fog in view.GetComponentsInChildren<FogVisibilityComponent>(true))
                Assert.That(References(fog, "renderers"), Is.SupersetOf(glows), $"{path}: fog must hide the engine glows.");
            foreach (UnitRenderStateComponent renderState in view.GetComponentsInChildren<UnitRenderStateComponent>(true))
                Assert.That(References(renderState, "meshRenderers").Intersect(glows), Is.Empty,
                    $"{path}: the render state would overwrite the glow fade.");

            // Each glow belongs to an engine hardpoint when the ship has any; a destroyed engine switches it off.
            List<HardPoint> shipUnits = view.GetComponentInChildren<HealthComponent>(true).ShipUnits;
            bool hasEngines = shipUnits.Any(unit => unit.HardPointType == HardPointType.Engines);
            for (int i = 0; i < entries.arraySize; i++)
            {
                int engine = entries.GetArrayElementAtIndex(i).FindPropertyRelative(ShipEngineGlowSetup.GLOW_ENGINE_PROPERTY).intValue;
                if (!hasEngines)
                    Assert.That(engine, Is.EqualTo(EngineGlowRenderer.NO_ENGINE), $"{path}: glow {i} names an engine the ship lacks.");
                else
                    Assert.That(engine >= 0 && engine < shipUnits.Count && shipUnits[engine].HardPointType == HardPointType.Engines,
                        Is.True, $"{path}: glow {i} must point at an engine hardpoint (index {engine}).");
            }

            // The component reads idle level and fade time from its first glow's material.
            Material[] materials = glows.SelectMany(glow => glow.sharedMaterials).Distinct().ToArray();
            Assert.That(materials.Select(material => material.GetFloat("_IdleIntensity")).Distinct().Count(), Is.LessThanOrEqualTo(1), path);
            Assert.That(materials.Select(material => material.GetFloat("_FadeSeconds")).Distinct().Count(), Is.LessThanOrEqualTo(1), path);
        }

        private static MeshRenderer[] GlowRenderers(GameObject view) =>
            view.GetComponentsInChildren<MeshRenderer>(true).Where(ShipEngineGlowSetup.UsesEngineGlow).ToArray();

        private static Object[] References(Object component, string field)
        {
            SerializedProperty array = new SerializedObject(component).FindProperty(field);
            return Enumerable.Range(0, array.arraySize).Select(i => array.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
        }
    }
}
