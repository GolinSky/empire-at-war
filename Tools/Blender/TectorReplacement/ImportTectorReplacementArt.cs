using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class ImportTectorReplacementArt
{
    private const string OUTPUT = "F:/CodexArtifacts/tector-preview/UnityReplacement/";
    private const string MATERIALS = "Assets/Art/Materials/Models/EmpireShips/Tector/";

    public static string Main()
    {
        AssetDatabase.Refresh();
        var manifest = JObject.Parse(File.ReadAllText(OUTPUT + "ArtManifest.json"));
        var textures = (JObject)manifest["textures"];
        var normalNames = manifest["meshes"].Children<JProperty>().Select(p => (string)p.Value["properties"]["NormalTexture"]).Where(n => n != null).ToHashSet();
        foreach (var entry in textures.Properties())
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath((string)entry.Value["path"]);
            bool normal = normalNames.Contains(entry.Name);
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = !normal;
            importer.alphaIsTransparency = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 4096;
            if (normal) importer.flipGreenChannel = true;
            importer.SaveAndReimport();
        }
        string modelPath = (string)manifest["model_path"];
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        var importerModel = (ModelImporter)AssetImporter.GetAtPath(modelPath);
        importerModel.globalScale = .02f;
        importerModel.importAnimation = false;
        importerModel.preserveHierarchy = true;
        importerModel.isReadable = true;
        importerModel.meshCompression = ModelImporterMeshCompression.Off;
        var imported = new JObject();
        foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
        {
            var definition = manifest["meshes"][renderer.name];
            if (definition == null) throw new InvalidOperationException("Unexpected imported mesh: " + renderer.name);
            var properties = definition["properties"];
            string shader = (string)definition["shader"];
            string baseName = (string)properties["BaseTexture"];
            string normalName = (string)properties["NormalTexture"];
            bool effect = shader.Contains("Additive") || shader.Contains("Shield");
            string materialPath = MATERIALS + "Tector_Remake_" + (baseName == null ? "Helper" : Path.GetFileNameWithoutExtension(baseName)) + "_" + shader.Replace(".fx", "") + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(Shader.Find(effect ? "Universal Render Pipeline/Particles/Unlit" : "EmpireAtWar/Ship Lit"));
                AssetDatabase.CreateAsset(material, materialPath);
            }
            material.SetColor("_BaseColor", Color.white);
            if (baseName != null) material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>((string)textures[baseName]["path"]));
            if (normalName != null)
            {
                material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>((string)textures[normalName]["path"]));
                material.EnableKeyword("_NORMALMAP");
            }
            if (effect)
            {
                material.SetFloat("_Surface", 1); material.SetFloat("_Blend", 2);
                material.SetFloat("_SrcBlend", (float)BlendMode.One); material.SetFloat("_DstBlend", (float)BlendMode.One);
                material.SetFloat("_SrcBlendAlpha", 1); material.SetFloat("_DstBlendAlpha", 10);
                material.SetFloat("_Cull", 0); material.SetFloat("_ZWrite", 0);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.renderQueue = 3000;
            }
            else
            {
                material.SetFloat("_Smoothness", .25f); material.SetFloat("_Metallic", .1f);
                material.SetFloat("_TeamLiveryStrength", 0); material.SetFloat("_TeamMaskStrength", 0); material.SetFloat("_TeamRimStrength", .6f);
            }
            EditorUtility.SetDirty(material);
            importerModel.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), renderer.sharedMaterials.Single().name), material);
            imported[renderer.name] = new JObject { ["material_path"] = materialPath };
        }
        importerModel.SaveAndReimport();
        AssetDatabase.SaveAssets();
        manifest["imported"] = imported;
        File.WriteAllText(OUTPUT + "ArtManifest.json", manifest.ToString());
        model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        return new JObject { ["meshCount"] = model.GetComponentsInChildren<Renderer>(true).Length, ["materialCount"] = imported.Count, ["rendererTypes"] = JArray.FromObject(model.GetComponentsInChildren<Renderer>(true).Select(r => r.GetType().Name).Distinct()), ["roots"] = JArray.FromObject(model.transform.Cast<Transform>().Select(t => t.name)) }.ToString();
    }
}
