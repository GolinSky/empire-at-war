using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public static class VerifyImperialVenatorGeometry
{
    public static string Main()
    {
        var results=new List<object>();
        var manifest=JObject.Parse(File.ReadAllText("Temp/ImperialVenatorImport/ArtManifest.json"));
        foreach(var row in manifest.Properties())
        {
            var root=AssetDatabase.LoadAssetAtPath<GameObject>((string)row.Value["model_path"]);
            var meshes=root.GetComponentsInChildren<MeshFilter>(true).Select(f=>new{transform=f.transform,mesh=f.sharedMesh})
                .Concat(root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Select(r=>new{transform=r.transform,mesh=r.sharedMesh})).ToArray();
            var expected=(JObject)row.Value["after"]["meshes"];
            if(meshes.Length!=expected.Count)throw new InvalidOperationException(row.Name+" mesh count");
            foreach(var source in expected.Properties())
            {
                var mesh=meshes.Single(m=>m.transform.name==source.Name).mesh;
                if(mesh.triangles.Length/3!=(int)source.Value["triangles"] || mesh.subMeshCount!=source.Value["materials"].Count() || mesh.uv.Length!=mesh.vertexCount)
                    throw new InvalidOperationException(row.Name+"/"+source.Name+" triangles, slots or UVs");
            }
            float maximumBoneError=0;
            foreach(var bone in ((JObject)row.Value["after"]["bones"]).Properties())
            {
                var t=root.GetComponentsInChildren<Transform>(true).Single(t=>t.name==bone.Name && !meshes.Any(m=>m.transform==t));
                var head=bone.Value["head"];
                var position=new Vector3(-(float)head[0],(float)head[2],-(float)head[1])*.02f;
                maximumBoneError=Mathf.Max(maximumBoneError,Vector3.Distance(t.position,position));
                if(bone.Value["parent"].Type!=JTokenType.Null && t.parent.name!=(string)bone.Value["parent"])
                    throw new InvalidOperationException(row.Name+" bone parent "+bone.Name);
            }
            if(maximumBoneError>.0001f)throw new InvalidOperationException(row.Name+" bone positions "+maximumBoneError);
            results.Add(new{name=row.Name,meshes=meshes.Length,triangles=meshes.Sum(m=>m.mesh.triangles.Length/3),maximumBoneError});
        }
        File.WriteAllText("Temp/ImperialVenatorImport/UnityGeometry.json",JsonConvert.SerializeObject(results,Formatting.Indented));
        return JsonConvert.SerializeObject(results);
    }
}
