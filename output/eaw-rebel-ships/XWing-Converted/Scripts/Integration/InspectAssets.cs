using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json.Linq;

public static class InspectXWingAssets
{
    const string GAMEPLAY="Assets/Prefabs/Models/Squadrons/XWingSquadronView.prefab";
    const string VISUAL="Assets/Prefabs/Models/Squadrons/XWing.prefab";
    const string PLACEMENT="Assets/Prefabs/Ui/Reinforcement/XWingReinforcementView.prefab";
    public static string Main()
    {
        var reports=new List<object>();
        foreach(var path in new[]{VISUAL,GAMEPLAY,PLACEMENT})
        {
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var components=root.GetComponentsInChildren<Component>(true);
                var broken=new List<string>();
                var collections=new Dictionary<string,int>();
                foreach(var c in components.Where(c=>c!=null))
                {
                    var so=new SerializedObject(c);var p=so.GetIterator();
                    while(p.Next(true))
                        if(p.propertyType==SerializedPropertyType.ObjectReference && p.objectReferenceValue==null && p.objectReferenceInstanceIDValue!=0)broken.Add(c.GetType().Name+":"+p.propertyPath);
                    foreach(var field in new[]{"fighters","hardPoints","renderers","meshRenderers","animations","engineTrails","additionalGuns"})
                    {
                        var list=so.FindProperty(field);
                        if(list!=null && list.isArray)collections[c.GetType().Name+"/"+c.name+"/"+field]=list.arraySize;
                    }
                }
                var guns=components.Where(c=>c!=null && c.GetType().Name=="WeaponHardPoint").Select(c=>new {id=new SerializedObject(c).FindProperty("<Id>k__BackingField").intValue,parent=c.transform.parent.name,weapon=new SerializedObject(c).FindProperty("<WeaponType>k__BackingField").intValue}).ToArray();
                var hulls=components.OfType<MeshRenderer>().Where(r=>r.enabled).ToArray();
                var bounds=hulls[0].bounds;foreach(var r in hulls.Skip(1))bounds.Encapsulate(r.bounds);
                reports.Add(new {path,missingScripts=components.Count(c=>c==null),broken,collections,guns,visibleRenderers=hulls.Length,visibleTriangles=hulls.Sum(r=>r.GetComponents<MeshFilter>().Single().sharedMesh.triangles.Length/3),rootScale=root.transform.localScale,bounds});
            }
            finally {PrefabUtility.UnloadPrefabContents(root);}
        }
        var visual=PrefabUtility.LoadPrefabContents(VISUAL);
        float poseError=0,partitionError=0;
        var animationReports=new List<object>();
        try
        {
            var anim=visual.GetComponentsInChildren<Animation>(true).Single();
            var skin=visual.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(s=>s.name=="X_Wing_LOD1");
            var source=JObject.Parse(File.ReadAllText("Temp/XWingImport/Output/ConversionReport.json"))["source_poses"];
            foreach(var name in new[]{"CloseSFoils","OpenSFoils"})
            {
                var clip=anim.GetClip(name);
                foreach(int frame in new[]{0,15,30})
                {
                    clip.SampleAnimation(anim.gameObject,frame/30f);
                    var expected=source[name=="CloseSFoils"?"Deploy":"Undeploy"][frame.ToString()]["heads"];
                    foreach(JProperty pair in expected)
                    {
                        var bone=anim.GetComponentsInChildren<Transform>(true).Single(t=>t.name==pair.Name && t.GetComponents<Renderer>().Length==0);
                        var point=new Vector3(-pair.Value[0].Value<float>(),pair.Value[2].Value<float>(),-pair.Value[1].Value<float>())*.02f;
                        poseError=Mathf.Max(poseError,Vector3.Distance(bone.position,anim.transform.TransformPoint(point)));
                    }
                    foreach(var part in visual.GetComponentsInChildren<MeshFilter>(true).Where(m=>m.name.StartsWith("XWing_bone_")))
                    {
                        int bone=Array.IndexOf(skin.bones,part.transform.parent);
                        var vertices=skin.sharedMesh.vertices;var weights=skin.sharedMesh.boneWeights;
                        var expectedVertices=Enumerable.Range(0,vertices.Length).Where(i=>weights[i].boneIndex0==bone).Select(i=>skin.bones[bone].TransformPoint(skin.sharedMesh.bindposes[bone].MultiplyPoint3x4(vertices[i]))).ToArray();
                        foreach(var vertex in part.sharedMesh.vertices)
                            partitionError=Mathf.Max(partitionError,expectedVertices.Min(v=>Vector3.Distance(part.transform.TransformPoint(vertex),v)));
                    }
                    var rs=visual.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled).ToArray();var bounds=rs[0].bounds;foreach(var r in rs.Skip(1))bounds.Encapsulate(r.bounds);
                    animationReports.Add(new {name,frame,bounds});
                }
            }
        }
        finally {PrefabUtility.UnloadPrefabContents(visual);}
        var result=new {prefabs=reports,animationReports,poseError,partitionError,isCompiling=EditorApplication.isCompiling};
        File.WriteAllText("Temp/XWingImport/Output/AssetVerification.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented,new Newtonsoft.Json.JsonSerializerSettings{ReferenceLoopHandling=Newtonsoft.Json.ReferenceLoopHandling.Ignore}));
        return "Saved prefab reports: "+reports.Count+"; maximum animated bone error "+poseError+"; rigid partition error "+partitionError+"; compiling "+EditorApplication.isCompiling;
    }
}
