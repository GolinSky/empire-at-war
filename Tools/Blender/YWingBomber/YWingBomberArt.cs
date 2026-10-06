using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class YWingBomberArt
{
    private const string MODEL_FOLDER = "Assets/Art/Models/RebellionShips/YWingBomber/";
    private const string TEXTURE_FOLDER = "Assets/Art/Textures/Models/RebellionShips/YWingBomber/";
    private const string MATERIAL_FOLDER = "Assets/Art/Materials/Models/RebellionShips/YWingBomber/";
    private const string VISUAL = "Assets/Prefabs/Models/Squadrons/YWingBomber.prefab";
    private const string ICON = "Assets/Art/Textures/Ui/Icons/ShipIcon/YWingBomberIcon.png";
    private const int RENDER_LAYER = 31;

    public static object Main()
    {
        foreach (string part in new[] { "Hull_Albedo", "Hull_Normal", "Engine_Albedo" })
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(TEXTURE_FOLDER + "YWingBomber_" + part + ".png");
            importer.textureType = part == "Hull_Normal" ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = part != "Hull_Normal";
            importer.flipGreenChannel = part == "Hull_Normal";
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.SaveAndReimport();
        }
        var hull = new Material(Shader.Find("EmpireAtWar/Ship Lit"));
        hull.name = "YWingBomber_Hull";
        hull.SetTexture("_BaseMap", Texture("Hull_Albedo"));
        hull.SetTexture("_BumpMap", Texture("Hull_Normal"));
        hull.EnableKeyword("_NORMALMAP");
        hull.SetFloat("_Metallic", 0.2f); hull.SetFloat("_Smoothness", 0.25f);
        hull.SetFloat("_TeamMaskStrength", 0f);
        hull.SetFloat("_TeamLiveryHue", 0.145f);
        hull.SetFloat("_TeamLiveryHueRange", 0.06f);
        hull.SetFloat("_TeamLiveryMinSaturation", 0.35f);
        hull.SetFloat("_TeamLiveryStrength", 1f);
        AssetDatabase.CreateAsset(hull, MATERIAL_FOLDER + hull.name + ".mat");
        var engine = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        engine.name = "YWingBomber_Engine";
        engine.SetTexture("_BaseMap", Texture("Engine_Albedo"));
        engine.SetFloat("_Surface", 1); engine.SetFloat("_Blend", 2);
        engine.SetFloat("_SrcBlend", (float)BlendMode.One); engine.SetFloat("_DstBlend", (float)BlendMode.One);
        engine.SetFloat("_ZWrite", 0); engine.SetFloat("_Cull", (float)CullMode.Off);
        engine.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        engine.SetOverrideTag("RenderType", "Transparent"); engine.renderQueue = 3000;
        AssetDatabase.CreateAsset(engine, MATERIAL_FOLDER + engine.name + ".mat");
        foreach (string stem in new[] { "YWingBomber", "YWingBomberTurret" })
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(MODEL_FOLDER + stem + ".fbx");
            importer.importAnimation = false; importer.isReadable = true;
            foreach (string part in new[] { "Hull", "Helper", "Helper.001", "Engine" })
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), stem + "_" + part), part == "Engine" ? engine : hull);
            importer.SaveAndReimport();
        }
        var scene = EditorSceneManager.NewPreviewScene();
        var root = new GameObject("YWingBomber"); SceneManager.MoveGameObjectToScene(root, scene);
        try
        {
            var geometry = new GameObject("Geometry"); geometry.transform.SetParent(root.transform, false);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_FOLDER + "YWingBomber.fbx"), scene);
            model.transform.SetParent(geometry.transform, false);
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = renderer.name == "REB_YWing" || renderer.name == "Glass" || renderer.name.StartsWith("Plane");
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            }
            var attachment = model.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Turretbase");
            var turret = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_FOLDER + "YWingBomberTurret.fbx"), scene);
            turret.transform.SetParent(geometry.transform, false); turret.transform.position = attachment.position;
            foreach (var renderer in turret.GetComponentsInChildren<Renderer>(true)) renderer.shadowCastingMode = ShadowCastingMode.Off;
            var bounds = Bounds(root, false);
            float scale = 4f / bounds.size.z;
            geometry.transform.localScale = Vector3.one * scale;
            geometry.transform.localPosition = -bounds.center * scale;
            PrefabUtility.SaveAsPrefabAsset(root, VISUAL);
            AssetDatabase.SaveAssets();
        }
        finally { Object.DestroyImmediate(root); EditorSceneManager.ClosePreviewScene(scene); }
        Render(VISUAL, ICON, 0);
        var icon = (TextureImporter)AssetImporter.GetAtPath(ICON);
        icon.textureType = TextureImporterType.Sprite; icon.spriteImportMode = SpriteImportMode.Single;
        icon.alphaIsTransparency = true; icon.mipmapEnabled = false; icon.SaveAndReimport();
        AssetDatabase.SaveAssets();
        return new { visual = VISUAL, icon = ICON };
    }

    private static Texture2D Texture(string part) => AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_FOLDER + "YWingBomber_" + part + ".png");

    public static object RenderAll()
    {
        for(int i=0;i<8;i++) Render(VISUAL,"Temp/YWingBomberImport/Previews/Team"+i+".png",i+1);
        Render("Assets/Prefabs/Ui/Reinforcement/YWingBomberReinforcementView.prefab","Temp/YWingBomberImport/Previews/Placement.png",0);
        return "Eight team previews and placement rendered";
    }

    public static object Inspect()
    {
        var visual=AssetDatabase.LoadAssetAtPath<GameObject>(VISUAL);
        var b=Bounds(visual,false);
        var renderers=visual.GetComponentsInChildren<MeshRenderer>(true);
        var result=new {
            size=new[]{b.size.x,b.size.y,b.size.z},center=new[]{b.center.x,b.center.y,b.center.z},
            rootScale=new[]{visual.transform.localScale.x,visual.transform.localScale.y,visual.transform.localScale.z},
            visible=renderers.Where(r=>r.enabled).Select(r=>r.name).ToArray(),
            hidden=renderers.Where(r=>!r.enabled).Select(r=>r.name).ToArray()
        };
        File.WriteAllText("Temp/YWingBomberImport/Dimensions.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
        return result;
    }

    public static Bounds Bounds(GameObject root, bool effects)
    {
        var filters = root.GetComponentsInChildren<MeshFilter>(true).Where(f => f.GetComponent<Renderer>().enabled && (effects || !f.name.StartsWith("Plane"))).ToArray();
        var bounds = new Bounds(filters[0].transform.TransformPoint(filters[0].sharedMesh.bounds.center), Vector3.zero);
        foreach (var filter in filters)
        {
            var b = filter.sharedMesh.bounds;
            for (int i = 0; i < 8; i++) bounds.Encapsulate(filter.transform.TransformPoint(b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1))));
        }
        return bounds;
    }

    public static void Render(string prefab, string path, int team)
    {
        var scene = EditorSceneManager.NewPreviewScene();
        var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefab), scene);
        var bounds = Bounds(root, true);
        var cameraObject = new GameObject("YWing Preview Camera"); SceneManager.MoveGameObjectToScene(cameraObject, scene);
        var camera = cameraObject.AddComponent<Camera>(); camera.scene = scene;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.clear;
        camera.cullingMask = 1 << RENDER_LAYER; camera.orthographic = true;
        camera.orthographicSize = bounds.extents.magnitude * 1.15f;
        camera.nearClipPlane = 0.01f; camera.farClipPlane = 200;
        camera.transform.position = bounds.center + new Vector3(-5, 6, 8).normalized * bounds.extents.magnitude * 4;
        camera.transform.LookAt(bounds.center);
        var previous = RenderTexture.active; var target = new RenderTexture(512, 512, 24, RenderTextureFormat.ARGB32);
        var image = new Texture2D(512, 512, TextureFormat.RGBA32, false);
        try
        {
            foreach (var transform in root.GetComponentsInChildren<Transform>(true)) transform.gameObject.layer = RENDER_LAYER;
            foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true)) renderer.SetShaderUserValue((uint)team);
            foreach (var rotation in new[] { new Vector3(45, -35, 0), new Vector3(25, 145, 0) })
            {
                var lightObject = new GameObject("YWing Preview Light"); SceneManager.MoveGameObjectToScene(lightObject, scene);
                var light = lightObject.AddComponent<Light>(); light.type = LightType.Directional;
                light.intensity = rotation.y < 0 ? 2f : 0.7f; light.cullingMask = 1 << RENDER_LAYER;
                light.transform.rotation = Quaternion.Euler(rotation);
            }
            camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, 512, 512), 0, 0); image.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllBytes(path, image.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = null; RenderTexture.active = previous;
            Object.DestroyImmediate(image); Object.DestroyImmediate(target); Object.DestroyImmediate(root);
            EditorSceneManager.ClosePreviewScene(scene);
        }
        if (path.StartsWith("Assets/")) AssetDatabase.ImportAsset(path);
    }
}
