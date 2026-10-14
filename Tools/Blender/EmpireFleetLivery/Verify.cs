using System;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json.Linq;
public static class VerifyFleetLivery
{
    private static int _checks;
    public static string Main()
    {
        _checks=0;var report=new JArray();
        foreach(var ship in new[]{"Procursator","Allegiance","Titan","Tartan"})
        {
            var view=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Ships/Empire/"+ship+"ShipView.prefab");
            var team=References(Component(view,"TeamColorView"),"meshRenderers");var fog=References(Component(view,"FogVisibilityComponent"),"renderers");
            var stripes=view.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.name.StartsWith("TeamStripes")).ToArray();
            Check(stripes.Length>0,ship+" stripes persist in gameplay");
            foreach(var stripe in stripes){Check(team.Contains(stripe) && fog.Contains(stripe),ship+" stripe team/fog ownership");}
            foreach(var renderer in view.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled))
            foreach(var material in renderer.sharedMaterials.Where(m=>m.shader.name=="EmpireAtWar/Ship Lit"))
            {
                Check(material.GetFloat("_TeamRimStrength")==0 && material.GetFloat("_TeamEmissionTint")==0,ship+" no whole-surface team glow");
                Check(material.GetFloat("_TeamMaskStrength")==0,ship+" no blanket paint mask");
                if(renderer.name.StartsWith("TeamStripes")){Check(material.GetFloat("_TeamLiveryStrength")==1 && Mathf.Abs(material.GetFloat("_TeamLiveryHue")-.968f)<.001f,ship+" Acclamator-style hue paint");Check(AssetDatabase.GetAssetPath(material.GetTexture("_BaseMap")).Contains("WornPaint"),ship+" derived scuffed albedo");}
                else Check(material.GetFloat("_TeamLiveryStrength")==0,ship+" neutral base hull");
            }
            var data=new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/Settings/Data/Ship/Empire/"+ship+"ShipData.asset"));
            var wreckData=new SerializedObject(data.FindProperty("<Wreck>k__BackingField").objectReferenceValue);Check(wreckData.FindProperty("<Prefab>k__BackingField").objectReferenceValue!=null,ship+" wreck data reference");
            foreach(var path in new[]{"Assets/Prefabs/Models/Ships/Empire/"+ship+".prefab","Assets/Prefabs/Models/Ships/Empire/"+ship+"ShipView.prefab","Assets/Prefabs/Models/Wrecks/"+ship+"WreckView.prefab","Assets/Prefabs/Ui/Reinforcement/"+ship+"ReinforcementView.prefab"})
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                foreach(var component in prefab.GetComponentsInChildren<Component>(true))
                {
                    Check(component!=null,path+" no missing scripts");var property=new SerializedObject(component).GetIterator();
                    while(property.Next(true))if(property.propertyType==SerializedPropertyType.ObjectReference)Check(property.objectReferenceValue!=null || property.objectReferenceEntityIdValue.Equals(default(EntityId)),path+" saved reference "+property.propertyPath);
                }
            }
            report.Add(new JObject{["ship"]=ship,["paintRenderers"]=stripes.Length});
        }
        File.WriteAllText("Tools/Blender/EmpireFleetLivery/Evidence/SavedVerification.json",new JObject{["checks"]=_checks,["ships"]=report}.ToString());
        return "Verified "+_checks+" saved-reference, neutral-hull, paint, team/fog and wreck checks.";
    }
    private static void Check(bool value,string message){_checks++;if(!value)throw new InvalidOperationException(message);}
    private static MonoBehaviour Component(GameObject root,string name)=>root.GetComponentsInChildren<MonoBehaviour>(true).Single(c=>c.GetType().Name==name);
    private static UnityEngine.Object[] References(MonoBehaviour component,string field){var array=new SerializedObject(component).FindProperty(field);return Enumerable.Range(0,array.arraySize).Select(i=>array.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();}
}
