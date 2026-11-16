using System;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Squadrons.Flight;
using EmpireAtWar.Editor.Balance;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.ViewComponents.Health;
using EmpireAtWar.ViewComponents.Squadrons;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class VaderIntegration
{
    private const string STEM = "DarthVaderTIEAdvanced";
    private const string PREFABS = "Assets/Prefabs/Models/Squadrons/";
    private const string DATA = "Assets/Settings/Data/Squadron/";
    private const string ICON = "Assets/Art/Textures/Ui/Icons/SquadronIcon/Empire/DarthVaderTIEAdvancedIcon.png";
    private const string PREVIEW = "Assets/Prefabs/Ui/Reinforcement/DarthVaderTIEAdvancedReinforcementView.prefab";

    public static object Apply()
    {
        var portrait = AssetDatabase.LoadAssetAtPath<Sprite>(ICON);
        if (portrait == null) throw new InvalidOperationException("Vader icon must be imported first.");
        BuildView(portrait);
        BuildPreview();
        string dataPath = DATA + STEM + "SquadronData.asset";
        if (AssetDatabase.LoadAssetAtPath<Object>(dataPath) == null)
            AssetDatabase.CopyAsset(DATA + "TIEFighterSquadronData.asset", dataPath);
        var data = Serialized(dataPath);
        data.targetObject.name=STEM+"SquadronData";
        Field(data,"ShipClass").intValue=9;
        foreach (var row in new[] { ("MemberHull",58f), ("MemberShields",0f), ("LeaderHull",3200f), ("LeaderShields",2400f),
            ("CruiseSpeed",58f), ("CombatSpeed",66f), ("Acceleration",66f), ("TurnRate",210f),
            ("ShieldRegenerateValue",5f), ("ShieldRegenerateDelay",1f), ("FormationSpacing",5f), ("NavigationRadius",22f) })
            Field(data,row.Item1).floatValue=row.Item2;
        Field(data,"ProtectLeaderUntilEscortsLost").boolValue=true;
        Field(data,"MemberCount").intValue=7;
        Field(data,"Abilities").arraySize=1;
        Field(data,"Abilities").GetArrayElementAtIndex(0).intValue=55;
        var loadout=data.FindProperty("weaponLoadout");loadout.arraySize=2;
        for(int i=0;i<2;i++) { var row=loadout.GetArrayElementAtIndex(i);row.FindPropertyRelative("weaponType").intValue=164+i;row.FindPropertyRelative("count").intValue=i==0?2:12; }
        Save(data);
        Register(portrait,dataPath);
        ConfigureAbility();
        AssetDatabase.SaveAssets();
        return new { unit=STEM, members=7, hull=3548, shields=2400, laserDps=526, speed="58/66" };
    }

    public static void ConfigureAbility()
    {
        var catalog=Serialized("Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset");
        var entry=Row(catalog.FindProperty("definitions.keyValue"),55).FindPropertyRelative("value");
        entry.FindPropertyRelative("icon").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Ui/Icons/ShipAbilities/FighterReinforcementsIcon.png");
        entry.FindPropertyRelative("displayName").stringValue="Replenish Wingmen";
        entry.FindPropertyRelative("description").stringValue="Replace destroyed TIE escorts while Vader survives, up to six escorts. Does not repair surviving fighters. 60 s cooldown.";
        entry.FindPropertyRelative("duration").floatValue=0f;entry.FindPropertyRelative("recoveryDelay").floatValue=60f;
        entry.FindPropertyRelative("canCancel").boolValue=false;entry.FindPropertyRelative("isToggle").boolValue=false;
        entry.FindPropertyRelative("requiresEnemyTarget").boolValue=false;entry.FindPropertyRelative("range").floatValue=0f;
        entry.FindPropertyRelative("aiUse").intValue=2;entry.FindPropertyRelative("autoPower").intValue=0;
        entry.FindPropertyRelative("settings").managedReferenceValue=new EmpireAtWar.Services.ShipAbilities.Abilities.ReplenishWingmenSettings();
        Save(catalog);AssetDatabase.SaveAssets();
    }

    private static void BuildView(Sprite portrait)
    {
        var root=PrefabUtility.LoadPrefabContents(PREFABS+"TIEFighterSquadronView.prefab");
        try
        {
            root.name=STEM+"SquadronView";
            var components=BalancePrefabBindings.Components(root).OfType<Component>().ToArray();
            var bindings=components.OfType<EntityBindings>().Single();
            var fighters=BalancePrefabBindings.References(bindings.SquadronHealth,"fighters").Cast<FighterView>().ToArray();
            var removed=new HashSet<Object>(fighters.Skip(7).SelectMany(f=>Objects(f.gameObject)));
            var leader=fighters[0];
            var oldModel=leader.Body.Cast<Transform>().Single(t=>t.name=="TIEFighter");
            removed.UnionWith(Objects(oldModel.gameObject));
            foreach(var component in components.Where(c=>c is MonoBehaviour && !removed.Contains(c)))
                RemoveReferences(component,removed);
            foreach(var fighter in fighters.Skip(7)) Object.DestroyImmediate(fighter.gameObject);
            Object.DestroyImmediate(oldModel.gameObject);
            fighters=fighters.Take(7).ToArray();
            leader.name="DarthVader";
            var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PREFABS+STEM+".prefab"),leader.Body);
            var modelComponents=BalancePrefabBindings.Components(model).OfType<Component>().ToArray();
            var muzzles=modelComponents.OfType<Transform>().Where(t=>t.name=="MuzzleB_00" || t.name=="MuzzleB_01").OrderBy(t=>t.name).ToArray();
            var guns=BalancePrefabBindings.Components(leader.gameObject).OfType<WeaponHardPoint>().OrderBy(g=>g.Id).ToArray();
            if(muzzles.Length!=2 || guns.Length!=2)throw new InvalidOperationException("Expected paired source laser attachments.");
            for(int i=0;i<2;i++) { guns[i].transform.position=muzzles[i].position;guns[i].transform.rotation=leader.Body.rotation; }
            var hull=modelComponents.OfType<MeshRenderer>().Where(r=>r.enabled).ToArray();
            Bounds bounds=hull[0].bounds;foreach(var r in hull.Skip(1))bounds.Encapsulate(r.bounds);
            var collider=(SphereCollider)new SerializedObject(leader).FindProperty("hitCollider").objectReferenceValue;
            collider.center=leader.transform.InverseTransformPoint(bounds.center);
            collider.radius=Mathf.Max(bounds.extents.x,bounds.extents.z);
            foreach(var trail in BalancePrefabBindings.References(leader,"engineTrails").Cast<TrailRenderer>())
                trail.transform.position=new Vector3(bounds.center.x,bounds.center.y,bounds.min.z);
            int gunId=0;
            for(int i=0;i<7;i++)
            {
                var slot=SquadronFormation.GetSlot(i,5f);fighters[i].transform.localPosition=new Vector3(slot.X,slot.Y,slot.Z);
                foreach(var gun in BalancePrefabBindings.Components(fighters[i].gameObject).OfType<WeaponHardPoint>())
                { var so=new SerializedObject(gun);Field(so,"Id").intValue=gunId++;Field(so,"WeaponType").intValue=i==0?164:165;Save(so); }
            }
            var all=BalancePrefabBindings.Components(root).OfType<Component>().ToArray();
            var fog=all.Single(c=>c.GetType().Name=="FogVisibilityComponent");
            SetList(fog,"renderers",all.OfType<Renderer>().Where(r=>r is MeshRenderer || r is TrailRenderer).Cast<Object>().ToArray());
            var icon=new SerializedObject(all.Single(c=>c.GetType().Name=="SquadronIconComponent"));
            ((UnityEngine.UI.Image)icon.FindProperty("iconImage").objectReferenceValue).sprite=portrait;
            var selection=new SerializedObject(all.Single(c=>c.GetType().Name=="SelectionComponent"));
            Vector2 centroid=Vector2.zero;
            for(int i=0;i<7;i++){var slot=SquadronFormation.GetSlot(i,5f);centroid+=new Vector2(slot.X,slot.Z)/7f;}
            float radius=0f;
            for(int i=0;i<7;i++) { var slot=SquadronFormation.GetSlot(i,5f);var hit=(SphereCollider)new SerializedObject(fighters[i]).FindProperty("hitCollider").objectReferenceValue;radius=Mathf.Max(radius,(new Vector2(slot.X,slot.Z)-centroid).magnitude+hit.radius); }
            float size=radius*2f*1.15f/(1038f/1254f);
            selection.FindProperty("ringSize").vector2Value=Vector2.one*size;Save(selection);
            var ring=(SpriteRenderer)selection.FindProperty("selectedRenderer").objectReferenceValue;
            ring.transform.localScale=new Vector3(size/ring.sprite.bounds.size.x,size/ring.sprite.bounds.size.y,1f);
            PrefabUtility.SaveAsPrefabAsset(root,PREFABS+root.name+".prefab");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void BuildPreview()
    {
        var root=PrefabUtility.LoadPrefabContents("Assets/Prefabs/Ui/Reinforcement/TIEFighterReinforcementView.prefab");
        try
        {
            root.name=STEM+"ReinforcementView";
            foreach(Transform child in root.transform.Cast<Transform>().ToArray())Object.DestroyImmediate(child.gameObject);
            var components=BalancePrefabBindings.Components(root).OfType<Component>().ToArray();
            var spawn=components.Single(c=>c.GetType().Name=="UnitSpawnView");
            var material=(Material)new SerializedObject(spawn).FindProperty("hologramMaterial").objectReferenceValue;
            var renderers=new List<MeshRenderer>();
            for(int i=0;i<7;i++)
            {
                var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PREFABS+(i==0?STEM:"TIEFighter")+".prefab"),root.transform);
                var slot=SquadronFormation.GetSlot(i,5f);model.transform.localPosition=new Vector3(slot.X,slot.Y,slot.Z);
                foreach(var r in BalancePrefabBindings.Components(model).OfType<MeshRenderer>()) { r.sharedMaterials=Enumerable.Repeat(material,r.sharedMaterials.Length).ToArray();renderers.Add(r); }
            }
            SetList(spawn,"meshRenderers",renderers.Cast<Object>().ToArray());
            Bounds bounds=renderers[0].bounds;foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);
            var collider=components.OfType<BoxCollider>().Single();collider.center=bounds.center;collider.size=bounds.size;
            PrefabUtility.SaveAsPrefabAsset(root,PREVIEW);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void Register(Sprite portrait,string dataPath)
    {
        var mapping=Serialized("Assets/Settings/AssetMappingData.asset");
        var reinforcement=Serialized("Assets/Settings/Data/Reinforcement/ReinforcementData.asset");
        var faction=Serialized("Assets/Settings/Data/Factions/Empire/EmpireFaction.asset");
        var ui=Serialized("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset");
        var tooltip=Serialized("Assets/Settings/Data/Tooltip/TooltipIconData.asset");
        var weapons=Serialized("Assets/Settings/Data/Models/Weapon/WeaponsData.asset");
        var audio=Serialized("Assets/Settings/Data/Models/Audio/ShipSfxData.asset");
        string view=PREFABS+STEM+"SquadronView.prefab";
        foreach(string path in new[]{view,dataPath})SetGuid(Row(mapping.FindProperty("assetMappings.keyValue"),System.IO.Path.GetFileNameWithoutExtension(path)).FindPropertyRelative("value"),path);
        SetGuid(Row(reinforcement.FindProperty("spawnSquadronWrapper.keyValue"),208).FindPropertyRelative("value"),PREVIEW);
        Row(ui.FindProperty("squadronIconWrapper.keyValue"),208).FindPropertyRelative("value").objectReferenceValue=portrait;
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(portrait,out string guid,out long localId);string iconKey=guid+":"+localId;
        Row(tooltip.FindProperty("icons"),iconKey).FindPropertyRelative("sprite").objectReferenceValue=portrait;
        var entry=Row(faction.FindProperty("squadrons.keyValue"),208).FindPropertyRelative("value");
        entry.FindPropertyRelative("displayName").stringValue="Darth Vader";
        entry.FindPropertyRelative("description").stringValue="Vader's shielded TIE Advanced leads six TIE escorts. Heavy lasers overpower Republic Jedi fighters; Replenish Wingmen replaces destroyed escorts every 60 seconds while Vader survives.";
        entry.FindPropertyRelative("role").stringValue="Hero / Fighter squadron";entry.FindPropertyRelative("isHero").boolValue=true;
        entry.FindPropertyRelative("matchups").objectReferenceValue=null;entry.FindPropertyRelative("iconKey").stringValue=iconKey;
        entry.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue=portrait;entry.FindPropertyRelative("<Name>k__BackingField").stringValue="Darth Vader - TIE Advanced";
        foreach(var item in new[]{("MaxCount",1),("AvailableLevel",4),("Price",5000),("BuildTime",35),("UnitCapacity",2)})entry.FindPropertyRelative("<"+item.Item1+">k__BackingField").intValue=item.Item2;
        var weaponData=(WeaponsData)weapons.targetObject;
        foreach(var pair in new[]{(164,163),(165,131)})
        {
            var source=weaponData.GetDefinition((WeaponType)pair.Item2);
            var row=Row(weapons.FindProperty("weapons"),pair.Item1,"weaponType");
            row.boxedValue=source;
            row.FindPropertyRelative("weaponType").intValue=pair.Item1;
            row.FindPropertyRelative("unit").stringValue=STEM;row.FindPropertyRelative("faction").intValue=3;
            row.FindPropertyRelative("displayName").stringValue=pair.Item1==164?"Vader Heavy Laser Cannon":"Vader Escort Laser Cannon";
            row.FindPropertyRelative("reason").stringValue=pair.Item1==164?"Two lasers deliver 400 raw DPS, above Republic hero fighters.":"Standard TIE laser tuning with explicit Vader squadron ownership.";
            if(pair.Item1==164)row.FindPropertyRelative("damageMultiplier").floatValue=18f;
        }
        var donor=((EmpireAtWar.Services.Audio.ShipSfxData)audio.targetObject).GetUnit(SquadronType.TIEFighter);
        Row(audio.FindProperty("squadrons.keyValue"),208).FindPropertyRelative("value").boxedValue=donor;
        foreach(var so in new[]{mapping,reinforcement,faction,ui,tooltip,weapons,audio})Save(so);
        var settings=UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
        foreach(var item in new[]{(view,"EmpireUnitViews"),(PREVIEW,"EmpireUnitViews"),(dataPath,"Data"),(ICON,"SharedIcons")})
        {var group=settings.FindGroup(item.Item2);var address=settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(item.Item1),group);address.address=item.Item1==ICON?ICON:System.IO.Path.GetFileNameWithoutExtension(item.Item1);EditorUtility.SetDirty(group);}
        EditorUtility.SetDirty(settings);
    }

    private static IEnumerable<Object> Objects(GameObject root)
    {yield return root;foreach(var c in BalancePrefabBindings.Components(root)){yield return c;if(c is Transform t && t!=root.transform)yield return t.gameObject;}}
    private static void RemoveReferences(Component component,HashSet<Object> removed)
    {
        var so=new SerializedObject(component);var p=so.GetIterator();bool enter=true;
        while(p.Next(enter)) {enter=true;if(p.isArray && p.propertyType!=SerializedPropertyType.String && p.arraySize>0 && p.GetArrayElementAtIndex(0).propertyType==SerializedPropertyType.ObjectReference)
        {var keep=Enumerable.Range(0,p.arraySize).Select(i=>p.GetArrayElementAtIndex(i).objectReferenceValue).Where(o=>!removed.Contains(o)).ToArray();p.arraySize=keep.Length;for(int i=0;i<keep.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=keep[i];enter=false;}}
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void SetList(Object target,string field,Object[] values)
    {var so=new SerializedObject(target);var p=so.FindProperty(field);p.arraySize=values.Length;for(int i=0;i<values.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=values[i];Save(so);}
    private static SerializedObject Serialized(string path)=>new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>(path));
    private static SerializedProperty Field(SerializedObject so,string name)=>so.FindProperty("<"+name+">k__BackingField");
    private static void Save(SerializedObject so){so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(so.targetObject);}
    private static SerializedProperty Row(SerializedProperty list,object key,string keyName="key")
    {for(int i=0;i<list.arraySize;i++){var row=list.GetArrayElementAtIndex(i);var field=row.FindPropertyRelative(keyName);if(key is int number?field.intValue==number:field.stringValue==(string)key)return row;}int index=list.arraySize++;var entry=list.GetArrayElementAtIndex(index);if(key is int id)entry.FindPropertyRelative(keyName).intValue=id;else entry.FindPropertyRelative(keyName).stringValue=(string)key;return entry;}
    private static void SetGuid(SerializedProperty property,string path)
    {property.FindPropertyRelative("m_AssetGUID").stringValue=AssetDatabase.AssetPathToGUID(path);property.FindPropertyRelative("m_SubObjectName").stringValue="";property.FindPropertyRelative("m_SubObjectType").stringValue="";property.FindPropertyRelative("m_SubObjectGUID").stringValue="";property.FindPropertyRelative("m_EditorAssetChanged").boolValue=false;}
}
