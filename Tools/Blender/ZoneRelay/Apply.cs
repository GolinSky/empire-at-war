using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class Apply
{
    public static object Run()
    {
        const string MODEL_PATH = "Assets/Art/Models/Other/SpaceSensorNode/SpaceSensorNode.fbx";
        const string TEXTURE_DIR = "Assets/Art/Textures/Models/Other/SpaceSensorNode/";
        const string MATERIAL_PATH = "Assets/Art/Materials/Models/Other/SpaceSensorNode/SpaceSensorNode_Hull.mat";
        const string VISUAL_PATH = "Assets/Prefabs/Models/Other/SpaceSensorNode.prefab";
        const string ZONE_PATH = "Assets/Prefabs/View/ReinforcementZones/ReinforcementZone.prefab";
        Directory.CreateDirectory(Path.GetDirectoryName(MATERIAL_PATH));
        Directory.CreateDirectory(Path.GetDirectoryName(VISUAL_PATH));
        AssetDatabase.Refresh();
        var albedoImporter = (TextureImporter)AssetImporter.GetAtPath(TEXTURE_DIR + "SpaceSensorNode_Albedo.png");
        albedoImporter.sRGBTexture = true;
        albedoImporter.alphaSource = TextureImporterAlphaSource.None;
        albedoImporter.SaveAndReimport();
        var normalImporter = (TextureImporter)AssetImporter.GetAtPath(TEXTURE_DIR + "SpaceSensorNode_Normal.png");
        normalImporter.textureType = TextureImporterType.NormalMap;
        normalImporter.sRGBTexture = false;
        normalImporter.flipGreenChannel = true;
        normalImporter.SaveAndReimport();
        var material = AssetDatabase.LoadAssetAtPath<Material>(MATERIAL_PATH);
        if (material == null)
        {
            material = new Material(Shader.Find("EmpireAtWar/Ship Lit"));
            AssetDatabase.CreateAsset(material, MATERIAL_PATH);
        }
        material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_DIR + "SpaceSensorNode_Albedo.png"));
        material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_DIR + "SpaceSensorNode_Normal.png"));
        material.SetColor("_BaseColor", Color.white);
        material.SetFloat("_Metallic", 0f);
        material.SetFloat("_Smoothness", 0.35f);
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();
        var importer = (ModelImporter)AssetImporter.GetAtPath(MODEL_PATH);
        importer.importAnimation = false;
        importer.animationType = ModelImporterAnimationType.None;
        importer.importNormals = ModelImporterNormals.Import;
        importer.importTangents = ModelImporterTangents.CalculateMikk;
        importer.meshCompression = ModelImporterMeshCompression.Off;
        foreach (var sourceMaterial in AssetDatabase.LoadAllAssetsAtPath(MODEL_PATH).OfType<Material>())
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(sourceMaterial), material);
        importer.SaveAndReimport();

        var original = AssetDatabase.LoadAssetAtPath<GameObject>(ZONE_PATH);
        var oldRenderer = original.transform.Find("Relay/RelayModel").GetComponent<MeshRenderer>();
        var targetBounds = oldRenderer.bounds;
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_PATH);
        var sources = model.GetComponentsInChildren<MeshRenderer>(true)
            .Where(renderer => renderer.name == "Object02" || renderer.name == "Shadow_02").ToArray();
        if (sources.Length != 2)
            throw new InvalidOperationException("Expected both source-visible sensor-node meshes.");
        var sourceBounds = sources[0].bounds;
        sourceBounds.Encapsulate(sources[1].bounds);
        var scale = Mathf.Max(targetBounds.size.x, targetBounds.size.z) / Mathf.Max(sourceBounds.size.x, sourceBounds.size.z);
        var visual = new GameObject("SpaceSensorNode");
        var previewScene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(visual, previewScene);
        try
        {
            foreach (var source in sources)
            {
                var part = new GameObject(source.name);
                part.transform.SetParent(visual.transform, false);
                part.transform.localPosition = (source.transform.position - sourceBounds.center) * scale;
                part.transform.localRotation = source.transform.rotation;
                part.transform.localScale = source.transform.lossyScale * scale;
                part.AddComponent<MeshFilter>().sharedMesh = source.GetComponent<MeshFilter>().sharedMesh;
                var renderer = part.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = true;
            }
            PrefabUtility.SaveAsPrefabAsset(visual, VISUAL_PATH);
        }
        finally
        {
            UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(previewScene);
        }
        var zone = PrefabUtility.LoadPrefabContents(ZONE_PATH);
        try
        {
            var relay = zone.transform.Find("Relay/RelayModel");
            UnityEngine.Object.DestroyImmediate(relay.GetComponent<MeshFilter>());
            UnityEngine.Object.DestroyImmediate(relay.GetComponent<MeshRenderer>());
        relay.localPosition = new Vector3(0f, 80f, 0f);
            relay.localRotation = Quaternion.identity;
            relay.localScale = Vector3.one;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(VISUAL_PATH), relay);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            PrefabUtility.SaveAsPrefabAsset(zone, ZONE_PATH);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(zone);
        }
        AssetDatabase.SaveAssets();
        return new { targetSize = targetBounds.size.ToString("F4"), targetCenter = targetBounds.center.ToString("F4"), sourceSize = sourceBounds.size.ToString("F4"), scale, visual = VISUAL_PATH };
    }
}
