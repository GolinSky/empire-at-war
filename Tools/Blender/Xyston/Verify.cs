using System.Collections.Generic;
using System.Linq;
using System.Text;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.ShipAbilities.Abilities;
using EmpireAtWar.ViewComponents.Health;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

/// <summary>Read-only audit of the saved Xyston assets and registrations. Run with --entry XystonVerify.Run.</summary>
public static class XystonVerify
{
    private const int SHIP_TYPE = 220;
    private const string VIEW = "Assets/Prefabs/Models/Ships/Empire/View/XystonShipView.prefab";
    private const string DATA = "Assets/Settings/Data/Ship/Empire/XystonShipData.asset";
    private const string PREVIEW = "Assets/Prefabs/Ui/Reinforcement/XystonReinforcementView.prefab";
    private const string WRECK = "Assets/Prefabs/Models/Wrecks/XystonWreckView.prefab";
    private const string ICON = "Assets/Art/Textures/Ui/Icons/ShipIcon/Empire/XystonIcon.png";

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
            Expect(weapons.Length == 21, "expected 21 source weapons");
            Expect(points.Count(p => p.HardPointType == HardPointType.ShieldGenerator) == 1, "shield generator count");
            Expect(points.Count(p => p.HardPointType == HardPointType.Engines) == 2, "engine count");
            Expect(points.Count(p => p.HardPointType == HardPointType.TractorBeam) == 1, "tractor beam hardpoint (Tractor Beam needs it)");
            Expect(points.All(p => p.HardPointType != HardPointType.Hangar) && root.GetComponentsInChildren<MonoBehaviour>(true)
                .All(c => c.GetType().Name != "HangarComponent"), "hangar present (user: no hangar)");

            var loadout = weapons.GroupBy(w => w.WeaponType).ToDictionary(g => g.Key, g => g.Count());
            var baked = data.WeaponLoadout.ToDictionary(e => e.WeaponType, e => e.Count);
            Expect(loadout.Count == baked.Count && loadout.All(kv => baked.TryGetValue(kv.Key, out int c) && c == kv.Value), "baked loadout != mounts");
            float engineZ = points.Where(p => p.HardPointType == HardPointType.Engines).Average(p => root.transform.InverseTransformPoint(p.transform.position).z);
            Expect(engineZ < 0f, "engines not aft (bow must be +Z)");

            var muzzle = (Transform)new SerializedObject(Find("Ship")).FindProperty("compositeBeamMuzzle").objectReferenceValue;
            Expect(muzzle != null && muzzle.root == root.transform && muzzle.name == "FP_01", "superlaser muzzle not bound to the cannon FP_01");
            Vector3 muzzleLocal = root.transform.InverseTransformPoint(muzzle.position);

            var move = new SerializedObject(Find("ShipMoveComponent"));
            var fog = Refs(Find("FogVisibilityComponent"), "renderers");
            var trails = root.GetComponentsInChildren<TrailRenderer>(true);
            Expect(move.FindProperty("enginePlumes").arraySize == 3 && trails.Length == 3 && trails.All(fog.Contains), "engine plumes not listed/fogged");
            var visible = root.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled && r.GetComponent<Shield>() == null).ToArray();
            Expect(visible.All(fog.Contains), "visible renderer missing from fog list");
            Expect(visible.All(r => !r.name.StartsWith("TeamStripes")), "team stripes still present (user: albedo livery only)");
            var livery = visible.Where(r => r.sharedMaterials.Any(m => m.HasProperty("_TeamLiveryStrength") && m.GetFloat("_TeamLiveryStrength") > 0f)).ToArray();
            Expect(livery.Length > 0 && livery.All(r => r.name == "Hull_Armor_Red"), "red-hue livery must be on Hull_Armor_Red only");
            Expect(visible.All(r => r.sharedMaterials.All(m => !m.HasProperty("_TeamMaskStrength") || m.GetFloat("_TeamMaskStrength") == 0f)), "team mask strength left on");
            var box = root.GetComponent<BoxCollider>();
            var pick = root.GetComponents<MeshCollider>();
            Expect(!box.enabled && pick.Length == 1 && pick[0].enabled && pick[0].convex && pick[0].sharedMesh != null &&
                pick[0].sharedMesh.name == "Xyston_PickHull", "battlecruiser must be picked by its convex pick hull, box disabled");
            Expect(root.GetComponentsInChildren<MeshRenderer>(true).All(r => !r.name.StartsWith("Shadow") && r.name != "Collision" &&
                !r.name.StartsWith("Cannon_Collision") && !r.name.StartsWith("Barrel_Shadow") && (r.name != "Shield" || r.GetComponent<Shield>() != null)),
                "helper meshes left in view");

            Bounds hull = visible.Select(r => r.bounds).Aggregate((a, b) => { a.Encapsulate(b); return a; });
            var selection = new SerializedObject(Find("SelectionComponent"));
            float ring = selection.FindProperty("ringSize").vector2Value.x * 0.8f * (1038f / 1254f) / Mathf.Max(hull.size.x, hull.size.z);
            // 2abaf7e31 resized the ring to 1.15x the diameter measured over all hull renderers (ratio ~0.99 here).
            Expect(ring >= 0.88f && ring <= 1.02f, $"selection ring ratio {ring:F3}");
            Expect(Mathf.Abs(hull.size.z - 2406f * 0.105f) < 0.5f, $"length {hull.size.z:F2} != 2,406 m");
            Expect(AssetDatabase.GetDependencies(VIEW, true).All(p => !p.Contains("/Allegiance")), "view still depends on Allegiance assets");

            report.AppendLine($"Xyston view: weapons={weapons.Length} systems={points.Length - weapons.Length} size={hull.size} ring={ring:F3} " +
                              $"muzzle={muzzleLocal} livery={livery.Length} loadout={string.Join(",", loadout.Select(kv => kv.Key + "x" + kv.Value))}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        Expect(data.Hull == 28000f && data.Shields == 11000f && data.ShipClass == ShipClass.BattleCruiser &&
            data.ShieldRegenTier == ShieldRegenTier.BattleCruiser && data.ShieldRegenerateDelay == 1f, "hull/shields/class/shield regen");
        Expect(data.Abilities.SequenceEqual(new[] { ShipAbilityId.XystonSuperlaser, ShipAbilityId.TractorBeam }), "abilities");
        Expect(data.HangarBays.Count == 0 && data.Wreck != null, "bays/wreck");
        var catalog = AssetDatabase.LoadAssetAtPath<ShipAbilityCatalog>("Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset");
        ShipAbilityDefinition superlaser = catalog.Get(ShipAbilityId.XystonSuperlaser);
        Expect(superlaser != null && superlaser.Settings is SuperlaserSettings s && s.Damage == 46800f && s.BeamColor.r > s.BeamColor.g && s.BeamColor.r > s.BeamColor.b && s.DamageType == DamageType.Beam &&
               superlaser.Duration == 6f && superlaser.RecoveryDelay == 60f && superlaser.Range == 1000f && superlaser.RequiresEnemyTarget &&
               superlaser.AutoPower == AutoPowerRole.Target && superlaser.Icon != null, "superlaser catalog entry");

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
        Expect(faction.Contains("- key: 220") && faction.Contains("displayName: Xyston"), "EmpireFaction roster");
        string audio = Yaml("Assets/Settings/Data/Models/Audio/ShipSfxData.asset");
        Expect(System.Text.RegularExpressions.Regex.Matches(audio, @"- key: 220\b").Count == 2, "ShipSfxData ships + voices rows");
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
