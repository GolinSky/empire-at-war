using System;using System.IO;using System.Linq;using Newtonsoft.Json.Linq;using UnityEditor;using UnityEngine;
public static class ApplyISDISourceTints
{
 public static string Main(){var manifest=JObject.Parse(File.ReadAllText("Temp/ISDIRemakeImport/ArtManifest.json"));int count=0;
 foreach(var entry in manifest.Properties())foreach(var row in entry.Value["materials"]){var parameters=(JObject)row["source_properties"];bool effect=((string)row["shader"]).Contains("Additive")||((string)row["shader"]).Contains("Shield");var tint=parameters[effect?"Color":"Diffuse"] as JArray;if(tint==null)continue;var color=new Color((float)tint[0],(float)tint[1],(float)tint[2],effect?(float)tint[3]:1);var material=AssetDatabase.LoadAssetAtPath<Material>((string)row["material_path"]);material.SetColor("_BaseColor",color);EditorUtility.SetDirty(material);count++;var wreck=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Wrecks/ISDI/"+material.name+"_Wreck.mat");if(wreck!=null){wreck.SetColor("_BaseColor",color);EditorUtility.SetDirty(wreck);}}
 AssetDatabase.SaveAssets();return "Preserved original RGB diffuse/effect tints on "+count+" material definitions.";}
}
