using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using EmpireAtWar.Editor.Rendering;

public static class FinalizeEndurance
{
    public static string Main()
    {
        var overlay=new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/Settings/Data/Ship/HardPointOverlayData.asset"));
        var rows=overlay.FindProperty("entries");
        var row=Enumerable.Range(0,rows.arraySize).Select(i=>rows.GetArrayElementAtIndex(i)).SingleOrDefault(p=>p.FindPropertyRelative("hardPointType").intValue==11);
        if(row==null){rows.InsertArrayElementAtIndex(rows.arraySize);row=rows.GetArrayElementAtIndex(rows.arraySize-1);}
        row.FindPropertyRelative("hardPointType").intValue=11;
        row.FindPropertyRelative("displayName").stringValue="Command Array";
        row.FindPropertyRelative("description").stringValue="Destroying it disables Fighter Coordination.";
        row.FindPropertyRelative("icon").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Ui/Icons/ShipIcon/EnduranceIcon.png");
        overlay.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(overlay.targetObject);
        string[] paths={"Assets/Prefabs/Models/Ships/Endurance.prefab","Assets/Prefabs/Models/Ships/EnduranceShipView.prefab","Assets/Prefabs/Models/Wrecks/EnduranceWreckView.prefab","Assets/Prefabs/Ui/Reinforcement/EnduranceReinforcementView.prefab"};
        string report=UnitHelperMeshStripper.Strip(paths,new string[0]);
        File.WriteAllText("Temp/EnduranceImport/HelperStripping.txt",report);
        AssetDatabase.SaveAssets();
        return report;
    }
}
