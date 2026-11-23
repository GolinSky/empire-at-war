using System.Collections.Generic;
using System.Linq;
using System.Text;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.ViewComponents.Health;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

/// <summary>Read-only audit of the saved Resurgent assets and registrations. Run with --entry ResurgentVerify.Run.</summary>
public static class ResurgentVerify
{
    private const string VIEW = "Assets/Prefabs/Models/Ships/Empire/View/ResurgentShipView.prefab";
    private const string DATA = "Assets/Settings/Data/Ship/Empire/ResurgentShipData.asset";
    private const string PREVIEW = "Assets/Prefabs/Ui/Reinforcement/ResurgentReinforcementView.prefab";
    private const string WRECK = "Assets/Prefabs/Models/Wrecks/ResurgentWreckView.prefab";
    private const string ICON = "Assets/Art/Textures/Ui/Icons/ShipIcon/Empire/ResurgentIcon.png";

    public static string Run()
    {
        var report = new StringBuilder();
        var failures = new List<string>();
        void Expect(bool condition, string message) { if (!condition) failures.Add(message); }

        var data = AssetDatabase.LoadAssetAtPath<ShipData>(DATA);
        GameObject root = PrefabUtility.LoadPrefabContents(VIEW);
        try
        {
            HardPoint[] points = root.GetComponentsInChildren<HardPoint>(true);
            WeaponHardPoint[] weapons = points.OfType<WeaponHardPoint>().ToArray();
            MonoBehaviour Find(string type) => root.GetComponentsInChildren<MonoBehaviour>(true).Single(c => c != null && c.GetType().Name == type);
            var units = Refs(Find("HealthComponent"), "<ShipUnits>k__BackingField");
            var weaponRefs = Refs(Find("WeaponComponent"), "hardPoints");
            var injectables = Refs(Find("EntityBindings"), "<Injectables>k__BackingField");
            Expect(root.GetComponentsInChildren<Component>(true).All(c => c != null), "missing script in view");
            Expect(units.Count == points.Length && points.All(units.Contains), "ShipUnits != all hardpoints");
            Expect(weaponRefs.Count == weapons.Length && weapons.All(weaponRefs.Contains), "WeaponComponent.hardPoints != weapons");
            Expect(points.All(injectables.Contains) && injectables.All(i => i != null), "Injectables incomplete or null");
            Expect(points.Select(p => p.Id).OrderBy(i => i).SequenceEqual(Enumerable.Range(0, points.Length)), "hardpoint ids not 0..n-1");
            var anchors = points.Select(p => (Transform)new SerializedObject(p).FindProperty("destroyedVfxAnchor").objectReferenceValue).ToArray();
            Expect(anchors.All(a => a != null && a.root == root.transform) && anchors.Distinct().Count() == anchors.Length, "destroyed VFX anchors");
            Expect(points.All(p => p.HealthOverride == 0f && p.HealthWeight > 0f && p.UnlockLevel == 1), "health override/weight/unlock");
            Expect(weapons.Length == 64, "expected 64 source weapons");
            Expect(points.Count(p => p.HardPointType == HardPointType.ShieldGenerator) == 2, "shield generator count");
            Expect(points.Count(p => p.HardPointType == HardPointType.Engines) == 3, "engine count");
            Expect(points.Count(p => p.HardPointType == HardPointType.TractorBeam) == 1, "tractor beam hardpoint");
            Expect(points.Count(p => p.HardPointType == HardPointType.CommandArray) == 1, "command array hardpoint");
            HardPoint hangarPoint = points.Single(p => p.HardPointType == HardPointType.Hangar);
            var hangar = new SerializedObject(Find("HangarComponent"));
            Expect(hangar.FindProperty("hangarHardPoint").objectReferenceValue == hangarPoint, "hangar hardpoint not bound");
            var launch = (Transform)hangar.FindProperty("launchPoint").objectReferenceValue;
            Expect(launch != null && launch.root == root.transform, "hangar launch point");
            Expect(weapons.Count(w => new SerializedObject(w).FindProperty("mainBattery").boolValue) == 16, "main batteries (8 octuple + 8 heavy quad)");

            var loadout = weapons.GroupBy(w => w.WeaponType).ToDictionary(g => g.Key, g => g.Count());
            var baked = data.WeaponLoadout.ToDictionary(e => e.WeaponType, e => e.Count);
            Expect(loadout.Count == baked.Count && loadout.All(kv => baked.TryGetValue(kv.Key, out int c) && c == kv.Value), "baked loadout != mounts");
            float engineZ = points.Where(p => p.HardPointType == HardPointType.Engines).Average(p => root.transform.InverseTransformPoint(p.transform.position).z);
            Expect(engineZ < 0f, "engines not aft (bow must be +Z)");
            Expect(launch.position.y < hangarPoint.transform.position.y, "launch point not below the hangar bay");

            var move = new SerializedObject(Find("ShipMoveComponent"));
            var fog = Refs(Find("FogVisibilityComponent"), "renderers");
            var trails = root.GetComponentsInChildren<TrailRenderer>(true);
            Expect(move.FindProperty("enginePlumes").arraySize == 3 && trails.Length == 3 && trails.All(fog.Contains), "engine plumes not listed/fogged");
            var visible = root.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled && r.GetComponent<Shield>() == null).ToArray();
            Expect(visible.All(fog.Contains), "visible renderer missing from fog list");
            Expect(visible.All(r => !r.name.StartsWith("TeamStripes")), "team stripes present (user: no stripes)");
            Expect(visible.All(r => r.sharedMaterials.All(m => (!m.HasProperty("_TeamMaskStrength") || m.GetFloat("_TeamMaskStrength") == 0f) &&
                                                               (!m.HasProperty("_TeamLiveryStrength") || m.GetFloat("_TeamLiveryStrength") == 0f))),
                "team mask/livery left on (source albedo has no team hue)");
            var box = root.GetComponent<BoxCollider>();
            var pick = root.GetComponents<MeshCollider>();
            Expect(!box.enabled && pick.Length == 1 && pick[0].convex && pick[0].sharedMesh != null, "battlecruiser must be picked by its convex hull");
            Expect(root.GetComponentsInChildren<MeshRenderer>(true).All(r => !r.name.StartsWith("Shadow") && !r.name.Contains("Collision") &&
                (r.name != "Shield" || r.GetComponent<Shield>() != null)), "helper meshes left in view");

            Bounds hull = visible.Select(r => r.bounds).Aggregate((a, b) => { a.Encapsulate(b); return a; });
            var selection = new SerializedObject(Find("SelectionComponent"));
            float ring = selection.FindProperty("ringSize").vector2Value.x * 0.8f * (1038f / 1254f) / Mathf.Max(hull.size.x, hull.size.z);
            Expect(ring >= 0.88f && ring <= 0.96f, $"selection ring ratio {ring:F3}");
            Expect(Mathf.Abs(hull.size.z - 2915.81f * 0.105f) < 1f, $"length {hull.size.z:F2} != 2,915.81 m");
            Expect(AssetDatabase.GetDependencies(VIEW, true).All(p => !p.Contains("/ISDII") && !p.Contains("/Allegiance")), "view still depends on donor assets");

            report.AppendLine($"Resurgent view: weapons={weapons.Length} systems={points.Length - weapons.Length} size={hull.size} ring={ring:F3} " +
                              $"loadout={string.Join(",", loadout.Select(kv => kv.Key + "x" + kv.Value))}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        Expect(data.Hull == 70000f && data.Shields == 35000f && data.ShipClass == ShipClass.BattleCruiser, "hull/shields/class");
        Expect(data.Abilities.SequenceEqual(new[] { ShipAbilityId.PowerToMainBatteries, ShipAbilityId.TractorBeam }), "abilities");
        Expect(data.HangarBays.Count == 3 && data.Wreck != null, "bays/wreck");

        var settings = AddressableAssetSettingsDefaultObject.Settings;
        foreach (var (path, group) in new[] { (VIEW, "EmpireUnitViews"), (PREVIEW, "EmpireUnitViews"), (WRECK, "EmpireUnitViews"), (DATA, "Data"), (ICON, "SharedIcons") })
        {
            var entry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(path));
            Expect(entry != null && entry.parentGroup.Name == group, $"addressable {path} not in {group}");
        }
        string Yaml(string path) => System.IO.File.ReadAllText(path);
        string viewGuid = AssetDatabase.AssetPathToGUID(VIEW), dataGuid = AssetDatabase.AssetPathToGUID(DATA), previewGuid = AssetDatabase.AssetPathToGUID(PREVIEW);
        Expect(Yaml("Assets/Settings/Data/Ship/Shared/ShipsData.asset").Contains(dataGuid), "ShipsData missing data GUID");
        Expect(Yaml("Assets/Settings/AssetMappingData.asset").Contains(viewGuid) && Yaml("Assets/Settings/AssetMappingData.asset").Contains(dataGuid), "AssetMappingData");
        Expect(Yaml("Assets/Settings/Data/Reinforcement/ReinforcementData.asset").Contains(previewGuid), "ReinforcementData preview");
        string iconGuid = AssetDatabase.AssetPathToGUID(ICON);
        Expect(Yaml("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset").Contains(iconGuid), "ShipUiData icon");
        Expect(Yaml("Assets/Settings/Data/Tooltip/TooltipIconData.asset").Contains(iconGuid), "TooltipIconData icon");
        string faction = Yaml("Assets/Settings/Data/Factions/Empire/EmpireFaction.asset");
        Expect(faction.Contains("- key: 221") && faction.Contains("displayName: Resurgent") && faction.Contains("<Price>k__BackingField: 18000"), "EmpireFaction roster");
        string audio = Yaml("Assets/Settings/Data/Models/Audio/ShipSfxData.asset");
        Expect(System.Text.RegularExpressions.Regex.Matches(audio, @"- key: 221\b").Count == 2, "ShipSfxData ships + voices rows");
        Expect(((TextureImporter)AssetImporter.GetAtPath(ICON)).textureType == TextureImporterType.Sprite, "icon not a sprite");

        report.AppendLine(failures.Count == 0 ? "ALL CHECKS PASSED" : "FAILURES:\n  " + string.Join("\n  ", failures));
        return report.ToString();
    }

    private static List<Object> Refs(Object target, string field)
    {
        var property = new SerializedObject(target).FindProperty(field);
        return Enumerable.Range(0, property.arraySize).Select(i => property.GetArrayElementAtIndex(i).objectReferenceValue).ToList();
    }
}
