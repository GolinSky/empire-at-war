using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.ViewComponents.Health;
using EmpireAtWar.ViewComponents.Station;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

// Run after BuildStationLevels. Keeps original IDs, unlock levels and artwork; extra mounts sit on hull triangles
// and keep the position chosen at their unlock level on every later level.
public static class BuildStationArmament
{
    private const string CONFIGS = "Tools/Blender/SpaceStations/";
    private const string GENERATED = "AdditionalWeaponMounts";
    // Matches the inspection's hull surface tolerance.
    private const float SURFACE_MATCH = .02f;
    private const int MAX_PLACEMENT_ATTEMPTS = 64;
    private static readonly int[] _installedPerLevel = { 1, 2, 4, 6, 8 };

    public static string Main(string faction)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Station construction requires Edit Mode.");
        var config = JObject.Parse(File.ReadAllText(CONFIGS + faction + ".json"));
        string viewPath = (string)config["view"];
        string[] levels = config["levels"].Select(p => (string)p).ToArray();
        var view = PrefabUtility.LoadPrefabContents(viewPath);
        var report = new JArray();
        try
        {
            var originals = view.GetComponentsInChildren<WeaponHardPoint>(true).Where(h => h.Id < 17).OrderBy(h => h.Id).ToArray();
            var weapons = new List<WeaponHardPoint>();
            int nextId = view.GetComponentsInChildren<HardPoint>(true).Max(h => h.Id) + 1;
            var authoredUnlocks = config["mounts"].ToDictionary(m => (int)m["hardPoint"],
                m => ((JObject)m["anchors"]).Properties().Min(p => int.Parse(p.Name)));
            foreach (var group in originals.GroupBy(h => h.WeaponType))
            {
                var existing = view.GetComponentsInChildren<WeaponHardPoint>(true).Where(h => h.WeaponType == group.Key).OrderBy(h => h.Id).ToList();
                while (existing.Count < _installedPerLevel[_installedPerLevel.Length - 1])
                {
                    var clone = UnityEngine.Object.Instantiate(group.First().gameObject, group.First().transform.parent);
                    clone.name = group.Key + "_" + nextId;
                    var hardPoint = clone.GetComponent<WeaponHardPoint>();
                    var data = new SerializedObject(hardPoint);
                    data.FindProperty("<Id>k__BackingField").intValue = nextId++;
                    data.ApplyModifiedPropertiesWithoutUndo();
                    existing.Add(hardPoint);
                }
                // Originals unlock with their source anchor; clones fill each level up to the installed count.
                foreach (var original in group) SetUnlockLevel(original, authoredUnlocks[original.Id]);
                var clones = new Queue<WeaponHardPoint>(existing.Where(h => h.Id >= 17));
                for (int level = 1; level <= _installedPerLevel.Length; level++)
                {
                    int missing = _installedPerLevel[level - 1] - group.Count(h => authoredUnlocks[h.Id] <= level)
                        - existing.Count(h => h.Id >= 17 && !clones.Contains(h));
                    if (missing < 0) throw new InvalidOperationException($"{faction} authors more {group.Key} mounts than level {level} installs.");
                    for (int index = 0; index < missing; index++) SetUnlockLevel(clones.Dequeue(), level);
                }
                weapons.AddRange(existing);
            }
            var points = view.GetComponentsInChildren<HardPoint>(true).OrderBy(h => h.Id).ToArray();
            var weaponById = weapons.ToDictionary(h => h.Id);
            var donorModel = AssetDatabase.LoadAssetAtPath<GameObject>(levels[4]).GetComponent<StationLevelModel>();
            var donorMounts = donorModel.Mounts.Where(m => m.HardPointId > 0 && m.HardPointId < 17).ToArray();
            var placed = new Dictionary<int, Vector3>();
            // Every level is open at once: a new mount must sit on the hull of its unlock level and of every later level.
            var roots = levels.Select(PrefabUtility.LoadPrefabContents).ToArray();
            var levelOldMounts = new StationMount[levels.Length][];
            var levelHulls = new MeshRenderer[levels.Length][];
            var levelPlating = new MeshRenderer[levels.Length][];
            try
            {
                for (int level = 1; level <= levels.Length; level++)
                {
                    var model = roots[level - 1].GetComponent<StationLevelModel>();
                    var sourceIds = config["mounts"].Where(m => ((JObject)m["anchors"]).Properties().Min(p => int.Parse(p.Name)) <= level)
                        .Select(m => (int)m["hardPoint"]).ToArray();
                    var oldMounts = model.Mounts.Where(m => sourceIds.Contains(m.HardPointId)).ToArray();
                    Transform previous = roots[level - 1].transform.Find(GENERATED);
                    var hull = model.HullRenderers.OfType<MeshRenderer>().Where(r => r.enabled && (previous == null || !r.transform.IsChildOf(previous))).ToArray();
                    levelOldMounts[level - 1] = oldMounts;
                    levelHulls[level - 1] = hull;
                    levelPlating[level - 1] = hull.Where(r => !oldMounts.Any(m => m.Art != null && r.transform.IsChildOf(m.Art.transform))).ToArray();
                    if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
                }

                bool OnLaterHulls(int level, Vector3 position) => Enumerable.Range(level, levels.Length - level)
                    .All(later => Vector3.Distance(position, StationHardPointSurface.Place(roots[later].transform, levelPlating[later],
                        position, Array.Empty<Vector3>(), 0f, levelHulls[later])) <= SURFACE_MATCH);

                for (int level = 1; level <= levels.Length; level++)
                {
                    var root = roots[level - 1];
                    var model = root.GetComponent<StationLevelModel>();
                    var oldMounts = levelOldMounts[level - 1];
                    var hull = levelHulls[level - 1];
                    var plating = levelPlating[level - 1];
                    var generated = new GameObject(GENERATED).transform;
                    generated.SetParent(root.transform, false);
                    var mounts = new List<(int Id, Transform Point, GameObject Art)>();
                    var occupied = oldMounts.Select(m => root.transform.InverseTransformPoint(m.Point.position)).ToList();
                    float spacing = Mathf.Clamp(Mathf.Max(model.HullBounds.size.x, model.HullBounds.size.z) * .025f, 4f, 10f);
                    mounts.AddRange(oldMounts.Select(m => (m.HardPointId, m.Point, m.Art)));
                    foreach (var hardPoint in weapons.Where(h => h.UnlockLevel <= level).OrderBy(h => h.Id))
                    {
                        if (oldMounts.Any(m => m.HardPointId == hardPoint.Id)) continue;
                        var nearby = oldMounts.Where(m => weaponById.ContainsKey(m.HardPointId) && weaponById[m.HardPointId].WeaponType == hardPoint.WeaponType).ToArray();
                        var donor = donorMounts.First(m => weaponById[m.HardPointId].WeaponType == hardPoint.WeaponType);
                        Vector3 preferred = nearby.Length > 0
                            ? root.transform.InverseTransformPoint(nearby[hardPoint.Id % nearby.Length].Point.position)
                            : donorModel.transform.InverseTransformPoint(donor.Point.position);
                        if (!placed.TryGetValue(hardPoint.Id, out Vector3 position))
                        {
                            var rejected = new List<Vector3>(occupied);
                            for (int attempt = 0; ; attempt++)
                            {
                                if (attempt == MAX_PLACEMENT_ATTEMPTS)
                                    throw new InvalidOperationException($"{faction}: no hull surface for hardpoint {hardPoint.Id} from level {level} on.");
                                position = StationHardPointSurface.Place(root.transform, plating, preferred, rejected, spacing, hull);
                                if (OnLaterHulls(level, position)) break;
                                rejected.Add(position);
                            }
                            placed[hardPoint.Id] = position;
                        }
                        // Anchor names carry the weapon class like source anchors: GPnn_TYPE_00.
                        var anchor = new GameObject($"GP{hardPoint.Id:00}_{MountCode(hardPoint.WeaponType)}_00").transform;
                        anchor.SetParent(generated, false);
                        anchor.localPosition = position;
                        // Embedded weapon geometry has no separable artwork, matching existing Republic/CIS mounts.
                        GameObject art = null;
                        if (donor.Art != null)
                        {
                            art = UnityEngine.Object.Instantiate(donor.Art, generated);
                            art.name = "WeaponArt" + hardPoint.Id;
                            art.SetActive(true);
                            art.transform.position = root.transform.TransformPoint(position) + donor.Art.transform.position - donor.Point.position;
                            art.transform.rotation = donor.Art.transform.rotation;
                            art.transform.localScale = donor.Art.transform.lossyScale;
                            var artRenderers = art.GetComponentsInChildren<MeshRenderer>(true);
                            Bounds artBounds = artRenderers[0].bounds;
                            foreach (var renderer in artRenderers.Skip(1)) artBounds.Encapsulate(renderer.bounds);
                            art.transform.position += root.transform.TransformPoint(position) - new Vector3(artBounds.center.x, artBounds.min.y, artBounds.center.z);
                        }
                        occupied.Add(position);
                        mounts.Add((hardPoint.Id, anchor, art));
                    }
                    var data = new SerializedObject(model);
                    var array = data.FindProperty("<Mounts>k__BackingField");
                    array.arraySize = mounts.Count;
                    for (int index = 0; index < mounts.Count; index++)
                    {
                        var item = array.GetArrayElementAtIndex(index);
                        item.FindPropertyRelative("<HardPointId>k__BackingField").intValue = mounts[index].Id;
                        item.FindPropertyRelative("<Point>k__BackingField").objectReferenceValue = mounts[index].Point;
                        item.FindPropertyRelative("<Art>k__BackingField").objectReferenceValue = mounts[index].Art;
                    }
                    Assign(data.FindProperty("<HullRenderers>k__BackingField"), hull.Concat(generated.GetComponentsInChildren<MeshRenderer>(true)).ToArray());
                    data.ApplyModifiedPropertiesWithoutUndo();
                    PrefabUtility.SaveAsPrefabAsset(root, levels[level - 1]);
                    report.Add(new JObject { ["level"] = level, ["hardpoints"] = mounts.Count, ["surfaceMounts"] = mounts.Count - oldMounts.Length });
                }
            }
            finally { foreach (var root in roots) PrefabUtility.UnloadPrefabContents(root); }
            var components = view.GetComponentsInChildren<MonoBehaviour>(true);
            SetArray(components.Single(c => c.GetType().Name == "HealthComponent"), "<ShipUnits>k__BackingField", points);
            SetArray(components.Single(c => c.GetType().Name == "WeaponComponent"), "hardPoints", weapons.OrderBy(h => h.Id).ToArray());
            SetArray(view.GetComponent<StationLevelView>(), "hardPoints", points);
            foreach (var hardPoint in points.Where(h => h.Id >= 17))
            {
                var anchor = (Transform)new SerializedObject(hardPoint).FindProperty("destroyedVfxAnchor").objectReferenceValue;
                anchor.localPosition = Vector3.zero;
            }
            var bindings = components.Single(c => c.GetType().Name == "EntityBindings");
            var bindingsData = new SerializedObject(bindings);
            var injectables = bindingsData.FindProperty("<Injectables>k__BackingField");
            var retained = Enumerable.Range(0, injectables.arraySize).Select(i => injectables.GetArrayElementAtIndex(i).objectReferenceValue).Where(o => !(o is HardPoint));
            Assign(injectables, retained.Concat(points).ToArray());
            bindingsData.ApplyModifiedPropertiesWithoutUndo();
            // Reopen the view after model saves so nested instances include the newly saved mounts/art.
            PrefabUtility.SaveAsPrefabAsset(view, viewPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(view); }
        view = PrefabUtility.LoadPrefabContents(viewPath);
        try
        {
            var renderers = view.GetComponentsInChildren<MeshRenderer>(true);
            var components = view.GetComponentsInChildren<MonoBehaviour>(true);
            SetArray(components.Single(c => c.GetType().Name == "UnitRenderStateComponent"), "meshRenderers", renderers);
            SetArray(components.Single(c => c.GetType().Name == "FogVisibilityComponent"), "renderers", renderers);
            view.GetComponent<StationLevelView>().ApplyLevel(1);
            PrefabUtility.SaveAsPrefabAsset(view, viewPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(view); }
        AssetDatabase.SaveAssets();
        return faction + ": saved 1/2/4/6/8 of every weapon type; " + report;
    }

    private static void SetUnlockLevel(HardPoint hardPoint, int level)
    {
        var data = new SerializedObject(hardPoint);
        data.FindProperty("unlockLevel").intValue = level;
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    private static string MountCode(WeaponType weapon)
    {
        string name = weapon.ToString();
        if (name.Contains("TurboLaser")) return "TBL";
        if (name.Contains("IonCannon")) return "IC";
        if (weapon == WeaponType.ConcussionMissile) return "CM";
        if (weapon == WeaponType.ProtonTorpedo) return "PRT";
        return "LC";
    }

    private static void SetArray(UnityEngine.Object target, string field, UnityEngine.Object[] values)
    {
        var data = new SerializedObject(target);
        Assign(data.FindProperty(field), values);
        data.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void Assign(SerializedProperty property, UnityEngine.Object[] values)
    {
        property.arraySize = values.Length;
        for (int index = 0; index < values.Length; index++) property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
    }
}
