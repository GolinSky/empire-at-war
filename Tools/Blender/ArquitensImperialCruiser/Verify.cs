using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using EmpireAtWar.ViewComponents.Health;
using EmpireAtWar.Components.AttackComponent;

public static class VerifyArquitensImperialCruiser
{
    const string VIEW = "Assets/Prefabs/Models/Ships/ArquitensImperialCruiserShipView.prefab";
    const string DATA = "Assets/Settings/Data/Ship/ArquitensImperialCruiserShipData.asset";
    public static string Main()
    {
        var root = AssetDatabase.LoadAssetAtPath<GameObject>(VIEW);
        var health = Config(root, "HealthComponent");
        Check(health.FindProperty("<ShipUnits>k__BackingField").arraySize == 0, "Hull-only targeting");
        var guns = References(Config(root, "WeaponComponent").FindProperty("hardPoints")).Cast<WeaponHardPoint>().ToArray();
        Check(guns.Length == 8 && guns.Select(g => g.Id).OrderBy(i => i).SequenceEqual(Enumerable.Range(0, 8)), "Eight ordered weapons");
        Check(guns.Count(g => (int)g.WeaponType == 39) == 4 && guns.Count(g => (int)g.WeaponType == 40) == 4, "Four turbolasers and four laser cannons");
        Check(!root.GetComponentsInChildren<MonoBehaviour>(true).Any(c => c.GetType().Name == "HangarComponent"), "No hangar");
        var teamRenderers = References(Config(root, "TeamColorView").FindProperty("meshRenderers")).Cast<MeshRenderer>().ToArray();
        var allRenderers = root.GetComponentsInChildren<MeshRenderer>(true);
        Check(teamRenderers.Length == allRenderers.Length && allRenderers.All(r => teamRenderers.Contains(r)), "Team color binds every mesh renderer, including hidden helpers");
        var data = new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>(DATA));
        Check(Number(data, "Hull") == 1300 && Number(data, "Shields") == 1200 && Number(data, "Speed") == 30, "Requested stats");
        Check(Number(data, "ShieldRegenerateValue") == 4, "Shield regeneration");
        Check(data.FindProperty("hardPointHealth").arraySize == 0 && data.FindProperty("hangarBays").arraySize == 0, "No targetable systems or fighters");
        Check(data.FindProperty("abilities").arraySize == 1 && data.FindProperty("abilities").GetArrayElementAtIndex(0).intValue == 23, "Own Boost Weapon Power");
        Check(Entry("Assets/Settings/Data/Ship/ShipsData.asset", "shipsData.keyValue", 204).FindPropertyRelative("m_AssetGUID").stringValue == AssetDatabase.AssetPathToGUID(DATA), "Ship registry");
        foreach (var path in new[] {VIEW, DATA})
        {
            var guid = AssetDatabase.AssetPathToGUID(path); var key = Path.GetFileNameWithoutExtension(path);
            Check(Entry("Assets/Settings/AssetMappingData.asset", "assetMappings.keyValue", key).FindPropertyRelative("m_AssetGUID").stringValue == guid, "Asset mapping " + key);
            Check(AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(guid).address == key, "Addressable " + key);
        }
        var icon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Ui/Icons/ShipIcon/ArquitensImperialCruiserIcon.png");
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(icon, out string iconGuid, out long localId);
        string iconKey = iconGuid + ":" + localId;
        var unit = Entry("Assets/Settings/Data/Factions/Empire/EmpireFaction.asset", "ships.keyValue", 204);
        Check(unit.FindPropertyRelative("<Name>k__BackingField").stringValue == "ArquitensImperialCruiser", "Empire roster identity");
        Check(unit.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue == icon && unit.FindPropertyRelative("iconKey").stringValue == iconKey, "Faction icon");
        Check(Entry("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset", "shipIconWrapper.keyValue", 204).objectReferenceValue == icon, "Battle icon");
        var tooltip = new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/Settings/Data/Tooltip/TooltipIconData.asset"));
        var icons = tooltip.FindProperty("icons");
        Check(Enumerable.Range(0, icons.arraySize).Any(i => icons.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue == iconKey && icons.GetArrayElementAtIndex(i).FindPropertyRelative("sprite").objectReferenceValue == icon), "Tooltip icon");
        var spawn = Entry("Assets/Settings/Data/Reinforcement/ReinforcementData.asset", "spawnShipWrapper.keyValue", 204).objectReferenceValue;
        Check(AssetDatabase.GetAssetPath(spawn) == "Assets/Prefabs/Ui/Reinforcement/ArquitensImperialCruiserReinforcementView.prefab", "Placement registration");
        var profiles = AssetDatabase.LoadAssetAtPath<WeaponsData>("Assets/Settings/Data/Models/Weapon/WeaponsData.asset");
        foreach (var expected in new[] {new {type = 39, damage = 67.5f, interval = 4.75f, reload = 9.75f, range = 562.5f}, new {type = 40, damage = 7.5f, interval = .5f, reload = 1.25f, range = 200f}})
        {
            var profile = profiles.GetProfile((WeaponType)expected.type);
            Check(profile.ShotsPerSalvo == 2 && profile.Damage == expected.damage && profile.ShotInterval == expected.interval && profile.Reload == expected.reload && profile.Range == expected.range, "Source weapon profile " + expected.type);
            Check(profile.ShotPrefab != null && !profile.Interceptable, "Projectile " + expected.type);
            var audio = new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/Settings/Data/Models/Audio/ShipSfxData.asset")).FindProperty("weapons");
            Check(Enumerable.Range(0, audio.arraySize).Count(i => audio.GetArrayElementAtIndex(i).FindPropertyRelative("weaponType").intValue == expected.type) == 1, "Weapon audio " + expected.type);
        }
        var ability = Entry("Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset", "definitions.keyValue", 23);
        Check(ability.FindPropertyRelative("duration").floatValue == 20 && ability.FindPropertyRelative("recoveryDelay").floatValue == 60, "Ability timings");
        var modifier = ability.FindPropertyRelative("settings.statModifier");
        foreach (var expected in new[] {new {name = "damageMultiplier", value = 1f}, new {name = "fireDelayMultiplier", value = .5f}, new {name = "speedMultiplier", value = .25f}, new {name = "shieldRegenMultiplier", value = 0f}, new {name = "damageTakenMultiplier", value = 1.5f}})
            Check(modifier.FindPropertyRelative(expected.name).floatValue == expected.value, "Ability " + expected.name);
        Check(ability.FindPropertyRelative("settings").managedReferenceId != Entry("Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset", "definitions.keyValue", 5).FindPropertyRelative("settings").managedReferenceId, "Independent ability settings");
        var abilityAudio = new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/Settings/Data/Models/Audio/ShipSfxData.asset")).FindProperty("abilities");
        Check(Enumerable.Range(0, abilityAudio.arraySize).Count(i => abilityAudio.GetArrayElementAtIndex(i).FindPropertyRelative("abilityId").intValue == 23) == 1, "Ability audio");
        var visual = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Ships/ArquitensImperialCruiser.prefab");
        var bounds = BoundsOf(visual); Check(Mathf.Abs(bounds.size.z - 40) < .01f && bounds.center.magnitude < .01f, "Centered hull scale");
        var preview = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GetAssetPath(spawn));
        var opaque = visual.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled && r.sharedMaterials.All(m => m.shader.name == "EmpireAtWar/Ship Lit")).ToArray();
        var opaqueBounds = opaque[0].bounds; foreach (var renderer in opaque.Skip(1)) opaqueBounds.Encapsulate(renderer.bounds);
        Check((BoundsOf(preview).size - opaqueBounds.size).magnitude < .01f, "Opaque-hull placement geometry");
        Check(preview.GetComponentsInChildren<MeshRenderer>(true).Count(r => r.enabled) == opaque.Length, "Placement excludes engine/light/helper planes");
        var wreckData = new SerializedObject(data.FindProperty("<Wreck>k__BackingField").objectReferenceValue);
        var wreck = ((Component)wreckData.FindProperty("<Prefab>k__BackingField").objectReferenceValue).gameObject;
        Check(AssetDatabase.GetAssetPath(wreck) == "Assets/Prefabs/Models/Wrecks/ArquitensImperialCruiserWreckView.prefab", "Own wreck");
        var wreckBindings = Config(wreck, "UnitWreckView");
        Check(References(wreckBindings.FindProperty("meshRenderers")).Length == opaque.Length && References(wreckBindings.FindProperty("meshFilters")).Length == opaque.Length, "Complete opaque wreck bindings");
        foreach (var prefab in new[] {root, visual, preview, wreck})
        {
            foreach (var t in prefab.GetComponentsInChildren<Transform>(true)) Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) == 0, "Missing script " + t.name);
            foreach (var r in prefab.GetComponentsInChildren<Renderer>(true)) Check(r.sharedMaterials.All(m => m != null), "Missing material " + r.name);
            foreach (var component in prefab.GetComponentsInChildren<MonoBehaviour>(true))
            {
                var iterator = new SerializedObject(component).GetIterator();
                while (iterator.Next(true)) if (iterator.propertyType == SerializedPropertyType.ObjectReference)
                    Check(iterator.objectReferenceValue != null || iterator.objectReferenceEntityIdValue == EntityId.None, "Broken reference " + component.name + "/" + iterator.propertyPath);
            }
        }
        var dependencies = AssetDatabase.GetDependencies(VIEW, true);
        Check(!dependencies.Any(p => p.Contains("C9979") || p.Contains("VictoryIAdvanced") || p.Contains("RepublicShips/Arquitens")), "No donor art dependencies");
        var geometry = new System.Collections.Generic.List<object>();
        foreach (var conversion in JArray.Parse(File.ReadAllText("Temp/ArquitensImperialCruiserImport/ConversionReport.json")))
        {
            string name = (string)conversion["name"];
            var model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/EmpireShips/" + name + "/" + name + ".fbx");
            var meshes = model.GetComponentsInChildren<MeshFilter>(true);
            Check(meshes.Length == ((JObject)conversion["source"]["meshes"]).Count, "Mesh count " + name);
            int triangles = meshes.Sum(m => m.sharedMesh.triangles.Length / 3);
            Check(triangles == conversion["source"]["meshes"].Children<JProperty>().Sum(p => (int)p.Value["triangles"]), "Triangle count " + name);
            Check(meshes.All(m => m.sharedMesh.uv.Length > 0), "UVs " + name);
            float error = 0;
            var transforms = model.GetComponentsInChildren<Transform>(true).Where(t => t.GetComponents<MeshFilter>().Length == 0).ToArray();
            foreach (var bone in conversion["source"]["bones"].Children<JProperty>())
            {
                var t = transforms.Single(b => b.name == bone.Name.Split('/').Last());
                var p = bone.Value["head"]; var expected = new Vector3(-(float)p[0], (float)p[2], -(float)p[1]) * .02f;
                error = Mathf.Max(error, Vector3.Distance(t.position, expected));
                if (bone.Value["parent"].Type != JTokenType.Null) Check(t.parent.name == (string)bone.Value["parent"], "Bone parent " + bone.Name);
            }
            Check(error < .0001f, "Unity attachment positions " + name + ": " + error);
            geometry.Add(new {name, meshes = meshes.Length, triangles, bones = ((JObject)conversion["source"]["bones"]).Count, maximumBoneError = error});
        }
        var report = new {asset = VIEW, hull = 1300, shields = 1200, speed = 30, targetableHardpoints = 0, teamRendererBindings = teamRenderers.Length, wreckMeshes = opaque.Length, weapons = guns.GroupBy(g => (int)g.WeaponType).Select(g => new {type = g.Key, count = g.Count()}).ToArray(), bounds = new[] {bounds.size.x, bounds.size.y, bounds.size.z}, geometry, registrations = true, missingScripts = 0, brokenReferences = 0, playModeStartedByThisImport = false, editorWasPlayingDuringReadback = EditorApplication.isPlaying, automatedTests = false};
        File.WriteAllText("Temp/ArquitensImperialCruiserImport/Verification.json", JsonConvert.SerializeObject(report, Formatting.Indented));
        return JsonConvert.SerializeObject(report);
    }
    static SerializedObject Config(GameObject root, string name) => new SerializedObject(root.GetComponentsInChildren<MonoBehaviour>(true).Single(m => m.GetType().Name == name));
    static UnityEngine.Object[] References(SerializedProperty list) => Enumerable.Range(0, list.arraySize).Select(i => list.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
    static SerializedProperty Entry(string path, string list, object key) { var array = new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>(path)).FindProperty(list); return Enumerable.Range(0, array.arraySize).Select(i => array.GetArrayElementAtIndex(i)).Single(r => key is int n ? r.FindPropertyRelative("key").intValue == n : r.FindPropertyRelative("key").stringValue == (string)key).FindPropertyRelative("value"); }
    static float Number(SerializedObject data, string name) => data.FindProperty("<" + name + ">k__BackingField").floatValue;
    static Bounds BoundsOf(GameObject root) { var renderers = root.GetComponentsInChildren<Renderer>(true).Where(r => r.enabled && !(r is LineRenderer) && !(r is ParticleSystemRenderer)).ToArray(); var b = renderers[0].bounds; foreach (var r in renderers.Skip(1)) b.Encapsulate(r.bounds); return b; }
    static void Check(bool condition, string detail) { if (!condition) throw new InvalidOperationException(detail); }
}
