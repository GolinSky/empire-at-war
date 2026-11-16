using System;
using System.IO;
using System.Linq;
using EmpireAtWar.Editor.Balance;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class LukeRedSquadronArt
{
    private const string STEM = "LukeRedSquadron";
    private const string STAGING = "Temp/LukeRedSquadron/Output/";
    private const string MODEL_DIRECTORY = "Assets/Art/Models/RebellionShips/LukeRedSquadron/";
    private const string MATERIAL_DIRECTORY = "Assets/Art/Materials/Models/RebellionShips/LukeRedSquadron/";
    private const string TEXTURE_DIRECTORY = "Assets/Art/Textures/Models/RebellionShips/LukeRedSquadron/";
    private const string ICON = "Assets/Art/Textures/Ui/Icons/SquadronIcon/Rebellion/LukeRedSquadronIcon.png";
    private const string PREFAB = "Assets/Prefabs/Models/Squadrons/LukeRedSquadron.prefab";

    public static object Apply()
    {
        Directory.CreateDirectory(MODEL_DIRECTORY);
        Directory.CreateDirectory(MATERIAL_DIRECTORY);
        Directory.CreateDirectory(TEXTURE_DIRECTORY);
        File.Copy(STAGING + STEM + ".fbx", MODEL_DIRECTORY + STEM + ".fbx", true);
        File.Copy(STAGING + STEM + "Icon.png", ICON, true);
        foreach (string texture in Directory.GetFiles(STAGING + "Textures", "*.png"))
            File.Copy(texture, TEXTURE_DIRECTORY + Path.GetFileName(texture), true);
        AssetDatabase.Refresh();

        var manifest = JArray.Parse(File.ReadAllText(STAGING + "UnityMaterials.json"));
        var importer = (ModelImporter)AssetImporter.GetAtPath(MODEL_DIRECTORY + STEM + ".fbx");
        importer.globalScale = 1f;
        importer.useFileScale = false;
        importer.importAnimation = false;
        importer.importCameras = false;
        importer.importLights = false;
        importer.importNormals = ModelImporterNormals.Import;
        foreach (var group in manifest.Where(row => !(bool)row["hidden"]).GroupBy(row => (string)row["signature"]))
        {
            var row = group.First();
            bool additive = (string)row["shader"] == "MeshAdditive.fx";
            string suffix = additive ? "EngineGlow" : (int)row["index"] == 4 ? "Wings" : (int)row["index"] == 9 ? "Astrodroid" : "Hull";
            string path = MATERIAL_DIRECTORY + STEM + "_" + suffix + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find(additive ? "Universal Render Pipeline/Unlit" : "EmpireAtWar/Ship Lit"));
                AssetDatabase.CreateAsset(material, path);
            }
            foreach (var texture in ((JObject)row["textures"]).Properties())
            {
                bool normal = texture.Name == "normal";
                var textureImporter = (TextureImporter)AssetImporter.GetAtPath((string)texture.Value);
                textureImporter.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                textureImporter.sRGBTexture = !normal;
                textureImporter.flipGreenChannel = normal;
                textureImporter.alphaIsTransparency = false;
                textureImporter.SaveAndReimport();
                material.SetTexture(normal ? "_BumpMap" : "_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>((string)texture.Value));
            }
            material.SetColor("_BaseColor", Color.white);
            if (additive)
            {
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", 2f);
                material.SetFloat("_SrcBlend", (float)BlendMode.One);
                material.SetFloat("_DstBlend", (float)BlendMode.One);
                material.SetFloat("_ZWrite", 0f);
                material.SetFloat("_Cull", (float)CullMode.Off);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = 3000;
                material.SetOverrideTag("RenderType", "Transparent");
            }
            else
            {
                material.SetFloat("_Metallic", 0.15f);
                material.SetFloat("_Smoothness", 0.35f);
                material.SetFloat("_TeamLiveryHue", 0f);
                material.SetFloat("_TeamLiveryHueRange", 0.07f);
                material.SetFloat("_TeamLiveryMinSaturation", 0.4f);
                material.SetFloat("_TeamLiveryStrength", suffix == "Astrodroid" ? 0f : 1f);
            }
            EditorUtility.SetDirty(material);
            foreach (var member in group)
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), (string)member["material"]), material);
        }
        importer.SaveAndReimport();
        var iconImporter = (TextureImporter)AssetImporter.GetAtPath(ICON);
        iconImporter.textureType = TextureImporterType.Sprite;
        iconImporter.spriteImportMode = SpriteImportMode.Single;
        iconImporter.alphaIsTransparency = true;
        iconImporter.mipmapEnabled = false;
        iconImporter.SaveAndReimport();

        var scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
        var root = new GameObject(STEM);
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
        try
        {
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_DIRECTORY + STEM + ".fbx"), root.transform);
            PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            var components = BalancePrefabBindings.Components(model).OfType<Component>().ToArray();
            var hiddenNames = manifest.Where(row => (bool)row["hidden"]).Select(row => (string)row["mesh"]).ToHashSet();
            foreach (var renderer in components.OfType<MeshRenderer>().Where(renderer => hiddenNames.Contains(renderer.name)).ToArray())
                UnityEngine.Object.DestroyImmediate(renderer.gameObject);
            var renderers = BalancePrefabBindings.Components(model).OfType<MeshRenderer>().ToArray();
            var points = BalancePrefabBindings.Components(model).OfType<MeshFilter>().SelectMany(filter =>
            {
                var bounds = filter.sharedMesh.bounds;
                return new[] { -1f, 1f }.SelectMany(x => new[] { -1f, 1f }.SelectMany(y => new[] { -1f, 1f }.Select(z =>
                    filter.transform.TransformPoint(bounds.center + Vector3.Scale(bounds.extents, new Vector3(x, y, z))))));
            }).ToArray();
            var visibleBounds = new Bounds(points[0], Vector3.zero);
            foreach (var point in points.Skip(1)) visibleBounds.Encapsulate(point);
            float scale = 6f / visibleBounds.size.z;
            model.transform.localScale *= scale;
            model.transform.localPosition = -visibleBounds.center * scale;
            foreach (var renderer in renderers)
            {
                renderer.enabled = true;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            }
            PrefabUtility.SaveAsPrefabAsset(root, PREFAB);
            AssetDatabase.SaveAssets();
            return new { sourceBounds = visibleBounds.ToString(), scale, renderers = renderers.Length, prefab = PREFAB };
        }
        finally
        {
            UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
        }
    }
}
