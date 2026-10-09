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
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.ShipAbilities.Abilities;
using EmpireAtWar.Editor.Rendering;

public static class VerifyImperialVenator
{
    const string VIEW = "Assets/Prefabs/Models/Ships/ImperialVenatorShipView.prefab";
    const string DATA = "Assets/Settings/Data/Ship/ImperialVenatorShipData.asset";
    static int _checks;
    public static string Main()
    {
        Check(!EditorApplication.isCompiling && !EditorApplication.isUpdating, "Editor finished importing and compiling");
        Check(!EditorUtility.scriptCompilationFailed, "Unity compilation succeeded");
        Check(Enum.GetName(typeof(EmpireAtWar.Models.Factions.ShipType), 211) == "ImperialVenator" && Enum.GetName(typeof(ShipAbilityId), 35) == "ImperialVenatorIntensifyFirepower", "Compiled identifiers");
        var root = AssetDatabase.LoadAssetAtPath<GameObject>(VIEW);
        var donor = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Ships/VenatorShipView.prefab");
        var targets = Objects(Config(root, "HealthComponent").FindProperty("<ShipUnits>k__BackingField")).Cast<HardPoint>().ToArray();
        var guns = Objects(Config(root, "WeaponComponent").FindProperty("hardPoints")).Cast<WeaponHardPoint>().ToArray();
        Check(targets.Length == 18 && targets.Select(p => p.Id).OrderBy(i => i).SequenceEqual(Enumerable.Range(0, 18)), "18 unique targets");
        Check(guns.Length == 15 && guns.All(targets.Contains), "All 15 weapons targetable");
        foreach (var expected in new[] { new { type = 43, count = 10 }, new { type = 2, count = 2 }, new { type = 11, count = 2 }, new { type = 12, count = 1 } })
            Check(guns.Count(g => (int)g.WeaponType == expected.type) == expected.count, "Weapon composition " + expected.type);
        foreach (var gun in guns)
        {
            var original = donor.GetComponentsInChildren<WeaponHardPoint>(true).Single(p => p.name == gun.name);
            Check(gun.MinYaw == original.MinYaw && gun.MaxYaw == original.MaxYaw && gun.HardPointType == original.HardPointType, "Copied arc/type " + gun.name);
            Check((int)gun.WeaponType == ((int)original.WeaponType == 9 ? 43 : (int)original.WeaponType), "Only DBY replacement " + gun.name);
        }
        Check(targets.All(p => p.GetComponents<Collider>().Length > 0), "Target colliders");
        var data = new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>(DATA));
        Check(Number(data, "Hull") == 15000 && Number(data, "Shields") == 15000 && Number(data, "Speed") == 18, "Saved hull/shield and mapped speed");
        Check(Ints(data.FindProperty("abilities")).SequenceEqual(new[] { 35 }), "Own Intensify Firepower");
        var bays = data.FindProperty("hangarBays"); Check(bays.arraySize == 2, "Two approved fighter bays");
        for (int i = 0; i < 2; i++)
        {
            var bay = bays.GetArrayElementAtIndex(i);
            Check(bay.FindPropertyRelative("squadronType").intValue == 200 + i && bay.FindPropertyRelative("reserve").intValue == (i == 0 ? 3 : 6) && bay.FindPropertyRelative("maxActive").intValue == (i == 0 ? 1 : 2), "Source tech-2 garrison " + i);
        }
        var healthTypes = Enumerable.Range(0, data.FindProperty("hardPointHealth").arraySize).Select(i => data.FindProperty("hardPointHealth").GetArrayElementAtIndex(i).FindPropertyRelative("hardPointType").intValue).ToArray();
        Check(targets.All(p => healthTypes.Contains((int)p.HardPointType)), "Health for every target type");
        var loadout = data.FindProperty("weaponLoadout");
        foreach (var group in guns.GroupBy(g => (int)g.WeaponType))
            Check(Enumerable.Range(0, loadout.arraySize).Any(i => loadout.GetArrayElementAtIndex(i).FindPropertyRelative("weaponType").intValue == group.Key && loadout.GetArrayElementAtIndex(i).FindPropertyRelative("count").intValue == group.Count()), "Loadout matches actual guns " + group.Key);
        Check(Entry("Assets/Settings/Data/Ship/ShipsData.asset", "shipsData.keyValue", 211).FindPropertyRelative("m_AssetGUID").stringValue == AssetDatabase.AssetPathToGUID(DATA), "Ship registry");
        foreach (var path in new[] { VIEW, DATA })
        {
            var guid = AssetDatabase.AssetPathToGUID(path); var key = Path.GetFileNameWithoutExtension(path);
            Check(Entry("Assets/Settings/AssetMappingData.asset", "assetMappings.keyValue", key).FindPropertyRelative("m_AssetGUID").stringValue == guid, "Mapping " + key);
            Check(AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(guid).address == key, "Addressable " + key);
        }
        var icon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Ui/Icons/ShipIcon/ImperialVenatorIcon.png"); Check(icon != null, "Rendered icon");
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(icon, out string iconGuid, out long localId); string iconKey = iconGuid + ":" + localId;
        var unit = Entry("Assets/Settings/Data/Factions/Empire/EmpireFaction.asset", "ships.keyValue", 211);
        Check(unit.FindPropertyRelative("<Name>k__BackingField").stringValue == "Imperial Venator", "Empire roster");
        Check(unit.FindPropertyRelative("<Price>k__BackingField").intValue == 17200 && unit.FindPropertyRelative("<BuildTime>k__BackingField").intValue == 70 && unit.FindPropertyRelative("<UnitCapacity>k__BackingField").intValue == 26 && unit.FindPropertyRelative("<AvailableLevel>k__BackingField").intValue == 2, "Mapped production stats");
        Check(unit.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue == icon && unit.FindPropertyRelative("iconKey").stringValue == iconKey, "Faction icon");
        Check(Entry("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset", "shipIconWrapper.keyValue", 211).objectReferenceValue == icon, "HUD icon");
        var icons = new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/Settings/Data/Tooltip/TooltipIconData.asset")).FindProperty("icons");
        Check(Enumerable.Range(0, icons.arraySize).Any(i => icons.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue == iconKey && icons.GetArrayElementAtIndex(i).FindPropertyRelative("sprite").objectReferenceValue == icon), "Tooltip icon");
        foreach (string path in AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets/Settings/Data/Factions" }).Select(AssetDatabase.GUIDToAssetPath).Where(p => p.EndsWith("Faction.asset") && !p.EndsWith("EmpireFaction.asset")))
        {
            var rows = new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>(path)).FindProperty("ships.keyValue");
            Check(!Enumerable.Range(0, rows.arraySize).Any(i => rows.GetArrayElementAtIndex(i).FindPropertyRelative("key").intValue == 211), "Empire-only " + path);
        }
        var profiles = AssetDatabase.LoadAssetAtPath<WeaponsData>("Assets/Settings/Data/Models/Weapon/WeaponsData.asset");
        foreach (var gun in guns) Check(profiles.GetProfile(gun.WeaponType).ShotPrefab != null && !profiles.GetProfile(gun.WeaponType).Interceptable, "Projectile and ability category " + gun.name);
        var green = profiles.GetProfile((WeaponType)43).Color;
        Check(green.g > green.r && green.g > green.b && profiles.GetProfile((WeaponType)43).ShotsPerSalvo == 2, "Existing green dual turbolaser");
        var boost = AssetDatabase.LoadAssetAtPath<ShipAbilityCatalog>("Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset").Get((ShipAbilityId)35);
        var settings = (FullSalvoSettings)boost.Settings;
        Check(boost.Duration == 15 && boost.RecoveryDelay == 60 && settings.ProjectileFireDelayMultiplier == 1 && settings.OtherFireDelayMultiplier == .33f, "Intensify timing and modifiers");
        Check(boost.Icon != null, "Ability icon");
        var audioRows = new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/Settings/Data/Models/Audio/ShipSfxData.asset")).FindProperty("abilities");
        Check(Enumerable.Range(0, audioRows.arraySize).Count(i => audioRows.GetArrayElementAtIndex(i).FindPropertyRelative("abilityId").intValue == 35) == 1, "Dedicated ability audio");
        var body = (Transform)Config(root, "ShipMoveComponent").FindProperty("bodyTransform").objectReferenceValue;
        Check(body.localScale == Vector3.one && body.localRotation == Quaternion.identity && body.localPosition == Vector3.zero, "Banking body identity");
        foreach (var point in targets) Check(point.transform.IsChildOf(body), "Banked hardpoint " + point.name);
        var visual = body.Cast<Transform>().Single(t => t.name == "ImperialVenator");
        Check(root.GetComponentsInChildren<Transform>(true).Count(t => t.name.StartsWith("ImperialVenator_Heavy_TL_") && t.parent.name.StartsWith("TL_")) == 8 && root.GetComponentsInChildren<Transform>(true).Count(t => t.name.StartsWith("ImperialVenator_Medium_TL_") && t.parent.name.StartsWith("TL_")) == 2, "Ten assembled turrets");
        float mountError = 0;
        foreach (var mapping in JArray.Parse(File.ReadAllText("Temp/ImperialVenatorImport/GameplayMounts.json")))
        {
            var point = targets.Single(p => p.Id == (int)mapping["Id"]); string path = (string)mapping["bone"];
            Vector3 position;
            if (path.StartsWith("mean")) position = visual.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Pe_Venator_")).Select(t => t.position).Aggregate(Vector3.zero, (a, p) => a + p) / 10;
            else
            {
                var parts = path.Split('/'); var parent = parts.Length == 2 ? visual.GetComponentsInChildren<Transform>(true).Single(t => t.name == parts[0]) : visual;
                position = parent.GetComponentsInChildren<Transform>(true).Single(t => t.name.Equals(parts.Last(), StringComparison.OrdinalIgnoreCase) && t != parent).position;
            }
            mountError = Mathf.Max(mountError, Vector3.Distance(point.transform.position, position));
        }
        Check(mountError < .001f, "Authored muzzle and system positions");
        var solid = root.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled && r.sharedMaterials.All(m => m.shader.name == "EmpireAtWar/Ship Lit")).ToArray();
        var points = solid.SelectMany(r => r.GetComponents<MeshFilter>().Single().sharedMesh.vertices.Select(v => r.transform.TransformPoint(v))).ToArray();
        var box = root.GetComponents<BoxCollider>().Single(); var collisionBounds = new Bounds(box.center, box.size); collisionBounds.Expand(.002f); Check(points.All(p => collisionBounds.Contains(p)), "Collision encloses opaque hull");
        var rolled = points.SelectMany(p => new[] { p, Quaternion.Euler(0, 0, 15) * p, Quaternion.Euler(0, 0, -15) * p }).ToArray();
        Check(rolled.All(p => p.y >= Number(data, "HullBottom") - .001f && p.y <= Number(data, "HullTop") + .001f), "Banking envelope");
        var launch = (Transform)Config(root, "HangarComponent").FindProperty("launchPoint").objectReferenceValue;
        Check(launch.IsChildOf(body) && launch.position.y < points.Min(p => p.y) - 7.9f, "Launch outside hull");
        var selection = Config(root, "SelectionComponent"); var selectionCanvas = (Canvas)selection.FindProperty("selectedCanvas").objectReferenceValue;
        var selectionImage = (UnityEngine.UI.Image)selection.FindProperty("selectedImage").objectReferenceValue;
        Check(selectionCanvas.transform.localScale == Vector3.one && selectionImage.rectTransform.sizeDelta == new Vector2(144, 144) && selectionCanvas.transform.localPosition.y < Number(data, "HullBottom"), "Fitted selection ring without inherited scale");
        Check(Config(root, "HangarComponent").FindProperty("bayHardPoints").arraySize == 0 && Config(root, "HangarComponent").FindProperty("bayLaunchPoints").arraySize == 0 && Config(root, "HangarComponent").FindProperty("isDestroyable").boolValue, "Two bays share one destroyable hangar and launch point");
        Check(Objects(Config(root, "TeamColorView").FindProperty("meshRenderers")).Length == root.GetComponentsInChildren<MeshRenderer>(true).Length, "Explicit team renderer ownership");
        var fog = Objects(Config(root, "FogVisibilityComponent").FindProperty("renderers")); Check(root.GetComponentsInChildren<Renderer>(true).Where(r => r.enabled).All(fog.Contains), "Fog bindings");
        Check(Objects(Config(root, "Ship").FindProperty("explosionHullRenderers")).SequenceEqual(solid), "Explosion hull bindings");
        foreach (var name in new[] { "Engine_Exhaust", "Engine_Exhaust_Glow", "Engine_Glow.005" })
        {
            var engine = root.GetComponentsInChildren<MeshRenderer>(true).Single(r => r.name == name);
            Check(engine.enabled && engine.gameObject.activeSelf, "Visible source engine " + name);
            Check(engine.GetComponents<MeshFilter>().Single().sharedMesh.triangles.Length / 3 == (name == "Engine_Glow.005" ? 1400 : 540), "Complete engine triangles " + name);
            if (name == "Engine_Exhaust") continue;
            var material = engine.sharedMaterial;
            Check(material.shader.name == "Universal Render Pipeline/Unlit" && material.GetFloat("_Cull") == 0 && material.GetFloat("_ZWrite") == 0 && material.GetFloat("_DstBlend") == 1, "Two-sided additive engine " + name);
            Check(material.GetTexture("_BaseMap") != null && material.GetColor("_BaseColor").maxColorComponent >= 1, "Emissive source engine texture " + name);
        }
        var shield = (Renderer)Config(root, "Shield").FindProperty("shieldRenderer").objectReferenceValue;
        Check(AssetDatabase.GetAssetPath(shield.GetComponents<MeshFilter>().Single().sharedMesh).Contains("ImperialVenator"), "Own baked shield");
        var spawn = Entry("Assets/Settings/Data/Reinforcement/ReinforcementData.asset", "spawnShipWrapper.keyValue", 211).objectReferenceValue;
        var previewPath = AssetDatabase.GetAssetPath(spawn); Check(previewPath == "Assets/Prefabs/Ui/Reinforcement/ImperialVenatorReinforcementView.prefab", "Own placement");
        var preview = AssetDatabase.LoadAssetAtPath<GameObject>(previewPath); var spawnConfig = Config(preview, "UnitSpawnView");
        var hologram = (Material)spawnConfig.FindProperty("hologramMaterial").objectReferenceValue;
        Check(Objects(spawnConfig.FindProperty("meshRenderers")).Length == solid.Length && Objects(spawnConfig.FindProperty("meshRenderers")).Cast<Renderer>().All(r => r.sharedMaterials.All(m => m == hologram)), "Hologram bindings");
        Check(preview.GetComponents<BoxCollider>().Single().isTrigger && preview.GetComponents<Rigidbody>().Single().isKinematic, "Placement trigger");
        var wreckData = data.FindProperty("<Wreck>k__BackingField").objectReferenceValue;
        Check(AssetDatabase.GetAssetPath(wreckData) == "Assets/Settings/Data/Ship/Wreck/ImperialVenatorWreckData.asset", "Own wreck data");
        var wreckPath = AssetDatabase.GetAssetPath(new SerializedObject(wreckData).FindProperty("<Prefab>k__BackingField").objectReferenceValue);
        Check(wreckPath == "Assets/Prefabs/Models/Wrecks/ImperialVenatorWreckView.prefab", "Own wreck prefab");
        string[] paths = { VIEW, "Assets/Prefabs/Models/Ships/ImperialVenator.prefab", "Assets/Prefabs/Models/Ships/ImperialVenatorDeath.prefab", previewPath, wreckPath };
        foreach (var path in paths)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path); Check(prefab.transform.localScale == Vector3.one && prefab.transform.localRotation == Quaternion.identity, "Identity root " + path);
            foreach (var t in prefab.GetComponentsInChildren<Transform>(true)) Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) == 0, "Script " + t.name);
            foreach (var r in prefab.GetComponentsInChildren<Renderer>(true)) Check(r.sharedMaterials.All(m => m != null), "Materials " + r.name);
            foreach (var component in prefab.GetComponentsInChildren<MonoBehaviour>(true))
            {
                var iterator = new SerializedObject(component).GetIterator();
                while (iterator.Next(true)) if (iterator.propertyType == SerializedPropertyType.ObjectReference)
                    Check(iterator.objectReferenceValue != null || iterator.objectReferenceEntityIdValue.Equals(default(UnityEngine.EntityId)), "Reference " + component.name + "/" + iterator.propertyPath);
            }
        }
        Check(UnitHelperMeshStripper.FindHelpers(paths, UnitHelperMeshStripper.FindUnitPrefabPaths().Except(paths).ToArray()).All(h => h.BlockedBy != null), "No strippable helpers");
        Check(!AssetDatabase.GetDependencies(VIEW).Any(p => p.Contains("RepublicVenator") || p.EndsWith("VenatorShipData.asset") && p != DATA), "Independent Imperial art");
        var report = new { checks = _checks, targetable = targets.Length, weapons = guns.Length, hull = 15000, shields = 15000, speed = 18, hangarBays = 2, maximumMountError = mountError, automatedTestsRun = false, playModeRun = false };
        File.WriteAllText("Temp/ImperialVenatorImport/Verification.json", JsonConvert.SerializeObject(report, Formatting.Indented));
        return JsonConvert.SerializeObject(report);
    }
    static void Check(bool value, string message) { _checks++; if (!value) throw new InvalidOperationException(message); }
    static SerializedObject Config(GameObject root, string name) => new SerializedObject(root.GetComponentsInChildren<MonoBehaviour>(true).Single(m => m.GetType().Name == name));
    static UnityEngine.Object[] Objects(SerializedProperty p) => Enumerable.Range(0, p.arraySize).Select(i => p.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
    static int[] Ints(SerializedProperty p) => Enumerable.Range(0, p.arraySize).Select(i => p.GetArrayElementAtIndex(i).intValue).ToArray();
    static float Number(SerializedObject data, string name) => data.FindProperty("<" + name + ">k__BackingField").floatValue;
    static SerializedProperty Entry(string path, string field, object key)
    {
        var rows = new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>(path)).FindProperty(field);
        return Enumerable.Range(0, rows.arraySize).Select(i => rows.GetArrayElementAtIndex(i)).Single(r => key is int n ? r.FindPropertyRelative("key").intValue == n : r.FindPropertyRelative("key").stringValue == (string)key).FindPropertyRelative("value");
    }
}
