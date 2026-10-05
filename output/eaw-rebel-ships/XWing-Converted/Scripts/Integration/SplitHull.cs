using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class SplitXWingHull
{
    public static string Main()
    {
        const string VISUAL="Assets/Prefabs/Models/Squadrons/XWing.prefab";
        var root=PrefabUtility.LoadPrefabContents(VISUAL);
        try
        {
            var skin=root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r=>r.enabled);
            var mesh=skin.sharedMesh;
            if(mesh.boneWeights.Any(w=>w.weight0!=1 || w.weight1!=0))throw new Exception("Rigid partition requires one full bone weight per vertex.");
            var triangles=mesh.triangles;
            int total=0;
            foreach(int boneIndex in mesh.boneWeights.Select(w=>w.boneIndex0).Distinct())
            {
                var bone=skin.bones[boneIndex];
                var map=new Dictionary<int,int>();
                var indices=new List<int>();
                for(int i=0;i<triangles.Length;i+=3)
                {
                    if(mesh.boneWeights[triangles[i]].boneIndex0!=boneIndex)continue;
                    for(int j=0;j<3;j++)
                    {
                        int vertex=triangles[i+j];
                        if(mesh.boneWeights[vertex].boneIndex0!=boneIndex)throw new Exception("Triangle crosses bone partitions.");
                        if(!map.ContainsKey(vertex))map.Add(vertex,map.Count);
                        indices.Add(map[vertex]);
                    }
                }
                var originals=map.OrderBy(pair=>pair.Value).Select(pair=>pair.Key).ToArray();
                var bind=mesh.bindposes[boneIndex];
                var part=new Mesh {name="XWing_"+bone.name};
                part.vertices=originals.Select(i=>bind.MultiplyPoint3x4(mesh.vertices[i])).ToArray();
                part.normals=originals.Select(i=>bind.MultiplyVector(mesh.normals[i]).normalized).ToArray();
                part.uv=originals.Select(i=>mesh.uv[i]).ToArray();
                part.tangents=originals.Select(i=>{var tangent=mesh.tangents[i];var v=bind.MultiplyVector(new Vector3(tangent.x,tangent.y,tangent.z)).normalized;return new Vector4(v.x,v.y,v.z,tangent.w);}).ToArray();
                part.triangles=indices.ToArray();part.RecalculateBounds();
                AssetDatabase.CreateAsset(part,"Assets/Art/Models/RebellionShips/XWing/"+part.name+".asset");
                var section=new GameObject(part.name);section.transform.SetParent(bone,false);
                section.AddComponent<MeshFilter>().sharedMesh=part;
                section.AddComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;
                total+=indices.Count/3;
            }
            if(total!=triangles.Length/3)throw new Exception("Partition lost triangles.");
            skin.enabled=false;
            PrefabUtility.SaveAsPrefabAsset(root,VISUAL);
            AssetDatabase.SaveAssets();
            return "Five rigid sections preserve all "+total+" triangles, normals, UVs and animated bone placement; source skinned mesh retained disabled.";
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
}
