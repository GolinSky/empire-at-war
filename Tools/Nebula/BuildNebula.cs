using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using EmpireAtWar.Components.Environment;

public static class BuildNebula
{
    private const string CLOUD_PREFAB = "Assets/Prefabs/Vfx/SkirmishVolumetricNebulaClouds.prefab";
    private const string MATERIAL_FOLDER = "Assets/Art/Materials/Vfx/Nebula/";
    private const string TEXTURE_FOLDER = "Assets/Art/Textures/Vfx/Nebula/";
    private const int WIDTH = 512;
    private const int HEIGHT = 320;
    private const int DEPTH = 48;

    public static void Run()
    {
        string[] names = { "AzureRift", "VioletPillars", "EmberWings" };
        var cool = new[] { new Color(0.30f,0.65f,0.88f), new Color(0.45f,0.40f,0.85f), new Color(0.62f,0.48f,0.92f) };
        var warm = new[] { new Color(0.72f,0.40f,0.12f), new Color(0.80f,0.23f,0.28f), new Color(1f,0.50f,0.15f) };
        var materials = new Material[names.Length];
        var bake = AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Art/Shaders/Vfx/NebulaFilamentBake.compute");
        var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Art/Shaders/Vfx/NebulaFilamentVolume.shader");
        for (int i = 0; i < names.Length; i++)
        {
            var texture = Bake(bake, i, names[i]);
            string path = MATERIAL_FOLDER + names[i] + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetTexture("_CloudField", texture);
            material.SetTexture("_DetailTex", AssetDatabase.LoadAssetAtPath<Texture3D>("Assets/Art/Textures/Particles/NebulaVolumeNoise3D.asset"));
            material.SetColor("_CoolColor", cool[i]);
            material.SetColor("_WarmColor", warm[i]);
            material.SetColor("_DustColor", new Color(0.004f,0.005f,0.009f));
            material.SetFloat("_Emission", 7f);
            material.SetFloat("_Extinction", 14f);
            material.SetFloat("_Samples", 128);
            material.SetFloat("_Flow", 0.002f);
            EditorUtility.SetDirty(material);
            materials[i] = material;
        }
        BuildVolume(materials);
        BuildStars();
        AssetDatabase.SaveAssets();
        Debug.Log("Built three match nebula volumes and star field.");
    }

    private static Texture3D Bake(ComputeShader shader, int variant, string name)
    {
        int count = WIDTH * HEIGHT * DEPTH;
        using var buffer = new ComputeBuffer(count, sizeof(uint));
        shader.SetInts("_Resolution", WIDTH, HEIGHT, DEPTH);
        shader.SetInt("_Variant", variant);
        shader.SetBuffer(0, "_Voxels", buffer);
        shader.Dispatch(0, WIDTH / 4, HEIGHT / 4, DEPTH / 4);
        var pixels = new uint[count];
        buffer.GetData(pixels);
        var generated = new Texture3D(WIDTH,HEIGHT,DEPTH,TextureFormat.RGBA32,false)
        {
            name = name + "Field", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp
        };
        generated.SetPixelData(pixels,0);
        generated.Apply(false,true);
        string path = TEXTURE_FOLDER + name + "Field.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Texture3D>(path);
        if (existing == null) AssetDatabase.CreateAsset(generated,path);
        else
        {
            EditorUtility.CopySerialized(generated,existing);
            UnityEngine.Object.DestroyImmediate(generated);
            generated = existing;
            EditorUtility.SetDirty(generated);
        }
        return generated;
    }

    private static void BuildVolume(Material[] materials)
    {
        var root = PrefabUtility.LoadPrefabContents(CLOUD_PREFAB);
        try
        {
            foreach (var component in root.GetComponents<Component>())
                if (!(component is Transform)) UnityEngine.Object.DestroyImmediate(component);
            while (root.transform.childCount > 0)
                UnityEngine.Object.DestroyImmediate(root.transform.GetChild(0).gameObject);
            root.layer = 11;
            var volume = new GameObject("Nebula Volume");
            volume.layer = 11;
            volume.transform.SetParent(root.transform,false);
            volume.transform.localPosition = new Vector3(0,-69000,48000);
            volume.transform.localRotation = Quaternion.Euler(55,0,0);
            volume.transform.localScale = new Vector3(85000,60000,22000);
            var mesh = volume.AddComponent<MeshFilter>();
            mesh.sharedMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            var renderer = volume.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = materials[0];
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            var selector = root.AddComponent<MatchNebula>();
            var serialized = new SerializedObject(selector);
            serialized.FindProperty("volumeRenderer").objectReferenceValue = renderer;
            var variants = serialized.FindProperty("variants");
            variants.arraySize = materials.Length;
            for (int i = 0; i < materials.Length; i++) variants.GetArrayElementAtIndex(i).objectReferenceValue = materials[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root,CLOUD_PREFAB);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void BuildStars()
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(MATERIAL_FOLDER + "StarMaterial.mat");
        material.shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Art/Shaders/Vfx/NebulaStars.shader");
        material.renderQueue = 2550;
        EditorUtility.SetDirty(material);
        const string PREFAB = "Assets/Prefabs/Vfx/SkirmishVfx.prefab";
        var root = PrefabUtility.LoadPrefabContents(PREFAB);
        try
        {
            var bright = root.transform.Find("BrightStars");
            if (bright != null) UnityEngine.Object.DestroyImmediate(bright.gameObject);
            foreach (var stars in root.GetComponentsInChildren<ParticleSystem>(true))
            {
                if (stars.name != "StarVfx") continue;
                stars.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                stars.transform.localScale = Vector3.one;
                stars.transform.localPosition = new Vector3(0,-100000,60000);
                var main = stars.main;
                main.loop = false;
                main.prewarm = false;
                main.startLifetime = 100000000;
                main.startSpeed = 0;
                main.startSize3D = false;
                main.startSize = new ParticleSystem.MinMaxCurve(160,500);
                main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.65f,0.78f,1f), new Color(1f,0.94f,0.84f));
                main.maxParticles = 6500;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
                var emission = stars.emission;
                emission.rateOverTime = 0;
                emission.SetBursts(new[] { new ParticleSystem.Burst(0,6500) });
                var shape = stars.shape;
                shape.enabled = true;
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.position = Vector3.zero;
                shape.rotation = Vector3.zero;
                shape.scale = new Vector3(180000,14000,130000);
                var color = stars.colorOverLifetime; color.enabled = false;
                var colorSpeed = stars.colorBySpeed; colorSpeed.enabled = false;
                var size = stars.sizeOverLifetime; size.enabled = false;
                var sizeSpeed = stars.sizeBySpeed; sizeSpeed.enabled = false;
                var velocity = stars.velocityOverLifetime; velocity.enabled = false;
                var noise = stars.noise; noise.enabled = false;
                var sheet = stars.textureSheetAnimation; sheet.enabled = false;
                stars.useAutoRandomSeed = true;
                var brightStars = UnityEngine.Object.Instantiate(stars, root.transform);
                brightStars.name = "BrightStars";
                var brightMain = brightStars.main;
                brightMain.startSize = new ParticleSystem.MinMaxCurve(900,2000);
                brightMain.maxParticles = 60;
                var brightEmission = brightStars.emission;
                brightEmission.SetBursts(new[] { new ParticleSystem.Burst(0,60) });
            }
            PrefabUtility.SaveAsPrefabAsset(root,PREFAB);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
}
