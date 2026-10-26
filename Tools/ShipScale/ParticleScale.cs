using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

public static class ParticleScale
{
    // Call once on a loaded prefab after its root receives the uniform multiplier.
    public static void Apply(GameObject root, float multiplier)
    {
        foreach (ParticleSystem particle in root.GetComponentsInChildren<ParticleSystem>(true))
        {
            ParticleSystemScalingMode mode = particle.main.scalingMode;
            if (mode == ParticleSystemScalingMode.Hierarchy) continue;
            if (mode != ParticleSystemScalingMode.Local || particle.transform.childCount != 0 ||
                particle.GetComponents<Component>().Any(component => !(component is Transform) &&
                    !(component is ParticleSystem) && !(component is ParticleSystemRenderer)))
                throw new InvalidOperationException("Unsupported particle scale hierarchy: " + particle.name);
            particle.transform.localScale *= multiplier;
        }
    }

    public static string VerifyLocalScale()
    {
        var results = new List<object>();
        GameObject cameraObject = new GameObject("ParticleScaleCamera") { hideFlags = HideFlags.HideAndDontSave };
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.enabled = false;
        camera.transform.position = new Vector3(0, 0, -1000);
        try
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Models/Ships" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                foreach (ParticleSystem source in prefab.GetComponentsInChildren<ParticleSystem>(true).Where(p => p.main.scalingMode == ParticleSystemScalingMode.Local))
                {
                    Vector3[] baseline = Bake(source, camera, 1f, false);
                    foreach (float factor in new[] { 0.3f, 1.5f })
                    {
                        Vector3[] scaled = Bake(source, camera, factor, true);
                        if (scaled.Length != baseline.Length || scaled.Length == 0)
                            throw new Exception("Particle count mismatch: " + path + " baseline=" + baseline.Length + " scaled=" + scaled.Length);
                        float error = 0;
                        for (int i = 0; i < scaled.Length; i++)
                            error = Mathf.Max(error, (scaled[i] - baseline[i] * factor).magnitude);
                        if (error > 0.001f)
                            throw new Exception("Particle geometry scaling mismatch: " + path + "/" + source.name + " error=" + error);
                        results.Add(new { path, particle = source.name, factor, vertices = scaled.Length, maxError = error });
                    }
                }
            }
            File.WriteAllText("Logs/ShipScale/particle-scaling-verification.json", JsonConvert.SerializeObject(results, Formatting.Indented));
            return "Verified " + results.Count + " particle scale comparisons; inspect particle-scaling-verification.json";
        }
        finally { UnityEngine.Object.DestroyImmediate(cameraObject); }
    }

    private static Vector3[] Bake(ParticleSystem source, Camera camera, float factor, bool compensate)
    {
        GameObject parent = new GameObject("ParticleScaleProbe") { hideFlags = HideFlags.HideAndDontSave };
        Mesh mesh = new Mesh();
        try
        {
            parent.transform.localScale = source.transform.parent.lossyScale * factor;
            ParticleSystem particle = UnityEngine.Object.Instantiate(source, parent.transform, false);
            particle.gameObject.hideFlags = HideFlags.HideAndDontSave;
            particle.transform.localPosition = Vector3.zero;
            particle.transform.localRotation = Quaternion.identity;
            particle.transform.localScale = source.transform.localScale * (compensate ? factor : 1f);
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particle.useAutoRandomSeed = false;
            particle.randomSeed = 12345;
            particle.gameObject.SetActive(true);
            var emission = particle.emission;
            emission.enabled = false;
            particle.Emit(32);
            particle.Simulate(0.5f, true, false, true);
            particle.GetComponent<ParticleSystemRenderer>().BakeMesh(mesh, camera, ParticleSystemBakeMeshOptions.BakeRotationAndScale);
            return mesh.vertices;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(parent);
            UnityEngine.Object.DestroyImmediate(mesh);
        }
    }

    public static string Inventory()
    {
        var rows = new List<object>();
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Models/Ships" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            foreach (ParticleSystem particle in root.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = particle.main;
                var shape = particle.shape;
                rows.Add(new
                {
                    path, particle = AnimationUtility.CalculateTransformPath(particle.transform, root.transform),
                    mode = main.scalingMode.ToString(), space = main.simulationSpace.ToString(),
                    localScale = particle.transform.localScale.ToString("R"), lossyScale = particle.transform.lossyScale.ToString("R"),
                    children = particle.transform.childCount,
                    components = particle.GetComponents<Component>().Select(component => component.GetType().Name).ToArray(),
                    shape = shape.enabled, shapeType = shape.shapeType.ToString(), shapeScale = shape.scale.ToString("R"),
                    shapePosition = shape.position.ToString("R"), shapeMesh = shape.mesh == null ? "" : AssetDatabase.GetAssetPath(shape.mesh),
                    startSize3D = main.startSize3D, startSize = main.startSizeMultiplier, startSpeed = main.startSpeedMultiplier,
                    gravity = main.gravityModifierMultiplier, velocity = particle.velocityOverLifetime.enabled,
                    force = particle.forceOverLifetime.enabled, noise = particle.noise.enabled,
                    limit = particle.limitVelocityOverLifetime.enabled, inherit = particle.inheritVelocity.enabled,
                    sizeBySpeed = particle.sizeBySpeed.enabled, collision = particle.collision.enabled,
                    trails = particle.trails.enabled, lights = particle.lights.enabled,
                    emissionDistance = particle.emission.rateOverDistanceMultiplier
                });
            }
        }
        Directory.CreateDirectory("Logs/ShipScale");
        File.WriteAllText("Logs/ShipScale/particles.json", JsonConvert.SerializeObject(rows, Formatting.Indented));
        return "Read-only inventory: " + rows.Count + " particles; Logs/ShipScale/particles.json";
    }
}
