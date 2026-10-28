using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Editor.AloImport
{
    public static class AloPreviewRenderer
    {
        private const int PREVIEW_LAYER = 30;
        private const float NO_CUT = 1000000f;

        public static JObject Render(JObject recipe, string jobDirectory)
        {
            var generation = (JObject)recipe["derivatives"]["generation"];
            if (generation == null) throw new InvalidOperationException("Rendering requires explicit derivatives.generation geometry and camera decisions.");
            var scene = EditorSceneManager.NewPreviewScene();
            var materials = new List<Material>();
            RenderTexture previous = RenderTexture.active;
            RenderTexture target = null;
            Texture2D pixels = null;
            try
            {
                using var geometry = new AloGeometrySource(recipe, scene);
                foreach (MeshRenderer renderer in geometry.Renderers) renderer.gameObject.layer = PREVIEW_LAYER;
                Material[][] originals = geometry.Renderers.Select(renderer => renderer.sharedMaterials).ToArray();
                Vector4[] palette = generation["palette"].Select(color => (Vector4)new Color((float)color[0], (float)color[1], (float)color[2], (float)color[3]).linear).ToArray();
                for (int index = 0; index < geometry.Renderers.Count; index++)
                    geometry.Renderers[index].sharedMaterials = originals[index].Select(source => Clone(source, materials, palette)).ToArray();
                var cameraObject = new GameObject("AloPreviewCamera");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject, scene);
                var camera = cameraObject.AddComponent<Camera>();
                camera.scene = scene;
                camera.enabled = false;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.clear;
                camera.cullingMask = 1 << PREVIEW_LAYER;
                camera.orthographic = true;
                camera.aspect = 1;
                camera.transform.rotation = Quaternion.Euler(Vector(generation["viewEulerDegrees"]));
                Bounds bounds = geometry.Bounds;
                float margin = (float)recipe["derivatives"]["icon"]["framingMargin"];
                if (margin < 0 || margin >= .5f) throw new InvalidOperationException("Icon framing margin must be in [0, 0.5).");
                Vector3 extents = Vector3.zero;
                Quaternion inverse = Quaternion.Inverse(camera.transform.rotation);
                for (int mask = 0; mask < 8; mask++)
                {
                    Vector3 corner = new Vector3((mask & 1) == 0 ? -bounds.extents.x : bounds.extents.x,
                        (mask & 2) == 0 ? -bounds.extents.y : bounds.extents.y, (mask & 4) == 0 ? -bounds.extents.z : bounds.extents.z);
                    Vector3 projected = inverse * corner;
                    extents = Vector3.Max(extents, new Vector3(Mathf.Abs(projected.x), Mathf.Abs(projected.y), Mathf.Abs(projected.z)));
                }
                camera.orthographicSize = Mathf.Max(extents.x, extents.y) / (1 - 2 * margin);
                float distance = bounds.size.magnitude * 2 + 1;
                camera.transform.position = bounds.center - camera.transform.forward * distance;
                camera.nearClipPlane = .01f;
                camera.farClipPlane = distance * 3;
                Light(scene, new Vector3(35, 30, 0), 1.6f);
                Light(scene, new Vector3(-20, -130, 0), .7f);
                int resolution = (int)recipe["derivatives"]["icon"]["resolution"];
                target = RenderTexture.GetTemporary(resolution, resolution, 24, RenderTextureFormat.ARGB32);
                camera.targetTexture = target;
                pixels = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
                string directory = Path.Combine(jobDirectory, "Evidence", "Previews");
                Directory.CreateDirectory(directory);
                var reports = new JArray();
                Save(camera, target, pixels, Path.Combine(directory, "Model.png"), reports);
                for (int team = 0; team < 8; team++)
                {
                    foreach (MeshRenderer renderer in geometry.Renderers) renderer.SetShaderUserValue((uint)(team + 1));
                    Save(camera, target, pixels, Path.Combine(directory, "ModelTeam" + team + ".png"), reports);
                }
                foreach (MeshRenderer renderer in geometry.Renderers) renderer.SetShaderUserValue(0);
                if (generation["placementRole"] != null)
                {
                    Material hologram = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath((string)generation["hologramMaterialGuid"]));
                    if (hologram == null) throw new InvalidOperationException("Missing explicit hologram material.");
                    foreach (MeshRenderer renderer in geometry.Renderers) renderer.sharedMaterials = Enumerable.Repeat(hologram, renderer.sharedMaterials.Length).ToArray();
                    Save(camera, target, pixels, Path.Combine(directory, "Placement.png"), reports);
                }
                if (generation["wreckRole"] != null)
                {
                    Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath((string)generation["wreckShaderGuid"]));
                    if (shader == null) throw new InvalidOperationException("Missing explicit wreck shader.");
                    for (int index = 0; index < geometry.Renderers.Count; index++)
                    {
                        MeshRenderer renderer = geometry.Renderers[index];
                        renderer.enabled &= originals[index].All(material => material.shader.name == "EmpireAtWar/Ship Lit");
                        renderer.sharedMaterials = originals[index].Select(source =>
                        {
                            Material material = Clone(source, materials, palette);
                            material.shader = shader;
                            material.SetVector("_WreckCuts", Vector4.one * NO_CUT);
                            material.SetFloat("_WreckLifetime", NO_CUT);
                            material.SetFloat("_WreckSeparationSpeed", 0);
                            material.SetFloat("_WreckTiltSpeed", 0);
                            material.SetFloat("_WreckSinkSpeed", 0);
                            return material;
                        }).ToArray();
                    }
                    for (int team = 0; team < 8; team++)
                    {
                        foreach (MeshRenderer renderer in geometry.Renderers) renderer.SetShaderUserValue((uint)(team + 1));
                        Save(camera, target, pixels, Path.Combine(directory, "WreckTeam" + team + ".png"), reports);
                    }
                }
                return new JObject { ["images"] = reports, ["visualReview"] = "needs-review", ["runtime"] = "not-run" };
            }
            finally
            {
                RenderTexture.active = previous;
                if (target != null) RenderTexture.ReleaseTemporary(target);
                if (pixels != null) Object.DestroyImmediate(pixels);
                foreach (Material material in materials) Object.DestroyImmediate(material);
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        private static Material Clone(Material source, List<Material> materials, Vector4[] palette)
        {
            var clone = new Material(source);
            clone.SetVectorArray("_TeamColors", palette);
            materials.Add(clone);
            return clone;
        }

        private static void Light(UnityEngine.SceneManagement.Scene scene, Vector3 rotation, float intensity)
        {
            var root = new GameObject("AloPreviewLight");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            var light = root.AddComponent<UnityEngine.Light>();
            light.type = LightType.Directional;
            light.intensity = intensity;
            light.cullingMask = 1 << PREVIEW_LAYER;
            root.transform.rotation = Quaternion.Euler(rotation);
        }

        private static void Save(Camera camera, RenderTexture target, Texture2D pixels, string path, JArray reports)
        {
            camera.Render();
            RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0, 0, pixels.width, pixels.height), 0, 0);
            pixels.Apply();
            byte[] png = pixels.EncodeToPNG();
            File.WriteAllBytes(path, png);
            using var sha = SHA256.Create();
            reports.Add(new JObject { ["path"] = path, ["sha256"] = BitConverter.ToString(sha.ComputeHash(png)).Replace("-", "").ToLowerInvariant() });
        }

        private static Vector3 Vector(JToken values) => new Vector3((float)values[0], (float)values[1], (float)values[2]);
    }
}
