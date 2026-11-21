using System;
using System.IO;
using System.Linq;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Services.Repair;
using EmpireAtWar.ViewComponents.Health;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Builds the Repair Satellite from the staged FBX/PNGs: art import, visual + gameplay prefabs, data, repair profile,
/// icon and registrations. Run each entry with `unity command script run --file Tools/Blender/RepairSatellite/Build.cs
/// --entry BuildRepairSatellite.<Step>` in order: Art, Prefabs, Ring, Data, Icon, Register.
/// </summary>
public static class BuildRepairSatellite
{
    private const string NAME = "RepairSatellite";
    private const string DONOR = "TurbolaserSatellite";
    private const string FBX = "Assets/Art/Models/DefendPlatforms/RepairSatellite/RepairSatellite.fbx";
    private const string TEXTURES = "Assets/Art/Textures/Models/DefendPlatforms/RepairSatellite/";
    private const string MATERIALS = "Assets/Art/Materials/Models/DefendPlatforms/RepairSatellite/";
    private const string PREFABS = "Assets/Prefabs/Models/DefendStation/";
    private const string DATA = "Assets/Settings/Data/Models/DefendPlatform/";
    private const string PROFILE = DATA + "RepairSatelliteRepairProfile.asset";
    private const string ICON = "Assets/Art/Textures/Ui/Icons/DefendPlatform/RepairSatelliteIcon.png";
    private const string REPORT = "Tools/Blender/RepairSatellite/ConversionReport.json";
    private const int PRICE = 500;
    private const int BUILD_TIME = 25;

