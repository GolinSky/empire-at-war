using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Editor;
using EmpireAtWar.ViewComponents.Health;
using EmpireAtWar.ViewComponents.Station;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

// Final, faction-independent step after a station's art/view builders: one shared level pivot, data-driven hardpoint
// mounts with type checks, launch exits, fitted shields and the gameplay view wiring.
public static class BuildStationLevels
{
    private const string CONFIGS = "Tools/Blender/SpaceStations/";
    private const float LAUNCH_DROP = 8f;
    private const float PIVOT_TOLERANCE = .01f;

    public static string Main(string faction)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Station construction requires Edit Mode.");
        var config = JObject.Parse(File.ReadAllText(CONFIGS + faction + ".json"));
        string viewPath = (string)config["view"];
        string[] levels = config["levels"].Select(l => (string)l).ToArray();
        string artPrefix = (string)config["artPrefix"];
        string alignmentAnchor = (string)config["alignmentAnchor"];
        var viewAsset = AssetDatabase.LoadAssetAtPath<GameObject>(viewPath);
        var hardPoints = viewAsset.GetComponentsInChildren<HardPoint>(true).ToDictionary(h => h.Id);
        var mounts = config["mounts"].Select(m => (Id: (int)m["hardPoint"], EmbeddedArt: m["art"] != null && m["art"].Type == JTokenType.Null,
            MissileSubstitution: (bool?)m["missileSubstitution"] ?? false,
            Anchors: ((JObject)m["anchors"]).Properties().ToDictionary(p => int.Parse(p.Name), p => (string)p.Value))).ToArray();
        if (!mounts.Select(m => m.Id).OrderBy(i => i).SequenceEqual(hardPoints.Keys.OrderBy(i => i)))
            throw new InvalidOperationException(faction + ": mount table must list every gameplay hardpoint exactly once.");
        foreach (var mount in mounts)
            if (mount.Anchors.Keys.Min() != hardPoints[mount.Id].UnlockLevel)
                throw new InvalidOperationException($"{faction}: hardpoint {mount.Id} unlocks at {hardPoints[mount.Id].UnlockLevel} but is first mounted at {mount.Anchors.Keys.Min()}.");

