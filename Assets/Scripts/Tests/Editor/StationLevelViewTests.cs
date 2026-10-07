using System.Linq;
using System.Reflection;
using EmpireAtWar.Components.AttackComponent;
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
        private const BindingFlags PRIVATE_INSTANCE = BindingFlags.Instance | BindingFlags.NonPublic;
        private const int LEVELS = 5;
        private const float PIVOT_TOLERANCE = .01f;
        private static readonly int[] ART_PIECES = { 5, 8, 11, 14, 18 };

        private static string ViewPath(string faction) => PREFABS + faction + "SpaceStationView.prefab";

        private static string LevelPath(string faction, int level) =>
            PREFABS + (faction == "Rebellion" ? "Rebel" : faction == "Separatist" ? "Cis" : faction) + "SpaceStationLevel" + level + ".prefab";

        private static StationLevelModel LoadModel(string faction, int level) =>
            AssetDatabase.LoadAssetAtPath<GameObject>(LevelPath(faction, level)).GetComponent<StationLevelModel>();

        [TestCase(1, 3656)]
        [TestCase(2, 4235)]
        [TestCase(3, 4641)]
        [TestCase(4, 6001)]
        [TestCase(5, 7407)]
        public void RebelLevel_UsesItsOriginalHullWithCompleteArt(int level, int triangles)
        {
            var model = LoadModel("Rebellion", level);
            var hull = model.HullRenderers.Single(r => r.name == "Level_0" + level);
            var mesh = hull.GetComponent<MeshFilter>().sharedMesh;
            Assert.That(model.HullRenderers, Has.Length.EqualTo(ART_PIECES[level - 1] + 1));
            Assert.That(mesh.triangles.Length / 3, Is.EqualTo(triangles));
            Assert.That(AssetDatabase.GetAssetPath(mesh), Does.EndWith("/Level" + level + "/RebelSpaceStationLevel" + level + "Hull.asset"));
            foreach (var renderer in model.HullRenderers.Where(r => r != hull))
            {
                Assert.That(AssetDatabase.GetAssetPath(renderer.GetComponent<MeshFilter>().sharedMesh), Does.Contain("/RebelSpaceStation/Attachments/"));
                Assert.That(renderer.transform.parent.parent.name.ToUpperInvariant(), Is.EqualTo(renderer.name.ToUpperInvariant() + "_BONE"));
                Assert.That(renderer.sharedMaterial, Is.SameAs(hull.sharedMaterial));
            }
            Assert.That(mesh.uv, Has.Length.EqualTo(mesh.vertexCount));
        }

        [TestCase(1, 6800)]
        [TestCase(2, 9236)]
        [TestCase(3, 9744)]
        [TestCase(4, 14160)]
        [TestCase(5, 16960)]
        public void RepublicLevel_UsesItsDistinctOriginalCumulativeHull(int level, int triangles)
        {
            var model = LoadModel("Republic", level);
            Assert.That(model.HullRenderers, Has.Length.EqualTo(level));
            Assert.That(model.HullRenderers.Sum(r => r.GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3), Is.EqualTo(triangles));
            foreach (var renderer in model.HullRenderers)
            {
                var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                string path = AssetDatabase.GetAssetPath(mesh);
                Assert.That(path, Does.EndWith("/Level" + level + "/RepublicSpaceStationLevel" + level + ".fbx"));
                Assert.That(((ModelImporter)AssetImporter.GetAtPath(path)).importNormals, Is.EqualTo(ModelImporterNormals.Import));
                Assert.That(mesh.normals, Has.Length.EqualTo(mesh.vertexCount));
            }
        }

        [Test]
        public void RepublicWreck_UsesOnlyFinalLevelHull()
        {
            var station = AssetDatabase.LoadAssetAtPath<GameObject>(ViewPath("Republic"));
            var levels = new SerializedObject(station.GetComponent<StationLevelView>()).FindProperty("levelModels");
            var finalModel = (StationLevelModel)levels.GetArrayElementAtIndex(LEVELS - 1).objectReferenceValue;
            var sources = EmpireAtWar.Editor.Rendering.ShipWreckBuilder.GetSourceRenderers(station);
            Assert.That(sources, Is.EquivalentTo(finalModel.HullRenderers));
            var wreck = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Wrecks/RepublicSpaceStationWreckView.prefab");
            Assert.That(wreck.transform.localScale, Is.EqualTo(Vector3.one));
            Assert.That(wreck.GetComponentsInChildren<MeshFilter>(true).Select(f => f.sharedMesh),
                Is.EquivalentTo(sources.Select(r => r.GetComponent<MeshFilter>().sharedMesh)));
        }

        [Test]
        public void RepublicEmbeddedArt_StaysVisibleWhenHardPointIsDestroyed()
        {
            var root = PrefabUtility.LoadPrefabContents(ViewPath("Republic"));
            try
            {
                var view = root.GetComponent<StationLevelView>();
                typeof(StationLevelView).GetMethod("Awake", PRIVATE_INSTANCE).Invoke(view, null);
                view.ApplyLevel(LEVELS);
                foreach (var point in root.GetComponentsInChildren<HardPoint>(true))
                {
                    point.SetInstalled(false);
                    point.UpdateData(0f);
                }
                view.ApplyLevel(LEVELS);
                Assert.That(view.CurrentModel.HullRenderers.All(r => r.gameObject.activeInHierarchy), Is.True);
                Assert.That(view.CurrentModel.Mounts.All(m => m.Art == null), Is.True);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        [Test]
        public void EachLevel_HasCompleteVisualAndGameplayData([Values("Rebellion", "Empire", "Republic", "Separatist")] string faction,
            [NUnit.Framework.Range(1, LEVELS)] int level)
        {
            var model = LoadModel(faction, level);
            var root = model.gameObject;
            Assert.That(root.transform.localScale, Is.EqualTo(Vector3.one));
            Assert.That(model.ShieldMesh, Is.Not.Null);
            Assert.That(model.ShieldPlanes, Has.Length.EqualTo(1024));
            Assert.That(model.LaunchExit.IsChildOf(root.transform), Is.True);
            Assert.That(model.LaunchExit.localPosition.y, Is.LessThan(model.HullBounds.min.y));
            Assert.That(root.GetComponentsInChildren<Transform>(true).Any(t => t.name == (faction == "Republic" ? "HP_Spawn_01" : "Spawn_00")), Is.True);
            Assert.That(root.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled)
                .SelectMany(r => r.sharedMaterials).All(m => m != null && m.shader != null), Is.True);
            Assert.That(root.GetComponentsInChildren<MeshRenderer>(true).Any(r => r.enabled && (r.name.Contains("Shadow") || r.name.EndsWith("_Blast"))), Is.False);
        }

        // EaW swaps station levels in place; the original models share one origin, so shared anchors never move.
        [TestCase("Rebellion")]
        [TestCase("Empire")]
        [TestCase("Republic")]
        [TestCase("Separatist")]
        public void Levels_ShareOnePivot(string faction)
        {
            Vector3? pivot = null;
            var anchors = new System.Collections.Generic.Dictionary<string, Vector3>();
            for (int level = 1; level <= LEVELS; level++)
            {
                var model = LoadModel(faction, level);
                Vector3 source = model.transform.Find(model.name).localPosition;
                if (faction == "Separatist")
                    source = model.transform.InverseTransformPoint(model.Mounts.Single(m => m.HardPointId == 0).Point.position);
                if (pivot.HasValue) Assert.That(Vector3.Distance(source, pivot.Value), Is.LessThan(PIVOT_TOLERANCE), "Level " + level);
                pivot = source;
                foreach (var point in model.Mounts.Select(m => m.Point))
                {
                    Vector3 position = model.transform.InverseTransformPoint(point.position);
                    if (anchors.TryGetValue(point.name, out Vector3 previous))
                        Assert.That(Vector3.Distance(previous, position), Is.LessThan(PIVOT_TOLERANCE), point.name + " at level " + level);
                    else anchors[point.name] = position;
                }
            }
        }

        [TestCase("Rebellion")]
        [TestCase("Empire")]
        [TestCase("Republic")]
        [TestCase("Separatist")]
        public void Mounts_MatchUnlockedHardPointTypesAndArt(string faction)
        {
            var hardPoints = AssetDatabase.LoadAssetAtPath<GameObject>(ViewPath(faction))
                .GetComponentsInChildren<HardPoint>(true).ToDictionary(h => h.Id);
            for (int level = 1; level <= LEVELS; level++)
            {
                var model = LoadModel(faction, level);
                Assert.That(model.Mounts.Select(m => m.HardPointId).OrderBy(i => i),
                    Is.EqualTo(hardPoints.Values.Where(h => h.UnlockLevel <= level).Select(h => h.Id).OrderBy(i => i)), "Level " + level);
                foreach (var mount in model.Mounts)
                {
                    Assert.That(mount.Point.IsChildOf(model.transform), Is.True);
                    if (faction == "Republic" || faction == "Separatist") Assert.That(mount.Art, Is.Null, "RaW embeds weapon art in the hull.");
                    else
                    {
                        Assert.That(mount.Art.transform.IsChildOf(model.transform), Is.True);
                        Assert.That(mount.Art.GetComponentsInChildren<MeshRenderer>(true), Is.Not.Empty);
                    }
                    bool missileSubstitution = faction == "Separatist" && new[] {8, 10, 15}.Contains(mount.HardPointId);
                    hardPoints[mount.HardPointId].TryGetWeaponType(out WeaponType weapon);
                    Assert.That(missileSubstitution ? mount.Point.name.Contains("_CCM_") && weapon == WeaponType.ProtonTorpedo
                        : MountTypeMatches(mount.Point.name, hardPoints[mount.HardPointId]), Is.True,
                        mount.Point.name + " mounts " + hardPoints[mount.HardPointId].name);
                }
            }
        }

        [Test]
        public void EntityLevelHandler_SwapsModelsWithoutReplacingGameplay([Values("Rebellion", "Empire", "Republic", "Separatist")] string faction,
            [Values(1, 5)] int initialLevel)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(ViewPath(faction));
            try
            {
                var station = root.GetComponent<SpaceStationEntity>();
                var view = root.GetComponent<StationLevelView>();
                var data = AssetDatabase.LoadAssetAtPath<SpaceStationData>("Assets/Settings/Data/Models/SpaceStation/SpaceStationData.asset");
                var selectionMarker = (RectTransform)new SerializedObject(view).FindProperty("selectionMarker").objectReferenceValue;
                var upgrade = new RecordingHealthUpgrade();
                var ionField = new RecordingIonField();
                typeof(SpaceStationEntity).GetField("_healthUpgrade", PRIVATE_INSTANCE).SetValue(station, upgrade);
                typeof(SpaceStationEntity).GetField("_ionField", PRIVATE_INSTANCE).SetValue(station, ionField);
                typeof(SpaceStationEntity).GetField("<Data>k__BackingField", PRIVATE_INSTANCE).SetValue(station, data);
                MethodInfo apply = typeof(SpaceStationEntity).GetMethod("ApplyLevel", PRIVATE_INSTANCE);
                var models = root.GetComponentsInChildren<StationLevelModel>(true);
                var hardpoints = root.GetComponentsInChildren<HardPoint>(true).OrderBy(h => h.Id).ToArray();
                var shield = root.GetComponentInChildren<Shield>(true);
                var launch = root.transform.Find("HangarLaunchPoint");
                var fog = new SerializedObject(root.GetComponents<MonoBehaviour>().Single(c => c.GetType().Name == "FogVisibilityComponent"));
                var team = new SerializedObject(root.GetComponents<MonoBehaviour>().Single(c => c.GetType().Name == "TeamColorView"));
                Assert.That(team.FindProperty("meshRenderers").arraySize, Is.EqualTo(root.GetComponentsInChildren<MeshRenderer>(true).Length));
                root.transform.SetPositionAndRotation(new Vector3(100, 20, -300), Quaternion.Euler(0, 60, 0));
                foreach (int level in new[] {initialLevel, 2, 3, 4, 5, 5})
                {
                    apply.Invoke(station, new object[] {level});
                    var current = view.CurrentModel;
                    Assert.That(models.Count(m => m.gameObject.activeSelf), Is.EqualTo(1));
                    Assert.That(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(current.gameObject), Is.EqualTo(LevelPath(faction, level)));
                    Assert.That(upgrade.Level, Is.EqualTo(level));
                    Assert.That(upgrade.HullScale, Is.EqualTo(data.GetLevelStats(level).HullMultiplier));
                    Assert.That(ionField.Bounds, Is.EqualTo(current.HullBounds));
                    Assert.That(root.GetComponent<BoxCollider>().center, Is.EqualTo(current.HullBounds.center));
                    Assert.That(root.GetComponent<BoxCollider>().size, Is.EqualTo(current.HullBounds.size));
                    float selectionSize = Mathf.Max(current.HullBounds.size.x, current.HullBounds.size.z);
                    Assert.That(selectionMarker.rect.width, Is.EqualTo(selectionSize).Within(.001f));
                    Assert.That(selectionMarker.rect.height, Is.EqualTo(selectionSize).Within(.001f));
                    Assert.That(shield.GetComponent<MeshFilter>().sharedMesh, Is.SameAs(current.ShieldMesh));
                    Assert.That(shield.transform.localPosition, Is.EqualTo(current.ShieldCenter));
                    Assert.That(Vector3.Distance(launch.position, current.LaunchExit.position), Is.LessThan(.001f));
                    foreach (var mount in current.Mounts)
                        Assert.That(Vector3.Distance(hardpoints.Single(h => h.Id == mount.HardPointId).transform.position, mount.Point.position), Is.LessThan(.001f));
                    var explosions = (Renderer[])typeof(SpaceStationEntity).GetField("explosionHullRenderers", PRIVATE_INSTANCE).GetValue(station);
                    Assert.That(explosions, Is.EqualTo(current.HullRenderers));
                    foreach (var renderer in current.GetComponentsInChildren<MeshRenderer>(true))
                    {
                        Assert.That(References(fog.FindProperty("renderers"), renderer), Is.True);
                        Assert.That(References(team.FindProperty("meshRenderers"), renderer), Is.True);
                    }
                    Assert.That(root.GetComponentsInChildren<HardPoint>(true).OrderBy(h => h.Id), Is.EqualTo(hardpoints));
                }
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        [TestCase("Rebellion")]
        [TestCase("Empire")]
        public void DestroyedHardPoint_HidesItsArtUntilRestored(string faction)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(ViewPath(faction));
            try
            {
                var view = root.GetComponent<StationLevelView>();
                typeof(StationLevelView).GetMethod("Awake", PRIVATE_INSTANCE).Invoke(view, null);
                view.ApplyLevel(LEVELS);
                foreach (var mount in view.CurrentModel.Mounts)
                {
                    var hardPoint = root.GetComponentsInChildren<HardPoint>(true).Single(h => h.Id == mount.HardPointId);
                    // Uninstalled views skip the destruction explosion, which needs runtime asset injection.
                    hardPoint.SetInstalled(false);
                    hardPoint.UpdateData(0f);
                    Assert.That(mount.Art.activeSelf, Is.False, mount.Art.name);
                    Assert.That(view.CurrentModel.Mounts.Where(m => m.HardPointId != mount.HardPointId).All(m => m.Art.activeSelf), Is.True);
                    view.ApplyLevel(LEVELS);
                    Assert.That(mount.Art.activeSelf, Is.False, "Level refresh must keep destroyed art hidden.");
                    hardPoint.UpdateData(1f);
                    Assert.That(mount.Art.activeSelf, Is.True, mount.Art.name);
                }
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        [Test]
        public void FactionMapping_UsesOwnModelsForEachLeveledFaction()
        {
            var mapping = new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/Settings/AssetMappingData.asset"));
            var rows = mapping.FindProperty("assetMappings.keyValue");
            var levels = AssetDatabase.LoadAssetAtPath<StationLevelData>("Assets/Settings/Data/Factions/Shared/StationLevelData.asset");
            foreach (var faction in new[] {FactionType.Rebellion, FactionType.Republic, FactionType.Empire, FactionType.Separatist})
            {
                var row = Enumerable.Range(0, rows.arraySize).Select(rows.GetArrayElementAtIndex).Single(p => p.FindPropertyRelative("key").stringValue == faction + "SpaceStationView");
                string path = AssetDatabase.GUIDToAssetPath(row.FindPropertyRelative("value.m_AssetGUID").stringValue);
                Assert.That(path, Is.EqualTo(ViewPath(faction.ToString())));
                var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                bool leveled = faction == FactionType.Rebellion || faction == FactionType.Empire || faction == FactionType.Republic || faction == FactionType.Separatist;
                Assert.That(root.GetComponent<StationLevelView>() != null, Is.EqualTo(leveled));
                if (!leveled) continue;
                Assert.That(new SerializedObject(root.GetComponent<StationLevelView>()).FindProperty("levelModels").arraySize, Is.EqualTo(levels.MaxLevel));
                Assert.That(root.GetComponentsInChildren<StationLevelModel>(true).Count(m => m.gameObject.activeSelf), Is.EqualTo(1));
            }
        }

        [TestCase("Rebellion")]
        [TestCase("Empire")]
        [TestCase("Republic")]
        [TestCase("Separatist")]
        public void ShieldSwap_UsesNewSurfaceForImpacts(string faction)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(ViewPath(faction));
            try
            {
                var view = root.GetComponent<StationLevelView>();
                var shield = root.GetComponentInChildren<Shield>(true);
                view.ApplyLevel(1);
                Vector3 small = view.CurrentModel.HullBounds.center;
                Vector3 smallHit = shield.GetSurfacePosition(small + new Vector3(1000, 0, 0), small);
                Assert.That(view.CurrentModel.HullBounds.Contains(small), Is.True);
                view.ApplyLevel(LEVELS);
                Vector3 large = view.CurrentModel.HullBounds.center;
                Vector3 largeHit = shield.GetSurfacePosition(large + new Vector3(1000, 0, 0), large);
                Assert.That(largeHit.x - large.x, Is.GreaterThan(smallHit.x - small.x + 1));
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

        // Anchor names carry the source weapon class: FPnn_TYPE_00, or HPnn_SHG_Bone for the shield generator.
        private static bool MountTypeMatches(string anchor, HardPoint hardPoint)
        {
            hardPoint.TryGetWeaponType(out WeaponType weapon);
            string name = weapon.ToString();
            return anchor.Split('_')[1].ToUpperInvariant().TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9') switch
            {
                "SHG" or "SHIELD" => hardPoint.HardPointType == HardPointType.ShieldGenerator,
                "TBL" or "TL" => name.Contains("TurboLaser"),
                "LC" => name.EndsWith("Laser") && !name.Contains("TurboLaser"),
                "CCM" or "CM" or "MIS" => weapon == WeaponType.ConcussionMissile,
                "PRT" or "TRP" => weapon == WeaponType.ProtonTorpedo,
                "IC" => name.Contains("IonCannon"),
                _ => false
            };
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

        private sealed class RecordingIonField : IIonFieldShape
        {
            public Bounds Bounds { get; private set; }
            public void SetIonFieldBounds(Bounds bounds) => Bounds = bounds;
        }
    }
}