    public static string Art()
    {
        AssetDatabase.Refresh();
        foreach (string path in Directory.GetFiles(TEXTURES, "*.png").Select(p => p.Replace('\\', '/')))
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            bool normal = path.EndsWith("_n.png");
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.alphaIsTransparency = false;
            importer.sRGBTexture = !normal && !path.Contains("TeamMask");
            importer.maxTextureSize = 2048;
            if (normal) { importer.textureType = TextureImporterType.NormalMap; importer.flipGreenChannel = true; }
            importer.SaveAndReimport();
        }
        var model = (ModelImporter)AssetImporter.GetAtPath(FBX);
        model.globalScale = .02f;
        model.importAnimation = false;
        model.importCameras = false;
        model.importLights = false;
        model.importNormals = ModelImporterNormals.Import;
        model.importTangents = ModelImporterTangents.CalculateMikk;
        model.weldVertices = false;
        model.meshCompression = ModelImporterMeshCompression.Off;
        model.preserveHierarchy = true;
        foreach (string part in new[] { "Base", "Utility" })
        {
            string path = MATERIALS + NAME + "_" + part + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("EmpireAtWar/Ship Lit"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURES + NAME + "_" + part + ".png"));
            material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURES + NAME + "_" + part + "_n.png"));
            material.EnableKeyword("_NORMALMAP");
            material.SetTexture("_TeamMaskMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURES + NAME + "_" + part + "_TeamMask.png"));
            material.SetFloat("_TeamMaskStrength", 1);
            material.SetFloat("_TeamLiveryStrength", 0);
            material.SetFloat("_TeamRimStrength", 0);
            material.SetFloat("_Metallic", .15f);
            material.SetFloat("_Smoothness", .25f);
            EditorUtility.SetDirty(material);
            model.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "W_Sattalite_" + part + " Material"), material);
        }
        model.SaveAndReimport();
        AssetDatabase.SaveAssets();
        var raw = AssetDatabase.LoadAssetAtPath<GameObject>(FBX);
        var report = JObject.Parse(File.ReadAllText(REPORT));
        var result = new JArray();
        foreach (var renderer in raw.GetComponentsInChildren<MeshRenderer>(true))
        {
            Mesh mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
            int expected = (int)report["meshes"][renderer.name]["triangles"];
            if (mesh.triangles.Length / 3 != expected)
                throw new InvalidOperationException($"{renderer.name}: {mesh.triangles.Length / 3} triangles, source {expected}.");
            result.Add(new JObject { ["mesh"] = renderer.name, ["triangles"] = expected, ["uv"] = mesh.uv.Length > 0,
                ["materials"] = new JArray(renderer.sharedMaterials.Select(m => m.name)) });
        }
        return result.ToString(Newtonsoft.Json.Formatting.None);
    }

    public static string Prefabs()
    {
        var report = JObject.Parse(File.ReadAllText(REPORT));
        var raw = AssetDatabase.LoadAssetAtPath<GameObject>(FBX);
        var donorVisual = AssetDatabase.LoadAssetAtPath<GameObject>(PREFABS + "QuadLaserSatellite.prefab");
        float donorWidth = HorizontalWidth(MeshBounds(donorVisual));

        // Visual prefab: visible source meshes only (collision and shadow helpers stay in the FBX).
        var scene = EditorSceneManager.NewPreviewScene();
        Bounds visualBounds;
        try
        {
            var root = new GameObject(NAME);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            foreach (var renderer in raw.GetComponentsInChildren<MeshRenderer>(true))
            {
                if ((bool)report["meshes"][renderer.name]["hidden"]) continue;
                var go = new GameObject(renderer.name);
                go.transform.SetParent(root.transform, false);
                go.transform.SetLocalPositionAndRotation(renderer.transform.position, renderer.transform.rotation);
                go.transform.localScale = renderer.transform.lossyScale;
                go.AddComponent<MeshFilter>().sharedMesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                go.AddComponent<MeshRenderer>().sharedMaterials = renderer.sharedMaterials;
            }
            // Same footprint as the other satellites on the shared pad.
            float scale = donorWidth / HorizontalWidth(MeshBounds(root));
            Vector3 center = MeshBounds(root).center;
            foreach (Transform child in root.transform)
            {
                child.localPosition = (child.localPosition - new Vector3(center.x, 0f, center.z)) * scale;
                child.localScale *= scale;
            }
            visualBounds = MeshBounds(root);
            PrefabUtility.SaveAsPrefabAsset(root, PREFABS + NAME + ".prefab");
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }

        string viewPath = PREFABS + NAME + "View.prefab";
        if (!File.Exists(viewPath)) AssetDatabase.CopyAsset(PREFABS + "QuadLaserSatelliteView.prefab", viewPath);
        var view = PrefabUtility.LoadPrefabContents(viewPath);
        try
        {
            view.name = NAME + "View";
            foreach (var point in view.GetComponentsInChildren<HardPoint>(true)) UnityEngine.Object.DestroyImmediate(point.gameObject);
            foreach (Transform child in view.transform.Cast<Transform>().ToArray())
                if (PrefabUtility.IsAnyPrefabInstanceRoot(child.gameObject)) UnityEngine.Object.DestroyImmediate(child.gameObject);
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>(PREFABS + NAME + ".prefab"), view.transform);
            visual.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            visual.transform.localScale = Vector3.one;
            var shield = view.GetComponentsInChildren<Shield>(true).Single();
            var renderers = visual.GetComponentsInChildren<MeshRenderer>(true);
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            var collider = view.GetComponent<BoxCollider>();
            collider.center = view.transform.InverseTransformPoint(bounds.center);
            collider.size = view.transform.InverseTransformVector(bounds.size);
            var withShield = renderers.Append(shield.GetComponent<MeshRenderer>()).ToArray();
            foreach (var behaviour in view.GetComponentsInChildren<MonoBehaviour>(true))
            {
                var serialized = new SerializedObject(behaviour);
                switch (behaviour.GetType().Name)
                {
                    case "EntityBindings":
                        var injectables = serialized.FindProperty("<Injectables>k__BackingField");
                        Assign(injectables, Enumerable.Range(0, injectables.arraySize)
                            .Select(i => injectables.GetArrayElementAtIndex(i).objectReferenceValue).Where(o => o != null).ToArray());
                        break;
                    case "DefendPlatform": Assign(serialized.FindProperty("explosionHullRenderers"), renderers); break;
                    case "ConstructionComponent": Assign(serialized.FindProperty("renderers"), renderers); break;
                    case "HealthComponent":
                        Assign(serialized.FindProperty("<ShipUnits>k__BackingField"), Array.Empty<UnityEngine.Object>());
                        serialized.FindProperty("ionFieldBounds").boundsValue = new Bounds(collider.center, collider.size);
                        break;
                    case "WeaponComponent": Assign(serialized.FindProperty("hardPoints"), Array.Empty<UnityEngine.Object>()); break;
                    case "FogVisibilityComponent":
                        Assign(serialized.FindProperty("renderers"), withShield);
                        serialized.FindProperty("revealRadius").floatValue = Mathf.Max(collider.size.x, collider.size.z) * .5f;
                        break;
                    case "UnitRenderStateComponent": Assign(serialized.FindProperty("meshRenderers"), withShield); break;
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            EmpireAtWar.Editor.ShieldHullBaker.Bake(view.transform, shield);
            PrefabUtility.SaveAsPrefabAsset(view, viewPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(view); }
        AssetDatabase.SaveAssets();
        return $"visual size {visualBounds.size}, donor width {donorWidth}";
    }

    public static string Data()
    {
        var profile = AssetDatabase.LoadAssetAtPath<RepairProfile>(PROFILE);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<RepairProfile>();
            AssetDatabase.CreateAsset(profile, PROFILE);
        }
        var profileData = new SerializedObject(profile);
        profileData.FindProperty("<Radius>k__BackingField").floatValue = 400f;
        profileData.FindProperty("<Interval>k__BackingField").floatValue = 1f;
        profileData.FindProperty("<PercentPerTick>k__BackingField").floatValue = .5f;
        profileData.FindProperty("<MaxTargets>k__BackingField").intValue = 10;
        profileData.FindProperty("<MaxHealersPerTarget>k__BackingField").intValue = 2;
        Save(profileData);

        string dataPath = DATA + NAME + "Data.asset";
        if (!File.Exists(dataPath)) AssetDatabase.CopyAsset(DATA + DONOR + "Data.asset", dataPath);
        var data = new SerializedObject(AssetDatabase.LoadMainAssetAtPath(dataPath));
        data.targetObject.name = NAME + "Data";
        data.FindProperty("weaponLoadout").arraySize = 0;
        data.FindProperty("<ComponentData>k__BackingField").FindPropertyRelative("hardPointHealth").arraySize = 0;
        data.FindProperty("<RepairProfile>k__BackingField").objectReferenceValue = profile;
        Save(data);
        AssetDatabase.SaveAssets();
        return "Saved repair profile and satellite data.";
    }

    public static string Icon()
    {
        var scene = EditorSceneManager.NewPreviewScene();
        var previousTarget = RenderTexture.active;
        var ambient = RenderSettings.ambientLight;
        var mode = RenderSettings.ambientMode;
        var cameraObject = new GameObject("RepairSatellitePreviewCamera");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject, scene);
        var camera = cameraObject.AddComponent<Camera>();
        camera.scene = scene; camera.enabled = false; camera.orthographic = true;
        camera.orthographicSize = 14; camera.nearClipPlane = .1f; camera.farClipPlane = 2000;
        camera.cullingMask = 1 << 30; camera.clearFlags = CameraClearFlags.SolidColor;
        camera.transform.position = new Vector3(45, 50, 65); camera.transform.LookAt(Vector3.zero);
        var lightObject = new GameObject("RepairSatellitePreviewLight");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lightObject, scene);
        var light = lightObject.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.8f; light.cullingMask = 1 << 30;
        light.transform.rotation = Quaternion.Euler(35, 30, 0);
        var target = RenderTexture.GetTemporary(512, 512, 24, RenderTextureFormat.ARGB32);
        var texture = new Texture2D(512, 512, TextureFormat.RGBA32, false);
        camera.targetTexture = target;
        Directory.CreateDirectory("Temp/RepairSatelliteImport/Previews");
        try
        {
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.5f, .52f, .57f);
            var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PREFABS + NAME + ".prefab"), scene);
            foreach (var child in root.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 30;
            foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true)) renderer.SetShaderUserValue(1);
            camera.backgroundColor = new Color(.025f, .035f, .055f, 1);
            Capture(camera, target, texture, "Temp/RepairSatelliteImport/Previews/RepairSatelliteTeam0.png");
            foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true)) renderer.SetShaderUserValue(0);
            camera.backgroundColor = Color.clear;
            Capture(camera, target, texture, ICON);
            AssetDatabase.ImportAsset(ICON);
            var importer = (TextureImporter)AssetImporter.GetAtPath(ICON);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true; importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 512;
            importer.SaveAndReimport();
            AssetDatabase.SaveAssets();
        }
        finally
        {
            RenderTexture.active = previousTarget; camera.targetTexture = null;
            RenderTexture.ReleaseTemporary(target); UnityEngine.Object.DestroyImmediate(texture);
            EditorSceneManager.ClosePreviewScene(scene);
            RenderSettings.ambientLight = ambient; RenderSettings.ambientMode = mode;
        }
        return "Rendered icon and team preview.";
    }

    public static string Register()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        var mapping = Load("Assets/Settings/AssetMappingData.asset");
        foreach (var (path, donor) in new[] { (PREFABS + NAME + "View.prefab", PREFABS + DONOR + "View.prefab"),
                     (DATA + NAME + "Data.asset", DATA + DONOR + "Data.asset") })
        {
            string guid = AssetDatabase.AssetPathToGUID(path);
            string key = Path.GetFileNameWithoutExtension(path);
            KeyedEntry(mapping.FindProperty("assetMappings.keyValue"), key).FindPropertyRelative("m_AssetGUID").stringValue = guid;
            var entry = settings.CreateOrMoveEntry(guid, settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(donor)).parentGroup);
            entry.address = key;
            EditorUtility.SetDirty(entry.parentGroup);
        }
        Save(mapping);
        string iconGuid = AssetDatabase.AssetPathToGUID(ICON);
        var iconEntry = settings.CreateOrMoveEntry(iconGuid, settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(
            "Assets/Art/Textures/Ui/Icons/DefendPlatform/" + DONOR + "Icon.png")).parentGroup);
        iconEntry.address = ICON;
        EditorUtility.SetDirty(iconEntry.parentGroup);
        EditorUtility.SetDirty(settings);

        var icon = AssetDatabase.LoadAssetAtPath<Sprite>(ICON);
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(icon, out string guidPart, out long iconId);
        string iconKey = guidPart + ":" + iconId;
        var data = AssetDatabase.LoadAssetAtPath<DefendPlatformData>(DATA + NAME + "Data.asset");
        var catalog = Load("Assets/Settings/Data/Factions/Shared/DefendPlatformCatalog.asset");
        var unit = CloneRow(catalog.FindProperty("entries.keyValue"), (int)DefendPlatformType.TurbolaserSatellite,
            (int)DefendPlatformType.RepairSatellite).FindPropertyRelative("value");
        unit.FindPropertyRelative("displayName").stringValue = "Repair Satellite";
        unit.FindPropertyRelative("<Name>k__BackingField").stringValue = "Repair Satellite";
        unit.FindPropertyRelative("role").stringValue = "Stationary fleet repair";
        unit.FindPropertyRelative("description").stringValue =
            FormattableString.Invariant($"{data.ComponentData.Hull:N0} hull, {data.ComponentData.Shields:N0} shields; no weapons. Repairs hull and live hardpoints of nearby allied ships and structures. Built on captured satellite pads.");
        unit.FindPropertyRelative("iconKey").stringValue = iconKey;
        unit.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue = icon;
        unit.FindPropertyRelative("<Price>k__BackingField").intValue = PRICE;
        unit.FindPropertyRelative("<BuildTime>k__BackingField").intValue = BUILD_TIME;
        Save(catalog);

        var tooltip = Load("Assets/Settings/Data/Tooltip/TooltipIconData.asset");
        var icons = tooltip.FindProperty("icons");
        int index = Enumerable.Range(0, icons.arraySize)
            .Where(i => icons.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue == iconKey).DefaultIfEmpty(-1).First();
        if (index < 0) { index = icons.arraySize; icons.arraySize++; }
        icons.GetArrayElementAtIndex(index).FindPropertyRelative("key").stringValue = iconKey;
        icons.GetArrayElementAtIndex(index).FindPropertyRelative("sprite").objectReferenceValue = icon;
        Save(tooltip);

        var sites = Load("Assets/Settings/Data/Models/CaptureSites/CaptureSiteData.asset");
        var cost = KeyedEntry(sites.FindProperty("facilityCosts.keyValue"), (int)SiteFacilityType.RepairSatellite);
        cost.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue = icon;
        cost.FindPropertyRelative("<Role>k__BackingField").stringValue = "FLEET REPAIR";
        cost.FindPropertyRelative("<Name>k__BackingField").stringValue = "Repair Satellite";
        cost.FindPropertyRelative("<Price>k__BackingField").floatValue = PRICE;
        cost.FindPropertyRelative("<BuildTime>k__BackingField").floatValue = BUILD_TIME;
        var allowed = KeyedEntry(sites.FindProperty("allowedFacilities.keyValue"), (int)CaptureSiteKind.Satellite);
        if (!Enumerable.Range(0, allowed.arraySize).Any(i => allowed.GetArrayElementAtIndex(i).intValue == (int)SiteFacilityType.RepairSatellite))
        {
            allowed.arraySize++;
            allowed.GetArrayElementAtIndex(allowed.arraySize - 1).intValue = (int)SiteFacilityType.RepairSatellite;
        }
        Save(sites);
        AssetDatabase.SaveAssets();
        return "Registered asset mapping, Addressables, catalog, tooltip icon and capture-site cost/allowed list.";
    }

    public static string Ring()
    {
        string viewPath = PREFABS + NAME + "View.prefab";
        var view = PrefabUtility.LoadPrefabContents(viewPath);
        try
        {
            var existing = view.GetComponentsInChildren<EmpireAtWar.Components.Repair.RepairRangeView>(true);
            foreach (var old in existing) UnityEngine.Object.DestroyImmediate(old.gameObject);
            var go = new GameObject("RepairRange");
            go.transform.SetParent(view.transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.enabled = false;
            line.useWorldSpace = true;
            line.loop = true;
            line.positionCount = 0;
            line.widthMultiplier = 3f;
            line.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Vfx/RangeDebugLine.mat");
            line.startColor = line.endColor = new Color(.35f, 1f, .55f, .8f);
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            var ringView = go.AddComponent<EmpireAtWar.Components.Repair.RepairRangeView>();
            var serializedRing = new SerializedObject(ringView);
            serializedRing.FindProperty("ring").objectReferenceValue = line;
            serializedRing.ApplyModifiedPropertiesWithoutUndo();
            var bindings = new SerializedObject(view.GetComponent<EmpireAtWar.Entities.BaseEntity.EntityBindings>());
            var injectables = bindings.FindProperty("<Injectables>k__BackingField");
            Assign(injectables, Enumerable.Range(0, injectables.arraySize)
                .Select(i => injectables.GetArrayElementAtIndex(i).objectReferenceValue).Where(o => o != null)
                .Append(ringView).ToArray());
            bindings.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(view, viewPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(view); }
        AssetDatabase.SaveAssets();
        return "Added repair range ring.";
    }

    private static void Capture(Camera camera, RenderTexture target, Texture2D texture, string path)
    {
        camera.Render(); RenderTexture.active = target;
        texture.ReadPixels(new Rect(0, 0, 512, 512), 0, 0); texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
    }

    private static Bounds MeshBounds(GameObject root)
    {
        var points = root.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled)
            .SelectMany(r => r.GetComponent<MeshFilter>().sharedMesh.vertices.Select(r.transform.TransformPoint)).ToArray();
        var bounds = new Bounds(points[0], Vector3.zero);
        foreach (var point in points.Skip(1)) bounds.Encapsulate(point);
        return bounds;
    }

    private static float HorizontalWidth(Bounds bounds) => Mathf.Max(bounds.size.x, bounds.size.z);
    private static SerializedObject Load(string path) => new SerializedObject(AssetDatabase.LoadMainAssetAtPath(path));
    private static void Save(SerializedObject data) { data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(data.targetObject); }

    private static void Assign(SerializedProperty property, UnityEngine.Object[] objects)
    {
        property.arraySize = objects.Length;
        for (int i = 0; i < objects.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = objects[i];
    }

    private static SerializedProperty CloneRow(SerializedProperty list, int source, int target)
    {
        int index = Enumerable.Range(0, list.arraySize).Where(i => list.GetArrayElementAtIndex(i).FindPropertyRelative("key").intValue == target)
            .DefaultIfEmpty(-1).First();
        if (index < 0)
        {
            int donor = Enumerable.Range(0, list.arraySize).Single(i => list.GetArrayElementAtIndex(i).FindPropertyRelative("key").intValue == source);
            list.InsertArrayElementAtIndex(donor);
            index = donor + 1;
            list.GetArrayElementAtIndex(index).FindPropertyRelative("key").intValue = target;
        }
        return list.GetArrayElementAtIndex(index);
    }

    private static SerializedProperty KeyedEntry(SerializedProperty list, string key)
    {
        int index = Enumerable.Range(0, list.arraySize).Where(i => list.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue == key)
            .DefaultIfEmpty(-1).First();
        if (index < 0) { index = list.arraySize; list.arraySize++; list.GetArrayElementAtIndex(index).FindPropertyRelative("key").stringValue = key; }
        return list.GetArrayElementAtIndex(index).FindPropertyRelative("value");
    }

    private static SerializedProperty KeyedEntry(SerializedProperty list, int key)
    {
        int index = Enumerable.Range(0, list.arraySize).Where(i => list.GetArrayElementAtIndex(i).FindPropertyRelative("key").intValue == key)
            .DefaultIfEmpty(-1).First();
        if (index < 0) { index = list.arraySize; list.arraySize++; list.GetArrayElementAtIndex(index).FindPropertyRelative("key").intValue = key; }
        return list.GetArrayElementAtIndex(index).FindPropertyRelative("value");
    }
}
