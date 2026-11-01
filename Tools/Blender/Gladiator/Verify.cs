using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using EmpireAtWar.ViewComponents.Health;

public static class VerifyGladiator
{
    const string VIEW = "Assets/Prefabs/Models/Ships/Empire/View/GladiatorShipView.prefab";
    const string DATA = "Assets/Settings/Data/Ship/Empire/GladiatorShipData.asset";
    const string VISUAL = "Assets/Prefabs/Models/Ships/Empire/Models/Gladiator.prefab";
    const string PREVIEW = "Assets/Prefabs/Ui/Reinforcement/GladiatorReinforcementView.prefab";
    const int SHIP_ID = 216;

    public static string Main()
    {
        var view = AssetDatabase.LoadAssetAtPath<GameObject>(VIEW);
        var data = Load(DATA);
        Require(data.FindProperty("<Hull>k__BackingField").floatValue == 6000, "Hull");
        Require(data.FindProperty("<Shields>k__BackingField").floatValue == 5000, "Shields");
        Require(data.FindProperty("<ShipClass>k__BackingField").intValue == 4, "Capital class");
        Require(data.FindProperty("<ShieldRegenTier>k__BackingField").intValue == 3, "Capital shield regen");
        Require(data.FindProperty("<Range>k__BackingField").floatValue == 600, "Radar range");
        var abilities = data.FindProperty("abilities");
        Require(abilities.arraySize == 1 && abilities.GetArrayElementAtIndex(0).intValue == (int)EmpireAtWar.Services.ShipAbilities.ShipAbilityId.BoostShieldPower, "Existing Boost Shield Power ability");
        var catalog = AssetDatabase.LoadAssetAtPath<EmpireAtWar.Services.ShipAbilities.ShipAbilityCatalog>("Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset");
        Require(catalog.Get(EmpireAtWar.Services.ShipAbilities.ShipAbilityId.BoostShieldPower).Settings is EmpireAtWar.Services.ShipAbilities.Abilities.BoostShieldPowerSettings, "Shared shield power settings");
        Require(view.transform.localScale == Vector3.one, "View root scale");
        var explosionRenderers = References(Config(view, "Ship").FindProperty("explosionHullRenderers"));
        Require(explosionRenderers.Length > 0 && explosionRenderers.All(r => r != null), "Explosion hull renderer bindings");
        var targets = References(Config(view, "HealthComponent").FindProperty("<ShipUnits>k__BackingField")).Cast<HardPoint>().ToArray();
        Require(targets.Length == 10, "Ten targetable hardpoints");
        Require(targets.Select(p => p.Id).Distinct().Count() == 10, "Unique hardpoint IDs");
        Require(targets.All(p => p.HealthOverride == 0 && p.HealthWeight == 1), "Hull-derived health weights");
        Require(targets.Count(p => (int)p.HardPointType == 2) == 1 && targets.Count(p => (int)p.HardPointType == 4) == 1, "Shield and hangar");
        var weapons = References(Config(view, "WeaponComponent").FindProperty("hardPoints")).Cast<WeaponHardPoint>().ToArray();
        Require(weapons.Length == 8 && weapons.All(p => targets.Contains(p)), "All eight weapons targetable");
        foreach (var type in new[] {5, 13, 14}) Require(weapons.Count(w => (int)w.WeaponType == type) == (type == 5 ? 4 : 2), "Weapon type " + type);
        Require(weapons.Where(w => (int)w.WeaponType == 14).All(w => (int)w.HardPointType == 8), "Point defense hardpoint classification");
        var loadout = data.FindProperty("weaponLoadout");
        Require(loadout.arraySize == 3, "Three weapon profile groups");
        foreach (var type in new[] {5, 13, 14})
        {
            var entry = Enumerable.Range(0, loadout.arraySize).Select(i => loadout.GetArrayElementAtIndex(i)).Single(row => row.FindPropertyRelative("weaponType").intValue == type);
            Require(entry.FindPropertyRelative("count").intValue == (type == 5 ? 4 : 2), "Baked loadout count " + type);
        }
        foreach (string socket in new[] {"DTL_02", "DTL_05"})
        {
            var point = weapons.Single(w => w.name == socket);
            var bone = view.GetComponentsInChildren<Transform>(true).Single(t => t.name == socket && t.GetComponents<HardPoint>().Length == 0);
            Require(Vector3.Distance(point.Position, bone.position) < .0001f, "Point defense source anchor " + socket);
        }
        var injections = References(Config(view, "EntityBindings").FindProperty("<Injectables>k__BackingField"));
        Require(targets.All(p => injections.Contains(p)), "Hardpoint injection bindings");
        var hangar = Config(view, "HangarComponent");
        Require(targets.Contains((HardPoint)hangar.FindProperty("hangarHardPoint").objectReferenceValue), "Hangar hardpoint");
        Require(hangar.FindProperty("launchPoint").objectReferenceValue != null && hangar.FindProperty("isDestroyable").boolValue, "Destroyable launch bay");
        var bays = data.FindProperty("hangarBays"); Require(bays.arraySize == 6, "Six Empire hangar bays");
        for (int i = 0; i < 6; i++)
        {
            var bay = bays.GetArrayElementAtIndex(i);
            Require(bay.FindPropertyRelative("squadronType").intValue == 200 + i && bay.FindPropertyRelative("reserve").intValue == 10 && bay.FindPropertyRelative("maxActive").intValue == 1, "Bay " + i);
        }
        foreach (var path in new[] {VIEW, VISUAL, PREVIEW, "Assets/Prefabs/Models/Wrecks/GladiatorWreckView.prefab"})
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Require(prefab != null, "Missing prefab " + path);
            Require(prefab.GetComponentsInChildren<Transform>(true).All(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) == 0), "Missing script " + path);
            foreach (var component in prefab.GetComponentsInChildren<MonoBehaviour>(true))
            {
                var iterator = new SerializedObject(component).GetIterator();
                while (iterator.NextVisible(true))
                    if (iterator.propertyType == SerializedPropertyType.ObjectReference)
                        Require(iterator.objectReferenceValue != null || iterator.objectReferenceInstanceIDValue == 0, path + "/" + component.GetType().Name + "/" + iterator.propertyPath);
            }
        }
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        foreach (var path in new[] {VIEW, PREVIEW, "Assets/Prefabs/Models/Wrecks/GladiatorWreckView.prefab"})
        {
            var entry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(path));
            Require(entry != null && entry.parentGroup.Name == "EmpireUnitViews" && entry.address == Path.GetFileNameWithoutExtension(path), "Addressable " + path);
        }
        var ships = Load("Assets/Settings/Data/Ship/Shared/ShipsData.asset");
        Require(Entry(ships.FindProperty("shipsData.keyValue"), SHIP_ID).FindPropertyRelative("m_AssetGUID").stringValue == AssetDatabase.AssetPathToGUID(DATA), "Ship data registration");
        var reinforcement = Load("Assets/Settings/Data/Reinforcement/ReinforcementData.asset");
        Require(Entry(reinforcement.FindProperty("spawnShipWrapper.keyValue"), SHIP_ID).FindPropertyRelative("m_AssetGUID").stringValue == AssetDatabase.AssetPathToGUID(PREVIEW), "Preview registration");
        var faction = Load("Assets/Settings/Data/Factions/Empire/EmpireFaction.asset");
        var unit = Entry(faction.FindProperty("ships.keyValue"), SHIP_ID);
        foreach (var pair in new[] {new {field="Price", value=4000}, new {field="BuildTime", value=40}, new {field="AvailableLevel", value=4}, new {field="UnitCapacity", value=8}})
            Require(unit.FindPropertyRelative("<" + pair.field + ">k__BackingField").intValue == pair.value, pair.field);
        var icon = unit.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue;
        Require(icon != null && Entry(Load("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset").FindProperty("shipIconWrapper.keyValue"), SHIP_ID).objectReferenceValue == icon, "Faction and ship icons");
        var geometry = Geometry();
        foreach (var name in new[] {"AudioShipComponent", "HealthComponent", "UnitRenderStateComponent", "ShipMoveComponent", "HangarComponent"})
        {
            var component = Config(view, name);
            var iterator = component.GetIterator();
            while (iterator.NextVisible(true))
                if (iterator.propertyType == SerializedPropertyType.ObjectReference && iterator.objectReferenceValue != null)
                {
                    string dependency = AssetDatabase.GetAssetPath(iterator.objectReferenceValue);
                    Require(!dependency.Contains("ImperialVenator") || name == "AudioShipComponent", "Donor dependency " + name + "/" + iterator.propertyPath);
                }
        }
        var report = new JObject { ["savedAssetChecks"]="passed", ["ability"]="BoostShieldPower", ["weapons"]=8, ["hardpoints"]=10, ["hull"]=6000, ["shields"]=5000, ["radar"]=600, ["testsRun"]=false, ["runtimeAcceptance"]="pending-user", ["geometry"]=geometry };
        File.WriteAllText("Temp/GladiatorImport/Verification.json", report.ToString());
        return report.ToString();
    }
    static JObject Geometry()
    {
        const string MODEL = "Assets/Art/Models/EmpireShips/Gladiator/Gladiator.fbx";
        const string MATERIALS = "Assets/Art/Materials/Models/EmpireShips/Gladiator/";
        var conversion = JObject.Parse(File.ReadAllText("Temp/GladiatorImport/ConversionReport.json"))["models"]["Gladiator"];
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(MODEL);
        Require(model != null, "Gladiator FBX");
        var importer = (ModelImporter)AssetImporter.GetAtPath(MODEL);
        Require(Mathf.Abs(importer.globalScale - .02f) < .000001f, "FBX import scale");
        var renderers = model.GetComponentsInChildren<Renderer>(true);
        var meshes = (JObject)conversion["before"]["meshes"];
        Require(renderers.Length == meshes.Count, "All ten source meshes in FBX");
        var rows = new JArray();
        foreach (var source in meshes.Properties())
        {
            var renderer = renderers.Single(r => r.name == source.Name);
            var mesh = renderer is SkinnedMeshRenderer skinned ? skinned.sharedMesh : renderer.GetComponents<MeshFilter>().Single().sharedMesh;
            int triangles = mesh.triangles.Length / 3;
            Require(triangles == (int)source.Value["triangles"], "Triangles " + source.Name);
            var names = source.Value["materials"].Values<string>().ToArray();
            Require(mesh.subMeshCount == names.Length, "Submesh count " + source.Name);
            Require(renderer.sharedMaterials.Select(m => m.name).SequenceEqual(names), "Material names " + source.Name);
            for (int index = 0; index < names.Length; index++)
                Require(AssetDatabase.GetAssetPath(renderer.sharedMaterials[index]) == MATERIALS + names[index] + ".mat", "External material " + source.Name);
            Require(mesh.uv.Length == mesh.vertexCount && (int)source.Value["uv_layers"] == 1, "UV channel coverage " + source.Name);
            rows.Add(new JObject {["mesh"]=source.Name, ["triangles"]=triangles, ["uvVertices"]=mesh.uv.Length, ["materials"]=new JArray(names)});
        }
        var bones = (JObject)conversion["before"]["bones"];
        var transforms = model.GetComponentsInChildren<Transform>(true);
        float maximumError = 0;
        foreach (var source in bones.Properties())
        {
            var bone = transforms.Single(t => t.name == source.Name && t.GetComponents<Renderer>().Length == 0);
            var head = source.Value["head"];
            var expected = new Vector3(-(float)head[0], (float)head[2], -(float)head[1]) * .02f;
            maximumError = Mathf.Max(maximumError, Vector3.Distance(bone.position, expected));
            string parent = (string)source.Value["parent"];
            Require(parent == null || bone.parent.name == parent, "Bone parent " + source.Name);
        }
        Require(maximumError < .0001f, "Bone coordinate conversion " + maximumError);
        return new JObject {["meshes"]=rows, ["bones"]=bones.Count, ["maximumBonePositionError"]=maximumError,
            ["uvVerification"]="UV channel coverage checked in Unity; Blender conversion report owns source-to-FBX corner comparison."};
    }
    static void Require(bool valid, string message) { if (!valid) throw new InvalidOperationException(message); }
    static SerializedObject Config(GameObject root, string name) => new SerializedObject(root.GetComponentsInChildren<MonoBehaviour>(true).Single(c => c.GetType().Name == name));
    static SerializedObject Load(string path) => new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>(path));
    static UnityEngine.Object[] References(SerializedProperty array) => Enumerable.Range(0,array.arraySize).Select(i => array.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
    static SerializedProperty Entry(SerializedProperty array, int key) => Enumerable.Range(0,array.arraySize).Select(i => array.GetArrayElementAtIndex(i)).Single(r => r.FindPropertyRelative("key").intValue == key).FindPropertyRelative("value");
}
