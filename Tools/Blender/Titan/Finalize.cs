using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public static class FinalizeTitan
{
    public static string Main()
    {
        var report=JObject.Parse(File.ReadAllText("Temp/TitanImport/ConversionReport.json"));
        foreach(var source in report["materials"])
        {
            var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Models/EmpireShips/Titan/"+(string)source["name"]+".mat");
            var properties=source["source_properties"];
            var tint=properties["Diffuse"] ?? properties["Color"];
            if(tint is JArray values && values.Count==4)
                material.SetColor("_BaseColor",new Color((float)values[0],(float)values[1],(float)values[2],1));
            EditorUtility.SetDirty(material);
        }
        var data=new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/Settings/Data/Ship/Empire/TitanShipData.asset"));
        data.FindProperty("<ShipClass>k__BackingField").intValue=5;
        data.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(data.targetObject);
        AssetDatabase.SaveAssets();
        return "Saved source material tints and HeavyCapital ship class.";
    }
}
