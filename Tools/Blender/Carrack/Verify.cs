using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.ViewComponents.Health;

public static class VerifyCarrack
{
    const int SHIP_ID = 15;
    const string VISUAL = "Assets/Prefabs/Models/Ships/Republic/Carrack.prefab";
    const string VIEW = "Assets/Prefabs/Models/Ships/Republic/CarrackShipView.prefab";
    const string DATA = "Assets/Settings/Data/Ship/Republic/CarrackShipData.asset";
    const string WRECK = "Assets/Prefabs/Models/Wrecks/CarrackWreckView.prefab";
    const string WRECK_DATA = "Assets/Settings/Data/Models/Wrecks/Republic/CarrackWreckData.asset";
    const string PREVIEW = "Assets/Prefabs/Ui/Reinforcement/CarrackReinforcementView.prefab";

    public static string Main()
    {
        var visual = AssetDatabase.LoadAssetAtPath<GameObject>(VISUAL);
        var view = AssetDatabase.LoadAssetAtPath<GameObject>(VIEW);
        var preview = AssetDatabase.LoadAssetAtPath<GameObject>(PREVIEW);
        var wreck = AssetDatabase.LoadAssetAtPath<GameObject>(WRECK);
        foreach (var prefab in new[] {visual, view, preview, wreck}) CheckReferences(prefab);
        var sourceRoot = visual.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Root");
        var sourceBones = new[] {"HULL", "Shieldgen", "Thruster", "engines", "collision", "shadow"}
            .Concat(Enumerable.Range(0, 12).Select(i => "MuzzleA_" + i.ToString("00")))
            .Concat(Enumerable.Range(1, 8).Select(i => "Hp" + i.ToString("00"))).ToArray();
        Check(sourceBones.Length + 2 == 28 && sourceBones.All(n => sourceRoot.Find(n) != null)
            && sourceRoot.Find("engines/pe_tartanengine_sml") != null, "All 28 source bones retained");
        var hull = Hull(view);
        Check(hull.Length == 1 && hull[0].name == "HULL" && hull[0].GetComponents<MeshFilter>().Single().sharedMesh.triangles.Length / 3 == 4638,
            "One source HULL with 4638 triangles");
        Check(Hull(visual).Length == 1 && wreck.GetComponentsInChildren<MeshRenderer>(true).Length == 1, "Single opaque visual and wreck mesh");
        foreach (var prefab in new[] {visual, view, preview, wreck})
            Check(!prefab.GetComponentsInChildren<MeshRenderer>(true).Any(r => new[] {"shadow", "collision", "engines"}.Contains(r.name)), "No source helper/effect renderer: " + prefab.name);

        var hardpoints = view.GetComponentsInChildren<HardPoint>(true).OrderBy(p => p.Id).ToArray();
        Check(hardpoints.Length == 14 && hardpoints.Select(p => p.Id).SequenceEqual(Enumerable.Range(0, 14)), "Fourteen ordered unique hardpoint IDs");
        var weapons = hardpoints.OfType<WeaponHardPoint>().ToArray();
        Check(weapons.Length == 12 && weapons.All(p => p.WeaponType == WeaponType.DualLaser), "Twelve dual laser mounts");
        Check(hardpoints[12].HardPointType == HardPointType.Engines && hardpoints[13].HardPointType == HardPointType.ShieldGenerator, "Engine and shield generator");
        var sockets = view.GetComponentsInChildren<Transform>(true).Where(t => t.parent != null && t.parent.name == "Root").ToArray();
        var socketNames = Enumerable.Range(0, 12).Select(i => "MuzzleA_" + i.ToString("00")).Concat(new[] {"Thruster", "Shieldgen"}).ToArray();
        foreach (var point in hardpoints)
        {
            Check(Vector3.Distance(point.transform.position, sockets.Single(t => t.name == socketNames[point.Id]).position) < .00001f, "Source socket: " + point.Id);
            Check(new SerializedObject(point).FindProperty("destroyedVfxAnchor").objectReferenceValue != null, "Destruction VFX anchor: " + point.Id);
        }
        var health = Config(view, "HealthComponent");
        Check(References(health.FindProperty("<ShipUnits>k__BackingField")).OrderBy(o => ((HardPoint)o).Id).SequenceEqual(hardpoints.Cast<UnityEngine.Object>()), "Every hardpoint targetable");
        Check(References(Config(view, "WeaponComponent").FindProperty("hardPoints")).OrderBy(o => ((HardPoint)o).Id).SequenceEqual(weapons.Cast<UnityEngine.Object>()), "Weapon bindings");
        var injectables = References(Config(view, "EntityBindings").FindProperty("<Injectables>k__BackingField"));
        var renderState = Component(view, "UnitRenderStateComponent");
        Check(hardpoints.All(injectables.Contains) && injectables.Count(o => o == renderState) == 1, "Hardpoint and render-state injection");
        var allRenderers = view.GetComponentsInChildren<MeshRenderer>(true);
        Check(References(new SerializedObject(renderState).FindProperty("meshRenderers")).OrderBy(o => o.GetInstanceID()).SequenceEqual(allRenderers.Cast<UnityEngine.Object>().OrderBy(o => o.GetInstanceID())), "Every renderer receives ownership");
        VerifyOwnership();
        var body = (Transform)Config(view, "ShipMoveComponent").FindProperty("bodyTransform").objectReferenceValue;
        Check(body.localScale == Vector3.one && hardpoints.All(p => p.transform.IsChildOf(body)), "Unit-scale banking body and mounted hardpoints");
        var points = hull.SelectMany(r => r.GetComponents<MeshFilter>().Single().sharedMesh.vertices.Select(v => view.transform.InverseTransformPoint(r.transform.TransformPoint(v)))).ToArray();
        var collision = view.GetComponents<BoxCollider>().Single();
        Check(points.All(p => new Bounds(collision.center, collision.size + Vector3.one * .001f).Contains(p)), "Collision contains visible hull");
        var engineTarget = view.transform.InverseTransformPoint(hardpoints[12].transform.position);
        var engineAnchor = (Transform)new SerializedObject(hardpoints[12]).FindProperty("destroyedVfxAnchor").objectReferenceValue;
        Check(Vector3.Distance(engineAnchor.position, view.transform.TransformPoint(points.OrderBy(p => (p - engineTarget).sqrMagnitude).First())) < .00001f, "Engine VFX on hull surface");
        var fog = References(Config(view, "FogVisibilityComponent").FindProperty("renderers"));
        var shield = (Component)health.FindProperty("shieldView").objectReferenceValue;
        Check(hull.All(fog.Contains) && shield.GetComponents<MeshRenderer>().All(fog.Contains), "Hull and shield fog bindings");

        var material = hull[0].sharedMaterial;
        Check(material.GetTexture("_TeamMaskMap") != null && Mathf.Approximately(material.GetFloat("_TeamMaskStrength"), .35f), "Source-alpha mask and reduced team tint");
        Check(!((TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(material.GetTexture("_TeamMaskMap")))).sRGBTexture, "Linear team mask");
        var wreckRenderer = wreck.GetComponentsInChildren<MeshRenderer>(true).Single();
        Check(wreckRenderer.sharedMaterials.All(m => m.shader == AssetDatabase.LoadAssetAtPath<Shader>("Assets/Art/Shaders/Units/ShipWreck.shader")
            && m.GetTexture("_TeamMaskMap") == material.GetTexture("_TeamMaskMap") && Mathf.Approximately(m.GetFloat("_TeamMaskStrength"), .35f)), "Wreck shader retains team tint");
        Check(References(Config(wreck, "UnitWreckView").FindProperty("meshRenderers")).Single() == wreckRenderer
            && References(Config(wreck, "UnitWreckView").FindProperty("meshFilters")).Single() == wreckRenderer.GetComponents<MeshFilter>().Single(), "Wreck mesh bindings");
        var data = Load(DATA);
        Check(Value(data, "Hull") == 1600 && Value(data, "Shields") == 1000 && Mathf.Abs(Value(data, "ShieldRegenerateValue") - 1000f / 120f) < .00001f, "Requested hull/shields and regeneration");
        Check(Value(data, "Speed") == 40 && data.FindProperty("hangarBays").arraySize == 0 && !view.GetComponentsInChildren<MonoBehaviour>(true).Any(m => m.GetType().Name == "HangarComponent"), "Inherited speed and no hangar");
        Check(data.FindProperty("abilities").arraySize == 1 && data.FindProperty("abilities").GetArrayElementAtIndex(0).intValue == 20, "Existing Power to Weapons");
        var ability = Entry(Load("Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset").FindProperty("definitions.keyValue"), 20);
        Check(ability.FindPropertyRelative("displayName").stringValue == "Power to Weapons" && !ability.FindPropertyRelative("requiresEnemyTarget").boolValue
            && ability.FindPropertyRelative("duration").floatValue == 10 && ability.FindPropertyRelative("recoveryDelay").floatValue == 60, "Self-activated weapon ability settings");
        var loadout = data.FindProperty("weaponLoadout");
        Check(loadout.arraySize == 1 && loadout.GetArrayElementAtIndex(0).FindPropertyRelative("weaponType").intValue == 7 && loadout.GetArrayElementAtIndex(0).FindPropertyRelative("count").intValue == 12, "Twelve dual lasers in AI loadout");
        foreach (var point in hardpoints)
            Check(Enumerable.Range(0, data.FindProperty("hardPointHealth").arraySize).Select(data.FindProperty("hardPointHealth").GetArrayElementAtIndex)
                .Any(row => row.FindPropertyRelative("hardPointType").intValue == (int)point.HardPointType && row.FindPropertyRelative("health").floatValue > 0), "Hardpoint health: " + point.Id);
        Check(data.FindProperty("<Wreck>k__BackingField").objectReferenceValue == Load(WRECK_DATA).targetObject && Load(WRECK_DATA).FindProperty("prefab.m_AssetGUID").stringValue == AssetDatabase.AssetPathToGUID(WRECK), "Own Addressable wreck");
        Check(!AssetDatabase.GetDependencies(DATA, true).Contains(WRECK), "Ship data has no hard wreck-prefab dependency");
        var spawn = Config(preview, "UnitSpawnView");
        Check(References(spawn.FindProperty("meshRenderers")).Length == 1 && preview.GetComponents<BoxCollider>().Single().isTrigger, "Single-hull placement preview");
        var hologram = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Vfx/Hologram.mat");
        Check(spawn.FindProperty("hologramMaterial").objectReferenceValue == hologram && References(spawn.FindProperty("meshRenderers")).Cast<MeshRenderer>().All(r => r.sharedMaterials.All(m => m == hologram)), "Tintable placement materials");
        VerifyRegistrations();
        var report = new JObject {["shipId"] = SHIP_ID, ["opaqueTriangles"] = 4638, ["sourceBones"] = 28, ["targetableHardpoints"] = 14, ["dualLasers"] = 12, ["hull"] = 1600, ["shields"] = 1000, ["cost"] = 1600, ["population"] = 1, ["level"] = 2, ["abilityId"] = 20, ["allEightOwnerColors"] = true, ["savedReferences"] = true};
        File.WriteAllText("Temp/CarrackImport/GameplayVerification.json", report.ToString());
        return report.ToString();
    }

