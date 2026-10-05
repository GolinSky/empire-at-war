using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json.Linq;

public static class InspectXWingGeometry
{
    public static string Main()
    {
        const string MODEL="Assets/Art/Models/RebellionShips/XWing/XWing.fbx";
        var model=AssetDatabase.LoadAssetAtPath<GameObject>(MODEL);
        var meshes=model.GetComponentsInChildren<MeshFilter>(true).Select(m=>new {mesh=m.sharedMesh,transform=m.transform,name=m.name}).Concat(model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Select(m=>new {mesh=m.sharedMesh,transform=m.transform,name=m.name}));
        var source=(JObject)JObject.Parse(File.ReadAllText("Temp/XWingImport/Output/ConversionReport.json"))["before"]["bones"];
        float boneError=0;
        foreach(var pair in source)
        {
            var bone=model.GetComponentsInChildren<Transform>(true).Single(t=>t.name==pair.Key && t.GetComponents<Renderer>().Length==0);
            var head=pair.Value["head"];
            var expected=new Vector3(-head[0].Value<float>(),head[2].Value<float>(),-head[1].Value<float>())*.02f;
            boneError=Mathf.Max(boneError,Vector3.Distance(bone.position,expected));
        }
        var geometry=meshes.Select(m=>new {m.name,triangles=Enumerable.Range(0,m.mesh.triangles.Length/3).Select(i=>Enumerable.Range(0,3).Select(j=>{int v=m.mesh.triangles[i*3+j];var p=m.transform.TransformPoint(m.mesh.vertices[v]);var uv=m.mesh.uv[v];return new[]{p.x,p.y,p.z,uv.x,uv.y};}).ToArray()).ToArray()}).ToArray();
        File.WriteAllText("Temp/XWingImport/Output/UnityGeometry.json",Newtonsoft.Json.JsonConvert.SerializeObject(geometry));
        File.WriteAllText("Temp/XWingImport/Output/UnityBoneError.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{boneError}));
        return "FBX meshes "+geometry.Length+", triangles "+geometry.Sum(m=>m.triangles.Length)+", maximum bone displacement "+boneError;
    }
}
