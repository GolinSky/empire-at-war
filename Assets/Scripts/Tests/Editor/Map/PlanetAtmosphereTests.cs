using System;
using EmpireAtWar.Components.Environment;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Planet;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Map")]
    public sealed class PlanetAtmosphereTests
    {
        private const string SETTINGS_PATH = "Assets/Settings/Data/Models/Map/MapGenerationSettings.asset";

        [Test]
        public void EveryBiome_HasOneAtmosphereWithSparseGrade()
        {
            var settings = AssetDatabase.LoadAssetAtPath<MapGenerationSettings>(SETTINGS_PATH);
            Assert.That(settings.Atmospheres, Is.Not.Null);
            Assert.That(settings.Atmospheres.Entries.Count, Is.EqualTo(Enum.GetValues(typeof(PlanetBiome)).Length));
            foreach (PlanetBiome biome in Enum.GetValues(typeof(PlanetBiome)))
            {
                VolumeProfile grade = settings.Atmospheres.Get(biome).Grade;
                Assert.That(grade, Is.Not.Null, $"{biome} has no grade profile.");
                Assert.That(grade.TryGet(out WhiteBalance _), Is.True, $"{biome} grade has no white balance.");
                Assert.That(grade.TryGet(out ColorAdjustments adjustments), Is.True,
                    $"{biome} grade has no color adjustments.");
                // The grade blends over the base battle profile; exposure and contrast stay owned by the base.
                Assert.That(adjustments.postExposure.overrideState, Is.False, $"{biome} overrides exposure.");
                Assert.That(adjustments.contrast.overrideState, Is.False, $"{biome} overrides contrast.");
                Assert.That(grade.TryGet(out Bloom _), Is.False, $"{biome} grade overrides bloom.");
            }
        }

        [Test]
        public void EveryPlanet_ResolvesAnAtmosphere()
        {
            var settings = AssetDatabase.LoadAssetAtPath<MapGenerationSettings>(SETTINGS_PATH);
            foreach (PlanetType planet in Enum.GetValues(typeof(PlanetType)))
                Assert.That(settings.Atmospheres.Get(settings.Profiles.Get(planet).Biome), Is.Not.Null,
                    $"{planet} has no atmosphere.");
        }

        [Test]
        public void View_AppliesSunAmbientAndGrade()
        {
            var settings = AssetDatabase.LoadAssetAtPath<MapGenerationSettings>(SETTINGS_PATH);
            PlanetAtmosphere atmosphere = settings.Atmospheres.Get(PlanetBiome.Desert);
            Color sky = RenderSettings.ambientSkyColor;
            Color equator = RenderSettings.ambientEquatorColor;
            Color ground = RenderSettings.ambientGroundColor;
            var root = new GameObject(nameof(View_AppliesSunAmbientAndGrade));
            try
            {
                Light sun = root.AddComponent<Light>();
                Volume volume = root.AddComponent<Volume>();
                BattleAtmosphereView view = root.AddComponent<BattleAtmosphereView>();
                using (var serialized = new SerializedObject(view))
                {
                    serialized.FindProperty("sun").objectReferenceValue = sun;
                    serialized.FindProperty("gradeVolume").objectReferenceValue = volume;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }

                view.Apply(atmosphere);

                Assert.That(sun.color, Is.EqualTo(atmosphere.SunColor));
                Assert.That(sun.intensity, Is.EqualTo(atmosphere.SunIntensity));
                Assert.That(RenderSettings.ambientSkyColor, Is.EqualTo(atmosphere.AmbientSky));
                Assert.That(RenderSettings.ambientEquatorColor, Is.EqualTo(atmosphere.AmbientEquator));
                Assert.That(RenderSettings.ambientGroundColor, Is.EqualTo(atmosphere.AmbientGround));
                Assert.That(volume.sharedProfile, Is.SameAs(atmosphere.Grade));
            }
            finally
            {
                Object.DestroyImmediate(root);
                RenderSettings.ambientSkyColor = sky;
                RenderSettings.ambientEquatorColor = equator;
                RenderSettings.ambientGroundColor = ground;
            }
        }
    }
}
