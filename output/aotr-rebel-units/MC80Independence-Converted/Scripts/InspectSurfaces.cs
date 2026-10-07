using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public static class InspectMC80Surfaces
{
    public static string Main()
    {
        var repair=JObject.Parse(File.ReadAllText("Temp/MC80IndependenceImport/SurfaceRepairReport.json"));
        var before=JObject.Parse(File.ReadAllText("Temp/MC80IndependenceImport/SourceSurfaceAudit.json"));
        var fbx=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/RebellionShips/MC80Independence/MC80Independence.fbx");
        var live=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Ships/MC80Independence.prefab");
        var meshes=fbx.GetComponentsInChildren<MeshFilter>(true).ToDictionary(f=>f.name);
        var renderers=live.GetComponentsInChildren<MeshRenderer>(true).ToDictionary(r=>r.name);
        var records=new JArray();
        float uvError=0;
        foreach(var source in repair["mesh_materials"])
        {
            string name=(string)source["name"];
            var mesh=meshes[name].sharedMesh;
            var uvSource=before["meshes"].Single(m=>(string)m["name"]==name);
            var minimum=new Vector2(mesh.uv.Min(v=>v.x),mesh.uv.Min(v=>v.y));
            var maximum=new Vector2(mesh.uv.Max(v=>v.x),mesh.uv.Max(v=>v.y));
            uvError=Mathf.Max(uvError,Mathf.Abs(minimum.x-(float)uvSource["uv_min"][0]),Mathf.Abs(minimum.y-(float)uvSource["uv_min"][1]),Mathf.Abs(maximum.x-(float)uvSource["uv_max"][0]),Mathf.Abs(maximum.y-(float)uvSource["uv_max"][1]));
            if((bool)repair["before"]["meshes"][name]["hidden"])continue;
            var renderer=renderers[name];
            var slots=source["materials"].ToArray();
            if(!renderer.enabled || mesh.subMeshCount!=slots.Length || renderer.sharedMaterials.Length!=slots.Length)
                throw new InvalidOperationException("Visible mesh or original material slots differ: "+name);
            for(int slot=0;slot<slots.Length;slot++)
            {
                var material=renderer.sharedMaterials[slot];
                string materialName=(string)slots[slot]["materialName"];
                if(material.name!=materialName || mesh.GetIndexCount(slot)/3!=(uint)slots[slot]["triangleCount"])
                    throw new InvalidOperationException("Submesh material or triangle range differs: "+name);
                bool additive=((string)slots[slot]["shader"]).Contains("Additive");
                if(material.shader.name!=(additive?"Universal Render Pipeline/Particles/Unlit":"EmpireAtWar/Ship Lit"))
                    throw new InvalidOperationException("Source shader intent differs: "+name);
                var definition=repair["materials"].Single(m=>(string)m["name"]==materialName);
                var texture=(Texture2D)material.GetTexture("_BaseMap");
                if(texture.name!=(string)definition["base"] || texture.wrapMode!=TextureWrapMode.Repeat || material.GetTextureScale("_BaseMap")!=Vector2.one || material.GetTextureOffset("_BaseMap")!=Vector2.zero)
                    throw new InvalidOperationException("Albedo assignment or wrap differs: "+name);
                if(!additive && material.GetTexture("_BumpMap").name!=(string)definition["normal"])
                    throw new InvalidOperationException("Source normal map differs: "+name);
            }
            records.Add(new JObject{["mesh"]=name,["slots"]=slots.Length,["triangles"]=mesh.triangles.Length/3,["materials"]=new JArray(renderer.sharedMaterials.Select(m=>m.name))});
        }
        if(uvError>.00001f || records.Count!=38)throw new InvalidOperationException("UV or visible mesh fidelity failed.");
        var result=new JObject{["meshCount"]=meshes.Count,["visibleMeshes"]=records.Count,["uvRangeError"]=uvError,["originalMaterialAssignmentsMatch"]=true,["visibleMeshRecords"]=records};
        File.WriteAllText("Temp/MC80IndependenceImport/UnitySurfaceInspection.json",result.ToString());
        return "Verified all 38 visible meshes, original per-submesh triangle/material ranges, opaque/additive intent, repeat sampling and unchanged UV ranges. Hull has four original slots.";
    }
}
