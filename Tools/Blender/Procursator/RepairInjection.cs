using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.TeamColor;
public static class RepairProcursatorInjection
{
    public static string Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Prefab repair requires Edit Mode.");
        const string VIEW="Assets/Prefabs/Models/Ships/Empire/ProcursatorShipView.prefab";
        var root=PrefabUtility.LoadPrefabContents(VIEW);
        try
        {
            var points=root.GetComponentsInChildren<HealthComponent>(true).Single().ShipUnits;
            var team=root.GetComponentsInChildren<TeamColorView>(true).Single();
            var settings=new SerializedObject(root.GetComponent<EntityBindings>());
            var list=settings.FindProperty("<Injectables>k__BackingField");
            list.arraySize=points.Count+1;
            for(int i=0;i<points.Count;i++)list.GetArrayElementAtIndex(i).objectReferenceValue=points[i];
            list.GetArrayElementAtIndex(points.Count).objectReferenceValue=team;
            settings.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root,VIEW);AssetDatabase.SaveAssets();
            return "Saved Procursator injection bindings: 17 current hardpoints + team-color view.";
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
}
