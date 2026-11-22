using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.ViewComponents.Health;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

/// <summary>Read-only audit of the saved Viscount assets. Run with --entry ViscountVerify.Run.</summary>
public static class ViscountVerify
{
    public static string Run()
    {
        var report = new StringBuilder();
        var failures = new List<string>();
        foreach (string name in new[] { "ViscountPrototype", "ViscountDreadnought" })
            Check(name, report, failures);
        report.AppendLine(failures.Count == 0 ? "ALL CHECKS PASSED" : "FAILURES:\n  " + string.Join("\n  ", failures));
        return report.ToString();
    }

    private static void Check(string name, StringBuilder report, List<string> failures)
    {
        void Expect(bool condition, string message) { if (!condition) failures.Add(name + ": " + message); }
        string viewPath = $"Assets/Prefabs/Models/Ships/Rebellion/View/{name}ShipView.prefab";
        var data = AssetDatabase.LoadAssetAtPath<ShipData>($"Assets/Settings/Data/Ship/Rebellion/{name}ShipData.asset");
        GameObject root = PrefabUtility.LoadPrefabContents(viewPath);
        try
        {
            HardPoint[] points = root.GetComponentsInChildren<HardPoint>(true);
            WeaponHardPoint[] weapons = points.OfType<WeaponHardPoint>().ToArray();
            var health = root.GetComponentsInChildren<MonoBehaviour>(true).Single(c => c.GetType().Name == "HealthComponent");
            var units = Refs(health, "<ShipUnits>k__BackingField");
            var weaponRefs = Refs(root.GetComponentsInChildren<MonoBehaviour>(true).Single(c => c.GetType().Name == "WeaponComponent"), "hardPoints");
            var injectables = Refs(root.GetComponent<MonoBehaviour>() is var _ ? root.GetComponentsInChildren<MonoBehaviour>(true).Single(c => c.GetType().Name == "EntityBindings") : null, "<Injectables>k__BackingField");
            Expect(units.Count == points.Length && points.All(units.Contains), "ShipUnits != all hardpoints");
            Expect(weaponRefs.Count == weapons.Length && weapons.All(weaponRefs.Contains), "WeaponComponent.hardPoints != weapons");
            Expect(points.All(injectables.Contains), "hardpoint missing from Injectables");
            Expect(points.Select(p => p.Id).Distinct().Count() == points.Length, "duplicate hardpoint ids");
            var anchors = points.Select(p => (Transform)new SerializedObject(p).FindProperty("destroyedVfxAnchor").objectReferenceValue).ToArray();
            Expect(anchors.All(a => a != null && a.root == root.transform), "anchor missing or outside prefab");
            Expect(anchors.Distinct().Count() == anchors.Length, "anchors shared between hardpoints");
            Expect(points.All(p => p.HealthOverride == 0f && p.HealthWeight > 0f && p.UnlockLevel == 1), "health override/weight/unlock");

            var loadout = weapons.GroupBy(w => (int)w.WeaponType).ToDictionary(g => g.Key, g => g.Count());
            var baked = data.WeaponLoadout.ToDictionary(e => (int)e.WeaponType, e => e.Count);
            Expect(loadout.Count == baked.Count && loadout.All(kv => baked.TryGetValue(kv.Key, out int c) && c == kv.Value), "baked loadout != mounts");

            var body = root.transform.Find("BodyPivot");
            var engines = points.Where(p => p.HardPointType == HardPointType.Engines).ToArray();
            float engineZ = engines.Average(p => root.transform.InverseTransformPoint(p.transform.position).z);
            Expect(engineZ < 0f, "engines not aft (bow must be +Z)");
            Expect(points.Count(p => p.HardPointType == HardPointType.ShieldGenerator) > 0, "no shield generator");
            Expect(points.Count(p => p.HardPointType == HardPointType.Hangar) > 0 == data.HangarBays.Count > 0, "hangar hardpoints vs bays");

            var move = new SerializedObject(root.GetComponentsInChildren<MonoBehaviour>(true).Single(c => c.GetType().Name == "ShipMoveComponent"));
            var fog = Refs(root.GetComponentsInChildren<MonoBehaviour>(true).Single(c => c.GetType().Name == "FogVisibilityComponent"), "renderers");
            var plumes = move.FindProperty("enginePlumes");
            var trails = root.GetComponentsInChildren<TrailRenderer>(true);
            Expect(plumes.arraySize > 0 && trails.Length == plumes.arraySize && trails.All(fog.Contains), "engine plumes not listed/fogged");

            Bounds hull = default; bool has = false;
            foreach (MeshRenderer r in root.GetComponentsInChildren<MeshRenderer>())
            {
                if (!r.enabled || r.GetComponent<Shield>() != null || r.localBounds.extents == Vector3.zero) continue;
                Bounds b = r.bounds;
                if (!has) { hull = b; has = true; } else hull.Encapsulate(b);
            }
            var selection = new SerializedObject(root.GetComponentsInChildren<MonoBehaviour>(true).Single(c => c.GetType().Name == "SelectionComponent"));
            float ring = selection.FindProperty("ringSize").vector2Value.x * 0.8f * (1038f / 1254f) / Mathf.Max(hull.size.x, hull.size.z);
            Expect(ring >= 0.88f && ring <= 0.96f, $"selection ring ratio {ring:F3}");

            int helpers = root.GetComponentsInChildren<MeshRenderer>(true).Count(r => r.name.StartsWith("Shadow") || r.name == "collision" || r.name == "Shield" && r.GetComponent<Shield>() == null);
            Expect(helpers == 0, "helper meshes left in view");

            report.AppendLine($"{name}: hull={data.Hull} shields={data.Shields} class={data.ShipClass} regen={data.ShieldRegenerateValue} speed={data.Speed} " +
                              $"weapons={weapons.Length} engines={engines.Length} shields={points.Count(p => p.HardPointType == HardPointType.ShieldGenerator)} " +
                              $"hangars={points.Count(p => p.HardPointType == HardPointType.Hangar)} bays={data.HangarBays.Count} plumes={plumes.arraySize} " +
                              $"size={hull.size} navRadius={data.NavigationRadius} ring={ring:F3} wreck={(data.Wreck != null)} " +
                              $"arcs={string.Join(",", weapons.GroupBy(w => $"{w.MinYaw}..{w.MaxYaw}").Select(g => g.Key + "x" + g.Count()))}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        var settings = AddressableAssetSettingsDefaultObject.Settings;
        foreach (var (path, group) in new[]
                 {
                     (viewPath, "RebellionUnitViews"),
                     ($"Assets/Prefabs/Ui/Reinforcement/{name}ReinforcementView.prefab", "RebellionUnitViews"),
                     ($"Assets/Prefabs/Models/Wrecks/{name}WreckView.prefab", "RebellionUnitViews"),
                     ($"Assets/Settings/Data/Ship/Rebellion/{name}ShipData.asset", "Data"),
                     ($"Assets/Art/Textures/Ui/Icons/ShipIcon/Rebellion/{name}Icon.png", "SharedIcons"),
                 })
        {
            var entry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(path));
            Expect(entry != null && entry.parentGroup.Name == group, $"addressable {path} not in {group}");
        }
        var icon = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Art/Textures/Ui/Icons/ShipIcon/Rebellion/{name}Icon.png");
        Expect(icon != null, "icon missing");
        var importer = (TextureImporter)AssetImporter.GetAtPath($"Assets/Art/Textures/Ui/Icons/ShipIcon/Rebellion/{name}Icon.png");
        Expect(importer.textureType == TextureImporterType.Sprite, "icon not a sprite");
    }

    private static List<UnityEngine.Object> Refs(UnityEngine.Object target, string field)
    {
        var property = new SerializedObject(target).FindProperty(field);
        return Enumerable.Range(0, property.arraySize).Select(i => property.GetArrayElementAtIndex(i).objectReferenceValue).ToList();
    }
}
