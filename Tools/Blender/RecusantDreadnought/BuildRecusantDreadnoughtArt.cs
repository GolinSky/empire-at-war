using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

public static class BuildRecusantDreadnoughtArt
{
    private const string NAME = "RecusantDreadnought";
    private const string STAGING = "Temp/RecusantDreadnoughtImport/Output/";
    private const string MODELS = "Assets/Art/Models/SeparatistShips/" + NAME;
    private const string TEXTURES = "Assets/Art/Textures/Models/SeparatistShips/" + NAME;
    private const string MATERIALS = "Assets/Art/Materials/Models/SeparatistShips/" + NAME;
    private const string MODEL = MODELS + "/" + NAME + ".fbx";
    private const string VISUAL = "Assets/Prefabs/Models/Ships/Separatist/" + NAME + ".prefab";
    private const string ICON = "Assets/Art/Textures/Ui/Icons/ShipIcon/Separatist/" + NAME + "Icon.png";
    private const float LENGTH = 266.90634f;

    public static string Main()
    {
        EnsureFolder(MODELS);
        EnsureFolder(TEXTURES);
        EnsureFolder(MATERIALS);
        File.Copy(STAGING + NAME + ".fbx", MODEL, true);
        Texture2D albedo = ImportTexture("Hull_Albedo", false, true);
        Texture2D normal = ImportTexture("Hull_Normal", true, false);
        Texture2D mask = ImportTexture("Hull_TeamMask", false, false);
        Texture2D lights = ImportTexture("Lights_Emissive", false, true);
        Material hull = LoadMaterial("Hull", "EmpireAtWar/Ship Lit");
        hull.SetTexture("_BaseMap", albedo);
        hull.SetTexture("_BumpMap", normal);
        hull.SetTexture("_TeamMaskMap", mask);
        hull.SetFloat("_TeamMaskStrength", 1f);
        hull.SetFloat("_TeamLiveryStrength", 0f);
        hull.SetFloat("_Metallic", 0.2f);
        hull.SetFloat("_Smoothness", 0.22f);
        EditorUtility.SetDirty(hull);

        Material glow = LoadMaterial("Lights", "Universal Render Pipeline/Particles/Unlit");
        glow.SetTexture("_BaseMap", lights);
        glow.SetFloat("_Surface", 1f);
        glow.SetFloat("_Blend", 2f);
        glow.SetFloat("_SrcBlend", (float)BlendMode.One);
        glow.SetFloat("_DstBlend", (float)BlendMode.One);
        glow.SetFloat("_ZWrite", 0f);
        glow.SetFloat("_Cull", (float)CullMode.Back);
        glow.SetOverrideTag("RenderType", "Transparent");
        glow.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        glow.renderQueue = (int)RenderQueue.Transparent;
        EditorUtility.SetDirty(glow);

        AssetDatabase.ImportAsset(MODEL, ImportAssetOptions.ForceSynchronousImport);
        var importer = (ModelImporter)AssetImporter.GetAtPath(MODEL);
        importer.globalScale = 0.02f;
        importer.preserveHierarchy = true;
        importer.importAnimation = false;
        importer.isReadable = true;
        importer.weldVertices = false;
        importer.meshCompression = ModelImporterMeshCompression.Off;
        importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), NAME + "_Hull"), hull);
        importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), NAME + "_Lights"), glow);
        importer.SaveAndReimport();

        var scene = EditorSceneManager.NewPreviewScene();
        try
        {
            var root = new GameObject(NAME);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(MODEL), root.transform);
            foreach (MeshRenderer renderer in model.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (renderer.sharedMaterials.All(material => material == hull))
                    continue;
                if (renderer.sharedMaterials.All(material => material == glow))
                {
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    continue;
                }
                throw new InvalidOperationException("Unmapped Recusant material on " + renderer.name);
            }

            Bounds source = HullBounds(root);
            float scale = LENGTH / source.size.z;
            model.transform.localScale *= scale;
            model.transform.localPosition = -source.center * scale;
            Bounds bounds = HullBounds(root);
            if (Mathf.Abs(bounds.size.z - LENGTH) > 0.02f || bounds.center.magnitude > 0.02f)
                throw new InvalidOperationException("Recusant hull bounds differ from the approved scale.");
            PrefabUtility.SaveAsPrefabAsset(root, VISUAL);
            RenderIcon(scene, bounds);
            AssetDatabase.SaveAssets();
            return $"Saved visual and icon. Model bounds {bounds.size}, centered at {bounds.center}.";
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    private static Texture2D ImportTexture(string suffix, bool normal, bool srgb)
    {
        string file = NAME + "_" + suffix + ".png";
        string path = TEXTURES + "/" + file;
        File.Copy(STAGING + "Textures/" + file, path, true);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        importer.sRGBTexture = srgb;
        importer.flipGreenChannel = normal;
        importer.alphaIsTransparency = false;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.maxTextureSize = 4096;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static Material LoadMaterial(string suffix, string shaderName)
    {
        string path = MATERIALS + "/" + NAME + "_" + suffix + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;

        Shader shader = Shader.Find(shaderName);
        if (shader == null) throw new InvalidOperationException("Missing shader " + shaderName);
        material = new Material(shader);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static Bounds HullBounds(GameObject root)
    {
        Vector3[] points = root.GetComponentsInChildren<MeshFilter>(true)
            .Where(filter => filter.gameObject.GetComponent<MeshRenderer>().sharedMaterial.shader.name == "EmpireAtWar/Ship Lit")
            .SelectMany(filter => filter.sharedMesh.vertices.Select(filter.transform.TransformPoint))
            .ToArray();
        var bounds = new Bounds(points[0], Vector3.zero);
        foreach (Vector3 point in points) bounds.Encapsulate(point);
        return bounds;
    }

    private static void RenderIcon(UnityEngine.SceneManagement.Scene scene, Bounds bounds)
    {
        var camera = new GameObject("Recusant icon camera").AddComponent<Camera>();
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(camera.gameObject, scene);
        camera.scene = scene;
        camera.enabled = false;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.clear;
        camera.orthographic = true;
        camera.orthographicSize = bounds.size.z * 0.55f;
        camera.transform.position = bounds.center + new Vector3(180, 205, 150);
        camera.transform.LookAt(bounds.center);
        AddLight(scene, new Vector3(38, 30, 0), 2f);
        AddLight(scene, new Vector3(-30, -130, 0), 1f);

        const int SIZE = 512;
        RenderTexture previous = RenderTexture.active;
        RenderTexture target = RenderTexture.GetTemporary(SIZE, SIZE, 24, RenderTextureFormat.ARGB32);
        var pixels = new Texture2D(SIZE, SIZE, TextureFormat.RGBA32, false);
        try
        {
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0, 0, SIZE, SIZE), 0, 0);
            pixels.Apply();
            File.WriteAllBytes(ICON, pixels.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = null;
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);
            Object.DestroyImmediate(pixels);
        }

        AssetDatabase.ImportAsset(ICON, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(ICON);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
    }

    private static void AddLight(UnityEngine.SceneManagement.Scene scene, Vector3 rotation, float intensity)
    {
        var light = new GameObject("Recusant icon light").AddComponent<Light>();
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(light.gameObject, scene);
        light.type = LightType.Directional;
        light.intensity = intensity;
        light.transform.rotation = Quaternion.Euler(rotation);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
