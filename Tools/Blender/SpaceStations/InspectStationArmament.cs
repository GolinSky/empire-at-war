using System;
using System.IO;
using System.Linq;
using EmpireAtWar.ViewComponents.Health;
using EmpireAtWar.ViewComponents.Station;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// Saved-prefab inspection and preview capture; never opens or changes the user's scenes.
public static class InspectStationArmament
{
    private const string OUTPUT = "Temp/StationArmament/";
    private static readonly int[] _counts = { 1, 2, 4, 6, 8 };
    private static readonly Color[] _markers = { Color.red, Color.green, Color.yellow, Color.cyan, Color.magenta };

    public static string Main(string faction)
    {
        Directory.CreateDirectory(OUTPUT);
        var config = JObject.Parse(File.ReadAllText("Tools/Blender/SpaceStations/" + faction + ".json"));
        var view = AssetDatabase.LoadAssetAtPath<GameObject>((string)config["view"]);
        var points = view.GetComponentsInChildren<HardPoint>(true).OrderBy(h => h.Id).ToArray();
        var weapons = points.OfType<WeaponHardPoint>().ToArray();
        Require(points.Length == 41 && points.Select(h => h.Id).Distinct().Count() == 41, "41 unique hardpoints");
        Require(weapons.Length == 40 && weapons.GroupBy(h => h.WeaponType).Count() == 5, "40 weapons across five types");
        var components = view.GetComponentsInChildren<MonoBehaviour>(true);
        RequireArray(components.Single(c => c.GetType().Name == "HealthComponent"), "<ShipUnits>k__BackingField", points);
        RequireArray(components.Single(c => c.GetType().Name == "WeaponComponent"), "hardPoints", weapons);
        RequireArray(view.GetComponent<StationLevelView>(), "hardPoints", points);
        var injection = new SerializedObject(components.Single(c => c.GetType().Name == "EntityBindings")).FindProperty("<Injectables>k__BackingField");
        Require(points.All(h => Enumerable.Range(0, injection.arraySize).Any(i => injection.GetArrayElementAtIndex(i).objectReferenceValue == h)), "injected hardpoints");
        foreach (var hardPoint in points.Where(h => h.Id >= 17))
        {
            var anchor = (Transform)new SerializedObject(hardPoint).FindProperty("destroyedVfxAnchor").objectReferenceValue;
            Require(anchor != null && anchor.IsChildOf(hardPoint.transform) && anchor.localPosition.sqrMagnitude < .000001f, "local destruction anchor " + hardPoint.Id);
        }
        var types = weapons.Select(h => h.WeaponType).Distinct().OrderBy(t => (int)t).ToArray();
        var legend = new JObject();
        for (int index = 0; index < types.Length; index++) legend[types[index].ToString()] = "#" + ColorUtility.ToHtmlStringRGB(_markers[index]);
        legend["ShieldGenerator"] = "#FFFFFF";
        var report = new JObject { ["faction"] = faction, ["hardpoints"] = points.Length, ["weapons"] = weapons.Length, ["legend"] = legend, ["levels"] = new JArray() };
        for (int level = 1; level <= 5; level++)
        {
            string path = (string)config["levels"][level - 1];
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<StationLevelModel>();
            int[] installed = points.Where(h => h.UnlockLevel <= level).Select(h => h.Id).OrderBy(i => i).ToArray();
            Require(model.Mounts.Select(m => m.HardPointId).OrderBy(i => i).SequenceEqual(installed), "mounted/unlocked IDs at level " + level);
            var linked = (StationLevelModel)new SerializedObject(view.GetComponent<StationLevelView>()).FindProperty("levelModels")
                .GetArrayElementAtIndex(level - 1).objectReferenceValue;
            Require(linked.Mounts.Select(m => m.HardPointId).OrderBy(i => i).SequenceEqual(installed)
                && linked.Mounts.All(m => m.Point != null), "nested view mounts at level " + level);
            foreach (var group in weapons.GroupBy(h => h.WeaponType))
                Require(group.Count(h => h.UnlockLevel <= level) == _counts[level - 1], group.Key + " count at level " + level);
            Transform generated = model.transform.Find("AdditionalWeaponMounts");
            var occluders = model.HullRenderers.OfType<MeshRenderer>().Where(r => r.enabled && !r.transform.IsChildOf(generated)).ToArray();
            var originalArt = model.Mounts.Where(m => m.HardPointId < 17 && m.Art != null && !m.Art.transform.IsChildOf(generated))
                .Select(m => m.Art.transform).ToArray();
            var hull = occluders.Where(r => !originalArt.Any(art => r.transform.IsChildOf(art))).ToArray();
            float maximumDistance = 0f;
            int generatedCount = 0;
            foreach (var mount in model.Mounts.Where(m => m.Point.IsChildOf(generated)))
            {
                Vector3 local = model.transform.InverseTransformPoint(mount.Point.position);
                Vector3 surface = StationHardPointSurface.Place(model.transform, hull, local, Array.Empty<Vector3>(), 0f, occluders);
                float distance = Vector3.Distance(local, surface);
                Require(distance <= .02f, "hull surface distance for hardpoint " + mount.HardPointId + " at level " + level);
                maximumDistance = Mathf.Max(maximumDistance, distance);
                generatedCount++;
            }
            ((JArray)report["levels"]).Add(new JObject { ["level"] = level, ["hardpoints"] = installed.Length,
                ["surfaceAnchors"] = generatedCount, ["maximumSurfaceDistance"] = maximumDistance });
            Render(path, faction, level, points, types.Select(t => (int)t).ToArray());
        }
        File.WriteAllText(OUTPUT + faction + ".json", report.ToString());
        return report.ToString();
    }

