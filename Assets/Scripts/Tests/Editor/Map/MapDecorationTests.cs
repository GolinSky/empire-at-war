using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Map.Generation;
using EmpireAtWar.Entities.Map.Hazards;
using EmpireAtWar.Utils;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Map")]
    public sealed class MapDecorationTests
    {
        private PlanetDecorationSet _set;
        private GameObject _prefab;
        private MapSizeSettings _size;

        [SetUp]
        public void SetUp()
        {
            _size = AssetDatabase.LoadAssetAtPath<MapGenerationSettings>(
                "Assets/Settings/Data/Models/Map/MapGenerationSettings.asset").GetSize(MapSize.Small);
            _set = ScriptableObject.CreateInstance<PlanetDecorationSet>();
            _prefab = new GameObject("Decoration");
            SerializedObject serialized = new SerializedObject(_set);
            SerializedProperty entries = serialized.FindProperty("entries");
            entries.arraySize = 1;
            SerializedProperty entry = entries.GetArrayElementAtIndex(0);
            entry.FindPropertyRelative("<Prefab>k__BackingField").objectReferenceValue = _prefab;
            entry.FindPropertyRelative("<MinimumCount>k__BackingField").intValue = 8;
            entry.FindPropertyRelative("<MaximumCount>k__BackingField").intValue = 8;
            entry.FindPropertyRelative("<MinimumScale>k__BackingField").floatValue = 1f;
            entry.FindPropertyRelative("<MaximumScale>k__BackingField").floatValue = 2f;
            entry.FindPropertyRelative("<ClearanceRadius>k__BackingField").floatValue = 10f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_set);
            Object.DestroyImmediate(_prefab);
        }

        [Test]
        public void SameSeedProducesSameSpotsAndIndependentSeedChangesThem()
        {
            IReadOnlyList<MapDecorationSpot> first = Place(42);
            IReadOnlyList<MapDecorationSpot> second = Place(42);
            Assert.That(first.Count, Is.EqualTo(8));
            for (int i = 0; i < first.Count; i++)
            {
                Assert.That(second[i].Position, Is.EqualTo(first[i].Position));
                Assert.That(second[i].Yaw, Is.EqualTo(first[i].Yaw));
                Assert.That(second[i].Scale, Is.EqualTo(first[i].Scale));
                Assert.That(first[i].Position.x, Is.InRange(_size.Bounds.Min.x + first[i].Radius,
                    _size.Bounds.Max.x - first[i].Radius));
                Assert.That(first[i].Position.z, Is.InRange(_size.Bounds.Min.y + first[i].Radius,
                    _size.Bounds.Max.y - first[i].Radius));
                for (int j = 0; j < i; j++)
                    Assert.That(PlanarGeometry.Distance(first[i].Position, first[j].Position),
                        Is.GreaterThanOrEqualTo(first[i].Radius + first[j].Radius));
            }
            Assert.That(Place(43)[0].Position, Is.Not.EqualTo(first[0].Position));
        }

        [Test]
        public void PlacementFractionKeepsWholePropsInsideCentralBounds()
        {
            SerializedObject serialized = new SerializedObject(_set);
            serialized.FindProperty("<PlacementBoundsFraction>k__BackingField").floatValue = 0.7f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Vector2 center = (_size.Bounds.Min + _size.Bounds.Max) * 0.5f;
            Vector2 halfSize = (_size.Bounds.Max - _size.Bounds.Min) * 0.35f;
            IReadOnlyList<MapDecorationSpot> spots = Place(42);
            Assert.That(spots.Count, Is.EqualTo(8));
            foreach (MapDecorationSpot spot in spots)
            {
                Assert.That(spot.Position.x, Is.InRange(center.x - halfSize.x + spot.Radius,
                    center.x + halfSize.x - spot.Radius));
                Assert.That(spot.Position.z, Is.InRange(center.y - halfSize.y + spot.Radius,
                    center.y + halfSize.y - spot.Radius));
            }
        }

        [Test]
        public void LaneAndFieldClearanceIgnoreHeight()
        {
            MapLane[] lanes = { new MapLane(new[] { Vector3.left * 100f, Vector3.right * 100f }) };
            AsteroidField[] fields = { new AsteroidField(new[] { new FieldVolume(Vector3.forward * 100f, 20f) },
                Array.Empty<AsteroidSpot>(), -10f, 10f) };
            Assert.That(Clear(new Vector3(0f, 1000f, 20f)), Is.False);
            Assert.That(Clear(new Vector3(0f, -1000f, 115f)), Is.False);
            Assert.That(Clear(Vector3.forward * 200f), Is.True);

            bool Clear(Vector3 point) => MapDecorationPlacer.HasClearance(point, 10f, 40f,
                Array.Empty<MapStation>(), Array.Empty<MapNode>(), lanes, fields,
                Array.Empty<HazardShape>(), Array.Empty<MapDecorationSpot>(), 0f);
        }

        [Test]
        public void StationsNodesAndExistingPropsReserveTheirRadii()
        {
            MapStation[] stations = { new MapStation(default, default, Vector3.zero, 50f) };
            MapNode[] nodes = { new MapNode(MapNodeKind.DefaultZone, Vector3.right * 200f, default, 30f) };
            MapDecorationSpot[] props = { new MapDecorationSpot(_prefab, Vector3.forward * 200f, 0f, 1f, 20f) };
            Assert.That(Clear(Vector3.right * 60f), Is.False);
            Assert.That(Clear(Vector3.right * 240f), Is.False);
            Assert.That(Clear(Vector3.forward * 220f), Is.False);
            Assert.That(Clear(new Vector3(300f, 0f, 300f)), Is.True);

            bool Clear(Vector3 point) => MapDecorationPlacer.HasClearance(point, 10f, 40f,
                stations, nodes, Array.Empty<MapLane>(), Array.Empty<AsteroidField>(),
                Array.Empty<HazardShape>(), props, 10f);
        }

        [Test]
        public void HazardOutlineRejectsPropFootprintAndGridMargin()
        {
            SpaceHazardData data = ScriptableObject.CreateInstance<SpaceHazardData>();
            try
            {
                HazardShape[] hazards = { TestHazards.CreateEllipse(data, Vector3.zero,
                    Vector3.forward, 100f, 50f, 5f) };
                Assert.That(Clear(Vector3.zero), Is.False);
                Assert.That(Clear(Vector3.right * 58f), Is.False);
                Assert.That(Clear(Vector3.right * 100f), Is.True);

                bool Clear(Vector3 point) => MapDecorationPlacer.HasClearance(point, 10f, 40f,
                    Array.Empty<MapStation>(), Array.Empty<MapNode>(), Array.Empty<MapLane>(),
                    Array.Empty<AsteroidField>(), hazards, Array.Empty<MapDecorationSpot>(), 0f);
            }
            finally
            {
                Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void FilledMapPlacesNothingWithoutFailing()
        {
            AsteroidField[] fields = { new AsteroidField(new[] { new FieldVolume(Vector3.zero, 100000f) },
                Array.Empty<AsteroidSpot>(), -10f, 10f) };
            Assert.That(MapDecorationPlacer.Place(_set, 42, _size, Array.Empty<MapStation>(),
                Array.Empty<MapNode>(), Array.Empty<MapLane>(), fields, Array.Empty<HazardShape>(), 0f), Is.Empty);
        }

        [Test]
        public void EveryAuthoredSetReferencesPrefabs()
        {
            string[] guids = AssetDatabase.FindAssets("t:PlanetDecorationSet");
            Assert.That(guids, Is.Not.Empty);
            foreach (string guid in guids)
            {
                PlanetDecorationSet set = AssetDatabase.LoadAssetAtPath<PlanetDecorationSet>(
                    AssetDatabase.GUIDToAssetPath(guid));
                Assert.That(set.Entries, Is.Not.Empty, set.name);
                foreach (PlanetDecorationEntry entry in set.Entries)
                    Assert.That(entry.Prefab, Is.Not.Null, set.name);
            }
        }

        [Test]
        public void SpawnPreservesImportedOrientationUnderWorldYaw()
        {
            GameObject parent = new GameObject("Decoration parent");
            try
            {
                _prefab.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                _prefab.transform.localScale = Vector3.one * 100f;
                MapDecorationView.Spawn(new MapDecorationSpot(_prefab, Vector3.right * 50f, 37f, 0.12f, 10f), parent.transform);
                Transform instance = parent.transform.GetChild(0);
                Assert.That(Quaternion.Angle(instance.rotation,
                    Quaternion.Euler(0f, 37f, 0f) * _prefab.transform.localRotation), Is.LessThan(0.01f));
                Assert.That(instance.localScale, Is.EqualTo(Vector3.one * 12f));
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }

        private IReadOnlyList<MapDecorationSpot> Place(int seed) => MapDecorationPlacer.Place(_set, seed,
            _size, Array.Empty<MapStation>(), Array.Empty<MapNode>(), Array.Empty<MapLane>(),
            Array.Empty<AsteroidField>(), Array.Empty<HazardShape>(), 0f);
    }
}
