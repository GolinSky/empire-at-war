using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class BuildFleetLivery
{
    private const float SURFACE_OFFSET = 0.006f;
    private const float STRIPE_SLOPE = 0.32f;
    private struct Vertex
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 Uv;
    }

    public static string Main(string ship)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Livery building requires Edit Mode.");
        AssetDatabase.Refresh();
        foreach(var guid in AssetDatabase.FindAssets("t:Material",new[]{"Assets/Art/Materials/Models/EmpireShips/"+ship,"Assets/Art/Materials/Wrecks/"+ship}))
        {
            var material=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            if(!material.shader.name.StartsWith("EmpireAtWar/Ship"))continue;
            material.SetFloat("_TeamRimStrength",0);material.SetFloat("_TeamEmissionTint",0);material.SetFloat("_TeamMaskStrength",0);material.SetFloat("_TeamLiveryStrength",0);
            EditorUtility.SetDirty(material);
        }
        var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Ships/Empire/"+ship+".prefab");
        foreach(var material in source.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled).SelectMany(r=>r.sharedMaterials).Distinct())
        {
            if(material.shader.name!="EmpireAtWar/Ship Lit")continue;
            material.SetFloat("_TeamRimStrength",0);material.SetFloat("_TeamEmissionTint",0);material.SetFloat("_TeamMaskStrength",0);material.SetFloat("_TeamLiveryStrength",0);
            EditorUtility.SetDirty(material);
        }
        var all=source.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled && !r.name.StartsWith("TeamStripes")).ToArray();
        var bounds=all[0].bounds;foreach(var renderer in all.Skip(1))bounds.Encapsulate(renderer.bounds);
        float length=bounds.size.z,stripeWidth=length*(ship=="Tartan" ? .018f : .009f);
        var renderers=all.Where(r=>(r.name.ToLowerInvariant().Contains("hull") || r.name.ToLowerInvariant().Contains("super")) && r.sharedMaterials.All(m=>m.shader.name=="EmpireAtWar/Ship Lit")).ToArray();
        var materials = renderers.SelectMany(r => r.sharedMaterials).Distinct().ToArray();
        var stripes = new List<(Mesh Mesh, Material Material)>();
        foreach (var material in materials)
        {
            var vertices = new List<Vector3>(); var normals = new List<Vector3>(); var uvs = new List<Vector2>(); var triangles = new List<int>();
            foreach (var renderer in renderers.Where(r => r.sharedMaterials.Contains(material)))
            {
                var mesh = renderer.GetComponents<MeshFilter>().Single().sharedMesh;
                var matrix = source.transform.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
                var normalMatrix = matrix.inverse.transpose;
                var positions = mesh.vertices; var sourceNormals = mesh.normals; var sourceUvs = mesh.uv;
                for (int slot = 0; slot < renderer.sharedMaterials.Length; slot++)
                {
                    if (renderer.sharedMaterials[slot] != material) continue;
                    var indices = mesh.GetTriangles(slot);
                    for (int index = 0; index < indices.Length; index += 3)
                    {
                        var face = Enumerable.Range(0, 3).Select(c => indices[index + c]).Select(i => new Vertex { Position = matrix.MultiplyPoint3x4(positions[i]), Normal = normalMatrix.MultiplyVector(sourceNormals[i]).normalized, Uv = sourceUvs[i] }).ToList();
                        if (face.Average(v => v.Normal.y) < 0.1f) continue;
                        foreach (int side in new[] { -1, 1 }) foreach (float start in new[] { length*.04f, length*.13f, length*.22f })
                        {
                            var polygon = Clip(face, v => side * v.Position.x - length*.06f);
                            polygon = Clip(polygon, v => length*.17f - side * v.Position.x);
                            polygon = Clip(polygon, v => v.Position.z - start - STRIPE_SLOPE * side * v.Position.x);
                            polygon = Clip(polygon, v => start + stripeWidth + STRIPE_SLOPE * side * v.Position.x - v.Position.z);
                            if (polygon.Count < 3) continue;
                            int offset = vertices.Count;
                            foreach (var vertex in polygon) { vertices.Add(vertex.Position + vertex.Normal * SURFACE_OFFSET); normals.Add(vertex.Normal); uvs.Add(vertex.Uv); }
                            for (int corner = 1; corner < polygon.Count - 1; corner++)
                                triangles.AddRange(matrix.determinant < 0 ? new[] { offset, offset + corner + 1, offset + corner } : new[] { offset, offset + corner, offset + corner + 1 });
                        }
                    }
                }
            }
            if (triangles.Count == 0) continue;
            string name = ship+"_TeamStripes_" + material.name;
            string meshPath = "Assets/Art/Models/EmpireShips/"+ship+"/" + name + ".asset";
            var saved = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (saved == null) { saved = new Mesh { name = name }; AssetDatabase.CreateAsset(saved, meshPath); }
            saved.Clear(); saved.SetVertices(vertices); saved.SetNormals(normals); saved.SetUVs(0, uvs); saved.SetTriangles(triangles, 0); saved.RecalculateTangents(); saved.RecalculateBounds(); saved.UploadMeshData(false); EditorUtility.SetDirty(saved);
            string materialPath = "Assets/Art/Materials/Models/EmpireShips/"+ship+"/" + name + ".mat";
            var paint = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (paint == null) { paint = new Material(material); AssetDatabase.CreateAsset(paint, materialPath); }
            paint.CopyPropertiesFromMaterial(material); paint.name = name;
            string texturePath="Assets/Art/Textures/Models/EmpireShips/"+ship+"/"+ship+"_WornPaint_"+material.name+".png";
            var importer=(TextureImporter)AssetImporter.GetAtPath(texturePath);importer.textureType=TextureImporterType.Default;importer.sRGBTexture=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            paint.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
            paint.SetFloat("_TeamMaskStrength",0);paint.SetFloat("_TeamRimStrength",0);paint.SetFloat("_TeamEmissionTint",0);
            paint.SetFloat("_TeamLiveryHue",.968f);paint.SetFloat("_TeamLiveryHueRange",.07f);paint.SetFloat("_TeamLiveryMinSaturation",.4f);paint.SetFloat("_TeamLiveryStrength",1);paint.SetFloat("_Smoothness",.15f);EditorUtility.SetDirty(paint);
            stripes.Add((saved, paint));
        }
        if (stripes.Count == 0) throw new InvalidOperationException("No source foredeck surfaces intersect team stripes.");
        foreach (string path in new[] { "Assets/Prefabs/Models/Ships/Empire/"+ship+".prefab" })
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (var old in root.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("TeamStripes")).ToArray()) UnityEngine.Object.DestroyImmediate(old.gameObject);
                foreach (var stripe in stripes)
                {
                    var obj = new GameObject("TeamStripes_" + stripe.Material.name); obj.transform.SetParent(root.transform, false);
                    obj.AddComponent<MeshFilter>().sharedMesh = stripe.Mesh; var renderer = obj.AddComponent<MeshRenderer>(); renderer.sharedMaterial = stripe.Material; renderer.shadowCastingMode = ShadowCastingMode.Off;
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        AssetDatabase.SaveAssets();
        UpdateViews(ship,stripes);
        EmpireAtWar.Editor.Rendering.ShipWreckBuilder.Build("Assets/Prefabs/Models/Ships/Empire/"+ship+"ShipView.prefab");
        AssetDatabase.SaveAssets();
        System.IO.File.WriteAllText("Tools/Blender/EmpireFleetLivery/Evidence/"+ship+"Geometry.json",Newtonsoft.Json.JsonConvert.SerializeObject(new {ship,width=stripeWidth,surfaces=stripes.Count,triangles=stripes.Sum(s=>s.Mesh.triangles.Length/3)},Newtonsoft.Json.Formatting.Indented));
        return "Saved " + stripes.Count + " source-albedo/UV stripe surfaces, " + stripes.Sum(s => s.Mesh.triangles.Length / 3) + " triangles.";
    }
    private static void UpdateViews(string ship,List<(Mesh Mesh,Material Material)> stripes)
    {
        string viewPath="Assets/Prefabs/Models/Ships/Empire/"+ship+"ShipView.prefab";
        var view=PrefabUtility.LoadPrefabContents(viewPath);
        try
        {
            var body=(Transform)new SerializedObject(Component(view,"ShipMoveComponent")).FindProperty("bodyTransform").objectReferenceValue;
            if(!view.GetComponentsInChildren<MeshRenderer>(true).Any(r=>r.name.StartsWith("TeamStripes")))AddStripes(body,stripes);
            Assign(Component(view,"TeamColorView"),"meshRenderers",view.GetComponentsInChildren<MeshRenderer>(true).Cast<UnityEngine.Object>().ToArray());
            Assign(Component(view,"FogVisibilityComponent"),"renderers",view.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled).Cast<UnityEngine.Object>().ToArray());
            PrefabUtility.SaveAsPrefabAsset(view,viewPath);
        }
        finally{PrefabUtility.UnloadPrefabContents(view);}
        string previewPath="Assets/Prefabs/Ui/Reinforcement/"+ship+"ReinforcementView.prefab";
        var preview=PrefabUtility.LoadPrefabContents(previewPath);
        try
        {
            foreach(var old in preview.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("TeamStripes")).ToArray())UnityEngine.Object.DestroyImmediate(old.gameObject);
            var hologram=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Vfx/Hologram.mat");
            AddStripes(preview.transform,stripes.Select(s=>(s.Mesh,hologram)).ToList());
            Assign(Component(preview,"UnitSpawnView"),"meshRenderers",preview.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled).Cast<UnityEngine.Object>().ToArray());
            PrefabUtility.SaveAsPrefabAsset(preview,previewPath);
        }
        finally{PrefabUtility.UnloadPrefabContents(preview);}
    }
    private static void AddStripes(Transform parent,List<(Mesh Mesh,Material Material)> stripes)
    {
        foreach(var stripe in stripes){var obj=new GameObject("TeamStripes_"+stripe.Mesh.name);obj.transform.SetParent(parent,false);obj.AddComponent<MeshFilter>().sharedMesh=stripe.Mesh;var renderer=obj.AddComponent<MeshRenderer>();renderer.sharedMaterial=stripe.Material;renderer.shadowCastingMode=ShadowCastingMode.Off;}
    }
    private static MonoBehaviour Component(GameObject root,string name)=>root.GetComponentsInChildren<MonoBehaviour>(true).Single(c=>c.GetType().Name==name);
    private static void Assign(MonoBehaviour component,string field,UnityEngine.Object[] values){var so=new SerializedObject(component);var list=so.FindProperty(field);list.arraySize=values.Length;for(int i=0;i<values.Length;i++)list.GetArrayElementAtIndex(i).objectReferenceValue=values[i];so.ApplyModifiedPropertiesWithoutUndo();}
    private static List<Vertex> Clip(List<Vertex> polygon, Func<Vertex, float> distance)
    {
        var output = new List<Vertex>();
        for (int index = 0; index < polygon.Count; index++)
        {
            var current = polygon[index];
            var previous = polygon[(index + polygon.Count - 1) % polygon.Count];
            float currentDistance = distance(current);
            float previousDistance = distance(previous);
            if ((currentDistance >= 0) != (previousDistance >= 0))
            {
                float weight = previousDistance / (previousDistance - currentDistance);
                output.Add(new Vertex {Position = Vector3.Lerp(previous.Position, current.Position, weight), Normal = Vector3.Lerp(previous.Normal, current.Normal, weight).normalized, Uv = Vector2.Lerp(previous.Uv, current.Uv, weight)});
            }
            if (currentDistance >= 0) output.Add(current);
        }
        return output;
    }

}
