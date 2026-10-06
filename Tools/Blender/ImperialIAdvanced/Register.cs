using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using EmpireAtWar.Services.ShipAbilities.Abilities;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.AttackComponent;

public static class RegisterImperialAdvanced
{
    const BindingFlags FIELDS=BindingFlags.NonPublic|BindingFlags.Instance;
    public static string Main()
    {
        string[] names={"ImperialIAdvanced","TIEInterceptor","TIEBrute","TIEPunisher"};int[] ids={201,203,204,205},prices={22000,525,600,1500},times={440,18,7,50};
        string[] labels={"Imperial I Star Destroyer — Advanced Loadout","TIE Interceptor Squadron","TIE Brute Squadron","TIE Punisher Squadron"};
        string[] descriptions={"20,000 hull; 16,000 shields; speed 250. Targetable: six heavy two-burst turbolasers, two heavy two-burst turbo-ion cannons, two medium turbo-ion cannons, two shield generators, two engines, tractor beam and hangar. Additional weapons: three medium three-burst turbolasers, four light turbolasers and four laser cannons. Launches TIE Interceptors, TIE Brutes and TIE Punishers. Boost Engine Power doubles speed, quadruples weapon delays and pauses shield regeneration for 20 s. Tractor Beam slows a selected enemy to 40% speed.","Eight fast TIE Interceptors, each with 15 hull and four laser cannons. Advanced-loadout carrier fighter.","Six heavy TIE Brutes, each with 50 hull and two heavy laser cannons. Advanced-loadout carrier fighter.","Four TIE Punishers, each with 55 hull, 30 shields, one laser cannon, two proton-torpedo launchers and two missile launchers. Advanced-loadout carrier bomber."};
        var faction=Load("Assets/Settings/Data/Factions/Empire/EmpireFaction.asset");var ui=Load("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset");var tooltips=Load("Assets/Settings/Data/Tooltip/TooltipIconData.asset");var reinforcement=Load("Assets/Settings/Data/Reinforcement/ReinforcementData.asset");var mapping=Load("Assets/Settings/AssetMappingData.asset");
        for(int i=0;i<names.Length;i++)
        {
            bool ship=i==0;string name=names[i],type=ship?"Ship":"Squadron",view="Assets/Prefabs/Models/"+(ship?"Ships":"Squadrons")+"/"+name+type+"View.prefab",data="Assets/Settings/Data/"+type+"/"+name+type+"Data.asset";
            var icon=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Ui/Icons/"+(ship?"ShipIcon":"SquadronIcon")+"/"+name+"Icon.png");string key=IconKey(icon);
            string matchupsPath="Assets/Settings/Data/Tooltip/Matchups/"+name+type+"Matchups.asset";
            if(!File.Exists(matchupsPath))AssetDatabase.CopyAsset("Assets/Settings/Data/Tooltip/Matchups/"+(ship?"ImperatorMatchups":i==3?"TIEBomberSquadronMatchups":"TIEFighterSquadronMatchups")+".asset",matchupsPath);
            var matchups=AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(matchupsPath);matchups.name=name+type+"Matchups";EditorUtility.SetDirty(matchups);
            var value=Upsert(faction.FindProperty((ship?"ships":"squadrons")+".keyValue"),"key",ids[i]).FindPropertyRelative("value");
            value.FindPropertyRelative("matchups").objectReferenceValue=matchups;value.FindPropertyRelative("description").stringValue=descriptions[i];value.FindPropertyRelative("role").stringValue=ship?"Capital ship / Advanced carrier":i==3?"Bomber / Anti-ship":"Fighter / Interceptor";value.FindPropertyRelative("iconKey").stringValue=key;value.FindPropertyRelative("isHero").boolValue=false;
            value.FindPropertyRelative("<Name>k__BackingField").stringValue=labels[i];value.FindPropertyRelative("<MaxCount>k__BackingField").intValue=ship?3:10;value.FindPropertyRelative("<AvailableLevel>k__BackingField").intValue=ship?3:1;value.FindPropertyRelative("<Price>k__BackingField").intValue=prices[i];value.FindPropertyRelative("<BuildTime>k__BackingField").intValue=times[i];value.FindPropertyRelative("<UnitCapacity>k__BackingField").intValue=ship?8:1;value.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue=icon;
            Upsert(ui.FindProperty((ship?"shipIconWrapper":"squadronIconWrapper")+".keyValue"),"key",ids[i]).FindPropertyRelative("value").objectReferenceValue=icon;
            Upsert(tooltips.FindProperty("icons"),"key",key).FindPropertyRelative("sprite").objectReferenceValue=icon;
            var preview=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Ui/Reinforcement/"+name+"ReinforcementView.prefab");
            Upsert(reinforcement.FindProperty((ship?"spawnShipWrapper":"spawnSquadronWrapper")+".keyValue"),"key",ids[i]).FindPropertyRelative("value").objectReferenceValue=preview.GetComponents<MonoBehaviour>().Single(m=>m.GetType().Name=="UnitSpawnView");
            foreach(string path in new[]{view,data})
            {
                string assetName=Path.GetFileNameWithoutExtension(path),guid=AssetDatabase.AssetPathToGUID(path);
                Upsert(mapping.FindProperty("assetMappings.keyValue"),"key",assetName).FindPropertyRelative("value.m_AssetGUID").stringValue=guid;
                var settings=AddressableAssetSettingsDefaultObject.Settings;var addressable=settings.CreateOrMoveEntry(guid,settings.FindGroup(path==view?"View":"Data"));addressable.address=assetName;EditorUtility.SetDirty(settings);
            }
            if(ship){var ships=Load("Assets/Settings/Data/Ship/ShipsData.asset");Upsert(ships.FindProperty("shipsData.keyValue"),"key",ids[i]).FindPropertyRelative("value.m_AssetGUID").stringValue=AssetDatabase.AssetPathToGUID(data);Save(ships);}
            else
            {
                var root=PrefabUtility.LoadPrefabContents(view);
                try{var c=root.GetComponentsInChildren<MonoBehaviour>(true).Single(m=>m.GetType().Name=="SquadronIconComponent");var so=new SerializedObject(c);((UnityEngine.UI.Image)so.FindProperty("silhouetteImage").objectReferenceValue).sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Ui/Icons/SquadronIcon/"+name+"Silhouette.png");PrefabUtility.SaveAsPrefabAsset(root,view);}
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
        }
        foreach(var so in new[]{faction,ui,tooltips,reinforcement,mapping})Save(so);
        var weapons=Load("Assets/Settings/Data/Models/Weapon/WeaponsData.asset");var profiles=weapons.FindProperty("weapons");
        foreach(var config in new[]{new[]{31,11,2},new[]{32,10,1},new[]{33,1,1}})
        {
            var p=Clone(profiles,"weaponType",config[0],config[1]);p.FindPropertyRelative("displayName").stringValue=config[0]==31?"Heavy Turbo-Ion Cannon":config[0]==32?"Medium Turbo-Ion Cannon":"Light Turbolaser";p.FindPropertyRelative("shotsPerSalvo").intValue=config[2];
            if(config[0]==33)p.FindPropertyRelative("damage").floatValue=10;
        }
        Save(weapons);
        var catalog=Load("Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset");var definitions=catalog.FindProperty("definitions.keyValue");
        var boost=new BoostEnginePowerSettings();typeof(BoostEnginePowerSettings).GetField("statModifier",FIELDS).SetValue(boost,new CombatStatModifier(1,4,2,0,1));
        var def=Clone(definitions,"key",21,4).FindPropertyRelative("value");Configure(def,"Boost Engine Power","Double speed, quadruple weapon delays and pause shield regeneration for 20 s; 50 s recovery.",boost,20,50,false,0);
        var tractor=new TractorBeamSettings();var weaponData=(WeaponsData)weapons.targetObject;var beam=JsonUtility.FromJson<WeaponProfile>(JsonUtility.ToJson(weaponData.GetProfile(WeaponType.LaserBeam)));typeof(WeaponProfile).GetField("color",FIELDS).SetValue(beam,new Color(.2f,.7f,1,1));typeof(WeaponProfile).GetField("size",FIELDS).SetValue(beam,new Vector3(.4f,.4f,1));typeof(TractorBeamSettings).GetField("beam",FIELDS).SetValue(tractor,beam);
        def=Clone(definitions,"key",22,10).FindPropertyRelative("value");Configure(def,"Tractor Beam","Restrict a targeted enemy to 40% speed for up to 20 s within 150 units. Ends when the beam hardpoint, caster or target is destroyed, the caster is ion-disabled, or the target leaves range. 25 s recovery.",tractor,20,25,true,150);def.FindPropertyRelative("canCancel").boolValue=true;Save(catalog);
        var audio=Load("Assets/Settings/Data/Models/Audio/ShipSfxData.asset");foreach(var pair in new[]{new[]{31,11},new[]{32,10},new[]{33,1}})Clone(audio.FindProperty("weapons"),"weaponType",pair[0],pair[1]);foreach(var pair in new[]{new[]{21,4},new[]{22,10}})Clone(audio.FindProperty("abilities"),"abilityId",pair[0],pair[1]);Save(audio);
        AssetDatabase.SaveAssets();return "Empire ship 201, fighter squadrons 203–205, abilities 21/22 and weapons 31–33 registered in faction, data/view Addressables, asset maps, UI, tooltips, placement and audio.";
    }
    static void Configure(SerializedProperty def,string name,string description,object settings,float duration,float recovery,bool target,float range){def.FindPropertyRelative("displayName").stringValue=name;def.FindPropertyRelative("description").stringValue=description;def.FindPropertyRelative("settings").managedReferenceValue=settings;def.FindPropertyRelative("duration").floatValue=duration;def.FindPropertyRelative("recoveryDelay").floatValue=recovery;def.FindPropertyRelative("requiresEnemyTarget").boolValue=target;def.FindPropertyRelative("range").floatValue=range;def.FindPropertyRelative("isToggle").boolValue=false;def.FindPropertyRelative("aiUse").intValue=target?2:1;}
    static SerializedObject Load(string path)=>new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path));
    static void Save(SerializedObject so){so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(so.targetObject);}
    static string IconKey(Sprite sprite){AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite,out string guid,out long id);return guid+":"+id;}
    static int Index(SerializedProperty array,string field,object key)=>Enumerable.Range(0,array.arraySize).Where(i=>key is int?array.GetArrayElementAtIndex(i).FindPropertyRelative(field).intValue==(int)key:array.GetArrayElementAtIndex(i).FindPropertyRelative(field).stringValue==(string)key).DefaultIfEmpty(-1).First();
    static SerializedProperty Upsert(SerializedProperty array,string field,object key){int i=Index(array,field,key);if(i<0){i=array.arraySize;array.InsertArrayElementAtIndex(i);}var p=array.GetArrayElementAtIndex(i);if(key is int)p.FindPropertyRelative(field).intValue=(int)key;else p.FindPropertyRelative(field).stringValue=(string)key;return p;}
    static SerializedProperty Clone(SerializedProperty array,string field,int key,int source){int i=Index(array,field,key);if(i>=0)return array.GetArrayElementAtIndex(i);i=Index(array,field,source);array.InsertArrayElementAtIndex(i);var p=array.GetArrayElementAtIndex(i);p.FindPropertyRelative(field).intValue=key;return p;}
}
