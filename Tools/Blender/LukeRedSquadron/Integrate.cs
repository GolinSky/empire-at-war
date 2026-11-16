using System;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Squadrons.Flight;
using EmpireAtWar.Editor.Balance;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Audio;
using EmpireAtWar.ViewComponents.Health;
using EmpireAtWar.ViewComponents.Squadrons;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class LukeIntegration
{
    private const string STEM = "LukeRedSquadron";
    private const string PREFABS = "Assets/Prefabs/Models/Squadrons/";
    private const string DATA = "Assets/Settings/Data/Squadron/";
    private const string ICON = "Assets/Art/Textures/Ui/Icons/SquadronIcon/Rebellion/LukeRedSquadronIcon.png";
    private const string PREVIEW = "Assets/Prefabs/Ui/Reinforcement/LukeRedSquadronReinforcementView.prefab";
    private const int UNIT_ID = (int)SquadronType.LukeRedSquadron;

    public static object Apply()
    {
        var portrait = AssetDatabase.LoadAssetAtPath<Sprite>(ICON);
        if (portrait == null) throw new InvalidOperationException("Import the AOTR model and Luke icon first.");
        BuildView(portrait);
        BuildPreview();
        string path = DATA + STEM + "SquadronData.asset";
        if (AssetDatabase.LoadAssetAtPath<Object>(path) == null)
            AssetDatabase.CopyAsset(DATA + "XWingSquadronData.asset", path);
        var data = Serialized(path);
        data.targetObject.name = STEM + "SquadronData";
        Field(data, "ShipClass").intValue = 9;
        foreach (var row in new[] { ("MemberHull",120f), ("MemberShields",60f), ("LeaderHull",3600f), ("LeaderShields",2600f),
            ("CruiseSpeed",68f), ("CombatSpeed",78f), ("Acceleration",78f), ("TurnRate",210f),
            ("ShieldRegenerateValue",1f), ("ShieldRegenerateDelay",1f), ("FormationSpacing",5f), ("NavigationRadius",22f) })
            Field(data, row.Item1).floatValue = row.Item2;
        Field(data, "ProtectLeaderUntilEscortsLost").boolValue = true;
        Field(data, "MemberCount").intValue = 6;
        Field(data, "Abilities").arraySize = 1;
        Field(data, "Abilities").GetArrayElementAtIndex(0).intValue = 56;
        var loadout = data.FindProperty("weaponLoadout");
        loadout.arraySize = 2;
        for (int i = 0; i < 2; i++)
        {
            var row = loadout.GetArrayElementAtIndex(i);
            row.FindPropertyRelative("weaponType").intValue = i == 0 ? (int)WeaponType.LukeSkywalkerLaser : (int)WeaponType.RedSquadronEscortLaser;
            row.FindPropertyRelative("count").intValue = i == 0 ? 4 : 20;
        }
        Save(data);
        Register(portrait, path);
        AssetDatabase.SaveAssets();
        return new { unit = STEM, members = 6, hull = 4200, shields = 2900, laserDps = 650 };
    }

    private static void BuildView(Sprite portrait)
    {
        var root = PrefabUtility.LoadPrefabContents(PREFABS + "XWingSquadronView.prefab");
        try
        {
            root.name = STEM + "SquadronView";
            var bindings = BalancePrefabBindings.Components(root).OfType<EntityBindings>().Single();
            var fighters = BalancePrefabBindings.References(bindings.SquadronHealth, "fighters").Cast<FighterView>().ToList();
            var extra = Object.Instantiate(fighters[4].gameObject, fighters[4].transform.parent);
            fighters.Add(BalancePrefabBindings.Components(extra).OfType<FighterView>().Single());
            Object.DestroyImmediate(bindings.SFoils);
            var bindingData = new SerializedObject(bindings);
            Field(bindingData, "SFoils").objectReferenceValue = null;
            Save(bindingData);
            string[] names = { "Luke Skywalker", "Wedge Antilles", "Biggs Darklighter", "Red Leader", "Hobbie", "Porkins" };
            for (int i = 0; i < fighters.Count; i++)
            {
                var fighter = fighters[i];
                fighter.name = names[i];
                var components = BalancePrefabBindings.Components(fighter.gameObject).OfType<Component>().ToArray();
                var guns = components.OfType<WeaponHardPoint>().Where(g => g.WeaponType == WeaponType.XWingLaser).OrderBy(g => g.name).ToArray();
                if (guns.Length != 4) throw new InvalidOperationException("X-Wing donor must have four laser mounts.");
                var trails = BalancePrefabBindings.References(fighter, "engineTrails").Cast<TrailRenderer>().ToArray();
                foreach (var gun in guns) gun.transform.SetParent(fighter.Body, true);
                foreach (var trail in trails) trail.transform.SetParent(fighter.Body, true);
                foreach (var torpedo in components.OfType<WeaponHardPoint>().Where(g => g.WeaponType != WeaponType.XWingLaser))
                    Object.DestroyImmediate(torpedo.gameObject);
                Object.DestroyImmediate(fighter.Body.Cast<Transform>().Single(t => t.name == "XWing").gameObject);
                var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PREFABS + STEM + ".prefab"), fighter.Body);
                var modelComponents = BalancePrefabBindings.Components(model).OfType<Component>().ToArray();
                foreach (var transform in modelComponents.OfType<Transform>()) transform.gameObject.layer = fighter.Body.gameObject.layer;
                for (int j = 0; j < guns.Length; j++)
                {
                    var muzzle = modelComponents.OfType<Transform>().Single(t => t.name == "MuzzleA_0" + j);
                    guns[j].transform.SetPositionAndRotation(muzzle.position, fighter.Body.rotation);
                    var gunData = new SerializedObject(guns[j]);
                    Field(gunData, "Id").intValue = i * 4 + j;
                    Field(gunData, "WeaponType").intValue = i == 0 ? (int)WeaponType.LukeSkywalkerLaser : (int)WeaponType.RedSquadronEscortLaser;
                    Save(gunData);
                }
                var fighterData = new SerializedObject(fighter);
                fighterData.FindProperty("id").intValue = i;
                fighterData.FindProperty("gun").objectReferenceValue = guns[0];
                var additional = fighterData.FindProperty("additionalGuns"); additional.arraySize = 3;
                for (int j = 1; j < 4; j++) additional.GetArrayElementAtIndex(j - 1).objectReferenceValue = guns[j];
                Save(fighterData);
                var hull = modelComponents.OfType<MeshRenderer>().Where(r => r.enabled).ToArray();
                Bounds bounds = hull[0].bounds; foreach (var r in hull.Skip(1)) bounds.Encapsulate(r.bounds);
                var collider = (SphereCollider)fighterData.FindProperty("hitCollider").objectReferenceValue;
                collider.center = fighter.transform.InverseTransformPoint(bounds.center);
                collider.radius = Mathf.Max(bounds.extents.x, bounds.extents.z);
                // The four source engine planes provide the exhaust locations without synthetic offsets.
                var engines = modelComponents.OfType<MeshRenderer>().Where(r => new[] { "Plane01", "Plane02", "Plane03", "Plane04" }.Contains(r.name)).OrderBy(r => r.name).ToArray();
                if (engines.Length != 4) throw new InvalidOperationException("AOTR X-Wing must retain four engine planes.");
                for (int j = 0; j < trails.Length; j++) trails[j].transform.position = engines[j].bounds.center;
                var slot = SquadronFormation.GetSlot(i, 5f);
                fighter.transform.localPosition = new Vector3(slot.X, slot.Y, slot.Z);
            }
            SetList(bindings.SquadronHealth, "fighters", fighters.Cast<Object>().ToArray());
            SetList(bindings.SquadronFlight, "fighters", fighters.Cast<Object>().ToArray());
            var all = BalancePrefabBindings.Components(root).OfType<Component>().ToArray();
            var gunsAll = fighters.SelectMany(f => BalancePrefabBindings.Components(f.gameObject).OfType<WeaponHardPoint>()).ToArray();
            SetList(bindings.Weapons.Single(), "hardPoints", gunsAll.Cast<Object>().ToArray());
            SetList(bindings, "<Injectables>k__BackingField", fighters.Cast<Object>().Concat(gunsAll).ToArray());
            SetList(bindings.FogVisibility.Single(), "renderers", all.OfType<Renderer>().Where(r => r is MeshRenderer || r is TrailRenderer).Cast<Object>().ToArray());
            var icon = new SerializedObject(bindings.SquadronIcon);
            ((UnityEngine.UI.Image)icon.FindProperty("iconImage").objectReferenceValue).sprite = portrait;
            var selection = new SerializedObject(bindings.Selection.Single());
            Vector2 centroid = Vector2.zero;
            for (int i = 0; i < 6; i++) { var s = SquadronFormation.GetSlot(i, 5f); centroid += new Vector2(s.X, s.Z) / 6f; }
            float radius = 0f;
            for (int i = 0; i < 6; i++)
            {
                var s = SquadronFormation.GetSlot(i, 5f);
                var hit = (SphereCollider)new SerializedObject(fighters[i]).FindProperty("hitCollider").objectReferenceValue;
                radius = Mathf.Max(radius, (new Vector2(s.X, s.Z) - centroid).magnitude + hit.radius);
            }
            float size = radius * 2f * 1.15f / (1038f / 1254f);
            selection.FindProperty("ringSize").vector2Value = Vector2.one * size; Save(selection);
            var ring = (SpriteRenderer)selection.FindProperty("selectedRenderer").objectReferenceValue;
            ring.transform.localScale = new Vector3(size / ring.sprite.bounds.size.x, size / ring.sprite.bounds.size.y, 1f);
            PrefabUtility.SaveAsPrefabAsset(root, PREFABS + root.name + ".prefab");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void BuildPreview()
    {
        var root = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Ui/Reinforcement/XWingReinforcementView.prefab");
        try
        {
            root.name = STEM + "ReinforcementView";
            foreach (Transform child in root.transform.Cast<Transform>().ToArray()) Object.DestroyImmediate(child.gameObject);
            var components = BalancePrefabBindings.Components(root).OfType<Component>().ToArray();
            var spawn = components.Single(c => c.GetType().Name == "UnitSpawnView");
            var material = (Material)new SerializedObject(spawn).FindProperty("hologramMaterial").objectReferenceValue;
            var renderers = new List<MeshRenderer>();
            for (int i = 0; i < 6; i++)
            {
                var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PREFABS + STEM + ".prefab"), root.transform);
                var slot = SquadronFormation.GetSlot(i, 5f); model.transform.localPosition = new Vector3(slot.X, slot.Y, slot.Z);
                foreach (var r in BalancePrefabBindings.Components(model).OfType<MeshRenderer>())
                { r.sharedMaterials = Enumerable.Repeat(material, r.sharedMaterials.Length).ToArray(); renderers.Add(r); }
            }
            SetList(spawn, "meshRenderers", renderers.Cast<Object>().ToArray());
            Bounds bounds = renderers[0].bounds; foreach (var r in renderers.Skip(1)) bounds.Encapsulate(r.bounds);
            var collider = components.OfType<BoxCollider>().Single(); collider.center = bounds.center; collider.size = bounds.size;
            PrefabUtility.SaveAsPrefabAsset(root, PREVIEW);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void Register(Sprite portrait, string dataPath)
    {
        var mapping = Serialized("Assets/Settings/AssetMappingData.asset");
        var reinforcement = Serialized("Assets/Settings/Data/Reinforcement/ReinforcementData.asset");
        var faction = Serialized("Assets/Settings/Data/Factions/Rebellion/RebellionFaction.asset");
        var ui = Serialized("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset");
        var tooltip = Serialized("Assets/Settings/Data/Tooltip/TooltipIconData.asset");
        var weapons = Serialized("Assets/Settings/Data/Models/Weapon/WeaponsData.asset");
        var audio = Serialized("Assets/Settings/Data/Models/Audio/ShipSfxData.asset");
        string view = PREFABS + STEM + "SquadronView.prefab";
        foreach (string path in new[] { view, dataPath }) SetGuid(Row(mapping.FindProperty("assetMappings.keyValue"), System.IO.Path.GetFileNameWithoutExtension(path)).FindPropertyRelative("value"), path);
        SetGuid(Row(reinforcement.FindProperty("spawnSquadronWrapper.keyValue"), UNIT_ID).FindPropertyRelative("value"), PREVIEW);
        Row(ui.FindProperty("squadronIconWrapper.keyValue"), UNIT_ID).FindPropertyRelative("value").objectReferenceValue = portrait;
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(portrait, out string guid, out long localId); string iconKey = guid + ":" + localId;
        Row(tooltip.FindProperty("icons"), iconKey).FindPropertyRelative("sprite").objectReferenceValue = portrait;
        var entry = Row(faction.FindProperty("squadrons.keyValue"), UNIT_ID).FindPropertyRelative("value");
        entry.FindPropertyRelative("displayName").stringValue = "Luke Skywalker";
        entry.FindPropertyRelative("description").stringValue = "Luke leads five elite Red Squadron X-wings. Superior lasers dominate squadron duels. Lucky Shot launches a precision proton strike for 4,000 base hull damage through shields against capital ships and structures; 60 s cooldown.";
        entry.FindPropertyRelative("role").stringValue = "Hero / Elite fighter squadron";
        entry.FindPropertyRelative("isHero").boolValue = true;
        entry.FindPropertyRelative("matchups").objectReferenceValue = null;
        entry.FindPropertyRelative("iconKey").stringValue = iconKey;
        entry.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue = portrait;
        entry.FindPropertyRelative("<Name>k__BackingField").stringValue = "Luke Skywalker - Red Squadron";
        foreach (var item in new[] { ("MaxCount",1), ("AvailableLevel",4), ("Price",6000), ("BuildTime",40), ("UnitCapacity",3) })
            entry.FindPropertyRelative("<" + item.Item1 + ">k__BackingField").intValue = item.Item2;
        var weaponData = (WeaponsData)weapons.targetObject;
        foreach (var item in new[] { (WeaponType.LukeSkywalkerLaser,112.5f), (WeaponType.RedSquadronEscortLaser,10f) })
        {
            var donorType = item.Item1 == WeaponType.LukeSkywalkerLaser ? WeaponType.DarthVaderLaser : WeaponType.XWingLaser;
            var source = weaponData.GetDefinition(donorType);
            float donorDps = weaponData.GetProfile(donorType).DamagePerSecond;
            var row = Row(weapons.FindProperty("weapons"), (int)item.Item1, "weaponType");
            row.boxedValue = JsonUtility.FromJson<WeaponDefinition>(JsonUtility.ToJson(source));
            row.FindPropertyRelative("weaponType").intValue = (int)item.Item1;
            row.FindPropertyRelative("unit").stringValue = STEM;
            row.FindPropertyRelative("faction").intValue = (int)FactionType.Rebellion;
            row.FindPropertyRelative("overrideColor").boolValue = false;
            row.FindPropertyRelative("displayName").stringValue = item.Item1 == WeaponType.LukeSkywalkerLaser ? "Luke's Precision Laser Cannon" : "Red Squadron Laser Cannon";
            row.FindPropertyRelative("damageMultiplier").floatValue = source.DamageMultiplier * item.Item2 / donorDps;
            row.FindPropertyRelative("reason").stringValue = item.Item1 == WeaponType.LukeSkywalkerLaser
                ? "Four source-mounted lasers deliver 450 raw DPS; five escorts add 200 for 650 total."
                : "Twenty escort lasers deliver 200 raw DPS; Luke's four lasers add 450 for 650 total.";
        }
        var donor = ((ShipSfxData)audio.targetObject).GetUnit(SquadronType.XWing);
        Row(audio.FindProperty("squadrons.keyValue"), UNIT_ID).FindPropertyRelative("value").boxedValue = donor;
        foreach (var so in new[] { mapping, reinforcement, faction, ui, tooltip, weapons, audio }) Save(so);
        var settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
        foreach (var item in new[] { (view,"RebellionUnitViews"), (PREVIEW,"RebellionUnitViews"), (dataPath,"Data"), (ICON,"SharedIcons") })
        {
            var group = settings.FindGroup(item.Item2);
            var address = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(item.Item1), group);
            address.address = item.Item1 == ICON ? ICON : System.IO.Path.GetFileNameWithoutExtension(item.Item1);
            EditorUtility.SetDirty(group);
        }
        EditorUtility.SetDirty(settings);
    }

    private static void SetList(Object target, string field, Object[] values)
    {
        var so = new SerializedObject(target); var p = so.FindProperty(field); p.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i]; Save(so);
    }
    private static SerializedObject Serialized(string path) => new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>(path));
    private static SerializedProperty Field(SerializedObject so, string name) => so.FindProperty("<" + name + ">k__BackingField");
    private static void Save(SerializedObject so) { so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(so.targetObject); }
    private static SerializedProperty Row(SerializedProperty list, object key, string keyName = "key")
    {
        for (int i = 0; i < list.arraySize; i++) { var row = list.GetArrayElementAtIndex(i); var field = row.FindPropertyRelative(keyName); if (key is int number ? field.intValue == number : field.stringValue == (string)key) return row; }
        int index = list.arraySize++; var entry = list.GetArrayElementAtIndex(index);
        if (key is int id) entry.FindPropertyRelative(keyName).intValue = id; else entry.FindPropertyRelative(keyName).stringValue = (string)key; return entry;
    }
    private static void SetGuid(SerializedProperty property, string path)
    {
        property.FindPropertyRelative("m_AssetGUID").stringValue = AssetDatabase.AssetPathToGUID(path);
        property.FindPropertyRelative("m_SubObjectName").stringValue = ""; property.FindPropertyRelative("m_SubObjectType").stringValue = "";
        property.FindPropertyRelative("m_SubObjectGUID").stringValue = ""; property.FindPropertyRelative("m_EditorAssetChanged").boolValue = false;
    }
}
