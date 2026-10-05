using System;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.ShipAbilities.Abilities;
using EmpireAtWar.Components.Combat;

public static class RegisterXWing
{
    const string GAMEPLAY="Assets/Prefabs/Models/Squadrons/XWingSquadronView.prefab";
    const string DATA="Assets/Settings/Data/Squadron/XWingSquadronData.asset";
    const string ICON="Assets/Art/Textures/Ui/Icons/SquadronIcon/XWingIcon.png";
    const int SQUADRON_ID=300;
    const int ABILITY_ID=18;
    const int WEAPON_ID=24;
    public static string Main()
    {
        var icon=AssetDatabase.LoadAssetAtPath<Sprite>(ICON);
        var silhouette=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Ui/Icons/SquadronIcon/XWingSilhouette.png");
        string key=IconKey(icon);
        const string MATCHUPS="Assets/Settings/Data/Tooltip/Matchups/XWingSquadronMatchups.asset";
        AssetDatabase.CopyAsset("Assets/Settings/Data/Tooltip/Matchups/AWingInterceptorSquadronMatchups.asset",MATCHUPS);
        var matchups=Load(MATCHUPS);matchups.targetObject.name="XWingSquadronMatchups";
        var strong=matchups.FindProperty("strongAgainst");strong.arraySize=2;
        SetMatchup(strong.GetArrayElementAtIndex(0),"Fighters","Assets/Art/Textures/Ui/Icons/SquadronIcon/TIEFighterIcon.png");
        SetMatchup(strong.GetArrayElementAtIndex(1),"Bombers","Assets/Art/Textures/Ui/Icons/SquadronIcon/TIEBomberIcon.png");
        SetMatchup(matchups.FindProperty("weakAgainst").GetArrayElementAtIndex(0),"Anti-fighter corvettes","Assets/Art/Textures/Ui/Icons/ShipIcon/CorellianCorvetteIcon.png");Save(matchups);
        var faction=Load("Assets/Settings/Data/Factions/Rebellion/RebellionFaction.asset");
        var entry=Append(faction.FindProperty("squadrons.keyValue"));entry.FindPropertyRelative("key").intValue=SQUADRON_ID;
        var value=entry.FindPropertyRelative("value");
        value.FindPropertyRelative("matchups").objectReferenceValue=matchups.targetObject;
        value.FindPropertyRelative("description").stringValue="Five Rebel multirole fighters, each with 60 hull, 20 shields, 3/s shield regeneration and four laser cannons. Intercept bombers, dogfight fighters, escort bombers and scout. Lock S-Foils closes the wings: speed x1.3, weapon delays x3 and shield regeneration x3. Toggle again to reopen the wings and return to combat configuration.";
        value.FindPropertyRelative("role").stringValue="Multirole fighter / Anti-bomber";
        value.FindPropertyRelative("iconKey").stringValue=key;
        value.FindPropertyRelative("<Name>k__BackingField").stringValue="T-65 X-Wing Squadron";
        value.FindPropertyRelative("<MaxCount>k__BackingField").intValue=10;
        value.FindPropertyRelative("<AvailableLevel>k__BackingField").intValue=1;
        value.FindPropertyRelative("<Price>k__BackingField").intValue=500;
        value.FindPropertyRelative("<BuildTime>k__BackingField").intValue=15;
        value.FindPropertyRelative("<UnitCapacity>k__BackingField").intValue=1;
        value.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue=icon;Save(faction);
        var ui=Load("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset");entry=Append(ui.FindProperty("squadronIconWrapper.keyValue"));entry.FindPropertyRelative("key").intValue=SQUADRON_ID;entry.FindPropertyRelative("value").objectReferenceValue=icon;Save(ui);
        var tooltips=Load("Assets/Settings/Data/Tooltip/TooltipIconData.asset");entry=Append(tooltips.FindProperty("icons"));entry.FindPropertyRelative("key").stringValue=key;entry.FindPropertyRelative("sprite").objectReferenceValue=icon;Save(tooltips);
        var reinforcement=Load("Assets/Settings/Data/Reinforcement/ReinforcementData.asset");
        var preview=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Ui/Reinforcement/XWingReinforcementView.prefab");
        entry=Append(reinforcement.FindProperty("spawnSquadronWrapper.keyValue"));entry.FindPropertyRelative("key").intValue=SQUADRON_ID;entry.FindPropertyRelative("value").objectReferenceValue=preview.GetComponents<MonoBehaviour>().Single(m=>m.GetType().Name=="UnitSpawnView");Save(reinforcement);
        var mapping=Load("Assets/Settings/AssetMappingData.asset");
        foreach(var asset in new[]{GAMEPLAY,DATA})
        {
            entry=Append(mapping.FindProperty("assetMappings.keyValue"));entry.FindPropertyRelative("key").stringValue=System.IO.Path.GetFileNameWithoutExtension(asset);entry.FindPropertyRelative("value.m_AssetGUID").stringValue=AssetDatabase.AssetPathToGUID(asset);
            var settings=AddressableAssetSettingsDefaultObject.Settings;
            var addressable=settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(asset),settings.FindGroup(asset==GAMEPLAY?"View":"Data"));addressable.address=System.IO.Path.GetFileNameWithoutExtension(asset);EditorUtility.SetDirty(settings);
        }
        Save(mapping);
        var root=PrefabUtility.LoadPrefabContents(GAMEPLAY);
        try
        {
            var component=root.GetComponentsInChildren<MonoBehaviour>(true).Single(m=>m.GetType().Name=="SquadronIconComponent");
            var so=new SerializedObject(component);var image=(UnityEngine.UI.Image)so.FindProperty("silhouetteImage").objectReferenceValue;image.sprite=silhouette;
            PrefabUtility.SaveAsPrefabAsset(root,GAMEPLAY);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        var catalog=Load("Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset");
        var definitions=catalog.FindProperty("definitions.keyValue");
        int index=Enumerable.Range(0,definitions.arraySize).Single(i=>definitions.GetArrayElementAtIndex(i).FindPropertyRelative("key").intValue==4);
        definitions.InsertArrayElementAtIndex(index);entry=definitions.GetArrayElementAtIndex(index);entry.FindPropertyRelative("key").intValue=ABILITY_ID;
        value=entry.FindPropertyRelative("value");
        var ability=new LockSFoilsSettings();
        typeof(LockSFoilsSettings).GetField("statModifier",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(ability,new CombatStatModifier(1,3,1.3f,3,1));
        value.FindPropertyRelative("settings").managedReferenceValue=ability;
        value.FindPropertyRelative("icon").objectReferenceValue=icon;
        value.FindPropertyRelative("displayName").stringValue="Lock S-Foils";
        value.FindPropertyRelative("description").stringValue="Toggle closed wings: speed x1.3, weapon delay x3 and shield regeneration x3 (9/s per fighter). Reopen to return to normal combat configuration. No cooldown.";
        value.FindPropertyRelative("duration").floatValue=0;value.FindPropertyRelative("recoveryDelay").floatValue=0;
        value.FindPropertyRelative("isToggle").boolValue=true;value.FindPropertyRelative("canCancel").boolValue=true;value.FindPropertyRelative("requiresEnemyTarget").boolValue=false;value.FindPropertyRelative("aiUse").intValue=1;Save(catalog);
        var weapons=Load("Assets/Settings/Data/Models/Weapon/WeaponsData.asset");
        var profiles=weapons.FindProperty("weapons");index=Enumerable.Range(0,profiles.arraySize).Single(i=>profiles.GetArrayElementAtIndex(i).FindPropertyRelative("weaponType").intValue==15);
        profiles.InsertArrayElementAtIndex(index);entry=profiles.GetArrayElementAtIndex(index);entry.FindPropertyRelative("weaponType").intValue=WEAPON_ID;entry.FindPropertyRelative("shotsPerSalvo").intValue=1;entry.FindPropertyRelative("shotInterval").floatValue=.1f;entry.FindPropertyRelative("reload").floatValue=1.5f;entry.FindPropertyRelative("range").floatValue=45;Save(weapons);
        var audio=Load("Assets/Settings/Data/Models/Audio/ShipSfxData.asset");
        var sounds=audio.FindProperty("weapons");index=Enumerable.Range(0,sounds.arraySize).Single(i=>sounds.GetArrayElementAtIndex(i).FindPropertyRelative("weaponType").intValue==15);
        sounds.InsertArrayElementAtIndex(index);sounds.GetArrayElementAtIndex(index).FindPropertyRelative("weaponType").intValue=WEAPON_ID;
        sounds=audio.FindProperty("abilities");index=Enumerable.Range(0,sounds.arraySize).Single(i=>sounds.GetArrayElementAtIndex(i).FindPropertyRelative("abilityId").intValue==4);
        sounds.InsertArrayElementAtIndex(index);sounds.GetArrayElementAtIndex(index).FindPropertyRelative("abilityId").intValue=ABILITY_ID;Save(audio);
        AssetDatabase.SaveAssets();
        return "Registered Rebellion XWing=300, S-Foils toggle=18, laser=24, 500/15 s/level 1/population 1, view/data Addressables, own placement, icons, audio and matchups.";
    }
    static SerializedObject Load(string path)=>new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path));
    static SerializedProperty Append(SerializedProperty array){array.InsertArrayElementAtIndex(array.arraySize);return array.GetArrayElementAtIndex(array.arraySize-1);}
    static void Save(SerializedObject so){so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(so.targetObject);}
    static string IconKey(Sprite sprite){AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite,out string guid,out long id);return guid+":"+id;}
    static void SetMatchup(SerializedProperty row,string label,string path){row.FindPropertyRelative("label").stringValue=label;row.FindPropertyRelative("iconKey").stringValue=IconKey(AssetDatabase.LoadAssetAtPath<Sprite>(path));}
}
