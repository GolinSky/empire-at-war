using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public static class InspectVictoryIIAdvancedGeometry
{
    const string ROOT = "Temp/VictoryIIAdvancedImport/";
    public static string Main()
    {
        var results = new List<object>();
        foreach (var name in new[] { "VictoryIIAdvanced", "VictoryIIAdvancedTurret01", "VictoryIIAdvancedTurret02" })
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/EmpireShips/" + name + "/" + name + ".fbx");
            var filters = model.GetComponentsInChildren<MeshFilter>(true);
            var source = JObject.Parse(File.ReadAllText(ROOT + name + "SourceGeometry.json"));
            var rows = new List<object>();
            float maximumError = 0;
            foreach (var row in source.Properties())
            {
                var corners = (JArray)row.Value;
                string basename = row.Name.Split('|')[0];
                var candidates = filters.Where(f => BaseName(f.name) == basename)
                    .Select(f => new { filter=f, error=Error(f,corners) }).OrderBy(x=>x.error).ToArray();
                var filter=candidates[0].filter;
                float error=candidates[0].error;
                maximumError = Mathf.Max(maximumError, error);
                rows.Add(new { filter.name, corners = corners.Count, maximumPositionError = error });
            }
            if (maximumError > .01f) throw new InvalidOperationException("Source geometry displacement: " + name + " " + maximumError);
            results.Add(new { name, maximumPositionError = maximumError, meshes = rows });
        }
        string json = JsonConvert.SerializeObject(results, Formatting.Indented);
        File.WriteAllText(ROOT + "UnityGeometry.json", json);
        return json;
    }
    static float Error(MeshFilter filter,JArray corners)
    {
        var mesh=filter.sharedMesh;var vertices=mesh.vertices;var uv=mesh.uv;
        var byUv=new Dictionary<string,List<int>>();
        for(int i=0;i<vertices.Length;i++)
        {
            string key=UvKey(uv[i]);
            if(!byUv.TryGetValue(key,out var list))byUv[key]=list=new List<int>();
            list.Add(i);
        }
        float error=0;
        foreach(var corner in corners)
        {
            var expected=Position(corner);var expectedUv=new Vector2((float)corner[3],(float)corner[4]);
            if(!byUv.TryGetValue(UvKey(expectedUv),out var candidates))
                candidates=Enumerable.Range(0,uv.Length).Where(i=>Vector2.Distance(uv[i],expectedUv)<.00001f).ToList();
            if(candidates.Count==0)return float.MaxValue;
            float distance=candidates.Min(i=>Vector3.Distance(expected,filter.transform.TransformPoint(vertices[i])));
            error=Mathf.Max(error,distance);
        }
        return error;
    }
    static Vector3 Position(JToken p) => new Vector3(-(float)p[0], (float)p[2], -(float)p[1]) * .02f;
    static string BaseName(string name) => name.Contains(".") ? name.Substring(0, name.LastIndexOf('.')) : name;
    static string UvKey(Vector2 uv) => Math.Round(uv.x, 5).ToString("F5", System.Globalization.CultureInfo.InvariantCulture) + "/" + Math.Round(uv.y, 5).ToString("F5", System.Globalization.CultureInfo.InvariantCulture);
}
