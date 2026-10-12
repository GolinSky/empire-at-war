using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using EmpireAtWar.Components.Hangar;

public static class RemoveMC30cDonorHangar
{
    const string PREFAB_PATH = "Assets/Prefabs/Models/Ships/MC30cShipView.prefab";

    public static string Main()
    {
        var root = PrefabUtility.LoadPrefabContents(PREFAB_PATH);
        int removed = 0;
        try
        {
            foreach(var component in root.GetComponentsInChildren<HangarComponent>(true))
            {
                if(component.transform.childCount != 0 || component.GetComponents<Component>().Any(c=>!(c is Transform) && c!=component))
                    throw new InvalidOperationException("Donor hangar contains other components or children; review before removal.");
                UnityEngine.Object.DestroyImmediate(component.gameObject);
                removed++;
            }
            PrefabUtility.SaveAsPrefabAsset(root,PREFAB_PATH);
            AssetDatabase.SaveAssets();
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.ImportAsset(PREFAB_PATH,ImportAssetOptions.ForceUpdate);
        if(AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH).GetComponentsInChildren<HangarComponent>(true).Length != 0)
            throw new InvalidOperationException("Saved MC30c still contains HangarComponent.");
        return "Removed "+removed+" donor hangar object(s); saved MC30c has no injectable hangar.";
    }
}