        var donorShield = viewAsset.GetComponentInChildren<Shield>(true);
        Vector3 pivot = Vector3.zero;
        Vector3 alignedPosition = Vector3.zero;
        var anchorPositions = new Dictionary<string, Vector3>();
        var report = new JArray();
        for (int level = 1; level <= levels.Length; level++)
        {
            var root = PrefabUtility.LoadPrefabContents(levels[level - 1]);
            try
            {
                // Original models share one origin; per-level centering would make the station jump on upgrade.
                Transform source = root.transform.Find(root.name);
                if (level == 1) pivot = source.localPosition;
                source.localPosition = pivot;
                if (alignmentAnchor != null)
                {
                    Transform anchor = source.GetComponentsInChildren<Transform>(true).Single(t => t.name == alignmentAnchor);
                    Vector3 position = root.transform.InverseTransformPoint(anchor.position);
                    if (level == 1) alignedPosition = position;
                    source.localPosition += alignedPosition - position;
                }
                PrefabUtility.RecordPrefabInstancePropertyModifications(source);

                var visible = root.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled).ToArray();
                Bounds bounds = BoundsOf(root.transform, visible);
                var transforms = root.GetComponentsInChildren<Transform>(true);
                var serializedMounts = new List<(int Id, Transform Point, GameObject Art)>();
                foreach (var mount in mounts.Where(m => m.Anchors.Keys.Min() <= level))
                {
                    string anchorName = mount.Anchors.Where(a => a.Key <= level).OrderBy(a => a.Key).Last().Value;
                    Transform point = transforms.Single(t => t.name == anchorName);
                    RequireMatchingType(faction, hardPoints[mount.Id], anchorName, mount.MissileSubstitution);
                    Vector3 position = root.transform.InverseTransformPoint(point.position);
                    if (anchorPositions.TryGetValue(anchorName, out Vector3 previous) && Vector3.Distance(previous, position) > PIVOT_TOLERANCE)
                        throw new InvalidOperationException($"{faction}: {anchorName} moves {Vector3.Distance(previous, position):F3} units at level {level}; levels do not share a pivot.");
                    anchorPositions[anchorName] = position;
                    serializedMounts.Add((mount.Id, point, mount.EmbeddedArt ? null : ArtOf(transforms, anchorName, artPrefix).gameObject));
                }

                Transform launch = root.transform.Find("GameplayLaunchExit");
                if (launch == null) launch = new GameObject("GameplayLaunchExit").transform;
                launch.SetParent(root.transform, false);
                Vector3 spawn = root.transform.InverseTransformPoint(transforms.Single(t => t.name == ((string)config["spawnAnchor"] ?? "Spawn_00")).position);
                launch.localPosition = new Vector3(spawn.x, bounds.min.y - LAUNCH_DROP, spawn.z);

                var surface = UnityEngine.Object.Instantiate(donorShield.gameObject, root.transform);
                var shield = surface.GetComponent<Shield>();
                ShieldHullBaker.Bake(root.transform, shield);
                var planes = new SerializedObject(shield).FindProperty("hullPlanes");

                var model = new SerializedObject(root.GetComponent<StationLevelModel>());
                Assign(model.FindProperty("<HullRenderers>k__BackingField"),
                    visible.Where(r => r.sharedMaterials.Any(m => m.shader.name == "EmpireAtWar/Ship Lit")).ToArray());
                model.FindProperty("<HullBounds>k__BackingField").boundsValue = bounds;
                var mountProperty = model.FindProperty("<Mounts>k__BackingField");
                mountProperty.arraySize = serializedMounts.Count;
                for (int i = 0; i < serializedMounts.Count; i++)
                {
                    var element = mountProperty.GetArrayElementAtIndex(i);
                    element.FindPropertyRelative("<HardPointId>k__BackingField").intValue = serializedMounts[i].Id;
                    element.FindPropertyRelative("<Point>k__BackingField").objectReferenceValue = serializedMounts[i].Point;
                    element.FindPropertyRelative("<Art>k__BackingField").objectReferenceValue = serializedMounts[i].Art;
                }
                model.FindProperty("<LaunchExit>k__BackingField").objectReferenceValue = launch;
                model.FindProperty("<ShieldMesh>k__BackingField").objectReferenceValue = surface.GetComponent<MeshFilter>().sharedMesh;
                model.FindProperty("<ShieldCenter>k__BackingField").vector3Value = surface.transform.localPosition;
                var targetPlanes = model.FindProperty("<ShieldPlanes>k__BackingField");
                targetPlanes.arraySize = planes.arraySize;
                for (int i = 0; i < planes.arraySize; i++) targetPlanes.GetArrayElementAtIndex(i).vector4Value = planes.GetArrayElementAtIndex(i).vector4Value;
                model.ApplyModifiedPropertiesWithoutUndo();
                UnityEngine.Object.DestroyImmediate(surface);
                PrefabUtility.SaveAsPrefabAsset(root, levels[level - 1]);
                report.Add(new JObject { ["level"] = level, ["mounts"] = serializedMounts.Count,
                    ["center"] = new JArray(bounds.center.x, bounds.center.y, bounds.center.z),
                    ["size"] = new JArray(bounds.size.x, bounds.size.y, bounds.size.z) });
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        var view = PrefabUtility.LoadPrefabContents(viewPath);
        try
        {
            var nested = view.GetComponentsInChildren<StationLevelModel>(true);
            var models = levels.Select(path => nested.Single(m => PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(m.gameObject) == path)).ToArray();
            var levelView = view.GetComponent<StationLevelView>();
            var shield = view.GetComponentInChildren<Shield>(true);
            var serializedView = new SerializedObject(levelView);
            Assign(serializedView.FindProperty("levelModels"), models);
            Assign(serializedView.FindProperty("hardPoints"), view.GetComponentsInChildren<HardPoint>(true).OrderBy(h => h.Id).ToArray());
            serializedView.FindProperty("hullCollider").objectReferenceValue = view.GetComponent<BoxCollider>();
            serializedView.FindProperty("launchPoint").objectReferenceValue = view.transform.Find("HangarLaunchPoint");
            serializedView.FindProperty("selectionMarker").objectReferenceValue = view.transform.Find("SelectedCanvas");
            serializedView.FindProperty("shield").objectReferenceValue = shield;
            serializedView.FindProperty("shieldMesh").objectReferenceValue = shield.GetComponent<MeshFilter>();
            Save(serializedView);
            var entity = new SerializedObject(view.GetComponent<EmpireAtWar.Entities.SpaceStation.SpaceStation>());
            entity.FindProperty("levelView").objectReferenceValue = levelView;
            Assign(entity.FindProperty("explosionHullRenderers"), models[0].HullRenderers);
            Save(entity);
            var fog = new SerializedObject(view.GetComponents<MonoBehaviour>().Single(c => c.GetType().Name == "FogVisibilityComponent"));
            fog.FindProperty("revealRadius").floatValue = models.Max(m => FarthestXZ(m.HullBounds));
            Save(fog);
            var health = new SerializedObject(view.GetComponents<MonoBehaviour>().Single(c => c.GetType().Name == "HealthComponent"));
            health.FindProperty("ionFieldBounds").boundsValue = models[0].HullBounds;
            Save(health);
            levelView.ApplyLevel(1);
            PrefabUtility.SaveAsPrefabAsset(view, viewPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(view); }
        AssetDatabase.SaveAssets();
        return faction + ": shared pivot, typed mounts, launch exits, shields and view wiring saved.\n" + report;
    }

    // Station attachment art hangs from HPnn_TYPE_BONE; its fire points are named FPnn_TYPE_00.
    private static Transform ArtOf(Transform[] transforms, string anchorName, string artPrefix)
    {
        string bone = anchorName.StartsWith("HP", StringComparison.OrdinalIgnoreCase)
            ? anchorName
            : "HP" + anchorName.Substring(2, anchorName.LastIndexOf('_') - 2) + "_BONE";
        Transform parent = transforms.Single(t => t.name.Equals(bone, StringComparison.OrdinalIgnoreCase));
        return parent.Cast<Transform>().Single(t => t.name.StartsWith(artPrefix, StringComparison.Ordinal));
    }

    private static void RequireMatchingType(string faction, HardPoint hardPoint, string anchorName, bool missileSubstitution)
    {
        string token = anchorName.Split('_')[1].ToUpperInvariant().TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
        hardPoint.TryGetWeaponType(out WeaponType weapon);
        string name = weapon.ToString();
        if (missileSubstitution)
        {
            if (token != "CCM" || weapon != WeaponType.ProtonTorpedo)
                throw new InvalidOperationException($"{faction}: only proton torpedoes may use an explicit CCM mount substitution.");
            return;
        }
        bool matches = token switch
        {
            "SHG" or "SHIELD" => hardPoint.HardPointType == HardPointType.ShieldGenerator,
            "TBL" or "TL" => name.Contains("TurboLaser"),
            "LC" => name.EndsWith("Laser") && !name.Contains("TurboLaser"),
            "CCM" or "CM" or "MIS" => weapon == WeaponType.ConcussionMissile,
            "PRT" or "TRP" => weapon == WeaponType.ProtonTorpedo,
            "IC" => name.Contains("IonCannon"),
            _ => throw new InvalidOperationException($"{faction}: unknown station anchor type {token} in {anchorName}.")
        };
        if (!matches) throw new InvalidOperationException($"{faction}: {anchorName} cannot mount {hardPoint.name} ({hardPoint.HardPointType}/{weapon}).");
    }

    private static Bounds BoundsOf(Transform root, MeshRenderer[] renderers)
    {
        var points = renderers.SelectMany(r => r.GetComponent<MeshFilter>().sharedMesh.vertices
            .Select(v => root.InverseTransformPoint(r.transform.TransformPoint(v)))).ToArray();
        Bounds bounds = new Bounds(points[0], Vector3.zero);
        foreach (var point in points.Skip(1)) bounds.Encapsulate(point);
        return bounds;
    }

    private static float FarthestXZ(Bounds bounds)
    {
        float x = Mathf.Max(Mathf.Abs(bounds.min.x), Mathf.Abs(bounds.max.x));
        float z = Mathf.Max(Mathf.Abs(bounds.min.z), Mathf.Abs(bounds.max.z));
        return new Vector2(x, z).magnitude;
    }

    private static void Assign(SerializedProperty property, UnityEngine.Object[] objects)
    {
        property.arraySize = objects.Length;
        for (int i = 0; i < objects.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = objects[i];
    }

    private static void Save(SerializedObject data) { data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(data.targetObject); }
}
