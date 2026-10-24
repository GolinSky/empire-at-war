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

public static class VerifyCR70
{
    const int SHIP_ID = 13;
    const string VIEW = "Assets/Prefabs/Models/Ships/Republic/CR70ShipView.prefab";
    const string DATA = "Assets/Settings/Data/Ship/Republic/CR70ShipData.asset";
    const string WRECK = "Assets/Prefabs/Models/Wrecks/CR70WreckView.prefab";
    const string WRECK_DATA = "Assets/Settings/Data/Models/Wrecks/Republic/CR70WreckData.asset";
    const string PREVIEW = "Assets/Prefabs/Ui/Reinforcement/CR70ReinforcementView.prefab";

    public static string Main()
    {
        var view = AssetDatabase.LoadAssetAtPath<GameObject>(VIEW);
        var preview = AssetDatabase.LoadAssetAtPath<GameObject>(PREVIEW);
        var wreck = AssetDatabase.LoadAssetAtPath<GameObject>(WRECK);
        foreach (var prefab in new[] {view, preview, wreck}) CheckReferences(prefab);
        var hardpoints = view.GetComponentsInChildren<HardPoint>(true).OrderBy(p => p.Id).ToArray();
        Check(hardpoints.Length == 12 && hardpoints.Select(p => p.Id).SequenceEqual(Enumerable.Range(0, 12)), "Twelve unique ordered hardpoint IDs");
        var weapons = hardpoints.OfType<WeaponHardPoint>().ToArray();
        Check(weapons.Count(p => p.WeaponType == WeaponType.Laser) == 8 && weapons.Count(p => p.WeaponType == WeaponType.PointDefense) == 2, "Eight lasers and two point defense");
        Check(hardpoints[10].HardPointType == HardPointType.Engines && hardpoints[11].HardPointType == HardPointType.ShieldGenerator, "Engine and shield generator");
        var health = Config(view, "HealthComponent");
        Check(References(health.FindProperty("<ShipUnits>k__BackingField")).OrderBy(o => ((HardPoint)o).Id).SequenceEqual(hardpoints.Cast<UnityEngine.Object>()), "All hardpoints bound to health");
        Check(References(Config(view, "WeaponComponent").FindProperty("hardPoints")).Length == 10, "Weapon component bindings");
        var injectables = References(Config(view, "EntityBindings").FindProperty("<Injectables>k__BackingField"));
        Check(hardpoints.All(injectables.Contains), "Every hardpoint has dependency injection binding");
        var body = (Transform)Config(view, "ShipMoveComponent").FindProperty("bodyTransform").objectReferenceValue;
        Check(body.localScale == Vector3.one && hardpoints.All(p => p.transform.IsChildOf(body)), "Unit-scale banking body and mounted hardpoints");
        var sockets = view.GetComponentsInChildren<Transform>(true).Where(t => t.parent != null && t.parent.name == "Root").ToArray();
        string[] weaponSockets = {"HP01", "HP02", "HP03", "HP04", "HP01", "HP02", "HP03", "HP04", "HP05", "HP06", "Engines"};
        for (int i = 0; i < 11; i++) Check(Vector3.Distance(hardpoints[i].transform.position, sockets.Single(t => t.name == weaponSockets[i]).position) < .00001f, "CR70 source socket " + i);
        foreach (var point in hardpoints)
            Check(new SerializedObject(point).FindProperty("destroyedVfxAnchor").objectReferenceValue != null, "Destruction VFX anchor " + point.Id);
        var meshRenderers = view.GetComponentsInChildren<MeshRenderer>(true);
        var renderState = Component(view, "UnitRenderStateComponent");
        var team = References(new SerializedObject(renderState).FindProperty("meshRenderers"));
        Check(injectables.Contains(renderState), "Render state component dependency injection binding");
        Check(meshRenderers.All(team.Contains), "Every mesh renderer bound to team ownership");
        VerifyOwnership();
        var visible = meshRenderers.Where(r => r.enabled && r.sharedMaterials.All(m => m.shader.name == "EmpireAtWar/Ship Lit")).ToArray();
        Check(visible.Length == 2 && visible.Sum(r => r.GetComponents<MeshFilter>().Single().sharedMesh.triangles.Length / 3) == 4161 && visible.Any(r => r.name == "Hull") && visible.Any(r => r.name == "Object02"), "Correct ReV_light_frigate hull and Object02 geometry");
        var hullPoints = visible.SelectMany(r => r.GetComponents<MeshFilter>().Single().sharedMesh.vertices.Select(v => view.transform.InverseTransformPoint(r.transform.TransformPoint(v)))).ToArray();
        var hullBounds = new Bounds(hullPoints[0], Vector3.zero);
        foreach (var point in hullPoints) hullBounds.Encapsulate(point);
        var generatorTarget = view.transform.InverseTransformPoint(sockets.Single(t => t.name == "Hull").position);
        generatorTarget.x = hullBounds.center.x; generatorTarget.y = hullBounds.max.y;
        var generatorVertex = hullPoints.OrderBy(p => (p - generatorTarget).sqrMagnitude).First();
        Check(Vector3.Distance(hardpoints[11].transform.position, view.transform.TransformPoint(generatorVertex)) < .00001f, "Generator fitted to this model's dorsal hull surface");
        var engineAnchor = (Transform)new SerializedObject(hardpoints[10]).FindProperty("destroyedVfxAnchor").objectReferenceValue;
        var engineTarget = view.transform.InverseTransformPoint(hardpoints[10].transform.position);
        Check(Vector3.Distance(engineAnchor.position, view.transform.TransformPoint(hullPoints.OrderBy(p => (p - engineTarget).sqrMagnitude).First())) < .00001f, "Engine VFX fitted to this model's hull surface");
        var fog = References(Config(view, "FogVisibilityComponent").FindProperty("renderers"));
        Check(visible.All(fog.Contains) && fog.Contains(health.FindProperty("shieldView").objectReferenceValue is Component shield ? shield.GetComponents<MeshRenderer>().Single() : null), "Hull and shield fog bindings");
        var hullMaterial = visible[0].sharedMaterial;
        Check(visible.All(r => r.sharedMaterials.All(m => m.GetTexture("_TeamMaskMap") != null && Mathf.Approximately(m.GetFloat("_TeamMaskStrength"), .35f))), "Reduced source-alpha team tint on both hull pieces");
        var maskImporter = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(hullMaterial.GetTexture("_TeamMaskMap")));
        Check(!maskImporter.sRGBTexture, "Linear team-mask import");
        var data = Load(DATA);
        Check(Value(data, "Hull") == 750 && Value(data, "Shields") == 650, "Requested hull and shields");
        Check(Mathf.Abs(Value(data, "ShieldRegenerateValue") - 650f / 120f) < .00001f, "Corvette shield regeneration scaling");
        Check(Value(data, "Speed") == 40 && Value(data, "RotationSpeed") == 60 && Value(data, "TurnAcceleration") == 60, "Live donor movement defaults");
        Check(data.FindProperty("abilities").arraySize == 1 && data.FindProperty("abilities").GetArrayElementAtIndex(0).intValue == 14, "Power to Engines binding");
        var ability = Entry(Load("Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset").FindProperty("definitions.keyValue"), 14);
        Check(ability.FindPropertyRelative("duration").floatValue == 20 && ability.FindPropertyRelative("recoveryDelay").floatValue == 50 && ability.FindPropertyRelative("settings.statModifier.speedMultiplier").floatValue == 2 && ability.FindPropertyRelative("settings.statModifier.fireDelayMultiplier").floatValue == 3 && ability.FindPropertyRelative("settings.statModifier.shieldRegenMultiplier").floatValue == 0, "Existing engine ability settings");
        Check(data.FindProperty("hangarBays").arraySize == 0, "No hangar");
        var loadout = data.FindProperty("weaponLoadout");
        Check(loadout.arraySize == 2 && loadout.GetArrayElementAtIndex(0).FindPropertyRelative("weaponType").intValue == 8 && loadout.GetArrayElementAtIndex(0).FindPropertyRelative("count").intValue == 8 && loadout.GetArrayElementAtIndex(1).FindPropertyRelative("weaponType").intValue == 14 && loadout.GetArrayElementAtIndex(1).FindPropertyRelative("count").intValue == 2, "AI weapon loadout");
        Check(data.FindProperty("<Wreck>k__BackingField").objectReferenceValue == Load(WRECK_DATA).targetObject && Load(WRECK_DATA).FindProperty("prefab.m_AssetGUID").stringValue == AssetDatabase.AssetPathToGUID(WRECK), "Own wreck data AssetReference");
        var wreckRenderers = wreck.GetComponentsInChildren<MeshRenderer>(true);
        Check(wreckRenderers.Length == visible.Length && wreckRenderers.All(r => r.sharedMaterials.All(m => m.shader == AssetDatabase.LoadAssetAtPath<Shader>("Assets/Art/Shaders/Units/ShipWreck.shader") && m.GetTexture("_TeamMaskMap") == hullMaterial.GetTexture("_TeamMaskMap") && Mathf.Approximately(m.GetFloat("_TeamMaskStrength"), .35f))), "Current wreck shader and reduced team tint");
        Check(References(Config(wreck, "UnitWreckView").FindProperty("meshRenderers")).Length == wreckRenderers.Length && References(Config(wreck, "UnitWreckView").FindProperty("meshFilters")).Length == wreckRenderers.Length, "Wreck renderer/filter bindings");
        Check(References(Config(preview, "UnitSpawnView").FindProperty("meshRenderers")).Length == visible.Length && preview.GetComponents<BoxCollider>().Single().isTrigger, "Own placement hull and collider");
        var faction = Entry(Load("Assets/Settings/Data/Factions/Republic/RepublicFaction.asset").FindProperty("ships.keyValue"), SHIP_ID);
        Check(faction.FindPropertyRelative("<Name>k__BackingField").stringValue == "CR70 Corvette" && faction.FindPropertyRelative("<Price>k__BackingField").intValue == 1250 && faction.FindPropertyRelative("<UnitCapacity>k__BackingField").intValue == 2, "Republic roster, cost and population");
        var icon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Ui/Icons/ShipIcon/CR70Icon.png");
        Check(icon != null && faction.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue == icon && Entry(Load("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset").FindProperty("shipIconWrapper.keyValue"), SHIP_ID).objectReferenceValue == icon, "Own roster and HUD icon");
        Check(faction.FindPropertyRelative("matchups").objectReferenceValue == AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/Settings/Data/Tooltip/Matchups/CR70Matchups.asset"), "Own matchup asset");
        var tooltipIcons = Load("Assets/Settings/Data/Tooltip/TooltipIconData.asset").FindProperty("icons");
        Check(Enumerable.Range(0, tooltipIcons.arraySize).Count(i => tooltipIcons.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue == faction.FindPropertyRelative("iconKey").stringValue && tooltipIcons.GetArrayElementAtIndex(i).FindPropertyRelative("sprite").objectReferenceValue == icon) == 1, "Tooltip icon mapping");
        Check(Entry(Load("Assets/Settings/Data/Ship/Shared/ShipsData.asset").FindProperty("shipsData.keyValue"), SHIP_ID).FindPropertyRelative("m_AssetGUID").stringValue == AssetDatabase.AssetPathToGUID(DATA), "Ship data registration");
        Check(Entry(Load("Assets/Settings/Data/Reinforcement/ReinforcementData.asset").FindProperty("spawnShipWrapper.keyValue"), SHIP_ID).objectReferenceValue == Component(preview, "UnitSpawnView"), "Placement registration");
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        foreach (var path in new[] {VIEW, DATA, WRECK})
            Check(settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(path)).address == Path.GetFileNameWithoutExtension(path), "Addressables " + path);
        foreach (var path in new[] {VIEW, DATA})
            Check(Entry(Load("Assets/Settings/AssetMappingData.asset").FindProperty("assetMappings.keyValue"), Path.GetFileNameWithoutExtension(path)).FindPropertyRelative("m_AssetGUID").stringValue == AssetDatabase.AssetPathToGUID(path), "Asset mapping " + path);
        var dependencies = new[] {VIEW, PREVIEW, WRECK}.SelectMany(p => AssetDatabase.GetDependencies(p, true)).Distinct().ToArray();
        Check(!dependencies.Any(p => p.Contains("/CorellianCorvette/") || p.EndsWith("CorellianCorvette.prefab") || p.EndsWith("CorellianCorvetteShipView.prefab") || p.Contains("RV_Corvette")), "No vanilla Corvette visual or transform dependencies");
        var report = new JObject {["sourceModel"] = "ReV_light_frigate.ALO", ["opaqueTriangles"] = 4161, ["shipId"] = SHIP_ID, ["targetableHardpoints"] = 12, ["lasers"] = 8, ["pointDefense"] = 2, ["hull"] = 750, ["shields"] = 650, ["cost"] = 1250, ["population"] = 2, ["abilityId"] = 14, ["teamUserValues1Through8"] = true, ["teamMaskAndWreckBindings"] = true, ["savedReferences"] = true, ["donorVisualDependencies"] = false};
        File.WriteAllText("Temp/CR70ChargerImport/GameplayVerification.json", report.ToString());
        return report.ToString();
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
                Check(root.GetComponentsInChildren<MeshRenderer>(true).All(r => r.GetShaderUserValue() == value), "Team user value " + value);
            }
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    static void CheckReferences(GameObject root)
    {
        Check(root != null, "Missing prefab");
        foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) == 0, "Missing script " + transform.name);
        foreach (var component in root.GetComponentsInChildren<Component>(true))
        {
            var iterator = new SerializedObject(component).GetIterator();
            while (iterator.Next(true))
                if (iterator.propertyType == SerializedPropertyType.ObjectReference)
                    Check(iterator.objectReferenceValue != null || iterator.objectReferenceInstanceIDValue == 0, "Broken reference " + root.name + ":" + iterator.propertyPath);
        }
    }
    static UnityEngine.Object[] References(SerializedProperty array) => Enumerable.Range(0, array.arraySize).Select(i => array.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
    static SerializedProperty Entry(SerializedProperty array, object key) => Enumerable.Range(0, array.arraySize).Select(i => array.GetArrayElementAtIndex(i)).Single(row => key is int n ? row.FindPropertyRelative("key").intValue == n : row.FindPropertyRelative("key").stringValue == (string)key).FindPropertyRelative("value");
    static MonoBehaviour Component(GameObject root, string name) => root.GetComponentsInChildren<MonoBehaviour>(true).Single(m => m.GetType().Name == name);
    static SerializedObject Config(GameObject root, string name) => new SerializedObject(Component(root, name));
    static SerializedObject Load(string path) => new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>(path));
    static float Value(SerializedObject data, string name) => data.FindProperty("<" + name + ">k__BackingField").floatValue;
    static void Check(bool value, string detail) { if (!value) throw new InvalidOperationException(detail); }
}
