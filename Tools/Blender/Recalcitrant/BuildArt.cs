using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BuildRecalcitrantArt
{
    const string TASK = "Temp/RecalcitrantImport/";
    const string MODELS = "Assets/Art/Models/SeparatistShips/Recalcitrant/";
    const string TEXTURES = "Assets/Art/Textures/Models/SeparatistShips/Recalcitrant/";
    const string MATERIALS = "Assets/Art/Materials/Models/SeparatistShips/Recalcitrant/";
    const string VISUAL = "Assets/Prefabs/Models/Ships/Separatist/Recalcitrant.prefab";
    const float GAMEPLAY_LENGTH = 100f;

    public static string Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Recalcitrant art construction requires Edit Mode.");
        AssetDatabase.Refresh();
        EnsureFolder(MATERIALS.TrimEnd('/'));
        var report = JObject.Parse(File.ReadAllText(TASK + "ConversionReport.json"));
        var normals = report["materials"].Select(m => (string)m["normal"]).Where(n => n != null).Select(n => TEXTURES + n + ".png").Distinct().ToArray();
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] {TEXTURES.TrimEnd('/')}))
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
            bool normal = normals.Contains(importer.assetPath);
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = false;
            importer.sRGBTexture = !normal;
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            if (normal) importer.flipGreenChannel = true;
            importer.SaveAndReimport();
        }
        foreach (var row in report["materials"])
        {
            string name = (string)row["name"], shader = (string)row["shader"];
            string baseMap = (string)row["base"], normalMap = (string)row["normal"];
            // The visible Engine_Glow uses EaW MeshShield; retain its base glow as additive art.
            bool additive = shader.Contains("Additive") || shader == "MeshShield.fx";
            string path = MATERIALS + name + ".mat";
            var unityShader = Shader.Find(additive ? "Universal Render Pipeline/Particles/Unlit" : baseMap != null ? "EmpireAtWar/Ship Lit" : "Universal Render Pipeline/Unlit");
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(unityShader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = unityShader;
            if (baseMap != null) material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURES + baseMap + ".png"));
            material.SetColor("_BaseColor", Color.white);
            if (normalMap != null)
            {
                material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURES + normalMap + ".png"));
                material.EnableKeyword("_NORMALMAP");
            }
            if (additive)
            {
                material.SetFloat("_Surface", 1); material.SetFloat("_Blend", 2);
                material.SetFloat("_SrcBlend", 1); material.SetFloat("_DstBlend", 1); material.SetFloat("_ZWrite", 0);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.renderQueue = 3000;
                if (row["properties"]["Color"] is JArray tint) material.SetColor("_BaseColor", new Color((float)tint[0], (float)tint[1], (float)tint[2], 1));
            }
            if (material.HasProperty("_TeamLiveryStrength"))
            {
                // Source textures have blue hull paint and purple turret accents; keep warm panels neutral.
                material.SetFloat("_TeamMaskStrength", 0);
                material.SetFloat("_TeamLiveryHue", baseMap != null && baseMap.Contains("LC_Turret") ? .75f : .60f);
                material.SetFloat("_TeamLiveryHueRange", .07f);
                material.SetFloat("_TeamLiveryMinSaturation", .2f);
                material.SetFloat("_TeamLiveryStrength", shader.Contains("Colorize") ? 1 : 0);
                material.SetFloat("_Metallic", .1f); material.SetFloat("_Smoothness", .3f);
            }
            EditorUtility.SetDirty(material);
        }
        foreach (var record in ((JObject)report["models"]).Properties())
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(MODELS + record.Name + ".fbx");
            importer.globalScale = .02f;
            importer.preserveHierarchy = true;
            importer.importAnimation = false;
            importer.isReadable = true;
            importer.weldVertices = false;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            foreach (var key in importer.GetExternalObjectMap().Keys.Where(k => k.type == typeof(Material)).ToArray()) importer.RemoveRemap(key);
            foreach (var row in record.Value["materials"])
            {
                string name = (string)row["name"];
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), name), AssetDatabase.LoadAssetAtPath<Material>(MATERIALS + name + ".mat"));
            }
            importer.SaveAndReimport();
        }
        var scene = EditorSceneManager.NewPreviewScene();
        var root = new GameObject("Recalcitrant");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
        try
        {
            var model = ImportModel("Recalcitrant", root.transform, report);
            for (int index = 1; index <= 8; index++)
            {
                var socket = model.GetComponentsInChildren<Transform>(true).Single(t => t.name == "TL_0" + index);
                var turret = ImportModel("RecalcitrantTurret0" + index, socket, report);
                turret.transform.localPosition = Vector3.zero;
                // Cancel the FBX axis/unit conversion before applying the source attachment frame.
                turret.transform.localRotation = Quaternion.Inverse(turret.transform.GetChild(0).localRotation);
                turret.transform.localScale = Vector3.one / turret.transform.GetChild(0).localScale.x;
            }
            var front = model.GetComponentsInChildren<Transform>(true).Single(t => t.name == "IC_01");
            var engine = model.GetComponentsInChildren<Transform>(true).Single(t => t.name == "HP_E");
            if (front.position.z < engine.position.z) model.transform.localRotation = Quaternion.Euler(0, 180, 0) * model.transform.localRotation;
            var bounds = BoundsOf(root);
            float scale = GAMEPLAY_LENGTH / bounds.size.z;
            model.transform.localScale *= scale;
            model.transform.localPosition = -bounds.center * scale;
            bounds = BoundsOf(root);
            PrefabUtility.SaveAsPrefabAsset(root, VISUAL);
            AssetDatabase.SaveAssets();
            File.WriteAllText(TASK + "ArtReport.json", new JObject {
                ["size"] = new JArray(bounds.size.x, bounds.size.y, bounds.size.z),
                ["center"] = new JArray(bounds.center.x, bounds.center.y, bounds.center.z),
                ["gameplayLength"] = GAMEPLAY_LENGTH, ["modelScale"] = scale,
                ["scaleBasis"] = "Provisional 100-unit light cruiser length; between Wavecrest (90) and Recusant (124.6333); no verified physical length.",
                ["visibleMeshes"] = root.GetComponentsInChildren<MeshRenderer>(true).Count(r => r.enabled),
                ["frontZ"] = front.position.z, ["engineZ"] = engine.position.z,
                ["weaponFacing"] = WeaponFacing(model, root.transform),
                ["sockets"] = new JObject(model.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("HP_") || t.name.StartsWith("TL_") || t.name.StartsWith("IC_")).GroupBy(t => t.name).Select(group => group.First()).Select(t => new JProperty(t.name, new JObject {
                    ["position"] = new JArray(t.position.x,t.position.y,t.position.z),
                    ["forward"] = new JArray(t.forward.x,t.forward.y,t.forward.z),
                    ["up"] = new JArray(t.up.x,t.up.y,t.up.z)
                })))
            }.ToString());
            return "Saved Recalcitrant hull, eight source-mounted turrets and materials; bounds " + bounds.size;
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    static JObject WeaponFacing(GameObject model, Transform root)
    {
        var result = new JObject();
        var bones = model.GetComponentsInChildren<Transform>(true);
        for (int index = 1; index <= 8; index++)
        {
            var socket = bones.Single(t => t.name == "TL_" + index.ToString("00"));
            var turret = socket.Cast<Transform>().Single(t => t.name == "RecalcitrantTurret" + index.ToString("00"));
            var parts = turret.GetComponentsInChildren<Transform>(true);
            var barrel = parts.Single(t => t.name == "Barrel" && t.Cast<Transform>().Any(child => child.name == "FP_01"));
            var muzzle = (parts.Single(t => t.name == "FP_01").position + parts.Single(t => t.name == "FP_02").position) * .5f;
            result[socket.name] = Facing(root.InverseTransformVector(muzzle - barrel.position), "Source Barrel to FP_01/FP_02 midpoint");
        }
        var ion = bones.Single(t => t.name == "IC_01");
        var ionMuzzle = (bones.Single(t => t.name == "IC_01_FP_01").position + bones.Single(t => t.name == "IC_01_FP_02").position) * .5f;
        result[ion.name] = Facing(root.InverseTransformVector(ionMuzzle - ion.position), "Source IC_01 to muzzle midpoint");
        for (int index = 1; index <= 6; index++)
        {
            var socket = bones.Single(t => t.name == "HP_" + index.ToString("00"));
            result[socket.name] = Facing(root.InverseTransformPoint(socket.position), "Provisional outward bearing from centered hull; no separate missile muzzle");
        }
        return result;
    }

    static JObject Facing(Vector3 direction, string basis)
    {
        direction.y = 0;
        direction.Normalize();
        return new JObject { ["yaw"] = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg,
            ["direction"] = new JArray(direction.x, direction.y, direction.z), ["basis"] = basis };
    }

    static GameObject ImportModel(string name, Transform parent, JObject report)
    {
        var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(MODELS + name + ".fbx"), parent);
        PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        foreach (var lod in model.GetComponentsInChildren<LODGroup>(true)) UnityEngine.Object.DestroyImmediate(lod);
        foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
        {
            var record = ((JObject)report["models"][name]["before"]["meshes"]).Properties().Single(row => (string)row.Value["exported_name"] == renderer.name);
            bool helper = renderer.sharedMaterials.Any(m => m.name.Contains("collision") || m.name.Contains("shadow"));
            if ((bool)record.Value["hidden"] || helper)
            {
                foreach (var filter in renderer.GetComponents<MeshFilter>()) UnityEngine.Object.DestroyImmediate(filter);
                UnityEngine.Object.DestroyImmediate(renderer);
                continue;
            }
            renderer.name = record.Name;
            renderer.enabled = true;
            if (!(renderer is SkinnedMeshRenderer skinned)) continue;
            var mesh = new Mesh(); skinned.BakeMesh(mesh, false);
            string meshPath = MODELS + name + "_" + record.Name + "Mesh.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (existing == null) AssetDatabase.CreateAsset(mesh, meshPath);
            else { EditorUtility.CopySerialized(mesh, existing); UnityEngine.Object.DestroyImmediate(mesh); mesh = existing; EditorUtility.SetDirty(mesh); }
            var owner = renderer.gameObject; var materials = renderer.sharedMaterials;
            UnityEngine.Object.DestroyImmediate(renderer);
            owner.AddComponent<MeshFilter>().sharedMesh = mesh;
            owner.AddComponent<MeshRenderer>().sharedMaterials = materials;
        }
        return model;
    }

    static Bounds BoundsOf(GameObject root)
    {
        var points = root.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled && r.sharedMaterials.All(m => m.shader.name == "EmpireAtWar/Ship Lit"))
            .SelectMany(r => r.GetComponents<MeshFilter>().Single().sharedMesh.vertices.Select(v => root.transform.InverseTransformPoint(r.transform.TransformPoint(v)))).ToArray();
        var bounds = new Bounds(points[0], Vector3.zero);
        foreach (var point in points) bounds.Encapsulate(point);
        return bounds;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
        AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\', '/'), Path.GetFileName(path));
    }
}
