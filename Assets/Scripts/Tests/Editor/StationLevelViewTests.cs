using System.Linq;
using System.Reflection;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.SpaceStation;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.ViewComponents.Health;
using EmpireAtWar.ViewComponents.Station;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using SpaceStationEntity = EmpireAtWar.Entities.SpaceStation.SpaceStation;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class StationLevelViewTests
    {
        private const string PREFABS = "Assets/Prefabs/Models/Stations/";
        private const string VIEW = PREFABS + "RebellionSpaceStationView.prefab";
        private const BindingFlags PRIVATE_INSTANCE = BindingFlags.Instance | BindingFlags.NonPublic;

        [TestCase(1, 3656, 5)]
        [TestCase(2, 4235, 8)]
        [TestCase(3, 4641, 11)]
        [TestCase(4, 6001, 14)]
        [TestCase(5, 7407, 18)]
        public void EachLevel_UsesItsOriginalHullWithCompleteBindings(int level, int triangles, int attachments)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFABS + "RebelSpaceStationLevel" + level + ".prefab");
            var model = prefab.GetComponent<StationLevelModel>();
            Assert.That(prefab.transform.localScale, Is.EqualTo(Vector3.one));
            Assert.That(model.HullRenderers, Has.Length.EqualTo(attachments + 1));
            var mesh = model.HullRenderers.Single(r => r.name == "Level_0" + level).GetComponent<MeshFilter>().sharedMesh;
            Assert.That(mesh.triangles.Length / 3, Is.EqualTo(triangles));
            Assert.That(AssetDatabase.GetAssetPath(mesh), Does.EndWith("/Level" + level + "/RebelSpaceStationLevel" + level + "Hull.asset"));
            foreach (var renderer in model.HullRenderers.Where(r => r.name != "Level_0" + level))
            {
                Assert.That(AssetDatabase.GetAssetPath(renderer.GetComponent<MeshFilter>().sharedMesh),
                    Does.Contain("/RebelSpaceStation/Attachments/"));
                Assert.That(renderer.transform.parent.parent.name.ToUpperInvariant(), Is.EqualTo(renderer.name.ToUpperInvariant() + "_BONE"));
                Assert.That(renderer.sharedMaterial, Is.SameAs(model.HullRenderers.Single(r => r.name == "Level_0" + level).sharedMaterial));
            }
            Assert.That(mesh.uv, Has.Length.EqualTo(mesh.vertexCount));
            Assert.That(model.ShieldMesh, Is.Not.Null);
            Assert.That(model.ShieldPlanes, Has.Length.EqualTo(1024));
            Assert.That(model.AttachmentPoints, Has.Length.EqualTo(18));
            Assert.That(model.AttachmentPoints.All(t => t != null && t.IsChildOf(prefab.transform)), Is.True);
            Assert.That(model.AttachmentPoints[17].localPosition.y, Is.LessThan(model.HullBounds.min.y));
            Assert.That(prefab.GetComponentsInChildren<Transform>(true).Any(t => t.name == "Spawn_00"), Is.True);
            Assert.That(prefab.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled)
                .SelectMany(r => r.sharedMaterials).All(m => m != null && m.shader != null), Is.True);
            Assert.That(prefab.GetComponentsInChildren<MeshRenderer>(true).Any(r => r.enabled && (r.name.Contains("Shadow") || r.name.EndsWith("_Blast"))), Is.False);
        }

        [TestCase(1)]
        [TestCase(5)]
        public void EntityLevelHandler_SwapsModelsAndShieldWithoutReplacingGameplay(int initialLevel)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(VIEW);
            try
            {
                var station = root.GetComponent<SpaceStationEntity>();
                var view = root.GetComponent<StationLevelView>();
                var data = AssetDatabase.LoadAssetAtPath<SpaceStationData>("Assets/Settings/Data/Models/SpaceStation/SpaceStationData.asset");
                var upgrade = new RecordingHealthUpgrade();
                typeof(SpaceStationEntity).GetField("_healthUpgrade", PRIVATE_INSTANCE).SetValue(station, upgrade);
                typeof(SpaceStationEntity).GetField("<Data>k__BackingField", PRIVATE_INSTANCE).SetValue(station, data);
                MethodInfo apply = typeof(SpaceStationEntity).GetMethod("ApplyLevel", PRIVATE_INSTANCE);
                var models = root.GetComponentsInChildren<StationLevelModel>(true);
                var hardpoints = root.GetComponentsInChildren<HardPoint>(true).OrderBy(h => h.Id).ToArray();
                var shield = root.GetComponentInChildren<Shield>(true);
                var fog = new SerializedObject(root.GetComponents<MonoBehaviour>().Single(c => c.GetType().Name == "FogVisibilityComponent"));
                var team = new SerializedObject(root.GetComponents<MonoBehaviour>().Single(c => c.GetType().Name == "TeamColorView"));
                Assert.That(team.FindProperty("meshRenderers").arraySize, Is.EqualTo(root.GetComponentsInChildren<MeshRenderer>(true).Length));
                root.transform.SetPositionAndRotation(new Vector3(100, 20, -300), Quaternion.Euler(0, 60, 0));
                foreach (int level in new[] {initialLevel, 2, 3, 4, 5, 5})
                {
                    apply.Invoke(station, new object[] {level});
                    Assert.That(models.Count(m => m.gameObject.activeSelf), Is.EqualTo(1));
                    Assert.That(view.CurrentModel.name, Is.EqualTo("RebelSpaceStationLevel" + level));
                    Assert.That(upgrade.Level, Is.EqualTo(level));
                    Assert.That(upgrade.HullScale, Is.EqualTo(data.GetLevelStats(level).HullMultiplier));
                    Assert.That(root.GetComponent<BoxCollider>().size, Is.EqualTo(view.CurrentModel.HullBounds.size));
                    Assert.That(shield.GetComponent<MeshFilter>().sharedMesh, Is.SameAs(view.CurrentModel.ShieldMesh));
                    for (int i = 0; i < hardpoints.Length; i++)
                        Assert.That(Vector3.Distance(hardpoints[i].transform.position, view.CurrentModel.AttachmentPoints[i].position), Is.LessThan(.001f));
                    var explosions = (Renderer[])typeof(SpaceStationEntity).GetField("explosionHullRenderers", PRIVATE_INSTANCE).GetValue(station);
                    Assert.That(explosions, Is.EqualTo(view.CurrentModel.HullRenderers));
                    foreach (var renderer in view.CurrentModel.GetComponentsInChildren<MeshRenderer>(true))
                    {
                        Assert.That(References(fog.FindProperty("renderers"), renderer), Is.True);
                        Assert.That(References(team.FindProperty("meshRenderers"), renderer), Is.True);
                    }
                    Assert.That(root.GetComponentsInChildren<HardPoint>(true).OrderBy(h => h.Id), Is.EqualTo(hardpoints));
                }
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        [Test]
        public void FactionMapping_UsesRebelModelsOnlyForRebellionAndCoversConfiguredLevels()
        {
            var mapping = new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/Settings/AssetMappingData.asset"));
            var rows = mapping.FindProperty("assetMappings.keyValue");
            foreach (var faction in new[] {FactionType.Rebellion, FactionType.Republic, FactionType.Empire, FactionType.Separatist})
            {
                var row = Enumerable.Range(0, rows.arraySize).Select(rows.GetArrayElementAtIndex).Single(p => p.FindPropertyRelative("key").stringValue == faction + "SpaceStationView");
                string path = AssetDatabase.GUIDToAssetPath(row.FindPropertyRelative("value.m_AssetGUID").stringValue);
                string expected = faction == FactionType.Rebellion ? VIEW : PREFABS + (faction == FactionType.Separatist ? "Separatist" : "Republic") + "SpaceStationView.prefab";
                Assert.That(path, Is.EqualTo(expected));
                var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Assert.That(root.GetComponent<StationLevelView>() != null, Is.EqualTo(faction == FactionType.Rebellion));
            }
            var levels = AssetDatabase.LoadAssetAtPath<StationLevelData>("Assets/Settings/Data/Factions/Shared/StationLevelData.asset");
            var rebel = AssetDatabase.LoadAssetAtPath<GameObject>(VIEW);
            Assert.That(new SerializedObject(rebel.GetComponent<StationLevelView>()).FindProperty("levelModels").arraySize, Is.EqualTo(levels.MaxLevel));
            Assert.That(rebel.GetComponentsInChildren<StationLevelModel>(true).Count(m => m.gameObject.activeSelf), Is.EqualTo(1));
        }

        [Test]
        public void ShieldSwap_UsesNewSurfaceForImpacts()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(VIEW);
            try
            {
                var models = root.GetComponentsInChildren<StationLevelModel>(true).OrderBy(m => m.name).ToArray();
                var shield = root.GetComponentInChildren<Shield>(true);
                var filter = shield.GetComponent<MeshFilter>();
                shield.SetHull(filter, models[0].ShieldMesh, models[0].ShieldPlanes);
                Vector3 smallHit = shield.GetSurfacePosition(new Vector3(1000, 0, 0), Vector3.zero);
                shield.SetHull(filter, models[4].ShieldMesh, models[4].ShieldPlanes);
                Vector3 largeHit = shield.GetSurfacePosition(new Vector3(1000, 0, 0), Vector3.zero);
                Assert.That(largeHit.x, Is.GreaterThan(smallHit.x + 1));
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

[TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        public void DomeRepair_PreservesGeometryAndRepairsOnlyTheDarkAtlasStrip(int level)
        {
            string folder = "Assets/Art/Models/SpaceStations/RebelSpaceStation/Level" + level + "/";
            string name = "RebelSpaceStationLevel" + level;
            var raw = AssetDatabase.LoadAssetAtPath<GameObject>(folder + name + ".fbx");
            var renderer = raw.GetComponentsInChildren<MeshRenderer>(true).Single(r => r.name == "Level_0" + level);
            var source = renderer.GetComponent<MeshFilter>().sharedMesh;
            var repaired = AssetDatabase.LoadAssetAtPath<Mesh>(folder + name + "Hull.asset");
            Assert.That(repaired.vertices, Is.EqualTo(source.vertices));
            Assert.That(repaired.triangles, Is.EqualTo(source.triangles));
            Assert.That(repaired.normals, Is.EqualTo(source.normals));
            Assert.That(repaired.bounds, Is.EqualTo(source.bounds));
            var before = source.uv;
            var after = repaired.uv;
            var changed = Enumerable.Range(0, before.Length).Where(i => before[i] != after[i]).ToArray();
            Assert.That(changed, Has.Length.EqualTo(15));
            foreach (int index in changed)
            {
                Assert.That(before[index].y, Is.InRange(-.867f, -.587f));
                Assert.That(after[index].x, Is.InRange(.35f, .46f));
                Assert.That(after[index].y, Is.InRange(-.3f, 0));
                Vector3 point = renderer.transform.TransformPoint(source.vertices[index]);
                Assert.That(point.y, Is.InRange(3f, 5.05f));
            }
            Assert.That(repaired.tangents.All(t => !float.IsNaN(t.x) && !float.IsNaN(t.y) && !float.IsNaN(t.z)), Is.True);
        }

        private static bool References(SerializedProperty array, UnityEngine.Object reference) =>
            Enumerable.Range(0, array.arraySize).Any(i => array.GetArrayElementAtIndex(i).objectReferenceValue == reference);

        private sealed class RecordingHealthUpgrade : IHealthUpgrade
        {
            public int Level { get; private set; }
            public float HullScale { get; private set; }
            public void Upgrade(int level, float hullScale, float shieldsScale, float shieldRegenerateScale)
            {
                Level = level;
                HullScale = hullScale;
            }
        }
    }
}