    static void VerifyRegistrations()
    {
        var roster = Entry(Load("Assets/Settings/Data/Factions/Republic/RepublicFaction.asset").FindProperty("ships.keyValue"), SHIP_ID);
        Check(roster.FindPropertyRelative("<Name>k__BackingField").stringValue == "Carrack-class Cruiser", "Republic roster name");
        foreach (var field in new[] {new {name = "AvailableLevel", value = 2}, new {name = "Price", value = 1600}, new {name = "UnitCapacity", value = 1}, new {name = "BuildTime", value = 20}})
            Check(roster.FindPropertyRelative("<" + field.name + ">k__BackingField").intValue == field.value, "Roster " + field.name);
        var icon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Ui/Icons/ShipIcon/CarrackIcon.png");
        Check(icon != null && roster.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue == icon
            && Entry(Load("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset").FindProperty("shipIconWrapper.keyValue"), SHIP_ID).objectReferenceValue == icon, "Own roster/HUD sprite");
        var icons = Load("Assets/Settings/Data/Tooltip/TooltipIconData.asset").FindProperty("icons");
        Check(Enumerable.Range(0, icons.arraySize).Count(i => icons.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue == roster.FindPropertyRelative("iconKey").stringValue && icons.GetArrayElementAtIndex(i).FindPropertyRelative("sprite").objectReferenceValue == icon) == 1, "Tooltip sprite registration");
        Check(roster.FindPropertyRelative("matchups").objectReferenceValue == AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/Settings/Data/Tooltip/Matchups/CarrackMatchups.asset"), "Own matchup data");
        Check(Entry(Load("Assets/Settings/Data/Ship/Shared/ShipsData.asset").FindProperty("shipsData.keyValue"), SHIP_ID).FindPropertyRelative("m_AssetGUID").stringValue == AssetDatabase.AssetPathToGUID(DATA), "Ship data registry");
        Check(AssetDatabase.GetAssetPath(Entry(Load("Assets/Settings/Data/Reinforcement/ReinforcementData.asset").FindProperty("spawnShipWrapper.keyValue"), SHIP_ID).objectReferenceValue) == PREVIEW, "Own placement registry");
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        foreach (var path in new[] {VIEW, DATA, WRECK})
            Check(settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(path)).address == Path.GetFileNameWithoutExtension(path), "Addressable: " + path);
        Check(settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(WRECK)).parentGroup.Name == "View", "Wreck View group");
        foreach (var path in new[] {VIEW, DATA})
            Check(Entry(Load("Assets/Settings/AssetMappingData.asset").FindProperty("assetMappings.keyValue"), Path.GetFileNameWithoutExtension(path)).FindPropertyRelative("m_AssetGUID").stringValue == AssetDatabase.AssetPathToGUID(path), "Asset mapping: " + path);
        var dependencies = new[] {VIEW, PREVIEW, WRECK}.SelectMany(p => AssetDatabase.GetDependencies(p, true)).Distinct().ToArray();
        Check(!dependencies.Any(p => p.Contains("/CorellianCorvette/") || p.Contains("/CR70/") || p.EndsWith("CorellianCorvette.prefab") || p.EndsWith("CR70.prefab") || p.Contains("RV_Corvette")), "No donor visual dependencies");
    }

    static void VerifyOwnership()
    {
        var root = PrefabUtility.LoadPrefabContents(VIEW);
        try
        {
            var state = Component(root, "UnitRenderStateComponent");
            var apply = state.GetType().GetMethod("ApplyUserValue", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            for (uint value = 1; value <= 8; value++)
            {
                apply.Invoke(state, new object[] {value});
                Check(root.GetComponentsInChildren<MeshRenderer>(true).All(r => r.GetShaderUserValue() == value), "Owner color: " + value);
            }
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    static void CheckReferences(GameObject root)
    {
        Check(root != null && root.transform.localScale == Vector3.one, "Prefab exists with unit root scale");
        foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) == 0, "Missing script: " + transform.name);
        foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            Check(renderer.GetComponents<MeshFilter>().Single().sharedMesh != null && renderer.sharedMaterials.All(m => m != null), "Saved mesh/material: " + renderer.name);
        foreach (var component in root.GetComponentsInChildren<Component>(true))
        {
            var property = new SerializedObject(component).GetIterator();
            while (property.Next(true))
                if (property.propertyType == SerializedPropertyType.ObjectReference)
                    Check(property.objectReferenceValue != null || property.objectReferenceInstanceIDValue == 0, "Broken reference: " + root.name + "/" + property.propertyPath);
        }
    }
    static MeshRenderer[] Hull(GameObject root) => root.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled && r.sharedMaterials.All(m => m.shader.name == "EmpireAtWar/Ship Lit")).ToArray();
    static UnityEngine.Object[] References(SerializedProperty array) => Enumerable.Range(0, array.arraySize).Select(i => array.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
    static SerializedProperty Entry(SerializedProperty array, object key) => Enumerable.Range(0, array.arraySize).Select(i => array.GetArrayElementAtIndex(i)).Single(row => key is int n ? row.FindPropertyRelative("key").intValue == n : row.FindPropertyRelative("key").stringValue == (string)key).FindPropertyRelative("value");
    static MonoBehaviour Component(GameObject root, string name) => root.GetComponentsInChildren<MonoBehaviour>(true).Single(m => m.GetType().Name == name);
    static SerializedObject Config(GameObject root, string name) => new SerializedObject(Component(root, name));
    static SerializedObject Load(string path) => new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>(path));
    static float Value(SerializedObject data, string name) => data.FindProperty("<" + name + ">k__BackingField").floatValue;
    static void Check(bool value, string detail) { if (!value) throw new InvalidOperationException(detail); }
}
