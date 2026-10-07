using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public static class VerifyExecutorGeometry
{
    public static string Main()
    {
        var geometry=new List<object>();
        foreach(var conversion in JArray.Parse(File.ReadAllText("Temp/ExecutorImport/ConversionReport.json")))
        {
            string name=(string)conversion["name"];
            var model=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/EmpireShips/"+name+"/"+name+".fbx");
            var meshes=model.GetComponentsInChildren<MeshFilter>(true);
            Check(meshes.Length==((JObject)conversion["source"]["meshes"]).Count,"Mesh count: "+name);
            int triangles=meshes.Sum(m=>m.sharedMesh.triangles.Length/3);
            Check(triangles==conversion["source"]["meshes"].Children<JProperty>().Sum(p=>(int)p.Value["triangles"]),"Triangles: "+name);
            Check(meshes.All(m=>m.sharedMesh.uv.Length>0),"UVs: "+name);
            var transforms=model.GetComponentsInChildren<Transform>(true);
            float error=0;
            foreach(var bone in conversion["source"]["bones"].Children<JProperty>())
            {
                string boneName=bone.Name.Split('/').Last();
                var t=transforms.Single(b=>b.name==boneName && !b.GetComponents<MeshFilter>().Any());
                var p=bone.Value["head"];
                var expected=new Vector3(-(float)p[0],(float)p[2],-(float)p[1])*.02f;
                error=Mathf.Max(error,Vector3.Distance(t.position,expected));
                string parent=(string)bone.Value["parent"];
                if(parent!=null)Check(t.parent.name==parent,"Bone parent: "+boneName);
            }
            Check(error<.001f,"Attachment displacement: "+name+" "+error);
            geometry.Add(new{name,meshes=meshes.Length,triangles,bones=((JObject)conversion["source"]["bones"]).Count,maximumBoneError=error});
        }
        var root=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Ships/ExecutorShipView.prefab");
        foreach(var component in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            var iterator=new SerializedObject(component).GetIterator();
            while(iterator.Next(true))if(iterator.propertyType==SerializedPropertyType.ObjectReference)
                Check(iterator.objectReferenceValue!=null || iterator.objectReferenceInstanceIDValue==0,"Broken reference: "+component.name+"/"+iterator.propertyPath);
        }
        var visual=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Ships/Executor.prefab");
        var preview=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Ui/Reinforcement/ExecutorReinforcementView.prefab");
        var bounds=BoundsOf(visual);
        Check(Mathf.Abs(bounds.size.z-1900)<.01f,"Hull length");
        Check((BoundsOf(preview).size-bounds.size).magnitude<.01f,"Preview size");
        var report=new{geometry,visibleSize=new[]{bounds.size.x,bounds.size.y,bounds.size.z},brokenReferences=0,previewMatches=true};
        File.WriteAllText("Temp/ExecutorImport/UnityGeometry.json",JsonConvert.SerializeObject(report,Formatting.Indented));
        return JsonConvert.SerializeObject(report);
    }
    static Bounds BoundsOf(GameObject root)
    {
        var renderers=root.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled).ToArray();
        var bounds=renderers[0].bounds;foreach(var renderer in renderers.Skip(1))bounds.Encapsulate(renderer.bounds);return bounds;
    }
    static void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
}
