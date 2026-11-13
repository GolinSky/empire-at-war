using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using EmpireAtWar.Entities.Map;

public static class BuildPlanetDecorationArt
{
    const string INPUT = "Temp/PlanetDecorationsImport/";
    const string PREFABS = "Assets/Prefabs/Models/Other/PlanetDecorations/";
    public static string Main()
    {
        var evidence = new JArray();
        foreach (string specPath in JArray.Parse(File.ReadAllText(INPUT + "Specs.json")))
        {
            var spec = JObject.Parse(File.ReadAllText(specPath));
            string name = (string)spec["piece"]["pieceId"];
            string input = INPUT + name + "/";
            var report = JObject.Parse(File.ReadAllText(input + "ConversionReport.json"));
            string model = "Assets/Art/Models/Other/" + name + "/" + name + ".fbx";
            string textures = "Assets/Art/Textures/Models/Other/" + name + "/";
            string materials = "Assets/Art/Materials/Models/Other/" + name + "/";
            foreach (string dir in new[] { Path.GetDirectoryName(model), textures, materials, PREFABS }) Directory.CreateDirectory(dir);
            File.Copy(input + name + ".fbx", model, true);
            foreach (var tex in ((JObject)spec["textures"]).Properties()) File.Copy((string)tex.Value["path"], textures + Path.GetFileName((string)tex.Value["path"]), true);
            AssetDatabase.Refresh();
            foreach (string path in Directory.GetFiles(textures, "*.png"))
            {
                var ti = (TextureImporter)AssetImporter.GetAtPath(path);
                bool normal = path.Contains("_normal");
                ti.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                ti.sRGBTexture = !normal;
                ti.flipGreenChannel = normal;
                ti.mipmapEnabled = true;
                ti.textureCompression = TextureImporterCompression.Compressed;
                ti.SaveAndReimport();
            }
            var importer = (ModelImporter)AssetImporter.GetAtPath(model);
            importer.importAnimation = false;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            importer.weldVertices = false;
            foreach (var mesh in report["sourceMap"]["meshes"])
                foreach (var slot in mesh["slots"])
                {
                    string matName = (string)slot["exportedMaterialName"];
                    string path = materials + matName + ".mat";
                    var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, path); }
                    var policy = spec["recipe"]["materials"].Single(p => (int)p["selector"]["meshIndex"] == (int)mesh["index"] && (int)p["selector"]["slot"] == (int)slot["index"]);
                    foreach (var tex in policy["textures"])
                    {
                        string source = ((string)tex["source"]).ToLowerInvariant();
                        string texPath = textures + Path.GetFileName((string)spec["textures"][source]["path"]);
                        string property = (string)tex["semantic"] == "normal" ? "_BumpMap" : "_BaseMap";
                        mat.SetTexture(property, AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
                        if (property == "_BumpMap") mat.EnableKeyword("_NORMALMAP");
                    }
                    mat.SetFloat("_Smoothness", .2f);
                    mat.enableInstancing = true;
                    if ((string)policy["surface"] == "alpha")
                    {
                        mat.SetFloat("_AlphaClip", 1); mat.SetFloat("_Cutoff", .35f);
                        mat.SetFloat("_Cull", 0); mat.EnableKeyword("_ALPHATEST_ON"); mat.renderQueue = 2450;
                    }
                    EditorUtility.SetDirty(mat);
                    importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), matName), mat);
                }
            importer.SaveAndReimport();
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(model), scene);
                root.name = name;
                foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
                {
                    var row = report["sourceMap"]["meshes"].Single(m => (string)m["exportedName"] == renderer.name);
                    var source = spec["piece"]["meshes"][(int)row["index"]];
                    bool helper = source["materials"].All(m => ((string)m["shader"]).Contains("ShadowVolume") || ((string)m["shader"]).Contains("Collision"));
                    renderer.enabled = !(bool)row["hidden"] && !helper;
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                }
                PrefabUtility.SaveAsPrefabAsset(root, PREFABS + name + ".prefab");
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
            AssetDatabase.SaveAssets();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFABS + name + ".prefab");
            var visible = prefab.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled).ToArray();
            var bounds = visible[0].bounds;
            foreach (var renderer in visible.Skip(1)) bounds.Encapsulate(renderer.bounds);
            float radius = new Vector2(Mathf.Max(Mathf.Abs(bounds.min.x),Mathf.Abs(bounds.max.x)), Mathf.Max(Mathf.Abs(bounds.min.z),Mathf.Abs(bounds.max.z))).magnitude;
            evidence.Add(new JObject { ["name"] = name, ["radius"] = radius, ["bounds"] = bounds.size.ToString(), ["renderers"] = visible.Length, ["triangles"] = visible.Sum(r => r.GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3), ["colliders"] = prefab.GetComponentsInChildren<Collider>(true).Length });
        }
        File.WriteAllText(INPUT + "UnityReport.json", evidence.ToString());
        return evidence.ToString();
    }
}
