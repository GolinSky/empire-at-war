using System;
using System.Linq;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Services.VContainer;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Map")]
    public sealed class PlanetCatalogTests
    {
        private const string SETTINGS_PATH = "Assets/Settings/Data/Models/Map/MapGenerationSettings.asset";
        private const string SHARED_MESH_FOLDER = "Assets/Art/Models/Planets/Shared/";
        private const string MATERIAL_FOLDER = "Assets/Art/Materials/Models/Planets/Original/";
        private const string TEXTURE_FOLDER = "Assets/Art/Textures/Models/Planets/Original/";

        [Test]
        public void PlanetIds_PreserveSavedSelectionsAndDropdownIndices()
        {
            Assert.That((int)PlanetType.Coruscant, Is.EqualTo(0));
            Assert.That((int)PlanetType.Kamino, Is.EqualTo(1));
            Array values = Enum.GetValues(typeof(PlanetType));
            for (int index = 0; index < values.Length; index++)
                Assert.That((int)values.GetValue(index), Is.EqualTo(index));
        }

        [Test]
        public void EverySelectablePlanet_HasBoundPrefabAndSurfaceRenderers()
        {
            var settings = AssetDatabase.LoadAssetAtPath<MapGenerationSettings>(SETTINGS_PATH);
            foreach (PlanetType type in Enum.GetValues(typeof(PlanetType)))
            {
                PlanetScope prefab = settings.GetPlanetPrefab(type);
                Assert.That(prefab, Is.Not.Null, $"{type} has no prefab.");
                Assert.That(prefab.View, Is.Not.Null, $"{type} has no view.");
                using (var scope = new SerializedObject(prefab))
                    Assert.That(scope.FindProperty("superWeaponOrigin").objectReferenceValue, Is.Not.Null,
                        $"{type} has no superweapon origin.");
                using (var view = new SerializedObject(prefab.View))
                {
                    SerializedProperty surface = view.FindProperty("planetRenderers");
                    Assert.That(surface.arraySize, Is.GreaterThan(0), $"{type} has no surface renderer.");
                    foreach (string field in new[] { "planetRenderers", "cloudRenderers" })
                    {
                        SerializedProperty renderers = view.FindProperty(field);
                        for (int index = 0; index < renderers.arraySize; index++)
                            Assert.That(renderers.GetArrayElementAtIndex(index).objectReferenceValue, Is.Not.Null,
                                $"{type} has an unbound {field} entry.");
                    }
                }
            }
        }

        [Test]
        public void PlanetSurfaces_ShareOneGpuOnlyMeshAndNewPlanetsLimitCloudOverdraw()
        {
            var sphere = AssetDatabase.LoadAssetAtPath<Mesh>(SHARED_MESH_FOLDER + "PlanetSphere.asset");
            Assert.That(sphere, Is.Not.Null);
            Assert.That(sphere.vertexCount, Is.EqualTo(4753));
            Assert.That(sphere.isReadable, Is.False, "The shared sphere must not retain a CPU geometry copy.");
            var settings = AssetDatabase.LoadAssetAtPath<MapGenerationSettings>(SETTINGS_PATH);
            foreach (PlanetType type in Enum.GetValues(typeof(PlanetType)))
            {
                using (var view = new SerializedObject(settings.GetPlanetPrefab(type).View))
                {
                    SerializedProperty surfaces = view.FindProperty("planetRenderers");
                    for (int index = 0; index < surfaces.arraySize; index++)
                    {
                        var renderer = (MeshRenderer)surfaces.GetArrayElementAtIndex(index).objectReferenceValue;
                        Assert.That(renderer.GetComponent<MeshFilter>().sharedMesh, Is.SameAs(sphere),
                            $"{type} duplicates the shared surface geometry.");
                    }
                    if (type == PlanetType.Coruscant || type == PlanetType.Kamino) continue;
                    SerializedProperty clouds = view.FindProperty("cloudRenderers");
                    Assert.That(clouds.arraySize, Is.LessThanOrEqualTo(1), $"{type} adds redundant transparent shells.");
                    for (int index = 0; index < clouds.arraySize; index++)
                    {
                        var renderer = (MeshRenderer)clouds.GetArrayElementAtIndex(index).objectReferenceValue;
                        Assert.That(renderer.GetComponent<MeshFilter>().sharedMesh, Is.SameAs(sphere),
                            $"{type} cloud shell duplicates the shared geometry.");
                    }
                }
            }
        }

        [TestCase(PlanetType.Geonosis, "Rings", "w_planet_rings", 2.1f, 2.2f)]
        [TestCase(PlanetType.Kuat, "building_ring", "eb_station", 1.5f, 1.7f)]
        [TestCase(PlanetType.Kuat, "Cylinder01", "w_ring_mesh", 1.45f, 1.52f)]
        public void RingedPlanets_BindSourceRingGeometryAndArtworkAtPlanetScale(
            PlanetType type, string part, string texture, float minimumRadiusRatio, float maximumRadiusRatio)
        {
            var settings = AssetDatabase.LoadAssetAtPath<MapGenerationSettings>(SETTINGS_PATH);
            PlanetScope prefab = settings.GetPlanetPrefab(type);
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(SHARED_MESH_FOLDER + type + "_" + part + ".asset");
            Assert.That(mesh, Is.Not.Null);
            Assert.That(mesh.isReadable, Is.False);
            MeshFilter[] rings = prefab.GetComponentsInChildren<MeshFilter>(true)
                .Where(filter => filter.sharedMesh == mesh).ToArray();
            Assert.That(rings.Length, Is.EqualTo(1), $"{type} must contain one bound {part} ring.");
            MeshFilter ring = rings[0];
            var renderer = ring.GetComponent<MeshRenderer>();
            Assert.That(renderer.bounds.max.y, Is.LessThan(0), "Planet rings must stay below the battle plane.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(MATERIAL_FOLDER + texture + "_Rings.mat");
            Assert.That(material, Is.Not.Null);
            Assert.That(renderer.sharedMaterial, Is.SameAs(material));
            var artwork = AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_FOLDER + texture + "_Rings.png");
            Assert.That(artwork, Is.Not.Null);
            Assert.That(material.GetTexture("_BaseMap"), Is.SameAs(artwork));

            using (var view = new SerializedObject(prefab.View))
            {
                var surface = (MeshRenderer)view.FindProperty("planetRenderers").GetArrayElementAtIndex(0).objectReferenceValue;
                float planetRadius = surface.GetComponent<MeshFilter>().sharedMesh.bounds.extents.x * surface.transform.localScale.x;
                float ringRadius = Mathf.Max(mesh.bounds.extents.x, mesh.bounds.extents.z) * ring.transform.localScale.x;
                Assert.That(ringRadius / planetRadius, Is.InRange(minimumRadiusRatio, maximumRadiusRatio),
                    "Source ring dimensions must be converted to the same scale as the planet surface.");
                Assert.That(ring.transform.localPosition, Is.EqualTo(surface.transform.localPosition));
            }
        }
    }
}
