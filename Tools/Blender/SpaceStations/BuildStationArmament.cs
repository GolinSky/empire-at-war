using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EmpireAtWar.ViewComponents.Health;
using EmpireAtWar.ViewComponents.Station;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

// Run after BuildStationLevels. Keeps original IDs and artwork; extra mounts sit on hull triangles.
public static class BuildStationArmament
{
    private const string CONFIGS = "Tools/Blender/SpaceStations/";
    private const string GENERATED = "AdditionalWeaponMounts";
    private static readonly int[] _unlockLevels = { 1, 2, 3, 3, 4, 4, 5, 5 };

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
            foreach (var group in originals.GroupBy(h => h.WeaponType))
            {
                var existing = view.GetComponentsInChildren<WeaponHardPoint>(true).Where(h => h.WeaponType == group.Key).OrderBy(h => h.Id).ToList();
                while (existing.Count < _unlockLevels.Length)
                {
                    var clone = UnityEngine.Object.Instantiate(group.First().gameObject, group.First().transform.parent);
                    clone.name = group.Key + "_" + nextId;
                    var hardPoint = clone.GetComponent<WeaponHardPoint>();
                    var data = new SerializedObject(hardPoint);
                    data.FindProperty("<Id>k__BackingField").intValue = nextId++;
                    data.ApplyModifiedPropertiesWithoutUndo();
                    existing.Add(hardPoint);
                }
                for (int index = 0; index < existing.Count; index++)
                {
                    var data = new SerializedObject(existing[index]);
                    data.FindProperty("unlockLevel").intValue = _unlockLevels[index];
                    data.ApplyModifiedPropertiesWithoutUndo();
                }
                weapons.AddRange(existing);
            }
            var points = view.GetComponentsInChildren<HardPoint>(true).OrderBy(h => h.Id).ToArray();
            var weaponById = weapons.ToDictionary(h => h.Id);
            var donorModel = AssetDatabase.LoadAssetAtPath<GameObject>(levels[4]).GetComponent<StationLevelModel>();
            var donorMounts = donorModel.Mounts.Where(m => m.HardPointId > 0 && m.HardPointId < 17).ToArray();
            for (int level = 1; level <= levels.Length; level++)
            {
                var root = PrefabUtility.LoadPrefabContents(levels[level - 1]);
                try
                {
                    var model = root.GetComponent<StationLevelModel>();
                    var sourceIds = config["mounts"].Where(m => ((JObject)m["anchors"]).Properties().Min(p => int.Parse(p.Name)) <= level)
                        .Select(m => (int)m["hardPoint"]).ToArray();
                    var oldMounts = model.Mounts.Where(m => sourceIds.Contains(m.HardPointId)).ToArray();
                    Transform previous = root.transform.Find(GENERATED);
                    var hull = model.HullRenderers.OfType<MeshRenderer>().Where(r => r.enabled && (previous == null || !r.transform.IsChildOf(previous))).ToArray();
                    var plating = hull.Where(r => !oldMounts.Any(m => m.Art != null && r.transform.IsChildOf(m.Art.transform))).ToArray();
                    if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
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
                        Vector3 position = StationHardPointSurface.Place(root.transform, plating, preferred, occupied, spacing, hull);
                        var anchor = new GameObject("HardPoint" + hardPoint.Id).transform;
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
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
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
