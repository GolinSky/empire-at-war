using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.ViewComponents.Health;

public static class FixTitanIntegration
{
    public static string Main()
    {
        var weapons=AssetDatabase.LoadAssetAtPath<WeaponsData>("Assets/Settings/Data/Models/Weapon/WeaponsData.asset");
        var settings=new SerializedObject(weapons);var definitions=settings.FindProperty("weapons");
        var row=Enumerable.Range(0,definitions.arraySize).Select(i=>definitions.GetArrayElementAtIndex(i)).SingleOrDefault(p=>p.FindPropertyRelative("weaponType").intValue==88);
        if(row==null)
        {
            int donor=Enumerable.Range(0,definitions.arraySize).Single(i=>definitions.GetArrayElementAtIndex(i).FindPropertyRelative("weaponType").intValue==20);
            definitions.InsertArrayElementAtIndex(donor);row=definitions.GetArrayElementAtIndex(donor);
        }
        row.FindPropertyRelative("weaponType").intValue=88;
        row.FindPropertyRelative("displayName").stringValue="Titan Heavy Quad Turbolaser";
        row.FindPropertyRelative("unit").stringValue="Titan";
        row.FindPropertyRelative("reason").stringValue="Titan main battery; existing heavy long-range quad tuning retained.";
        settings.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(weapons);

        const string VIEW="Assets/Prefabs/Models/Ships/Empire/TitanShipView.prefab";
        var root=PrefabUtility.LoadPrefabContents(VIEW);
        try
        {
            var points=root.GetComponentsInChildren<HardPoint>(true);
            foreach(var point in points.OfType<WeaponHardPoint>().Where(p=>(int)p.WeaponType==20))
            {
                var so=new SerializedObject(point);so.FindProperty("<WeaponType>k__BackingField").intValue=88;so.ApplyModifiedPropertiesWithoutUndo();
            }
            Assign(Component(root,"TeamColorView"),"meshRenderers",root.GetComponentsInChildren<MeshRenderer>(true));
            Assign(Component(root,"EntityBindings"),"<Injectables>k__BackingField",points.Cast<UnityEngine.Object>().Concat(new[]{Component(root,"TeamColorView")}).ToArray());
            PrefabUtility.SaveAsPrefabAsset(root,VIEW);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }

        var data=AssetDatabase.LoadAssetAtPath<ShipData>("Assets/Settings/Data/Ship/Empire/TitanShipData.asset");
        var serialized=new SerializedObject(data);var loadout=serialized.FindProperty("weaponLoadout");
        for(int i=0;i<loadout.arraySize;i++)
        {
            var type=loadout.GetArrayElementAtIndex(i).FindPropertyRelative("weaponType");if(type.intValue==20)type.intValue=88;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        serialized.FindProperty("<Range>k__BackingField").floatValue=data.WeaponLoadout.Max(entry=>weapons.GetProfile(entry.WeaponType).Range);
        serialized.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
        return "Titan weapon 88, all explicit target injectables/renderer bindings, radar "+data.Range;
    }

    static MonoBehaviour Component(GameObject root,string name)=>root.GetComponentsInChildren<MonoBehaviour>(true).Single(c=>c.GetType().Name==name);
    static void Assign(MonoBehaviour component,string field,UnityEngine.Object[] values)
    {
        var so=new SerializedObject(component);var array=so.FindProperty(field);array.arraySize=values.Length;
        for(int i=0;i<values.Length;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
