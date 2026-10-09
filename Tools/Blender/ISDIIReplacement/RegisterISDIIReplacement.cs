using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using EmpireAtWar.Services.ShipAbilities.Abilities;

public static class RegisterISDIIReplacement
{
    const string VIEW="Assets/Prefabs/Models/Ships/ISDIIShipView.prefab";
    const string DATA="Assets/Settings/Data/Ship/ISDIIShipData.asset";
    const int SHIP_ID=205;
    public static string Main()
    {
        var weapons=Load("Assets/Settings/Data/Models/Weapon/WeaponsData.asset");
        Profile(weapons,41,70,"ISD II Octuple Turbolaser",135,1,.75f,4.5f,350,133,2);
        Profile(weapons,42,71,"ISD II Quad Ion Cannon",225,1,.7f,6,300,55,4);
        Save(weapons);
        var audio=Load("Assets/Settings/Data/Models/Audio/ShipSfxData.asset");
        CloneRow(audio.FindProperty("weapons"),"weaponType",41,70);
        CloneRow(audio.FindProperty("weapons"),"weaponType",42,71);Save(audio);
        var faction=Load("Assets/Settings/Data/Factions/Empire/EmpireFaction.asset");
        var unit=Entry(faction.FindProperty("ships.keyValue"),SHIP_ID);
        unit.FindPropertyRelative("<Name>k__BackingField").stringValue="ISD II";
        unit.FindPropertyRelative("role").stringValue="Capital ship / Ship of the line";
        unit.FindPropertyRelative("description").stringValue="5,500 hull; 4,200 shields; speed 25. Eight octuple turbolasers, two quad ion turrets, six ion cannons, five hull turbolasers and three center turrets. Every weapon is targetable; shield generator, hangar, tractor beam are targetable. Power to Main Batteries and Tractor Beam. Existing TIE Interceptor, Brute and Punisher complement.";
        Save(faction);
        AssetDatabase.SaveAssets();
        return "ISD II retains ship 205 and all existing registrations; dedicated source-variant weapons 70/71 and audio added once. Existing economy/abilities preserved.";
    }
    static SerializedProperty Entry(SerializedProperty array,object key)
    {
        for(int i=0;i<array.arraySize;i++)
        {
            var row=array.GetArrayElementAtIndex(i);var item=row.FindPropertyRelative("key");
            if(key is int?item.intValue==(int)key:item.stringValue==(string)key)return row.FindPropertyRelative("value");
        }
        int index=array.arraySize;array.arraySize++;var added=array.GetArrayElementAtIndex(index);
        if(key is int)added.FindPropertyRelative("key").intValue=(int)key;else added.FindPropertyRelative("key").stringValue=(string)key;
        return added.FindPropertyRelative("value");
    }
    static SerializedProperty CloneRow(SerializedProperty array,string field,int source,int target)
    {
        for(int i=0;i<array.arraySize;i++)if(array.GetArrayElementAtIndex(i).FindPropertyRelative(field).intValue==target)return array.GetArrayElementAtIndex(i);
        int index=Enumerable.Range(0,array.arraySize).Single(i=>array.GetArrayElementAtIndex(i).FindPropertyRelative(field).intValue==source);
        array.InsertArrayElementAtIndex(index);var row=array.GetArrayElementAtIndex(index);row.FindPropertyRelative(field).intValue=target;return row;
    }
    static void Profile(SerializedObject weapons,int source,int type,string name,float damage,int shots,float interval,float reload,float range,float speed,int damageType)
    {
        var p=CloneRow(weapons.FindProperty("weapons"),"weaponType",source,type);
        p.FindPropertyRelative("displayName").stringValue=name;p.FindPropertyRelative("damageType").intValue=damageType;
        p.FindPropertyRelative("damage").floatValue=damage;p.FindPropertyRelative("shotsPerSalvo").intValue=shots;
        p.FindPropertyRelative("shotInterval").floatValue=interval;p.FindPropertyRelative("reload").floatValue=reload;
        p.FindPropertyRelative("range").floatValue=range;p.FindPropertyRelative("projectileSpeed").floatValue=speed;
    }
    static SerializedObject Load(string path)=>new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>(path));
    static void Save(SerializedObject data){data.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(data.targetObject);}
}
