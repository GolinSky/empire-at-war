using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using EmpireAtWar.Editor;
using EmpireAtWar.Editor.Rendering;
using EmpireAtWar.ViewComponents.Health;

public static class IntegrateMiningFacilities
{
    const string VISUALS = "Assets/Prefabs/Models/MiningFacilities/Vanilla/";
    const string MINE = "Assets/Prefabs/Models/MiningFacilities/AsteroidMiningFacilityView.prefab";
    const string BATTLE = "Assets/Prefabs/Models/DefendStation/AsteroidDefendPlatformView.prefab";
    public static string Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Facility integration requires Edit Mode.");
        var report = new List<object>();
        foreach (string path in new[] {MINE, BATTLE})
        {
            bool battle = path == BATTLE;
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                root.transform.localScale = Vector3.one;
                var old = root.transform.Cast<Transform>().Single(t => t.name == (battle ? "Facility" : "Machinery") || t.name == (battle ? "BattleMiningFacility" : "MiningFacility"));
                UnityEngine.Object.DestroyImmediate(old.gameObject);
                var art = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(VISUALS + (battle ? "BattleMiningFacility" : "MiningFacility") + ".prefab"), root.transform);
                var hull = art.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled).ToArray();
                foreach (var t in art.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = root.layer;
                var bounds = BoundsOf(hull);
                var collider = root.GetComponents<BoxCollider>().Single(); collider.center = bounds.center; collider.size = bounds.size;
                var health = new SerializedObject(Component(root, "HealthComponent")); health.FindProperty("ionFieldBounds").boundsValue = bounds; Save(health);
                var shield = (Shield)health.FindProperty("shieldView").objectReferenceValue;
                ShieldHullBaker.Bake(root.transform, shield);
                SetArray(Component(root, "FogVisibilityComponent"), "renderers", hull.Cast<UnityEngine.Object>().Append(shield.GetComponents<MeshRenderer>().Single()).ToArray());
                SetArray(Component(root, "TeamColorView"), "meshRenderers", hull);
                SetArray(Component(root, "ConstructionComponent"), "renderers", hull);
                SetArray(Component(root, battle ? "DefendPlatform" : "MiningFacility"), "explosionHullRenderers", hull.Where(r => r.sharedMaterials.Any(m => m.shader.name == "EmpireAtWar/Ship Lit")).ToArray());
                var selection = root.transform.Cast<Transform>().Single(t => t.name == "SelectedCanvas");
                float diameter = Mathf.Max(bounds.size.x, bounds.size.z) * 1.12f;
                selection.localScale = Vector3.one;
                selection.localPosition = new Vector3(0, bounds.min.y - 2, 0);
                var select = new SerializedObject(Component(root, "SelectionComponent")); select.FindProperty("ringSize").vector2Value = Vector2.one * diameter; Save(select);
                var generator = root.GetComponentsInChildren<Transform>(true).Single(t => t.name == "ShieldGenerator");
                generator.position = new Vector3(0, bounds.max.y, 0);
                if (battle)
                {
                    var weapon = new SerializedObject(Component(root, "WeaponComponent"));
                    var points = weapon.FindProperty("hardPoints");
                    string[] mounts = {"HP_HT1_bone", "HP_HT2_bone", "HP_TL1_bone", "HP_TL2_bone", "HP_TL3_bone", "HP_TL4_bone"};
                    if (points.arraySize != mounts.Length) throw new InvalidOperationException("Expected six existing battle asteroid weapons.");
                    for (int i = 0; i < points.arraySize; i++)
                    {
                        var bone = art.transform.Find("BattleMiningFacilityModel/Root/" + mounts[i]);
                        var point = ((UnityEngine.Component)points.GetArrayElementAtIndex(i).objectReferenceValue).transform;
                        var muzzle = bone.GetComponentsInChildren<Transform>(true).First(t => t.name.StartsWith("MuzzleA_", StringComparison.Ordinal));
                        point.position = muzzle.position;
                        var direction = muzzle.position - bone.position; direction.y = 0;
                        point.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
                    }
                    var shieldBone = art.transform.Find("BattleMiningFacilityModel/Root/HP_SHG_bone"); generator.position = shieldBone.position;
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
                report.Add(new {path, size = bounds.size.ToString(), rootScale = root.transform.localScale.ToString(), hullRenderers = hull.Length});
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            var wreck = ShipWreckBuilder.Build(path);
            string dataPath = "Assets/Settings/Data/Models/Wrecks/Shared/" + (battle ? "AsteroidDefendPlatform" : "AsteroidMiningFacility") + "WreckData.asset";
            var data = new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(dataPath)); data.FindProperty("<Prefab>k__BackingField").objectReferenceValue = wreck; Save(data);
        }
        foreach (string name in new[] {"CaptureSite", "BattleAsteroidCaptureSite"})
        {
            string path = "Assets/Prefabs/View/CaptureSites/" + name + ".prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                root.transform.localScale = Vector3.one * 2;
                var config = new SerializedObject(Component(root, "CaptureSiteView"));
                var rocks = root.transform.Cast<Transform>().Single(t => t.name == "AsteroidRocks");
                var obstacles = config.FindProperty("rockObstacles");
                Type obstacleType = name == "BattleAsteroidCaptureSite" ? obstacles.GetArrayElementAtIndex(0).objectReferenceValue.GetType() : null;
                foreach (var child in rocks.Cast<Transform>().ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject);
                rocks.localPosition = new Vector3(0, name == "CaptureSite" ? -30 : 0, 0);
                rocks.localRotation = Quaternion.identity; rocks.localScale = Vector3.one;
                var asteroidArt = AssetDatabase.LoadAssetAtPath<GameObject>(VISUALS + (name == "CaptureSite" ? "MiningFacility" : "BattleMiningFacility") + ".prefab");
                var asteroid = asteroidArt.GetComponentsInChildren<MeshRenderer>(true).Single(r => r.name == "Asteroid");
                var rock = new GameObject("Asteroid"); rock.transform.SetParent(rocks, false);
                rock.transform.localPosition = asteroid.transform.position * .5f; rock.transform.localRotation = asteroid.transform.rotation; rock.transform.localScale = asteroid.transform.lossyScale * .5f;
                var mesh = asteroid.GetComponents<MeshFilter>().Single().sharedMesh;
                rock.AddComponent<MeshFilter>().sharedMesh = mesh; rock.AddComponent<MeshRenderer>().sharedMaterials = asteroid.sharedMaterials;
                obstacles.arraySize = obstacleType == null ? 0 : 1;
                if (obstacleType != null)
                {
                    rock.layer = LayerMask.NameToLayer("Obstacle");
                    var rockCollider = rock.AddComponent<MeshCollider>(); rockCollider.sharedMesh = mesh;
                    var obstacle = rock.AddComponent(obstacleType); var obstacleConfig = new SerializedObject(obstacle);
                    obstacleConfig.FindProperty("obstacleCollider").objectReferenceValue = rockCollider; Save(obstacleConfig);
                    obstacles.GetArrayElementAtIndex(0).objectReferenceValue = obstacle;
                }
                config.FindProperty("asteroidRocks").objectReferenceValue = rocks;
                Save(config);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                report.Add(new {path, constructionVisual = "Structure ConstructionComponent"});
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        foreach (string path in new[]{"Assets/Settings/Data/Models/MiningFacilities/AsteroidMiningFacilityData.asset", "Assets/Settings/Data/Models/DefendPlatform/AsteroidDefendPlatformData.asset"})
        {
            var data = new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path));
            var p = data.GetIterator();
            while (p.Next(true)) if (p.name == "<SpawnBlockRadius>k__BackingField") p.floatValue = path.Contains("MiningFacilities") ? 160 : 200;
            Save(data);
        }
        AssetDatabase.SaveAssets();
        string json = JsonConvert.SerializeObject(report, Formatting.Indented); File.WriteAllText("Temp/MiningFacilitiesImport/IntegrationReport.json", json); return json;
    }
    static UnityEngine.Component Component(GameObject root, string name) => root.GetComponentsInChildren<UnityEngine.Component>(true).Single(c => c != null && c.GetType().Name == name);
    static void Save(SerializedObject value) { value.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(value.targetObject); }
    static void SetArray(UnityEngine.Component component, string name, UnityEngine.Object[] items)
    {
        var config = new SerializedObject(component); var array = config.FindProperty(name); array.arraySize = items.Length;
        for (int i = 0; i < items.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = items[i]; Save(config);
    }
    static Bounds BoundsOf(MeshRenderer[] renderers) { var bounds = renderers[0].bounds; foreach (var r in renderers.Skip(1)) bounds.Encapsulate(r.bounds); return bounds; }
}
