using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Editor;
using EmpireAtWar.Editor.Rendering;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.ViewComponents.Health;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

public static class DiamondImportBuilder
{
    const int SHIP_ID = (int)ShipType.Diamond; // 117
    const float LENGTH = 70f;

    const string FBX = "Assets/Art/Models/SeparatistShips/Diamond/Diamond.fbx";
    const string TEX_DIR = "Assets/Art/Textures/Models/SeparatistShips/Diamond";
    const string MAT_DIR = "Assets/Art/Materials/Models/SeparatistShips/Diamond";
    const string VISUAL = "Assets/Prefabs/Models/Ships/Separatist/Models/Diamond.prefab";
    const string VIEW = "Assets/Prefabs/Models/Ships/Separatist/View/DiamondShipView.prefab";
    const string DATA = "Assets/Settings/Data/Ship/Separatist/DiamondShipData.asset";
    const string WRECK_DATA = "Assets/Settings/Data/Models/Wrecks/Separatist/DiamondWreckData.asset";
    const string PREVIEW = "Assets/Prefabs/Ui/Reinforcement/DiamondReinforcementView.prefab";
    const string ICON = "Assets/Art/Textures/Ui/Icons/ShipIcon/Separatist/DiamondIcon.png";
    const string MATCHUPS = "Assets/Settings/Data/Tooltip/Matchups/DiamondMatchups.asset";

    const string DONOR_VIEW = "Assets/Prefabs/Models/Ships/Separatist/View/MunificentShipView.prefab";
    const string DONOR_DATA = "Assets/Settings/Data/Ship/Separatist/MunificentShipData.asset";
    const string DONOR_WRECK = "Assets/Settings/Data/Models/Wrecks/Separatist/MunificentWreckData.asset";
    const string DONOR_PREVIEW = "Assets/Prefabs/Ui/Reinforcement/MunificentReinforcementView.prefab";
    const string DONOR_MATCHUPS = "Assets/Settings/Data/Tooltip/Matchups/MunificentMatchups.asset";

    public static string BuildAll()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Diamond import requires Edit Mode.");

        AssetDatabase.Refresh();
        EnsureFolder(MAT_DIR);
        EnsureFolder("Assets/Prefabs/Models/Ships/Separatist/Models");
        EnsureFolder("Assets/Prefabs/Models/Ships/Separatist/View");
        EnsureFolder("Assets/Art/Textures/Ui/Icons/ShipIcon/Separatist");

