using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EmpireAtWar.Entities.Reinforcement;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.ViewComponents.Health;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

public static class VerifyInvincible
{
    const string VIEW = "Assets/Prefabs/Models/Ships/Separatist/InvincibleShipView.prefab";
    const string DATA = "Assets/Settings/Data/Ship/Separatist/InvincibleShipData.asset";
    const string VISUAL = "Assets/Prefabs/Models/Ships/Separatist/Invincible.prefab";
    const string WRECK = "Assets/Prefabs/Models/Wrecks/InvincibleWreckView.prefab";
    const string PREVIEW = "Assets/Prefabs/Ui/Reinforcement/InvincibleReinforcementView.prefab";
    const string WRECK_DATA = "Assets/Settings/Data/Models/Wrecks/Separatist/InvincibleWreckData.asset";
    const float TOLERANCE = .01f;

    public static string Main()
    {
        var prefabs = new[] {VISUAL, VIEW, WRECK, PREVIEW}.Select(path => AssetDatabase.LoadAssetAtPath<GameObject>(path)).ToArray();
        foreach (var prefab in prefabs)
        {
            Require(prefab != null, "Missing Invincible prefab.");
            Require((prefab.transform.localScale - Vector3.one).sqrMagnitude < .000001f, prefab.name + " root scale.");
            foreach (var component in All(prefab))
            {
                Require(component != null, prefab.name + " has a missing script.");
                var property = new SerializedObject(component).GetIterator();
                while (property.Next(true))
                    if (property.propertyType == SerializedPropertyType.ObjectReference)
                        Require(property.objectReferenceValue != null || property.objectReferenceEntityIdValue == EntityId.None, prefab.name + " broken reference " + property.propertyPath);
            }
            foreach (var renderer in All(prefab).OfType<Renderer>())
                Require(renderer.sharedMaterials.All(m => m != null && m.shader != null), prefab.name + " missing shader/material.");
            var donorDependencies = AssetDatabase.GetDependencies(AssetDatabase.GetAssetPath(prefab), true)
                .Where(path => path.Contains("Providence") && (path.StartsWith("Assets/Art/Models/") || path.StartsWith("Assets/Art/Materials/") || path.StartsWith("Assets/Art/Textures/Models/") || path.StartsWith("Assets/Prefabs/"))).ToArray();
            Require(donorDependencies.Length == 0, prefab.name + " retains Providence geometry/material/prefab: " + string.Join(",", donorDependencies));
        }
        var root = prefabs[1];
        var components = All(root).ToArray();
        var hardpoints = components.OfType<HardPoint>().OrderBy(p => p.Id).ToArray();
        var weapons = hardpoints.OfType<WeaponHardPoint>().ToArray();
        Require(hardpoints.Length == 31 && hardpoints.Select(p => p.Id).Distinct().Count() == 31, "Expected 31 unique hardpoints.");
        Require(hardpoints.Select(p => p.Id).SequenceEqual(Enumerable.Range(0, 31)), "Hardpoint IDs must cover 0..30.");
        Require(weapons.Length == 28, "Expected 28 weapon hardpoints.");
        int[] types = {3, 4, 5, 11}, counts = {12, 4, 8, 4};
        for (int i = 0; i < types.Length; i++) Require(weapons.Count(p => (int)p.WeaponType == types[i]) == counts[i], "Weapon profile count " + types[i]);
        var systems = hardpoints.Except(weapons).ToArray();
        Require(systems.Length == 3 && systems.Select(p => p.HardPointType).Distinct().Count() == 3, "Expected one shield, engine and hangar system.");
        var health = Config(components, "HealthComponent");
        var bindings = Config(components, "EntityBindings");
        Require(SetEquals(Refs(health.FindProperty("<ShipUnits>k__BackingField")), hardpoints), "Health must bind all 31 hardpoints.");
        Require(SetEquals(Refs(Config(components, "WeaponComponent").FindProperty("hardPoints")), weapons), "Weapons must bind all 28 mounts.");
        var injected = Refs(bindings.FindProperty("<Injectables>k__BackingField"));
        Require(hardpoints.All(p => injected.Contains(p)), "EntityBindings must inject every hardpoint.");
        var body = (Transform)Config(components, "ShipMoveComponent").FindProperty("bodyTransform").objectReferenceValue;
        Require(body != null && hardpoints.All(p => p.transform.IsChildOf(body)), "Every hardpoint must bank with the body.");
        foreach (var point in hardpoints)
            Require(new SerializedObject(point).FindProperty("destroyedVfxAnchor").objectReferenceValue is Transform anchor && anchor.IsChildOf(root.transform), "Required destruction anchor " + point.Id);
        Require(weapons.All(p => weapons.Count(q => (q.transform.position - p.transform.position).sqrMagnitude < .000001f) == 1), "Weapon sockets must be distinct.");
        var requiredBindings = new[] {"Selection", "Health", "Radar", "Weapons", "FogVisibility", "ShipMove", "Hangar", "AudioShip", "AudioDialog"};
        foreach (string field in requiredBindings)
        {
            var property = bindings.FindProperty("<" + field + ">k__BackingField");
            Require(property.isArray ? Refs(property).Length > 0 && Refs(property).All(p => p != null) : property.objectReferenceValue != null, "Required lifecycle binding " + field);
        }
        var data = Load(DATA); var donorData = Load("Assets/Settings/Data/Ship/Separatist/ProvidenceShipData.asset");
        Require(Float(data, "Hull") == 10000 && Float(data, "Shields") == 6000, "Expected 10000 hull / 6000 shields.");
        Require(Float(data, "Hull") == Float(donorData, "Hull") * 2 && Float(data, "Shields") == Float(donorData, "Shields") * 2, "Hull/shields must double Providence.");
        var changed = new[] {"<Hull>k__BackingField", "<Shields>k__BackingField", "<HullBottom>k__BackingField", "<HullTop>k__BackingField", "<NavigationRadius>k__BackingField", "<Wreck>k__BackingField", "weaponLoadout"};
        var iterator = donorData.GetIterator(); bool enter = true;
        while (iterator.NextVisible(enter))
        {
            enter = false;
            if (!iterator.name.StartsWith("m_") && !changed.Contains(iterator.name))
                Require(SerializedProperty.DataEquals(iterator, data.FindProperty(iterator.name)), "Unexpected Providence data change " + iterator.name);
        }
        var loadout = data.FindProperty("weaponLoadout");
        Require(loadout.arraySize == 4, "Weapon loadout must retain four profiles.");
        for (int i = 0; i < loadout.arraySize; i++)
        {
            var row = loadout.GetArrayElementAtIndex(i); int type = row.FindPropertyRelative("weaponType").intValue;
            Require(row.FindPropertyRelative("count").intValue == weapons.Count(p => (int)p.WeaponType == type), "Data/prefab weapon count " + type);
        }
        var points = HullPoints(root); var bounds = BoundsOf(points);
        var donorRoot = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Ships/Separatist/ProvidenceShipView.prefab");
        float donorLength = BoundsOf(HullPoints(donorRoot)).size.z;
        Require(Mathf.Abs(bounds.size.z / donorLength - 39f / 14f) < .0001f && bounds.center.magnitude < TOLERANCE, "Hull size/centering.");
        Require(bounds.size.z > bounds.size.y && bounds.size.z > bounds.size.x, "Hull length must follow Unity Z.");
        Require(root.transform.InverseTransformPoint(Nodes(root.transform).Single(t => t.name == "HP_Engine").position).z < bounds.center.z, "Engines must face the negative-Z stern.");
        Require(root.transform.InverseTransformPoint(Nodes(root.transform).Single(t => t.name == "HP_Shield").position).y > bounds.center.y, "Dorsal shield socket must face positive-Y up.");
        var collider = components.OfType<BoxCollider>().Single(c => c.gameObject == root);
        Require(Contains(new Bounds(collider.center, collider.size), points), "Root collider must contain the hull.");
        float bank = Float(data, "BodyRotationMaxAngle");
        var banked = new[] {-bank, 0f, bank}.SelectMany(angle => points.Select(p => Quaternion.Euler(0, 0, angle) * p)).ToArray();
        float hullBottom = Float(data, "HullBottom"), hullTop = Float(data, "HullTop");
        Require(banked.All(p => p.y >= hullBottom - TOLERANCE && p.y <= hullTop + TOLERANCE), "Banked hull height range.");
        Require(Contains(health.FindProperty("ionFieldBounds").boundsValue, banked), "Ion field must contain banked hull.");
        float radius = Float(data, "NavigationRadius");
        Require(points.All(p => new Vector2(p.x, p.z).magnitude <= radius + TOLERANCE), "Navigation radius must enclose hull.");
        var selection = Config(components, "SelectionComponent");
        Require(selection.FindProperty("ringSize").vector2Value.x >= 2 * radius, "Selection ring must fit hull.");
        var selectionRoot = (Transform)selection.FindProperty("selectionRoot").objectReferenceValue;
        var selectionPosition = root.transform.InverseTransformPoint(selectionRoot.position);
        Require(selectionRoot.parent == root.transform && selectionPosition.y <= hullBottom - 2 + TOLERANCE, "Selection/global-overlay anchor must stay below the banked hull.");
        Require(Mathf.Abs(selectionPosition.x) < TOLERANCE && Mathf.Abs(selectionPosition.z) < TOLERANCE, "Selection anchor must remain centered under the hull.");
        var selectionRenderer = (SpriteRenderer)selection.FindProperty("selectedRenderer").objectReferenceValue;
        Require(selectionRenderer.transform.localPosition.sqrMagnitude < .000001f, "Selection sprite must follow its anchor.");
        var markerSize = Vector3.Scale(selectionRenderer.sprite.bounds.size, selectionRenderer.transform.lossyScale);
        Require(Mathf.Abs(markerSize.x - radius * 2) < TOLERANCE && Mathf.Abs(markerSize.y - radius * 2) < TOLERANCE, "Saved selection sprite size must match the runtime ring size.");
        var shield = (Shield)health.FindProperty("shieldView").objectReferenceValue;
        Require(shield != null && shield.transform.IsChildOf(body), "Shield must bank with hull.");
        var shieldMesh = All(shield.gameObject).OfType<MeshFilter>().Single().sharedMesh;
        Require(AssetDatabase.GetAssetPath(shieldMesh) == "Assets/Art/Models/Shields/InvincibleShipViewShield.asset", "Shield must own its baked mesh.");
        var planeProperty = new SerializedObject(shield).FindProperty("hullPlanes");
        var planes = Enumerable.Range(0, planeProperty.arraySize).Select(i => planeProperty.GetArrayElementAtIndex(i).vector4Value).ToArray();
        var hullToShield = shield.transform.worldToLocalMatrix * root.transform.localToWorldMatrix;
        float maxViolation = float.NegativeInfinity;
        foreach (var localPoint in points)
        {
            var point = hullToShield.MultiplyPoint3x4(localPoint);
            foreach (var plane in planes)
                maxViolation = Mathf.Max(maxViolation, plane.x * point.x + plane.y * point.y + plane.z * point.z - plane.w);
        }
        Require(planes.Length > 0 && maxViolation <= TOLERANCE, "Baked shield planes must contain every hull vertex.");
        var hangar = Config(components, "HangarComponent");
        var launch = (Transform)hangar.FindProperty("launchPoint").objectReferenceValue;
        Require(root.transform.InverseTransformPoint(launch.position).z > bounds.max.z && !bounds.Contains(root.transform.InverseTransformPoint(launch.position)), "Hangar launch must clear hull.");
        Require(systems.Contains((HardPoint)hangar.FindProperty("hangarHardPoint").objectReferenceValue), "Hangar system binding.");
        var visible = All(prefabs[0]).OfType<MeshRenderer>().Where(r => r.enabled).ToArray();
        var hullMaterial = visible.Single(r => r.sharedMaterials.All(m => m.shader.name == "EmpireAtWar/Ship Lit")).sharedMaterial;
        var hullTint = hullMaterial.GetColor("_BaseColor");
        Require(hullTint == new Color(.55f, .58f, .62f, 1) && Mathf.Abs(hullMaterial.GetFloat("_Metallic") - .2f) < .0001f && Mathf.Abs(hullMaterial.GetFloat("_Smoothness") - .22f) < .0001f, "Tuned hull material must retain its darker, rougher settings.");
        var lightsMaterial = visible.Single(r => r.sharedMaterials.All(m => m.shader.name != "EmpireAtWar/Ship Lit")).sharedMaterial;
        Require(lightsMaterial.GetColor("_BaseColor") == new Color(.5f, .5f, .5f, 1), "Source lights must retain the reduced brightness.");
        Require(All(prefabs[2]).OfType<MeshRenderer>().Where(r => r.enabled).All(r => r.sharedMaterials.All(m => m.GetColor("_BaseColor") == hullTint)), "Wreck materials must match the tuned hull brightness.");
        var ownedRenderers = Refs(Config(components, "UnitRenderStateComponent").FindProperty("meshRenderers"));
        Require(injected.Contains(components.Single(c => c.GetType().Name == "UnitRenderStateComponent")), "Render state injection.");
        Require(All(root).OfType<MeshRenderer>().Where(r => r.enabled && (r.sharedMaterials.Any(m => m.shader.name == "EmpireAtWar/Ship Lit") || r.gameObject == shield.gameObject)).All(r => ownedRenderers.Contains(r)), "Unit render state must include hull and shield.");
        var spawn = All(prefabs[3]).OfType<UnitSpawnView>().Single(); var spawnConfig = new SerializedObject(spawn);
        var hologram = spawnConfig.FindProperty("hologramMaterial").objectReferenceValue;
        Require(hologram != null && Refs(spawnConfig.FindProperty("meshRenderers")).Cast<MeshRenderer>().All(r => r.enabled && r.sharedMaterials.All(m => m == hologram)), "Placement hologram bindings.");
        var previewCollider = All(prefabs[3]).OfType<BoxCollider>().Single(c => c.gameObject == prefabs[3]);
        Require(previewCollider.isTrigger && Contains(new Bounds(previewCollider.center, previewCollider.size), points), "Placement collision bounds.");
        var wreckData = Load(WRECK_DATA);
        Require(data.FindProperty("<Wreck>k__BackingField").objectReferenceValue == wreckData.targetObject && wreckData.FindProperty("prefab.m_AssetGUID").stringValue == AssetDatabase.AssetPathToGUID(WRECK), "Own wreck data/prefab.");
        Require(All(prefabs[2]).OfType<MeshRenderer>().Where(r => r.enabled).All(r => r.sharedMaterials.All(m => m.shader.name == "EmpireAtWar/Ship Wreck")), "Wreck must contain only wreck hull surfaces.");
        var icon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Ui/Icons/ShipIcon/Separatist/InvincibleIcon.png");
        Require(icon != null, "Invincible icon missing.");
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(icon, out string iconGuid, out long iconId); string iconKey = iconGuid + ":" + iconId;
        int shipId = (int)ShipType.Invincible;
        Require(Row(Load("Assets/Settings/Data/Ship/Shared/ShipsData.asset").FindProperty("shipsData.keyValue"), shipId).FindPropertyRelative("m_AssetGUID").stringValue == AssetDatabase.AssetPathToGUID(DATA), "Ship data registration.");
        Require(Row(Load("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset").FindProperty("shipIconWrapper.keyValue"), shipId).objectReferenceValue == icon, "HUD icon registration.");
        Require(Row(Load("Assets/Settings/Data/Reinforcement/ReinforcementData.asset").FindProperty("spawnShipWrapper.keyValue"), shipId).objectReferenceValue == spawn, "Placement registration.");
        var mapping = Load("Assets/Settings/AssetMappingData.asset").FindProperty("assetMappings.keyValue");
        foreach (var path in new[] {VIEW, DATA, WRECK})
        {
            string guid = AssetDatabase.AssetPathToGUID(path); var entry = AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(guid);
            Require(entry != null && entry.address == Path.GetFileNameWithoutExtension(path), "Addressable " + path);
            Require(Row(mapping, entry.address).FindPropertyRelative("m_AssetGUID").stringValue == guid, "Asset mapping " + path);
        }
        var factionGuids = AssetDatabase.FindAssets("t:FactionDefinition", new[] {"Assets/Settings/Data/Factions"});
        Require(factionGuids.Length > 0, "Faction asset search must find the registered rosters.");
        int separatistRosters = 0, price = 0, population = 0;
        foreach (var guid in factionGuids)
        {
            var faction = Load(AssetDatabase.GUIDToAssetPath(guid)); var rows = faction.FindProperty("ships.keyValue");
            int count = Enumerable.Range(0, rows.arraySize).Count(i => rows.GetArrayElementAtIndex(i).FindPropertyRelative("key").intValue == shipId);
            Require(count == (faction.FindProperty("factionType").intValue == (int)FactionType.Separatist ? 1 : 0), "Invincible must occur only once in Separatist roster.");
            if (count == 0) continue;
            separatistRosters++;
            var unit = Row(rows, shipId); var donor = Row(rows, (int)ShipType.Providence);
            Require(unit.FindPropertyRelative("iconKey").stringValue == iconKey && unit.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue == icon, "Faction icon binding.");
            Require(AssetDatabase.GetAssetPath(unit.FindPropertyRelative("matchups").objectReferenceValue) == "Assets/Settings/Data/Tooltip/Matchups/InvincibleMatchups.asset", "Own matchup data.");
            foreach (string field in new[] {"<MaxCount>k__BackingField", "<AvailableLevel>k__BackingField", "<BuildTime>k__BackingField", "isHero"})
                Require(SerializedProperty.DataEquals(unit.FindPropertyRelative(field), donor.FindPropertyRelative(field)), "Inherited roster field " + field);
            price = unit.FindPropertyRelative("<Price>k__BackingField").intValue;
            population = unit.FindPropertyRelative("<UnitCapacity>k__BackingField").intValue;
            Require(price == donor.FindPropertyRelative("<Price>k__BackingField").intValue * 2 && population == donor.FindPropertyRelative("<UnitCapacity>k__BackingField").intValue * 2, "Price and population must double Providence.");
        }
        Require(separatistRosters == 1, "Exactly one Separatist roster must register Invincible.");
        var tooltip = Load("Assets/Settings/Data/Tooltip/TooltipIconData.asset").FindProperty("icons");
        Require(Enumerable.Range(0, tooltip.arraySize).Count(i => tooltip.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue == iconKey && tooltip.GetArrayElementAtIndex(i).FindPropertyRelative("sprite").objectReferenceValue == icon) == 1, "Tooltip icon registration.");
        var mappingRows = JArray.Parse(File.ReadAllText("Temp/InvincibleImport/HardpointMapping.json"));
        Require(mappingRows.Count == 31 && mappingRows.Select(row => (string)row["source"]).Distinct().Count() == 31, "Distinct authored weapon/system socket mapping.");
        var report = new {success = true, shipType = shipId, hull = Float(data, "Hull"), shields = Float(data, "Shields"), hardpoints = hardpoints.Length, weapons = weapons.Length, systemCount = systems.Length, size = Values(bounds.size), center = Values(bounds.center), donorLength, sizeRatio = bounds.size.z / donorLength, radius, hullBottom, hullTop, selectionAnchor = Values(selectionPosition), hullTint = new[] {hullTint.r, hullTint.g, hullTint.b}, price, population, shieldPlanes = planes.Length, maxShieldPlaneViolation = maxViolation, visibleMeshes = visible.Length, ownPrefabCount = prefabs.Length, separatistRosters};
        string json = JsonConvert.SerializeObject(report, Formatting.Indented); File.WriteAllText("Temp/InvincibleImport/Verification.json", json); return json;
    }

