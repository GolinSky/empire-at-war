using System;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Entities.Capture;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Models.MiniMap;
using EmpireAtWar.Views.ReinforcementZones;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class IntegrateSatelliteSites
{
    private const string SITES = "Assets/Prefabs/View/CaptureSites/";
    private const string MODELS = "Assets/Prefabs/Models/DefendStation/";
    private const string ICONS = "Assets/Art/Textures/Ui/Icons/DefendPlatform/";
    private const string DATA = "Assets/Settings/Data/Models/CaptureSites/CaptureSiteData.asset";
    private const string SCENE = "Assets/Prefabs/View/LifetimeScopes/SceneContext.prefab";
    private const string UI = "Assets/Prefabs/Ui/SkirmishGame/CaptureSiteBuildUi.prefab";
    private static readonly string[] TYPES = { "QuadLaser", "Turbolaser", "Ion", "MassDriver", "Missile" };
    private static readonly string[] NAMES = { "Quad Laser Satellite", "Turbolaser Satellite", "Ion Satellite", "Mass Driver Satellite", "Missile Satellite" };
    private static readonly string[] ROLES = { "ANTI-FIGHTER / BOMBER", "GENERAL ANTI-SHIP", "SHIELD STRIPPING", "LONG-RANGE ANTI-CAPITAL", "BURST ANTI-HULL" };
    private static readonly float[] COSTS = { 300f, 500f, 400f, 1600f, 1000f };
    private static readonly float[] TIMES = { 21f, 25f, 23f, 47f, 35f };

    public static string Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Satellite site integration requires Edit Mode.");
        AssetDatabase.Refresh();
        MigrateRing(SITES + "CaptureSite.prefab", true);
        MigrateRing(SITES + "BattleAsteroidCaptureSite.prefab", true);
        MigrateRing("Assets/Prefabs/View/ReinforcementZones/ReinforcementZone.prefab", false);
        BuildPad();
        ConfigureData();
        ConfigureUi();
        EditPrefab(SCENE, root =>
        {
            SerializedObject map = new SerializedObject(Components<MapLayoutView>(root).Single());
            map.FindProperty("satelliteSitePrefab").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<CaptureSiteView>(SITES + "SatelliteCaptureSite.prefab");
            map.ApplyModifiedPropertiesWithoutUndo();
        });
        MapGenerationSettings settings = AssetDatabase.LoadAssetAtPath<MapGenerationSettings>(
            "Assets/Settings/Data/Models/Map/MapGenerationSettings.asset");
        SerializedObject mapSettings = new SerializedObject(settings);
        mapSettings.FindProperty("<SatellitePadOffset>k__BackingField").floatValue = 120f;
        mapSettings.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(settings);
        MiniMapData miniMap = AssetDatabase.LoadAssetAtPath<MiniMapData>("Assets/Settings/Data/Models/MiniMap/MiniMapData.asset");
        SerializedObject mini = new SerializedObject(miniMap);
        SerializedProperty markers = mini.FindProperty("<MarkWrapper>k__BackingField.keyValue");
        SerializedProperty marker = Entry(markers, (int)MarkType.SatelliteSite);
        marker.FindPropertyRelative("value").objectReferenceValue = SpriteAt(ICONS + "QuadLaserSatelliteIcon.png");
        mini.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(miniMap);
        AssetDatabase.SaveAssets();
        return "Migrated three capture rings; created satellite pad; wired map, seven facility costs, allowed lists, UI cards and minimap.";
    }

    private static void MigrateRing(string path, bool site)
    {
        // Exact renderer names and palettes recovered from the pre-migration prefab YAML in HEAD.
        EditPrefab(path, root =>
        {
            Component view = site ? (Component)Components<CaptureSiteView>(root).Single()
                : Components<ReinforcementZoneView>(root).Single();
            SerializedObject serialized = new SerializedObject(view);
            CaptureRingView ring = (CaptureRingView)serialized.FindProperty("captureRing").objectReferenceValue;
            if (ring != null) return;
            MeshRenderer renderer = Components<MeshRenderer>(root).Single(r => r.name == (site ? "CaptureRing" : "SphereRenderer"));
            ring = view.gameObject.AddComponent<CaptureRingView>();
            SerializedObject palette = new SerializedObject(ring);
            palette.FindProperty("ringRenderer").objectReferenceValue = renderer;
            palette.FindProperty("siteBorder").boolValue = site;
            Color[] colors = site ? new[] { new Color(.58f,.64f,.72f,.04f), new Color(.22f,.74f,.97f,.05f),
                new Color(.36f,.9f,.62f,.05f), new Color(.94f,.27f,.27f,.05f), new Color(1f,.75f,.1f,.07f) }
                : new[] { new Color(.48f,.55f,.62f,.08f), new Color(.18f,.53f,.68f,.1f),
                    new Color(.27f,.62f,.43f,.1f), new Color(.68f,.27f,.29f,.1f), new Color(.73f,.56f,.23f,.1f) };
            string[] fields = { "neutralColor", "playerColor", "allyColor", "opponentColor", "contestedColor" };
            for (int i = 0; i < fields.Length; i++) palette.FindProperty(fields[i]).colorValue = colors[i];
            palette.ApplyModifiedPropertiesWithoutUndo();
            serialized.FindProperty("captureRing").objectReferenceValue = ring;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        });
    }

    private static void BuildPad()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(SITES + "CaptureSite.prefab");
        try
        {
            root.name = "SatelliteCaptureSite";
            root.transform.localScale = Vector3.one;
            CaptureSiteView view = Components<CaptureSiteView>(root).Single();
            SerializedObject site = new SerializedObject(view);
            Object.DestroyImmediate(((Transform)site.FindProperty("asteroidRocks").objectReferenceValue).gameObject);
            SerializedProperty frameworks = site.FindProperty("constructionFrameworks.keyValue");
            for (int i = 0; i < frameworks.arraySize; i++)
                Object.DestroyImmediate(((Transform)frameworks.GetArrayElementAtIndex(i).FindPropertyRelative("value").objectReferenceValue).gameObject);
            GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>(MODELS + "SatelliteBase.prefab"), root.transform);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            site.FindProperty("asteroidRocks").objectReferenceValue = model.transform;
            site.FindProperty("rockObstacles").arraySize = 0;
            site.FindProperty("radius").floatValue = 20f;
            site.FindProperty("facilityHeightOffset").floatValue = 0f;
            MeshRenderer renderer = Components<MeshRenderer>(root).Single(r => r.name == "CaptureRing");
            renderer.transform.localScale = new Vector3(40f, 40f, 1f);
            frameworks.arraySize = TYPES.Length;
            for (int i = 0; i < TYPES.Length; i++)
            {
                GameObject framework = (GameObject)PrefabUtility.InstantiatePrefab(
                    AssetDatabase.LoadAssetAtPath<GameObject>(MODELS + TYPES[i] + "Satellite.prefab"), root.transform);
                PrefabUtility.UnpackPrefabInstance(framework, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                // Keep only the six weapon mounts: the empty pad already renders the shared base during construction.
                for (int child = framework.transform.childCount - 1; child >= 0; child--)
                    if (!framework.transform.GetChild(child).name.StartsWith("Turret_", StringComparison.Ordinal))
                        Object.DestroyImmediate(framework.transform.GetChild(child).gameObject);
                framework.name = TYPES[i] + "Construction";
                framework.transform.localPosition = Vector3.zero;
                framework.transform.localRotation = Quaternion.identity;
                framework.SetActive(false);
                SerializedProperty entry = frameworks.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("key").intValue = i + 2;
                entry.FindPropertyRelative("value").objectReferenceValue = framework.transform;
            }
            site.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, SITES + "SatelliteCaptureSite.prefab");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
    }

    private static void ConfigureData()
    {
        CaptureSiteData data = AssetDatabase.LoadAssetAtPath<CaptureSiteData>(DATA);
        SerializedObject serialized = new SerializedObject(data);
        SerializedProperty costs = serialized.FindProperty("facilityCosts.keyValue");
        costs.arraySize = 7;
        for (int i = 0; i < TYPES.Length; i++)
        {
            SerializedProperty entry = costs.GetArrayElementAtIndex(i + 2);
            entry.FindPropertyRelative("key").intValue = i + 2;
            SerializedProperty value = entry.FindPropertyRelative("value");
            value.FindPropertyRelative("<Name>k__BackingField").stringValue = NAMES[i];
            value.FindPropertyRelative("<Role>k__BackingField").stringValue = ROLES[i];
            value.FindPropertyRelative("<Price>k__BackingField").floatValue = COSTS[i];
            value.FindPropertyRelative("<BuildTime>k__BackingField").floatValue = TIMES[i];
            value.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue = SpriteAt(ICONS + TYPES[i] + "SatelliteIcon.png");
        }
        for (int i = 0; i < 2; i++)
        {
            SerializedProperty value = costs.GetArrayElementAtIndex(i).FindPropertyRelative("value");
            value.FindPropertyRelative("<Role>k__BackingField").stringValue = i == 0 ? "RESOURCE INCOME" : "AREA DEFENSE";
            value.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Art/Textures/Ui/Icons/FacilityIcon/" + (i == 0 ? "MiningAsteroidIcon.png" : "BattleAsteroidIcon.png"));
        }
        SerializedProperty allowed = serialized.FindProperty("allowedFacilities.keyValue");
        allowed.arraySize = 3;
        for (int kind = 0; kind < 3; kind++)
        {
            SerializedProperty entry = allowed.GetArrayElementAtIndex(kind);
            entry.FindPropertyRelative("key").intValue = kind;
            SerializedProperty types = entry.FindPropertyRelative("value");
            types.arraySize = kind == 2 ? 5 : 2;
            for (int i = 0; i < types.arraySize; i++) types.GetArrayElementAtIndex(i).intValue = kind == 2 ? i + 2 : i;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
    }

    private static void ConfigureUi()
    {
        EditPrefab(UI, root =>
        {
            foreach (SiteFacilityOptionView option in Components<SiteFacilityOptionView>(root))
            {
                SerializedObject row = new SerializedObject(option);
                row.FindProperty("icon").objectReferenceValue = Components<Image>(option.gameObject).Single(image => image.name == "Icon");
                row.FindProperty("roleText").objectReferenceValue = Components<TMP_Text>(option.gameObject).Single(text => text.name == "Role");
                row.ApplyModifiedPropertiesWithoutUndo();
            }
            foreach (Button button in Components<Button>(root))
            {
                button.transition = Selectable.Transition.ColorTint;
                button.spriteState = default;
            }
        });
    }

    private static Sprite SpriteAt(string path)
    {
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.maxTextureSize = 512;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static SerializedProperty Entry(SerializedProperty entries, int key)
    {
        for (int i = 0; i < entries.arraySize; i++)
            if (entries.GetArrayElementAtIndex(i).FindPropertyRelative("key").intValue == key) return entries.GetArrayElementAtIndex(i);
        int index = entries.arraySize++;
        SerializedProperty entry = entries.GetArrayElementAtIndex(index);
        entry.FindPropertyRelative("key").intValue = key;
        return entry;
    }

    private static IEnumerable<T> Components<T>(GameObject root) where T : Component
    {
        SerializedProperty components = new SerializedObject(root).FindProperty("m_Component");
        for (int i = 0; i < components.arraySize; i++)
            if (components.GetArrayElementAtIndex(i).FindPropertyRelative("component").objectReferenceValue is T component)
                yield return component;
        for (int i = 0; i < root.transform.childCount; i++)
            foreach (T component in Components<T>(root.transform.GetChild(i).gameObject)) yield return component;
    }

    private static void EditPrefab(string path, Action<GameObject> edit)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try { edit(root); PrefabUtility.SaveAsPrefabAsset(root, path); }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
    }
}
