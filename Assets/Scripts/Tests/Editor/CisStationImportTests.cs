using System.Linq;
using System.Reflection;
using EmpireAtWar.ViewComponents.Health;
using EmpireAtWar.ViewComponents.Station;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class CisStationImportTests
    {
        private const string PREFABS = "Assets/Prefabs/Models/Stations/";
        private const string MODELS = "Assets/Art/Models/SpaceStations/CisSpaceStation/";
        private const string MATERIALS = "Assets/Art/Materials/Models/SpaceStations/CisSpaceStation/";
        private const BindingFlags PRIVATE_INSTANCE = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly int[] HULL_TRIANGLES = {3442, 6364, 7180, 9603, 12869};
        private static readonly int[] BONE_COUNTS = {98, 181, 183, 183, 183};

        [Test]
        public void OriginalLevel_UsesItsDistinctHullAndImportedNormals([NUnit.Framework.Range(1, 5)] int level)
        {
            string name = "CisSpaceStationLevel" + level;
            string path = MODELS + "Level" + level + "/" + name + ".fbx";
            var raw = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var hull = raw.GetComponentsInChildren<MeshRenderer>(true).Single(r => r.name == "seb_station_lvl_" + level);
            Mesh mesh = hull.GetComponent<MeshFilter>().sharedMesh;
            Assert.That(mesh.triangles.Length / 3, Is.EqualTo(HULL_TRIANGLES[level - 1]));
            Assert.That(mesh.normals, Has.Length.EqualTo(mesh.vertexCount));
            Assert.That(mesh.uv, Has.Length.EqualTo(mesh.vertexCount));
            Assert.That(((ModelImporter)AssetImporter.GetAtPath(path)).importNormals, Is.EqualTo(ModelImporterNormals.Import));
            Assert.That(raw.GetComponentsInChildren<Transform>(true).Count(t => t.GetComponent<Renderer>() == null) - 2,
                Is.EqualTo(BONE_COUNTS[level - 1]), "Exclude the FBX and armature object roots.");
            var visual = AssetDatabase.LoadAssetAtPath<GameObject>(PREFABS + name + ".prefab");
            var visibleHull = visual.GetComponentsInChildren<MeshRenderer>(true).Single(r => r.name == hull.name);
            Assert.That(visibleHull.GetComponent<MeshFilter>().sharedMesh, Is.SameAs(mesh));
            Assert.That(visibleHull.sharedMaterial.shader.name, Is.EqualTo("EmpireAtWar/Ship Lit"));
            Assert.That(visibleHull.sharedMaterial, Is.SameAs(AssetDatabase.LoadAssetAtPath<Material>(MATERIALS + "CisSpaceStation_CisStationSlot00.mat")));
        }

        [Test]
        public void Girders_UseTheirOwnCutoutTexture([NUnit.Framework.Range(2, 5)] int level)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(PREFABS + "CisSpaceStationLevel" + level + ".prefab");
            var girder = model.GetComponentsInChildren<MeshRenderer>(true).Single(r => r.name == "girders");
            Assert.That(girder.enabled, Is.True);
            Assert.That(girder.sharedMaterial.GetTexture("_BaseMap").name, Is.EqualTo("CisSpaceStation_seb_girder"));
            Assert.That(girder.sharedMaterial.GetFloat("_AlphaClip"), Is.EqualTo(1));
            Assert.That(girder.sharedMaterial.IsKeywordEnabled("_ALPHATEST_ON"), Is.True);
        }

        [Test]
        public void CisModels_AreDependenciesOfOnlyTheSeparatistStation()
        {
            foreach (string faction in new[] {"Republic", "Separatist", "Rebellion", "Empire"})
            {
                string path = PREFABS + faction + "SpaceStationView.prefab";
                int cisModels = AssetDatabase.GetDependencies(path, true).Count(p => p.StartsWith(MODELS) && p.EndsWith(".fbx"));
                Assert.That(cisModels, Is.EqualTo(faction == "Separatist" ? 5 : 0), faction);
            }
        }

        [Test]
        public void StaticWreck_UsesOnlyTheHighestLevelHull()
        {
            var wreck = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Wrecks/SeparatistSpaceStationWreckView.prefab");
            var hulls = wreck.GetComponentsInChildren<MeshRenderer>(true);
            Assert.That(hulls, Has.Length.EqualTo(1), "Inactive station levels must not overlap in the fixed faction wreck.");
            string path = AssetDatabase.GetAssetPath(hulls[0].GetComponent<MeshFilter>().sharedMesh);
            Assert.That(path, Is.EqualTo(MODELS + "Level5/CisSpaceStationLevel5.fbx"));
            Assert.That(wreck.transform.localScale, Is.EqualTo(Vector3.one));
            Assert.That(hulls[0].sharedMaterial.shader.name, Is.EqualTo("EmpireAtWar/Ship Wreck"));
        }

        [Test]
        public void EmbeddedHardPointDamage_KeepsTheHullAndSourceBonesVisible()
        {
            var root = PrefabUtility.LoadPrefabContents(PREFABS + "SeparatistSpaceStationView.prefab");
            try
            {
                var view = root.GetComponent<StationLevelView>();
                typeof(StationLevelView).GetMethod("Awake", PRIVATE_INSTANCE).Invoke(view, null);
                view.ApplyLevel(5);
                foreach (var mount in view.CurrentModel.Mounts)
                {
                    Assert.That(mount.Art, Is.Null);
                    var hardpoint = root.GetComponentsInChildren<HardPoint>(true).Single(h => h.Id == mount.HardPointId);
                    hardpoint.SetInstalled(false);
                    hardpoint.UpdateData(0f);
                    view.ApplyLevel(5);
                    Assert.That(mount.Point.gameObject.activeInHierarchy, Is.True);
                    Assert.That(view.CurrentModel.HullRenderers.All(r => r.enabled && r.gameObject.activeInHierarchy), Is.True);
                    hardpoint.UpdateData(1f);
                }
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}
