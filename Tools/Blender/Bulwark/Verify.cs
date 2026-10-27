using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EmpireAtWar.ViewComponents.Health;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

public static class VerifyBulwark
{
    const int SHIP_ID = 112;
    const string TASK = "Temp/BulwarkImport/";
    const string VISUAL = "Assets/Prefabs/Models/Ships/Separatist/Bulwark.prefab";
    const string VIEW = "Assets/Prefabs/Models/Ships/Separatist/BulwarkShipView.prefab";
    const string DATA = "Assets/Settings/Data/Ship/Separatist/BulwarkShipData.asset";
    const string WRECK = "Assets/Prefabs/Models/Wrecks/BulwarkWreckView.prefab";
    const string WRECK_DATA = "Assets/Settings/Data/Models/Wrecks/Separatist/BulwarkWreckData.asset";
    const string PREVIEW = "Assets/Prefabs/Ui/Reinforcement/BulwarkReinforcementView.prefab";
    const string ICON = "Assets/Art/Textures/Ui/Icons/ShipIcon/Separatist/BulwarkIcon.png";
    const float TOLERANCE = .01f;

    // Saved-asset audit only: never instantiates gameplay or changes an open scene.
    public static string Main()
    {
        Require((int)EmpireAtWar.Models.Factions.ShipType.Bulwark == SHIP_ID, "Bulwark enum registration.");
        Require(Enum.GetValues(typeof(EmpireAtWar.Models.Factions.ShipType)).Cast<int>().Count(value => value == SHIP_ID) == 1, "Unique Bulwark ship ID.");
        var assets = new[] {VISUAL, VIEW, WRECK, PREVIEW}.Select(path => AssetDatabase.LoadAssetAtPath<GameObject>(path)).ToArray();
        foreach (var asset in assets)
        {
            Require(asset != null, "Missing Bulwark prefab.");
            Require((asset.transform.localScale - Vector3.one).sqrMagnitude < .000001f, asset.name + " root scale.");
            foreach (var component in All(asset))
            {
                Require(component != null, asset.name + " missing script.");
                var property = new SerializedObject(component).GetIterator();
                while (property.Next(true))
                    if (property.propertyType == SerializedPropertyType.ObjectReference)
                        Require(property.objectReferenceValue != null || property.objectReferenceEntityIdValue == EntityId.None, asset.name + " broken reference " + property.propertyPath);
            }
            foreach (var renderer in All(asset).OfType<Renderer>())
                Require(renderer.sharedMaterials.All(m => m != null && m.shader != null), asset.name + " missing material/shader.");
            var donorArt = AssetDatabase.GetDependencies(AssetDatabase.GetAssetPath(asset), true).Where(path =>
                (path.Contains("Providence") || path.Contains("Recusant") || path.Contains("Peleset")) &&
                (path.StartsWith("Assets/Art/Models/") || path.StartsWith("Assets/Art/Materials/") || path.StartsWith("Assets/Art/Textures/Models/") || path.StartsWith("Assets/Prefabs/Models/"))).ToArray();
            Require(donorArt.Length == 0, asset.name + " retains donor art: " + string.Join(", ", donorArt));
        }
        var root = assets[1]; var components = All(root).ToArray();
        var hardpoints = components.OfType<HardPoint>().OrderBy(p => p.Id).ToArray();
        var weapons = hardpoints.OfType<WeaponHardPoint>().ToArray();
        Require(hardpoints.Length == 16 && hardpoints.Select(p => p.Id).SequenceEqual(Enumerable.Range(0, 16)), "Sixteen hardpoints with IDs 0..15.");
        Require(weapons.Length == 13, "Thirteen weapon hardpoints.");
        int[] types = {90, 48, 4}, counts = {3, 2, 8};
        var definitions = Load("Assets/Settings/Data/Models/Weapon/WeaponsData.asset").FindProperty("weapons");
        for (int i = 0; i < types.Length; i++)
        {
            Require(weapons.Count(p => (int)p.WeaponType == types[i]) == counts[i], "Weapon count " + types[i]);
            Require(Elements(definitions).Single(row => row.FindPropertyRelative("weaponType").intValue == types[i]).FindPropertyRelative("caliber").intValue == 1, "Medium caliber " + types[i]);
        }
        Require(weapons.All(p => (int)p.HardPointType == 0), "Weapon HP category.");
        Require(hardpoints.Count(p => (int)p.HardPointType == 1) == 1 && hardpoints.Count(p => (int)p.HardPointType == 2) == 1 && hardpoints.Count(p => (int)p.HardPointType == 4) == 1, "One engine, shield and hangar.");
        var health = Config(components, "HealthComponent"); var bindings = Config(components, "EntityBindings");
        Require(SetEquals(Refs(health.FindProperty("<ShipUnits>k__BackingField")), hardpoints), "Health binds every hardpoint.");
        Require(SetEquals(Refs(Config(components, "WeaponComponent").FindProperty("hardPoints")), weapons), "Weapon component binds every weapon.");
        var injected = Refs(bindings.FindProperty("<Injectables>k__BackingField"));
        Require(hardpoints.All(p => injected.Contains(p)), "Every hardpoint is injected.");
        var body = (Transform)Config(components, "ShipMoveComponent").FindProperty("bodyTransform").objectReferenceValue;
        Require(body != null && hardpoints.All(p => p.transform.IsChildOf(body)), "Hardpoints bank with hull.");
        foreach (var point in hardpoints)
        {
            var config = new SerializedObject(point);
            Require(config.FindProperty("destroyedVfxAnchor").objectReferenceValue is Transform anchor && anchor.IsChildOf(root.transform), "Destruction anchor " + point.Id);
        }
        string[] sockets = {"HP_Bulwark_F1", "HP_Bulwark_F0", "HP_F_MAIN", "HP_Bulwark_L1", "HP_Bulwark_R1", "HP_Bulwark_L2", "HP_Bulwark_R2", "HP_Bulwark_L3", "HP_Bulwark_R3", "HP_Bulwark_L4", "HP_Bulwark_R4", "HP_Bulwark_L5", "HP_Bulwark_R5", "HP_Bulwark_Shield_Generator", "HP_Bulwark_Engines", "SPAWN_00"};
        var visualNodes = Nodes(assets[0].transform).ToArray();
        foreach (var socket in sockets)
        {
            var bone = visualNodes.Single(t => t.name == socket);
            var position = assets[0].transform.InverseTransformPoint(bone.position);
            Require(hardpoints.Count(p => Vector3.Distance(root.transform.InverseTransformPoint(p.transform.position), position) < TOLERANCE) == 1, "Distinct source socket " + socket);
        }
        var data = Load(DATA);
        Require(Float(data, "Hull") == 8500 && Float(data, "Shields") == 2500, "8500 hull / 2500 shields.");
        Require(data.FindProperty("<ShipClass>k__BackingField").intValue == 4 && data.FindProperty("<ShieldRegenTier>k__BackingField").intValue == 3, "Capital class and regeneration tier.");
        var hp = Elements(data.FindProperty("hardPointHealth")).ToArray();
        Require(hp.Length == 4, "Four hardpoint health categories.");
        foreach (var row in hp)
        {
            int type = row.FindPropertyRelative("hardPointType").intValue;
            Require(new[] {0, 1, 2, 4}.Contains(type) && row.FindPropertyRelative("health").floatValue == (type == 1 || type == 2 ? 600 : 400) && row.FindPropertyRelative("hullDamageMultiplier").floatValue == 1, "Capital hardpoint HP pattern.");
        }
        var loadout = Elements(data.FindProperty("weaponLoadout")).ToArray();
        Require(loadout.Length == 3 && loadout.All(row => row.FindPropertyRelative("count").intValue == weapons.Count(p => (int)p.WeaponType == row.FindPropertyRelative("weaponType").intValue)), "Data loadout matches prefab.");
        var bays = Elements(data.FindProperty("hangarBays")).ToArray();
        Require(bays.Length == 2, "Two squadron profiles.");
        foreach (int type in new[] {102, 101})
        {
            var bay = bays.Single(row => row.FindPropertyRelative("squadronType").intValue == type);
            int active = type == 102 ? 2 : 1;
            Require(bay.FindPropertyRelative("maxActive").intValue == active && bay.FindPropertyRelative("reserve").intValue == active * 2, "Starting plus replacement pool " + type);
        }
        var hangar = Config(components, "HangarComponent");
        Require(hangar.FindProperty("isDestroyable").boolValue && hangar.FindProperty("hangarHardPoint").objectReferenceValue == hardpoints.Single(p => (int)p.HardPointType == 4), "Destroyable hangar binding.");
        var launch = (Transform)hangar.FindProperty("launchPoint").objectReferenceValue;
        Require(launch != null && launch.IsChildOf(body), "Explicit banking hangar launch transform.");
        foreach (string field in new[] {"bayHardPoints", "bayLaunchPoints"}) Require(hangar.FindProperty(field).arraySize == 0 || hangar.FindProperty(field).arraySize == bays.Length, "Hangar profile binding count " + field);
        var points = HullPoints(root); var bounds = BoundsOf(points);
        Require(bounds.center.magnitude < TOLERANCE && bounds.size.z > bounds.size.x && bounds.size.z > bounds.size.y, "Centered hull with Z length.");
        Require(root.transform.InverseTransformPoint(hardpoints.Single(p => (int)p.HardPointType == 1).transform.position).z < 0, "Engine at negative-Z stern.");
        Require(!bounds.Contains(root.transform.InverseTransformPoint(launch.position)), "Launch clears hull bounds.");
        var collider = components.OfType<BoxCollider>().Single(c => c.gameObject == root);
        Require(Contains(new Bounds(collider.center, collider.size), points), "Root collider encloses hull.");
        float bank = Float(data, "BodyRotationMaxAngle");
        var banked = new[] {-bank, 0f, bank}.SelectMany(angle => points.Select(p => Quaternion.Euler(0, 0, angle) * p)).ToArray();
        Require(banked.All(p => p.y >= Float(data, "HullBottom") - TOLERANCE && p.y <= Float(data, "HullTop") + TOLERANCE), "Banked height bounds.");
        Require(Contains(health.FindProperty("ionFieldBounds").boundsValue, banked), "Ion bounds enclose banked hull.");
        Require(points.All(p => new Vector2(p.x, p.z).magnitude <= Float(data, "NavigationRadius") + TOLERANCE), "Navigation radius encloses hull.");
        var shield = (Shield)health.FindProperty("shieldView").objectReferenceValue;
        Require(shield != null && shield.transform.IsChildOf(body), "Shield banks with hull.");
        Require(AssetDatabase.GetAssetPath(All(shield.gameObject).OfType<MeshFilter>().Single().sharedMesh) == "Assets/Art/Models/Shields/BulwarkShipViewShield.asset", "Own shield mesh.");
        var planes = Elements(new SerializedObject(shield).FindProperty("hullPlanes")).Select(p => p.vector4Value).ToArray();
        var hullToShield = shield.transform.worldToLocalMatrix * root.transform.localToWorldMatrix;
        Require(planes.Length > 0 && points.All(vertex => {
            var point = hullToShield.MultiplyPoint3x4(vertex);
            return planes.All(plane => plane.x * point.x + plane.y * point.y + plane.z * point.z - plane.w <= TOLERANCE);
        }), "Shield planes contain every hull vertex.");
        var selection = Config(components, "SelectionComponent");
        Require(selection.FindProperty("ringSize").vector2Value.x >= 2 * Float(data, "NavigationRadius"), "Selection ring fits hull.");
        var owned = Refs(Config(components, "UnitRenderStateComponent").FindProperty("meshRenderers"));
        var fog = Refs(Config(components, "FogVisibilityComponent").FindProperty("renderers"));
        Require(All(root).OfType<MeshRenderer>().Where(r => r.enabled).All(r => owned.Contains(r) && fog.Contains(r)), "Live renderer ownership and fog bindings.");
        var preview = All(assets[3]).Single(c => c.GetType().Name == "UnitSpawnView"); var previewConfig = new SerializedObject(preview);
        var hologram = previewConfig.FindProperty("hologramMaterial").objectReferenceValue;
        Require(hologram != null && Refs(previewConfig.FindProperty("meshRenderers")).Cast<MeshRenderer>().All(r => r.enabled && r.sharedMaterials.All(m => m == hologram)), "Placement hologram materials.");
        var previewCollider = All(assets[3]).OfType<BoxCollider>().Single(c => c.gameObject == assets[3]);
        Require(previewCollider.isTrigger && Contains(new Bounds(previewCollider.center, previewCollider.size), points), "Placement collision bounds.");
        Require(data.FindProperty("<Wreck>k__BackingField").objectReferenceValue == Load(WRECK_DATA).targetObject && Load(WRECK_DATA).FindProperty("prefab.m_AssetGUID").stringValue == AssetDatabase.AssetPathToGUID(WRECK), "Own wreck data and prefab.");
        Require(All(assets[2]).OfType<MeshRenderer>().Where(r => r.enabled).All(r => r.sharedMaterials.All(m => m.shader.name == "EmpireAtWar/Ship Wreck")), "Wreck opaque hull surfaces.");
        VerifyRegistrations();
        var geometry = JObject.Parse(File.ReadAllText(TASK + "GeometryReport.json"));
        Require((bool)geometry["success"], "Source/FBX geometry audit.");
        var report = new {success = true, shipType = SHIP_ID, hull = 8500, shields = 2500, hardpoints = 16, weaponMounts = 13, targetableSystems = 3, caliber = "Medium", startingVultures = 2, replacementVultures = 2, startingBombers = 1, replacementBombers = 1, size = new[] {bounds.size.x, bounds.size.y, bounds.size.z}, verification = "Saved assets only; no runtime or automated tests."};
        string json = JsonConvert.SerializeObject(report, Formatting.Indented); File.WriteAllText(TASK + "Verification.json", json); return json;
    }

