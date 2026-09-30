using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

public static class InspectParticles
{
    public static object Main()
    {
        var particles = new List<object>();
        var materials = new Dictionary<string, object>();
        var prefabs = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Vfx", "Assets/Prefabs/Models" })
            .Select(AssetDatabase.GUIDToAssetPath).OrderBy(path => path).ToArray();
        foreach (var path in prefabs)
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var pending = new Stack<Transform>();
            pending.Push(root.transform);
            while (pending.Count > 0)
            {
                var transform = pending.Pop();
                for (var child = 0; child < transform.childCount; child++) pending.Push(transform.GetChild(child));
                using var serialized = new SerializedObject(transform.gameObject);
                var components = serialized.FindProperty("m_Component");
                ParticleSystem system = null;
                ParticleSystemRenderer renderer = null;
                for (var index = 0; index < components.arraySize; index++)
                {
                    var component = components.GetArrayElementAtIndex(index).FindPropertyRelative("component").objectReferenceValue;
                    if (component is ParticleSystem foundSystem) system = foundSystem;
                    if (component is ParticleSystemRenderer foundRenderer) renderer = foundRenderer;
                }
                if (system == null) continue;
                var main = system.main;
                var emission = system.emission;
                var bursts = new ParticleSystem.Burst[emission.burstCount];
                emission.GetBursts(bursts);
                var materialPaths = new List<string>();
                if (renderer != null)
                {
                    foreach (var material in renderer.sharedMaterials)
                    {
                        if (material == null) { materialPaths.Add(null); continue; }
                        var materialPath = AssetDatabase.GetAssetPath(material);
                        materialPaths.Add(materialPath);
                        if (materials.ContainsKey(materialPath)) continue;
                        var textures = material.GetTexturePropertyNames().ToDictionary(name => name,
                            name => AssetDatabase.GetAssetPath(material.GetTexture(name)));
                        var propertyNames = new[] { "_Surface", "_Blend", "_SrcBlend", "_DstBlend", "_ZWrite", "_SoftParticlesEnabled", "_CameraFadingEnabled", "_FlipbookBlending" };
                        materials.Add(materialPath, new {
                            path = materialPath, name = material.name, shader = material.shader.name,
                            render_queue = material.renderQueue, instancing = material.enableInstancing,
                            keywords = material.shaderKeywords, textures,
                            properties = propertyNames.Where(material.HasProperty).ToDictionary(name => name, material.GetFloat)
                        });
                    }
                }
                var hierarchy = transform.name;
                for (var parent = transform.parent; parent != null; parent = parent.parent) hierarchy = parent.name + "/" + hierarchy;
                particles.Add(new {
                    prefab = path, hierarchy, active_self = transform.gameObject.activeSelf,
                    loop = main.loop, prewarm = main.prewarm, play_on_awake = main.playOnAwake, duration = main.duration,
                    max_particles = main.maxParticles, lifetime = Curve(main.startLifetime), size = Curve(main.startSize),
                    speed = Curve(main.startSpeed), simulation_space = main.simulationSpace.ToString(), culling_mode = main.cullingMode.ToString(),
                    emission_enabled = emission.enabled, rate_time = Curve(emission.rateOverTime), rate_distance = Curve(emission.rateOverDistance),
                    bursts = bursts.Select(burst => new { time = burst.time, count = Curve(burst.count), cycles = burst.cycleCount, interval = burst.repeatInterval, probability = burst.probability }).ToArray(),
                    trails = system.trails.enabled, lights = system.lights.enabled, collision = system.collision.enabled,
                    texture_sheet = system.textureSheetAnimation.enabled,
                    renderer = renderer == null ? null : new {
                        enabled = renderer.enabled, mode = renderer.renderMode.ToString(), sort = renderer.sortMode.ToString(),
                        gpu_instancing = renderer.enableGPUInstancing, max_particle_size = renderer.maxParticleSize,
                        shadow_casting = renderer.shadowCastingMode.ToString(), receive_shadows = renderer.receiveShadows,
                        materials = materialPaths, trail_material = AssetDatabase.GetAssetPath(renderer.trailMaterial)
                    }
                });
            }
        }
        var outputPath = "output/RenderAuditReview_20260930/particle-inventory.json";
        File.WriteAllText(outputPath, JsonConvert.SerializeObject(new { prefabs_scanned = prefabs.Length, particles, materials = materials.Values }, Formatting.Indented));
        return new { prefabs_scanned = prefabs.Length, particle_systems = particles.Count, materials = materials.Count, outputPath };
    }

    private static object Curve(ParticleSystem.MinMaxCurve curve)
    {
        return new { mode = curve.mode.ToString(), min = curve.constantMin, max = curve.constantMax, multiplier = curve.curveMultiplier };
    }
}