    private static void Render(string path, string faction, int level, HardPoint[] points, int[] types)
    {
        var scene = EditorSceneManager.NewPreviewScene();
        var previousTarget = RenderTexture.active;
        var previousAmbient = RenderSettings.ambientLight;
        var previousMode = RenderSettings.ambientMode;
        var previousPalette = Shader.GetGlobalVectorArray("_TeamColors");
        var target = RenderTexture.GetTemporary(1000, 1000, 24, RenderTextureFormat.ARGB32);
        var texture = new Texture2D(1000, 1000, TextureFormat.RGBA32, false);
        var materials = _markers.Append(Color.white).Select(color =>
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.SetColor("_BaseColor", color);
            return material;
        }).ToArray();
        try
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.5f, .52f, .57f);
            Shader.SetGlobalVectorArray("_TeamColors", Enumerable.Repeat((Vector4)Color.white, 8).ToArray());
            var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), scene);
            var model = root.GetComponent<StationLevelModel>();
            float radius = model.HullBounds.extents.magnitude;
            foreach (var mount in model.Mounts)
            {
                var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                marker.name = "Marker" + mount.HardPointId;
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(marker, scene);
                marker.transform.position = mount.Point.position;
                marker.transform.localScale = Vector3.one * Mathf.Max(2f, radius * .022f);
                var weapon = points.Single(h => h.Id == mount.HardPointId) as WeaponHardPoint;
                marker.GetComponent<MeshRenderer>().sharedMaterial = materials[weapon == null ? 5 : Array.IndexOf(types, (int)weapon.WeaponType)];
            }
            foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true)) renderer.SetShaderUserValue(1);
            var cameraObject = new GameObject("StationArmamentCamera");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.AddComponent<Camera>();
            camera.scene = scene; camera.enabled = false; camera.orthographic = true;
            camera.orthographicSize = radius * 1.3f; camera.nearClipPlane = .1f; camera.farClipPlane = radius * 12f;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.025f, .035f, .055f, 1);
            camera.targetTexture = target;
            var lightObject = new GameObject("StationArmamentLight");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lightObject, scene);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.8f; light.transform.rotation = Quaternion.Euler(35, 30, 0);
            Vector3 center = root.transform.TransformPoint(model.HullBounds.center);
            foreach (string angle in new[] { "Top", "Side" })
            {
                Vector3 direction = angle == "Top" ? Vector3.up : new Vector3(.8f, .3f, 1f).normalized;
                camera.transform.position = center + direction * radius * 5f;
                camera.transform.LookAt(center, angle == "Top" ? Vector3.forward : Vector3.up);
                camera.Render(); RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, 1000, 1000), 0, 0); texture.Apply();
                File.WriteAllBytes(OUTPUT + faction + "Level" + level + angle + ".png", texture.EncodeToPNG());
            }
        }
        finally
        {
            RenderTexture.active = previousTarget;
            RenderTexture.ReleaseTemporary(target);
            UnityEngine.Object.DestroyImmediate(texture);
            EditorSceneManager.ClosePreviewScene(scene);
            foreach (var material in materials) UnityEngine.Object.DestroyImmediate(material);
            RenderSettings.ambientLight = previousAmbient; RenderSettings.ambientMode = previousMode;
            // An unset global array has the shader's zero-filled default after a domain reload.
            Shader.SetGlobalVectorArray("_TeamColors", previousPalette == null || previousPalette.Length == 0 ? new Vector4[8] : previousPalette);
        }
    }

    private static void RequireArray(UnityEngine.Object target, string field, UnityEngine.Object[] expected)
    {
        var array = new SerializedObject(target).FindProperty(field);
        Require(array.arraySize == expected.Length && expected.All(item => Enumerable.Range(0, array.arraySize)
            .Any(i => array.GetArrayElementAtIndex(i).objectReferenceValue == item)), field + " bindings");
    }

    private static void Require(bool condition, string check)
    {
        if (!condition) throw new InvalidOperationException("Station armament inspection failed: " + check);
    }
}