    static void VerifyRegistrations()
    {
        var icon = AssetDatabase.LoadAssetAtPath<Sprite>(ICON); Require(icon != null, "Own icon.");
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(icon, out string iconGuid, out long iconId); string iconKey = iconGuid + ":" + iconId;
        Require(Row(Load("Assets/Settings/Data/Ship/Shared/ShipsData.asset").FindProperty("shipsData.keyValue"), SHIP_ID).FindPropertyRelative("m_AssetGUID").stringValue == AssetDatabase.AssetPathToGUID(DATA), "Ship-data registration.");
        Require(Row(Load("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset").FindProperty("shipIconWrapper.keyValue"), SHIP_ID).objectReferenceValue == icon, "HUD icon.");
        Require(Row(Load("Assets/Settings/Data/Reinforcement/ReinforcementData.asset").FindProperty("spawnShipWrapper.keyValue"), SHIP_ID).FindPropertyRelative("m_AssetGUID").stringValue == AssetDatabase.AssetPathToGUID(PREVIEW), "Placement AssetReference.");
        foreach (string path in new[] {VIEW, DATA, WRECK, PREVIEW})
        {
            var entry = AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(path));
            Require(entry != null && entry.address == Path.GetFileNameWithoutExtension(path), "Addressable " + path);
        }
        var mappings = Load("Assets/Settings/AssetMappingData.asset").FindProperty("assetMappings.keyValue");
        foreach (string path in new[] {VIEW, DATA}) Require(Row(mappings, Path.GetFileNameWithoutExtension(path)).FindPropertyRelative("m_AssetGUID").stringValue == AssetDatabase.AssetPathToGUID(path), "Asset mapping " + path);
        foreach (string guid in AssetDatabase.FindAssets("t:FactionDefinition", new[] {"Assets/Settings/Data/Factions"}))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid); var rows = Load(path).FindProperty("ships.keyValue");
            Require(Elements(rows).Count(row => row.FindPropertyRelative("key").intValue == SHIP_ID) == (path.Contains("/Separatist/") ? 1 : 0), "CIS-only roster " + path);
        }
        var unit = Row(Load("Assets/Settings/Data/Factions/Separatist/SeparatistFaction.asset").FindProperty("ships.keyValue"), SHIP_ID);
        Require(unit.FindPropertyRelative("<Name>k__BackingField").stringValue == "Bulwark Mk1" && unit.FindPropertyRelative("iconKey").stringValue == iconKey && unit.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue == icon, "Roster identity and icon.");
        Require(unit.FindPropertyRelative("<Price>k__BackingField").intValue == 6500 && unit.FindPropertyRelative("<UnitCapacity>k__BackingField").intValue == 3 && unit.FindPropertyRelative("<BuildTime>k__BackingField").intValue == 85, "Source cost, population, build time.");
        Require(AssetDatabase.GetAssetPath(unit.FindPropertyRelative("matchups").objectReferenceValue) == "Assets/Settings/Data/Tooltip/Matchups/BulwarkMatchups.asset", "Own matchup asset.");
        var icons = Load("Assets/Settings/Data/Tooltip/TooltipIconData.asset").FindProperty("icons");
        Require(Elements(icons).Count(row => row.FindPropertyRelative("key").stringValue == iconKey && row.FindPropertyRelative("sprite").objectReferenceValue == icon) == 1, "Tooltip icon.");
        Require(Row(Load("Assets/Settings/Data/Models/Audio/ShipSfxData.asset").FindProperty("ships.keyValue"), SHIP_ID) != null, "Ship audio registration.");
    }

    static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    static float Float(SerializedObject data, string name) => data.FindProperty("<" + name + ">k__BackingField").floatValue;
    static SerializedObject Load(string path) => new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>(path));
    static SerializedObject Config(Component[] components, string name) => new SerializedObject(components.Single(c => c.GetType().Name == name));
    static IEnumerable<SerializedProperty> Elements(SerializedProperty array) => Enumerable.Range(0, array.arraySize).Select(array.GetArrayElementAtIndex);
    static UnityEngine.Object[] Refs(SerializedProperty array) => Elements(array).Select(p => p.objectReferenceValue).ToArray();
    static bool SetEquals(IEnumerable<UnityEngine.Object> first, IEnumerable<UnityEngine.Object> second) => new HashSet<UnityEngine.Object>(first).SetEquals(second);
    static bool Contains(Bounds bounds, Vector3[] points) { bounds.Expand(TOLERANCE * 2); return points.All(bounds.Contains); }
    static Bounds BoundsOf(Vector3[] points) { var bounds = new Bounds(points[0], Vector3.zero); foreach (var point in points) bounds.Encapsulate(point); return bounds; }
    static Vector3[] HullPoints(GameObject root) => All(root).OfType<MeshRenderer>().Where(r => r.enabled && r.sharedMaterials.All(m => m.shader.name == "EmpireAtWar/Ship Lit")).SelectMany(r => Components(r.gameObject).OfType<MeshFilter>().Single().sharedMesh.vertices.Select(v => root.transform.InverseTransformPoint(r.transform.TransformPoint(v)))).ToArray();
    static SerializedProperty Row(SerializedProperty array, object key) => Elements(array).Single(row => key is int number ? row.FindPropertyRelative("key").intValue == number : row.FindPropertyRelative("key").stringValue == (string)key).FindPropertyRelative("value");
    static IEnumerable<Transform> Nodes(Transform node) { yield return node; foreach (Transform child in node) foreach (var descendant in Nodes(child)) yield return descendant; }
    static IEnumerable<Component> All(GameObject root) => Nodes(root.transform).SelectMany(t => Components(t.gameObject));
    static IEnumerable<Component> Components(GameObject obj) { var rows = new SerializedObject(obj).FindProperty("m_Component"); for (int i = 0; i < rows.arraySize; i++) yield return (Component)rows.GetArrayElementAtIndex(i).FindPropertyRelative("component").objectReferenceValue; }
}