        ConfigureTexturesAndMaterials();
        BuildVisualPrefab();
        var (bounds, bottom, top, radius, hull2D) = BuildGameplayPrefab();
        BuildDataAndWreck(bounds, bottom, top, radius, hull2D);
        BuildPreviewPrefab(bounds);
        RenderIcon();
        RegisterAll();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return $"Diamond (117) built & registered: bounds={bounds.size}, radius={radius}, bottom={bottom:F2}, top={top:F2}";
    }

    public static string RegisterOnly()
    {
        try
        {
            RegisterAll();
            AssetDatabase.SaveAssets();
            return "Diamond (117) registered.";
        }
        catch (Exception ex)
        {
            return "ERR: " + ex.ToString();
        }
    }

    static void ConfigureTexturesAndMaterials()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { TEX_DIR }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            bool isNm = path.EndsWith("-nm.png", StringComparison.OrdinalIgnoreCase);
            bool isMask = path.EndsWith("-mask.png", StringComparison.OrdinalIgnoreCase);
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = false;
            importer.sRGBTexture = !isNm && !isMask;
            importer.textureType = isNm ? TextureImporterType.NormalMap : TextureImporterType.Default;
            if (isNm) importer.flipGreenChannel = true;
            importer.SaveAndReimport();
        }

        var clTex = AssetDatabase.LoadAssetAtPath<Texture2D>(TEX_DIR + "/CIS_Diamond-cl.png");
        var nmTex = AssetDatabase.LoadAssetAtPath<Texture2D>(TEX_DIR + "/CIS_Diamond-nm.png");
        var lmTex = AssetDatabase.LoadAssetAtPath<Texture2D>(TEX_DIR + "/CIS_Diamond-lm.png");
        var maskTex = AssetDatabase.LoadAssetAtPath<Texture2D>(TEX_DIR + "/CIS_Diamond-mask.png");

        var hullMatPath = MAT_DIR + "/Diamond_Hull.mat";
        var hullMat = AssetDatabase.LoadAssetAtPath<Material>(hullMatPath);
        if (hullMat == null)
        {
            hullMat = new Material(Shader.Find("EmpireAtWar/Ship Lit"));
            AssetDatabase.CreateAsset(hullMat, hullMatPath);
        }
        hullMat.SetTexture("_BaseMap", clTex);
        hullMat.SetTexture("_BumpMap", nmTex);
        hullMat.EnableKeyword("_NORMALMAP");
        hullMat.SetTexture("_TeamMaskMap", maskTex);
        hullMat.SetFloat("_TeamMaskStrength", 1f);
        hullMat.SetFloat("_TeamLiveryStrength", 0f);
        hullMat.SetFloat("_Smoothness", 0.25f);
        hullMat.SetFloat("_Metallic", 0.1f);
        EditorUtility.SetDirty(hullMat);

        var lightsMatPath = MAT_DIR + "/Diamond_Lights.mat";
        var lightsMat = AssetDatabase.LoadAssetAtPath<Material>(lightsMatPath);
        if (lightsMat == null)
        {
            lightsMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            AssetDatabase.CreateAsset(lightsMat, lightsMatPath);
        }
        lightsMat.SetTexture("_BaseMap", lmTex);
        lightsMat.SetFloat("_Surface", 1f);
        lightsMat.SetFloat("_Blend", 2f);
        lightsMat.SetFloat("_SrcBlend", 5f);
        lightsMat.SetFloat("_DstBlend", 1f);
        lightsMat.SetFloat("_SrcBlendAlpha", 1f);
        lightsMat.SetFloat("_DstBlendAlpha", 1f);
        lightsMat.SetFloat("_ZWrite", 0f);
        lightsMat.SetFloat("_Cull", 2f);
        lightsMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        lightsMat.renderQueue = 3000;
        EditorUtility.SetDirty(lightsMat);

        var modelImporter = (ModelImporter)AssetImporter.GetAtPath(FBX);
        modelImporter.globalScale = 0.02f;
        modelImporter.preserveHierarchy = true;
        modelImporter.importAnimation = false;
        modelImporter.isReadable = true;
        modelImporter.weldVertices = false;
        modelImporter.meshCompression = ModelImporterMeshCompression.Off;
        modelImporter.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "CIS_Diamond-cl Material"), hullMat);
        modelImporter.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "CIS_Diamond-lm Material"), lightsMat);
        modelImporter.SaveAndReimport();
    }

    static void BuildVisualPrefab()
    {
        var root = new GameObject("Diamond");
        try
        {
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(FBX);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(fbx, root.transform);
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            instance.name = "CIS_Diamond";

            // Remove helper meshes (Collision, Shadow) from visual prefab
            foreach (var r in instance.GetComponentsInChildren<MeshRenderer>(true).ToArray())
            {
                if (r.name.Contains("Collision") || r.name.Contains("Shadow"))
                    UnityEngine.Object.DestroyImmediate(r.gameObject);
            }

            var bounds = BoundsOf(root);
            float scale = LENGTH / bounds.size.z;
            instance.transform.localScale *= scale;
            instance.transform.localPosition = -bounds.center * scale;

            PrefabUtility.SaveAsPrefabAsset(root, VISUAL);
            AssetDatabase.SaveAssets();
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    static (Bounds bounds, float bottom, float top, float radius, Vector2[] hull2D) BuildGameplayPrefab()
    {
        CopyAsset(DONOR_VIEW, VIEW);
        var root = PrefabUtility.LoadPrefabContents(VIEW);
        try
        {
            root.name = "DiamondShipView";
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            root.transform.localScale = Vector3.one;

            var body = (Transform)Config(root, "ShipMoveComponent").FindProperty("bodyTransform").objectReferenceValue;
            var health = Component(root, "HealthComponent");
            var shield = (Shield)new SerializedObject(health).FindProperty("shieldView").objectReferenceValue;
            var weaponComponent = Component(root, "WeaponComponent");

            var donorPoints = root.GetComponentsInChildren<HardPoint>(true).OrderBy(p => p.Id).ToArray();
            var weapons = donorPoints.OfType<WeaponHardPoint>().Where(w => (int)w.HardPointType == 0).ToList();

            string[] weaponSockets = { "Laser01", "Laser02", "Laser03", "Laser04", "Laser05", "Laser06" };
            string[] weaponNames = {
                "DualMediumTurboLaser_01", "DualMediumTurboLaser_02",
                "MediumLaser_01", "MediumLaser_02", "MediumLaser_03", "MediumLaser_04"
            };
            int[] weaponTypes = { 5, 5, 23, 23, 23, 23 };
            float[] yaws = { 0f, 0f, -90f, -90f, 90f, 90f };
            float[] widths = { 160f, 160f, 170f, 170f, 170f, 170f };

            var hardpoints = new List<HardPoint>();
            for (int i = 0; i < weaponNames.Length; i++)
            {
                var wp = weapons[i];
                wp.name = weaponNames[i];
                var cfg = new SerializedObject(wp);
                cfg.FindProperty("<Id>k__BackingField").intValue = i;
                cfg.FindProperty("healthOverride").floatValue = 0f;
                cfg.FindProperty("healthWeight").floatValue = 1f;
                cfg.FindProperty("<WeaponType>k__BackingField").intValue = weaponTypes[i];
                cfg.FindProperty("<HardPointType>k__BackingField").intValue = 0;
                cfg.FindProperty("mainBattery").boolValue = false;
                cfg.FindProperty("yAxisRange.<Min>k__BackingField").floatValue = yaws[i] - widths[i] * 0.5f;
                cfg.FindProperty("yAxisRange.<Max>k__BackingField").floatValue = yaws[i] + widths[i] * 0.5f;
                cfg.ApplyModifiedPropertiesWithoutUndo();
                hardpoints.Add(wp);
            }

            var shieldGen = donorPoints.Single(p => (int)p.HardPointType == 2);
            shieldGen.name = "ShieldGenerator";
            var sgCfg = new SerializedObject(shieldGen);
            sgCfg.FindProperty("<Id>k__BackingField").intValue = 6;
            sgCfg.FindProperty("healthOverride").floatValue = 0f;
            sgCfg.FindProperty("healthWeight").floatValue = 1f;
            sgCfg.ApplyModifiedPropertiesWithoutUndo();
            hardpoints.Add(shieldGen);

            var engines = donorPoints.Single(p => (int)p.HardPointType == 1);
            engines.name = "Engines";
            var engCfg = new SerializedObject(engines);
            engCfg.FindProperty("<Id>k__BackingField").intValue = 7;
            engCfg.FindProperty("healthOverride").floatValue = 0f;
            engCfg.FindProperty("healthWeight").floatValue = 1f;
            engCfg.ApplyModifiedPropertiesWithoutUndo();
            hardpoints.Add(engines);

            foreach (var p in hardpoints) p.transform.SetParent(root.transform, true);
            foreach (var p in donorPoints.Except(hardpoints)) UnityEngine.Object.DestroyImmediate(p.gameObject);
            foreach (var c in new MonoBehaviour[] { shield, weaponComponent, Component(root, "AudioShipComponent") })
                c.transform.SetParent(root.transform, true);

            foreach (var child in body.Cast<Transform>().ToArray())
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            body.localPosition = Vector3.zero;
            body.localRotation = Quaternion.identity;
            body.localScale = Vector3.one;

            var visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(VISUAL), body);
            var bones = visual.GetComponentsInChildren<Transform>(true);

            for (int i = 0; i < weaponSockets.Length; i++)
            {
                var wp = hardpoints[i];
                wp.transform.SetParent(body, false);
                wp.transform.localScale = Vector3.one;
                wp.transform.localRotation = Quaternion.identity;
                wp.transform.position = bones.Single(t => t.name == weaponSockets[i]).position;
            }

            var visible = visual.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled).ToArray();
            var hull = visible.Where(r => r.sharedMaterials.All(m => m != null && m.shader.name == "EmpireAtWar/Ship Lit")).ToArray();
            var points = hull.SelectMany(r => r.GetComponent<MeshFilter>().sharedMesh.vertices
                .Select(v => root.transform.InverseTransformPoint(r.transform.TransformPoint(v)))).ToArray();
            var bounds = new Bounds(points[0], Vector3.zero);
            foreach (var pt in points) bounds.Encapsulate(pt);

            shieldGen.transform.SetParent(body, false);
            shieldGen.transform.localPosition = bounds.center;
            shieldGen.transform.localRotation = Quaternion.identity;
            shieldGen.transform.localScale = Vector3.one;

            engines.transform.SetParent(body, false);
            engines.transform.localPosition = new Vector3(0f, bounds.center.y, bounds.min.z * 0.75f);
            engines.transform.localRotation = Quaternion.identity;
            engines.transform.localScale = Vector3.one;

            weaponComponent.transform.SetParent(body, true);

            foreach (var hp in hardpoints)
            {
                var anchor = (Transform)new SerializedObject(hp).FindProperty("destroyedVfxAnchor").objectReferenceValue;
                anchor.position = hp.transform.position;
                anchor.localScale = Vector3.one;
            }

            var banked = new[] { -15f, 0f, 15f }.SelectMany(a => points.Select(p => Quaternion.Euler(0, 0, a) * p)).ToArray();
            float bottom = banked.Min(p => p.y);
            float top = banked.Max(p => p.y);
            float radius = Mathf.Ceil(new Vector2(bounds.extents.x, bounds.extents.z).magnitude) + 2f;

            Assign(health, "<ShipUnits>k__BackingField", hardpoints.ToArray());
            Assign(weaponComponent, "hardPoints", hardpoints.OfType<WeaponHardPoint>().ToArray());
            Assign(Component(root, "EntityBindings"), "<Injectables>k__BackingField",
                hardpoints.Cast<UnityEngine.Object>().Concat(new[] { Component(root, "UnitRenderStateComponent") }).ToArray());
            Assign(Component(root, "Ship"), "explosionHullRenderers", hull);

            ShieldHullBaker.Bake(root.transform, shield);
            var shieldRenderer = shield.GetComponent<MeshRenderer>();
            Assign(Component(root, "UnitRenderStateComponent"), "meshRenderers",
                visible.Concat(new[] { shieldRenderer }).ToArray());
            Assign(Component(root, "FogVisibilityComponent"), "renderers",
                visible.Cast<Renderer>().Concat(new Renderer[] { shieldRenderer, root.GetComponent<LineRenderer>() }).ToArray());

            var healthCfg = new SerializedObject(health);
            healthCfg.FindProperty("ionFieldBounds").boundsValue = new Bounds(
                new Vector3(0, (bottom + top) * 0.5f, 0),
                new Vector3(bounds.size.x, top - bottom, bounds.size.z) * 1.05f);
            healthCfg.ApplyModifiedPropertiesWithoutUndo();

            var collider = root.GetComponent<BoxCollider>();
            collider.center = bounds.center;
            collider.size = bounds.size;

            var selection = Config(root, "SelectionComponent");
            selection.FindProperty("ringSize").vector2Value = Vector2.one * radius * 2f;
            Save(selection);
            foreach (var rect in root.GetComponentsInChildren<RectTransform>(true).Where(t => t.name == "SelectedImage"))
                rect.sizeDelta = Vector2.one * radius * 2f;

            PrefabUtility.SaveAsPrefabAsset(root, VIEW);
            var hull2D = ConvexHull2D(points.Select(p => new Vector2(p.x, p.z)).ToArray());
            return (bounds, bottom, top, radius, hull2D);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static void BuildDataAndWreck(Bounds bounds, float bottom, float top, float radius, Vector2[] hull2D)
    {
        var wreckView = ShipWreckBuilder.Build(VIEW);
        CopyAsset(DONOR_WRECK, WRECK_DATA);
        var wreck = Load(WRECK_DATA);
        wreck.targetObject.name = "DiamondWreckData";
        wreck.FindProperty("prefab.m_AssetGUID").stringValue = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(wreckView));
        Save(wreck);

        CopyAsset(DONOR_DATA, DATA);
        var data = Load(DATA);
        data.targetObject.name = "DiamondShipData";
        data.FindProperty("<Hull>k__BackingField").floatValue = 3000f;
        data.FindProperty("<Shields>k__BackingField").floatValue = 3000f;
        data.FindProperty("<Speed>k__BackingField").floatValue = 40f;
        data.FindProperty("<RotationSpeed>k__BackingField").floatValue = 35f;
        data.FindProperty("<TurnAcceleration>k__BackingField").floatValue = 35f;
        data.FindProperty("<BodyRotationMaxAngle>k__BackingField").floatValue = 20f;
        data.FindProperty("<ShipClass>k__BackingField").intValue = (int)ShipClass.Frigate;
        data.FindProperty("shieldRegenTiers").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/Settings/Data/Ship/Shared/ShipShieldRegenTiersData.asset");
        data.FindProperty("<ShieldRegenTier>k__BackingField").intValue = 1; // Frigate
        data.FindProperty("<HeightTier>k__BackingField").intValue = 5;
        data.FindProperty("<HullBottom>k__BackingField").floatValue = bottom;
        data.FindProperty("<HullTop>k__BackingField").floatValue = top;
        data.FindProperty("<NavigationRadius>k__BackingField").floatValue = radius;
        data.FindProperty("<Wreck>k__BackingField").objectReferenceValue = wreck.targetObject;

        var visionHull = data.FindProperty("visionHull");
        visionHull.arraySize = hull2D.Length;
        for (int i = 0; i < hull2D.Length; i++)
            visionHull.GetArrayElementAtIndex(i).vector2Value = hull2D[i];

        var abilities = data.FindProperty("abilities");
        abilities.arraySize = 1;
        abilities.GetArrayElementAtIndex(0).intValue = (int)ShipAbilityId.BoostWeaponPower;

        data.FindProperty("hangarBays").arraySize = 0;

        var loadout = data.FindProperty("weaponLoadout");
        loadout.arraySize = 2;
        loadout.GetArrayElementAtIndex(0).FindPropertyRelative("weaponType").intValue = 5; // DualMediumTurboLaser
        loadout.GetArrayElementAtIndex(0).FindPropertyRelative("count").intValue = 2;
        loadout.GetArrayElementAtIndex(1).FindPropertyRelative("weaponType").intValue = 23; // MediumLaser
        loadout.GetArrayElementAtIndex(1).FindPropertyRelative("count").intValue = 4;

        Save(data);
    }

    static void BuildPreviewPrefab(Bounds bounds)
    {
        CopyAsset(DONOR_PREVIEW, PREVIEW);
        var root = PrefabUtility.LoadPrefabContents(PREVIEW);
        try
        {
            root.name = "DiamondReinforcementView";
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            root.transform.localScale = Vector3.one;

            var spawnView = Component(root, "UnitSpawnView");
            var spawnCfg = new SerializedObject(spawnView);
            var holoMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Vfx/Hologram.mat");
            spawnCfg.FindProperty("hologramMaterial").objectReferenceValue = holoMat;

            foreach (Transform child in root.transform.Cast<Transform>().ToArray())
                UnityEngine.Object.DestroyImmediate(child.gameObject);

            var visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(VISUAL), root.transform);
            var renderers = visual.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled).ToArray();
            foreach (var r in renderers)
            {
                var mats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = holoMat;
                r.sharedMaterials = mats;
            }

            Assign(spawnView, "meshRenderers", renderers);
            spawnCfg.Update();
            spawnCfg.FindProperty("hologramMaterial").objectReferenceValue = holoMat;
            spawnCfg.ApplyModifiedPropertiesWithoutUndo();

            var collider = root.GetComponent<BoxCollider>();
            collider.center = bounds.center;
            collider.size = bounds.size;

            PrefabUtility.SaveAsPrefabAsset(root, PREVIEW);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static void RenderIcon()
    {
        var viewPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(VIEW);
        var clone = (GameObject)PrefabUtility.InstantiatePrefab(viewPrefab);
        // Strip gameplay components for isolated preview render
        foreach (var mb in clone.GetComponentsInChildren<MonoBehaviour>(true))
            UnityEngine.Object.DestroyImmediate(mb);

        const int LAYER = 31;
        foreach (var t in clone.GetComponentsInChildren<Transform>(true))
            t.gameObject.layer = LAYER;

        var renderers = clone.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled).ToArray();
        var bounds = new Bounds(renderers[0].bounds.center, Vector3.zero);
        foreach (var r in renderers) bounds.Encapsulate(r.bounds);

        var camGo = new GameObject("DiamondIconCam");
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0, 0, 0, 0);
        cam.cullingMask = 1 << LAYER;
        cam.fieldOfView = 35f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 1000f;

        var lightGo = new GameObject("DiamondIconLight");
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.6f;
        light.cullingMask = 1 << LAYER;
        lightGo.transform.rotation = Quaternion.Euler(45f, 140f, 0f);

        camGo.transform.rotation = Quaternion.Euler(40.76f, 125.00f, 0.00f);
        float maxExtent = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
        float dist = maxExtent / Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.45f;
        camGo.transform.position = bounds.center - camGo.transform.forward * dist;

        var rt = new RenderTexture(512, 512, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(512, 512, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, 512, 512), 0, 0);
        tex.Apply();

        File.WriteAllBytes(ICON, tex.EncodeToPNG());
        RenderTexture.active = null;
        UnityEngine.Object.DestroyImmediate(rt);
        UnityEngine.Object.DestroyImmediate(tex);
        UnityEngine.Object.DestroyImmediate(camGo);
        UnityEngine.Object.DestroyImmediate(lightGo);
        UnityEngine.Object.DestroyImmediate(clone);

        AssetDatabase.ImportAsset(ICON, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(ICON);
        importer.textureType = TextureImporterType.Sprite;
        importer.alphaIsTransparency = true;
        importer.sRGBTexture = true;
        importer.SaveAndReimport();
    }

    static void RegisterAll()
    {
        var audio = Load("Assets/Settings/Data/Models/Audio/ShipSfxData.asset");
        CloneRow(audio.FindProperty("ships.keyValue"), "key", 100, SHIP_ID);
        Save(audio);

        var icon = AssetDatabase.LoadAssetAtPath<Sprite>(ICON);
        if (icon == null)
        {
            var iconImporter = (TextureImporter)AssetImporter.GetAtPath(ICON);
            iconImporter.textureType = TextureImporterType.Sprite;
            iconImporter.spriteImportMode = SpriteImportMode.Single;
            iconImporter.alphaIsTransparency = true;
            iconImporter.sRGBTexture = true;
            iconImporter.SaveAndReimport();
            AssetDatabase.ImportAsset(ICON, ImportAssetOptions.ForceUpdate);
            icon = AssetDatabase.LoadAssetAtPath<Sprite>(ICON);
        }
        if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(icon, out string iconGuid, out long localId))
            throw new InvalidOperationException("Missing Diamond icon sprite.");
        string iconKey = iconGuid + ":" + localId;

        var settings = AddressableAssetSettingsDefaultObject.Settings;
        var sepGroup = settings.FindGroup("SeparatistUnitViews");
        var dataGroup = settings.FindGroup("Data");
        var iconGroup = settings.FindGroup("SharedIcons");

        var mapping = Load("Assets/Settings/AssetMappingData.asset");
        foreach (var path in new[] { VIEW, DATA })
        {
            string guid = AssetDatabase.AssetPathToGUID(path);
            Entry(mapping.FindProperty("assetMappings.keyValue"), Path.GetFileNameWithoutExtension(path))
                .FindPropertyRelative("m_AssetGUID").stringValue = guid;
            var group = path == VIEW ? sepGroup : dataGroup;
            var entry = settings.CreateOrMoveEntry(guid, group);
            entry.address = Path.GetFileNameWithoutExtension(path);
            EditorUtility.SetDirty(group);
        }
        Save(mapping);

        var previewEntry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(PREVIEW), sepGroup);
        previewEntry.address = "DiamondReinforcementView";

        var wreckPath = "Assets/Prefabs/Models/Wrecks/DiamondWreckView.prefab";
        var wreckEntry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(wreckPath), sepGroup);
        wreckEntry.address = "DiamondWreckView";

        var iconEntry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(ICON), iconGroup);
        iconEntry.address = ICON;

        EditorUtility.SetDirty(sepGroup);
        EditorUtility.SetDirty(iconGroup);
        EditorUtility.SetDirty(settings);

        var ships = Load("Assets/Settings/Data/Ship/Shared/ShipsData.asset");
        Entry(ships.FindProperty("shipsData.keyValue"), SHIP_ID)
            .FindPropertyRelative("m_AssetGUID").stringValue = AssetDatabase.AssetPathToGUID(DATA);
        Save(ships);

        var ui = Load("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset");
        Entry(ui.FindProperty("shipIconWrapper.keyValue"), SHIP_ID).objectReferenceValue = icon;
        Save(ui);

        var tooltip = Load("Assets/Settings/Data/Tooltip/TooltipIconData.asset");
        var icons = tooltip.FindProperty("icons");
        int idx = Enumerable.Range(0, icons.arraySize)
            .Where(i => icons.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue == iconKey)
            .DefaultIfEmpty(-1).First();
        if (idx < 0) { idx = icons.arraySize; icons.arraySize++; }
        icons.GetArrayElementAtIndex(idx).FindPropertyRelative("key").stringValue = iconKey;
        icons.GetArrayElementAtIndex(idx).FindPropertyRelative("sprite").objectReferenceValue = icon;
        Save(tooltip);

        var reinforcement = Load("Assets/Settings/Data/Reinforcement/ReinforcementData.asset");
        Entry(reinforcement.FindProperty("spawnShipWrapper.keyValue"), SHIP_ID)
            .FindPropertyRelative("m_AssetGUID").stringValue = AssetDatabase.AssetPathToGUID(PREVIEW);
        Save(reinforcement);

        CopyAsset(DONOR_MATCHUPS, MATCHUPS);
        var matchups = Load(MATCHUPS);
        matchups.targetObject.name = "DiamondMatchups";
        matchups.FindProperty("strongAgainst").arraySize = 0;
        matchups.FindProperty("weakAgainst").arraySize = 0;
        Save(matchups);

        var faction = Load("Assets/Settings/Data/Factions/Separatist/SeparatistFaction.asset");
        var unit = Entry(faction.FindProperty("ships.keyValue"), SHIP_ID);
        unit.FindPropertyRelative("displayName").stringValue = "Diamond";
        unit.FindPropertyRelative("<Name>k__BackingField").stringValue = "Diamond-class Cruiser";
        unit.FindPropertyRelative("role").stringValue = "Frigate \u00B7 Escort";
        unit.FindPropertyRelative("description").stringValue =
            "3,000 hull and 3,000 shields with frigate shield regeneration. Two dual medium turbolasers and four medium laser cannons. Boost Weapon Power increases rate of fire.";
        unit.FindPropertyRelative("iconKey").stringValue = iconKey;
        unit.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue = icon;
        unit.FindPropertyRelative("matchups").objectReferenceValue = matchups.targetObject;
        unit.FindPropertyRelative("isHero").boolValue = false;
        unit.FindPropertyRelative("<MaxCount>k__BackingField").intValue = 30;
        unit.FindPropertyRelative("<AvailableLevel>k__BackingField").intValue = 2;
        unit.FindPropertyRelative("<Price>k__BackingField").intValue = 2500;
        unit.FindPropertyRelative("<BuildTime>k__BackingField").intValue = 28;
        unit.FindPropertyRelative("<UnitCapacity>k__BackingField").intValue = 3;
        Save(faction);
    }

    static Vector2[] ConvexHull2D(Vector2[] pts)
    {
        var sorted = pts.Distinct().OrderBy(p => p.x).ThenBy(p => p.y).ToArray();
        if (sorted.Length <= 3) return sorted;
        var lower = new List<Vector2>();
        foreach (var p in sorted)
        {
            while (lower.Count >= 2 && Cross(lower[lower.Count - 2], lower[lower.Count - 1], p) <= 0)
                lower.RemoveAt(lower.Count - 1);
            lower.Add(p);
        }
        var upper = new List<Vector2>();
        for (int i = sorted.Length - 1; i >= 0; i--)
        {
            var p = sorted[i];
            while (upper.Count >= 2 && Cross(upper[upper.Count - 2], upper[upper.Count - 1], p) <= 0)
                upper.RemoveAt(upper.Count - 1);
            upper.Add(p);
        }
        lower.RemoveAt(lower.Count - 1);
        upper.RemoveAt(upper.Count - 1);
        lower.AddRange(upper);
        return lower.ToArray();
    }

    static float Cross(Vector2 o, Vector2 a, Vector2 b) => (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);

    static Bounds BoundsOf(GameObject root)
    {
        var pts = root.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled)
            .SelectMany(r => r.GetComponent<MeshFilter>().sharedMesh.vertices
                .Select(v => root.transform.InverseTransformPoint(r.transform.TransformPoint(v)))).ToArray();
        var b = new Bounds(pts[0], Vector3.zero);
        foreach (var p in pts) b.Encapsulate(p);
        return b;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parts = path.Split('/');
        string cur = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = cur + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(cur, parts[i]);
            cur = next;
        }
    }

    static void CopyAsset(string src, string dst)
    {
        if (File.Exists(dst)) AssetDatabase.DeleteAsset(dst);
        if (!AssetDatabase.CopyAsset(src, dst)) throw new InvalidOperationException($"CopyAsset failed: {src} -> {dst}");
    }

    static MonoBehaviour Component(GameObject root, string typeName) =>
        root.GetComponentsInChildren<MonoBehaviour>(true).Single(c => c != null && c.GetType().Name == typeName);

    static SerializedObject Config(GameObject root, string typeName) => new SerializedObject(Component(root, typeName));

    static void Assign(MonoBehaviour target, string propName, UnityEngine.Object[] objs)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(propName);
        prop.arraySize = objs.Length;
        for (int i = 0; i < objs.Length; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = objs[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static SerializedProperty CloneRow(SerializedProperty array, string key, int source, int target)
    {
        for (int i = 0; i < array.arraySize; i++)
            if (array.GetArrayElementAtIndex(i).FindPropertyRelative(key).intValue == target)
                return array.GetArrayElementAtIndex(i);
        int donor = Enumerable.Range(0, array.arraySize).Single(i => array.GetArrayElementAtIndex(i).FindPropertyRelative(key).intValue == source);
        array.InsertArrayElementAtIndex(donor);
        var row = array.GetArrayElementAtIndex(donor);
        row.FindPropertyRelative(key).intValue = target;
        return row;
    }

    static SerializedProperty Entry(SerializedProperty array, object key)
    {
        for (int i = 0; i < array.arraySize; i++)
        {
            var row = array.GetArrayElementAtIndex(i);
            var k = row.FindPropertyRelative("key");
            if (key is int n ? k.intValue == n : k.stringValue == (string)key)
                return row.FindPropertyRelative("value");
        }
        int idx = array.arraySize;
        array.arraySize++;
        var entry = array.GetArrayElementAtIndex(idx);
        if (key is int num) entry.FindPropertyRelative("key").intValue = num;
        else entry.FindPropertyRelative("key").stringValue = (string)key;
        return entry.FindPropertyRelative("value");
    }

    static SerializedObject Load(string path) => new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>(path));

    static void Save(SerializedObject data)
    {
        data.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(data.targetObject);
        foreach (var field in data.targetObject.GetType()
            .GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
            .Where(f => f.FieldType.IsGenericType && f.FieldType.GetGenericTypeDefinition() == typeof(Utilities.ScriptUtils.EditorSerialization.DictionaryWrapper<,>)))
        {
            var dictField = field.FieldType.GetField("dictionary", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var wrapper = field.GetValue(data.targetObject);
            if (dictField != null && wrapper != null) dictField.SetValue(wrapper, null);
        }
    }
}
