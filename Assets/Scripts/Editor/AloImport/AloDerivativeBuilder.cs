using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EmpireAtWar.Editor.Rendering;
using EmpireAtWar.Entities.Reinforcement;
using EmpireAtWar.ViewComponents.Wreck;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Editor.AloImport
{
    public static class AloDerivativeBuilder
    {
        public static JObject Build(JObject recipe, string jobDirectory)
        {
            var generation = (JObject)recipe["derivatives"]["generation"];
            if (generation == null) throw new InvalidOperationException("Derivative generation requires an explicit geometry/output contract.");
            var paths = new List<string>();
            foreach (string field in new[] { "placementRole", "wreckRole", "iconRole" })
                if (generation[field] != null) paths.Add(PathFor(recipe, (string)generation[field]));
            if (generation["wreckMaterials"] is JArray materialOutputs)
                paths.AddRange(materialOutputs.Select(material => PathFor(recipe, (string)material["outputRole"])));
            AloInvocationPolicy.RequireAssetWrite(jobDirectory, paths);
            AloPublicationJournal.Prepare(jobDirectory, paths);
            var scene = EditorSceneManager.NewPreviewScene();
            var prefabs = new List<string>();
            try
            {
                if (generation["placementRole"] != null)
                {
                    using var geometry = new AloGeometrySource(recipe, scene);
                    Material hologram = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath((string)generation["hologramMaterialGuid"]));
                    if (hologram == null) throw new InvalidOperationException("Missing explicit hologram material.");
                    foreach (MeshRenderer renderer in geometry.Renderers) renderer.sharedMaterials = Enumerable.Repeat(hologram, renderer.sharedMaterials.Length).ToArray();
                    var view = geometry.Root.AddComponent<UnitSpawnView>();
                    var serialized = new SerializedObject(view);
                    SetArray(serialized.FindProperty("meshRenderers"), geometry.Renderers.Cast<Object>().ToArray());
                    serialized.FindProperty("hologramMaterial").objectReferenceValue = hologram;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    var collider = geometry.Root.AddComponent<BoxCollider>();
                    collider.center = geometry.Bounds.center;
                    collider.size = geometry.Bounds.size;
                    collider.isTrigger = true;
                    var body = geometry.Root.AddComponent<Rigidbody>();
                    body.isKinematic = true;
                    body.useGravity = false;
                    string path = PathFor(recipe, (string)generation["placementRole"]);
                    SavePrefab(geometry.Root, path);
                    prefabs.Add(path);
                }
                if (generation["wreckRole"] != null)
                {
                    using var geometry = new AloGeometrySource(recipe, scene);
                    Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath((string)generation["wreckShaderGuid"]));
                    if (shader == null) throw new InvalidOperationException("Missing explicit wreck shader.");
                    var renderers = new List<MeshRenderer>();
                    var filters = new List<MeshFilter>();
                    var materials = new Dictionary<string, Material>();
                    for (int index = 0; index < geometry.Renderers.Count; index++)
                    {
                        MeshRenderer renderer = geometry.Renderers[index];
                        if (!renderer.enabled || !renderer.gameObject.activeInHierarchy || renderer.sharedMaterials.Any(material => material.shader.name != "EmpireAtWar/Ship Lit"))
                        {
                            Object.DestroyImmediate(renderer);
                            Object.DestroyImmediate(geometry.Filters[index]);
                            continue;
                        }
                        renderer.sharedMaterials = renderer.sharedMaterials.Select(source =>
                        {
                            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source));
                            if (materials.TryGetValue(guid, out Material existing)) return existing;
                            JToken mapping = generation["wreckMaterials"].Single(item => (string)item["sourceGuid"] == guid);
                            string path = PathFor(recipe, (string)mapping["outputRole"]);
                            var material = new Material(source) { shader = shader, name = Path.GetFileNameWithoutExtension(path) };
                            Material saved = AssetDatabase.LoadAssetAtPath<Material>(path);
                            Directory.CreateDirectory(Path.GetDirectoryName(path));
                            if (saved == null) { AssetDatabase.CreateAsset(material, path); saved = material; }
                            else { EditorUtility.CopySerialized(material, saved); Object.DestroyImmediate(material); EditorUtility.SetDirty(saved); }
                            materials.Add(guid, saved);
                            return saved;
                        }).ToArray();
                        renderers.Add(renderer);
                        filters.Add(geometry.Filters[index]);
                    }
                    if (renderers.Count == 0) throw new InvalidOperationException("Wreck selection has no enabled opaque hull geometry.");
                    var view = geometry.Root.AddComponent<UnitWreckView>();
                    var serialized = new SerializedObject(view);
                    SetArray(serialized.FindProperty("meshRenderers"), renderers.Cast<Object>().ToArray());
                    SetArray(serialized.FindProperty("meshFilters"), filters.Cast<Object>().ToArray());
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    string wreck = PathFor(recipe, (string)generation["wreckRole"]);
                    SavePrefab(geometry.Root, wreck);
                    prefabs.Add(wreck);
                }
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
            if ((string)recipe["derivatives"]["helperPolicy"] != "retain") UnitHelperMeshStripper.Strip(prefabs, Array.Empty<string>());
            JObject previews = AloPreviewRenderer.Render(recipe, jobDirectory);
            if (generation["iconRole"] != null)
            {
                string icon = PathFor(recipe, (string)generation["iconRole"]);
                Directory.CreateDirectory(Path.GetDirectoryName(icon));
                File.Copy(Path.Combine(jobDirectory, "Evidence", "Previews", "Model.png"), icon, true);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(icon);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            AssetDatabase.ForceReserializeAssets(paths);
            AssetDatabase.SaveAssets();
            AloPublicationJournal.Checkpoint(jobDirectory, "derivativesSaved", paths);
            return new JObject { ["outputs"] = new JArray(paths), ["previews"] = previews, ["visualReview"] = "needs-review" };
        }

        private static string PathFor(JObject recipe, string role) => (string)recipe["integration"]["outputs"].Single(output => (string)output["role"] == role)["path"];

        private static void SavePrefab(GameObject root, string path)
        {
            root.name = Path.GetFileNameWithoutExtension(path);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
            if (!success) throw new InvalidOperationException("Failed to persist derivative prefab " + path);
            AssetDatabase.SaveAssets();
        }

        private static void SetArray(SerializedProperty property, Object[] values)
        {
            property.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++) property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
        }
    }
}