    static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    static float[] Values(Vector3 value) => new[] {value.x, value.y, value.z};
    static float Float(SerializedObject data, string name) => data.FindProperty("<" + name + ">k__BackingField").floatValue;
    static SerializedObject Load(string path) => new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>(path));
    static SerializedObject Config(Component[] components, string name) => new SerializedObject(components.Single(c => c.GetType().Name == name));
    static UnityEngine.Object[] Refs(SerializedProperty array) => Enumerable.Range(0, array.arraySize).Select(i => array.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
    static bool SetEquals(IEnumerable<UnityEngine.Object> first, IEnumerable<UnityEngine.Object> second) => new HashSet<UnityEngine.Object>(first).SetEquals(second);
    static bool Contains(Bounds bounds, Vector3[] points) { bounds.Expand(TOLERANCE * 2); return points.All(bounds.Contains); }
    static Bounds BoundsOf(Vector3[] points) { var bounds = new Bounds(points[0], Vector3.zero); foreach (var point in points) bounds.Encapsulate(point); return bounds; }
    static Vector3[] HullPoints(GameObject root) => All(root).OfType<MeshRenderer>().Where(r => r.enabled && r.sharedMaterials.All(m => m.shader.name == "EmpireAtWar/Ship Lit")).SelectMany(r => Components(r.gameObject).OfType<MeshFilter>().Single().sharedMesh.vertices.Select(v => root.transform.InverseTransformPoint(r.transform.TransformPoint(v)))).Distinct().ToArray();
    static SerializedProperty Row(SerializedProperty array, object key) => Enumerable.Range(0, array.arraySize).Select(array.GetArrayElementAtIndex).Single(row => key is int number ? row.FindPropertyRelative("key").intValue == number : row.FindPropertyRelative("key").stringValue == (string)key).FindPropertyRelative("value");
    static IEnumerable<Transform> Nodes(Transform node) { yield return node; foreach (Transform child in node) foreach (var descendant in Nodes(child)) yield return descendant; }
    static IEnumerable<Component> All(GameObject root) => Nodes(root.transform).SelectMany(t => Components(t.gameObject));
    static IEnumerable<Component> Components(GameObject obj) { var rows = new SerializedObject(obj).FindProperty("m_Component"); for (int i = 0; i < rows.arraySize; i++) yield return (Component)rows.GetArrayElementAtIndex(i).FindPropertyRelative("component").objectReferenceValue; }
}
