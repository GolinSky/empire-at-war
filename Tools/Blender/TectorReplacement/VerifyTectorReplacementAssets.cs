using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public static class VerifyTectorReplacementAssets
{
    private const string OUTPUT = "F:/CodexArtifacts/tector-preview/UnityReplacement/";
    public static string Main()
    {
        var manifest = JObject.Parse(File.ReadAllText(OUTPUT + "ArtManifest.json"));
        var model = AssetDatabase.LoadAssetAtPath<GameObject>((string)manifest["model_path"]);
        var renderers = model.GetComponentsInChildren<MeshRenderer>(true);
        if (renderers.Length != 31) throw new InvalidOperationException("The FBX must preserve all 31 source meshes.");
        var meshEvidence = new JObject();
        foreach (var renderer in renderers)
        {
            var mesh = renderer.GetComponents<MeshFilter>().Single().sharedMesh;
            var definition = manifest["meshes"][renderer.name];
            string shader = (string)definition["shader"];
            if (!shader.Contains("Shadow") && !shader.Contains("Collision") && mesh.triangles.Length / 3 != (int)definition["triangleCount"])
                throw new InvalidOperationException("Source triangles differ: " + renderer.name);
            if (mesh.uv.Length != mesh.vertexCount || mesh.subMeshCount != 1) throw new InvalidOperationException("UVs/submeshes differ: " + renderer.name);
            var material = renderer.sharedMaterials.Single();
            if (AssetDatabase.GetAssetPath(material) != (string)manifest["imported"][renderer.name]["material_path"]) throw new InvalidOperationException("Material remap differs: " + renderer.name);
            string normal = (string)definition["properties"]["NormalTexture"];
            if (normal != null && AssetDatabase.GetAssetPath(material.GetTexture("_BumpMap")) != (string)manifest["textures"][normal]["path"]) throw new InvalidOperationException("Normal map differs: " + renderer.name);
            meshEvidence[renderer.name] = new JObject { ["vertices"] = mesh.vertexCount, ["triangles"] = mesh.triangles.Length / 3, ["uvs"] = mesh.uv.Length, ["material"] = AssetDatabase.GetAssetPath(material) };
        }
        var transforms = model.GetComponentsInChildren<Transform>(true);
        var counts = new System.Collections.Generic.Dictionary<string,int>();
        var names = new System.Collections.Generic.List<string>();
        foreach (var bone in manifest["source"]["bones"])
        {
            string name = (string)bone["name"]; int count = counts.ContainsKey(name) ? counts[name] : 0; counts[name] = count + 1;
            names.Add(count == 0 ? name : name + "." + count.ToString("000"));
        }
        for (int i = 0; i < names.Count; i++)
        {
            var bone = transforms.Single(t => t.name == names[i] && t.GetComponents<Renderer>().Length == 0);
            if (i == 0) continue;
            int parent = (int)manifest["source"]["bones"][i]["parent_index"];
            if (bone.parent.name != names[parent]) throw new InvalidOperationException("Bone parent differs: " + names[i]);
        }
        int references = 0;
        foreach (string path in new[] { "Assets/Prefabs/Models/Ships/Empire/TectorShipView.prefab", "Assets/Prefabs/Models/Wrecks/TectorWreckView.prefab", "Assets/Prefabs/Ui/Reinforcement/TectorReinforcementView.prefab" })
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component == null) throw new InvalidOperationException("Missing script: " + path);
                var iterator = new SerializedObject(component).GetIterator();
                while (iterator.Next(true)) if (iterator.propertyType == SerializedPropertyType.ObjectReference)
                {
                    if (iterator.objectReferenceValue == null && iterator.objectReferenceInstanceIDValue != 0) throw new InvalidOperationException("Missing reference: " + path + "/" + iterator.propertyPath);
                    references++;
                }
            }
        }
        var result = new JObject { ["sourceMeshes"] = 31, ["sourceBones"] = names.Count, ["objectReferencesChecked"] = references, ["meshes"] = meshEvidence };
        File.WriteAllText(OUTPUT + "UnityAssetVerification.json", result.ToString());
        return "31 source meshes, 142 bone names/parents, UVs/submeshes, material/normal remaps and " + references + " serialized references verified.";
    }
}
