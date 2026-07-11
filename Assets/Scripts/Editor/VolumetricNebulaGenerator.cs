using System.IO;
using UnityEditor;
using UnityEngine;

public class VolumetricNebulaGenerator
{
    [MenuItem("Tools/Volumetric Nebula/Setup Full Vfx")]
    public static void SetupFullVfx()
    {
        string dirTextures = "Assets/Graphics/Textures/Particles";
        string dirShaders = "Assets/Graphics/Shaders/Vfx";
        string dirMaterials = "Assets/Graphics/Materials/Vfx/Nebula";
        string dirPrefabs = "Assets/Prefabs/Vfx";

        if (!Directory.Exists(dirTextures)) Directory.CreateDirectory(dirTextures);
        if (!Directory.Exists(dirMaterials)) Directory.CreateDirectory(dirMaterials);
        if (!Directory.Exists(dirPrefabs)) Directory.CreateDirectory(dirPrefabs);

        // 1. Ensure Texture3D exists
        string texPath = dirTextures + "/NebulaVolumeNoise3D.asset";
        Texture3D tex3D = AssetDatabase.LoadAssetAtPath<Texture3D>(texPath);
        if (tex3D == null)
        {
            tex3D = GenerateNoiseTexture3D(32);
            AssetDatabase.CreateAsset(tex3D, texPath);
            Debug.Log("Created 3D Noise Texture at " + texPath);
        }

        // 2. Locate Shader
        string shaderPath = dirShaders + "/VolumetricNebulaRaymarch.shader";
        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
        if (shader == null)
        {
            Debug.LogError("Could not find shader at " + shaderPath + ". Please ensure the shader file is present.");
            return;
        }

        // 3. Create Material
        string matPath = dirMaterials + "/VolumetricNebulaSystemMat.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            mat = new Material(shader);
            mat.SetTexture("_NoiseTex3D", tex3D);
            mat.SetColor("_MainColor", new Color(0.2f, 0.4f, 1.0f, 1.0f));
            mat.SetColor("_CoreColor", new Color(0.6f, 0.9f, 1.0f, 1.0f));
            AssetDatabase.CreateAsset(mat, matPath);
            Debug.Log("Created Material at " + matPath);
        }

        // 4. Create or Update Particle System Prefab
        string prefabPath = dirPrefabs + "/VolumetricNebulaSystem.prefab";
        GameObject go;
        bool isNew = false;

        GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (existingPrefab != null)
        {
            go = (GameObject)PrefabUtility.InstantiatePrefab(existingPrefab);
        }
        else
        {
            go = new GameObject("VolumetricNebulaSystem");
            go.AddComponent<ParticleSystem>();
            isNew = true;
        }

        ParticleSystem ps = go.GetComponent<ParticleSystem>();

        var main = ps.main;
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 1f, 1f, 0.8f),
            new Color(0.8f, 0.9f, 1f, 0.5f)
        );
        main.startSize = new ParticleSystem.MinMaxCurve(20f, 60f); // Large overlapping volumes
        main.startLifetime = new ParticleSystem.MinMaxCurve(60f, 120f); // Long lived
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.5f); // Slow drifting
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f); // Random orientation
        main.maxParticles = 500;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 5f;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 20f) }); // Initial burst for immediate cloud

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 30f; // Wide area to form a massive cloud

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(Color.white, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.0f, 0.0f), new GradientAlphaKey(1.0f, 0.2f), new GradientAlphaKey(1.0f, 0.8f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        colorOverLifetime.color = grad;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0.0f, 0.5f);
        curve.AddKey(0.5f, 1.0f);
        curve.AddKey(1.0f, 1.2f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.0f, curve);

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = mat;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortMode = ParticleSystemSortMode.Distance;

        PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        GameObject.DestroyImmediate(go);

        if (isNew)
        {
            Debug.Log("Created Particle System Prefab at " + prefabPath);
        }
        else
        {
            Debug.Log("Updated Particle System Prefab at " + prefabPath);
        }

        AssetDatabase.Refresh();
        Debug.Log("Volumetric Nebula Setup Complete!");
    }

    private static Texture3D GenerateNoiseTexture3D(int size)
    {
        Texture3D tex = new Texture3D(size, size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;

        Color[] colors = new Color[size * size * size];

        for (int z = 0; z < size; z++)
        {
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float fx = (float)x / size;
                    float fy = (float)y / size;
                    float fz = (float)z / size;

                    float noise = Perlin3D(fx * 4f, fy * 4f, fz * 4f) * 0.5f
                                + Perlin3D(fx * 8f, fy * 8f, fz * 8f) * 0.25f
                                + Perlin3D(fx * 16f, fy * 16f, fz * 16f) * 0.125f;

                    noise = Mathf.Clamp01(noise + 0.5f);
                    colors[x + y * size + z * size * size] = new Color(noise, noise, noise, noise);
                }
            }
        }

        tex.SetPixels(colors);
        tex.Apply();
        return tex;
    }

    private static float Perlin3D(float x, float y, float z)
    {
        float ab = Mathf.PerlinNoise(x, y);
        float bc = Mathf.PerlinNoise(y, z);
        float ac = Mathf.PerlinNoise(x, z);
        float ba = Mathf.PerlinNoise(y, x);
        float cb = Mathf.PerlinNoise(z, y);
        float ca = Mathf.PerlinNoise(z, x);
        return (ab + bc + ac + ba + cb + ca) / 6f - 0.5f;
    }
}
