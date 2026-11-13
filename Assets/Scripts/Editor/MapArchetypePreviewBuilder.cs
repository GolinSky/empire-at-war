using System.IO;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Map.Generation;
using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Views.ReinforcementZones;
using UnityEditor;
using UnityEngine;
using Random = System.Random;

namespace EmpireAtWar.Editor
{
    /// <summary>Captures the real generated layout without entering Play Mode or changing open scenes.</summary>
    public static class MapArchetypePreviewBuilder
    {
        private const string SETTINGS_PATH = "Assets/Settings/Data/Models/Map/MapGenerationSettings.asset";
        private const string OUTPUT_PATH = "Assets/Art/Textures/Ui/MapPreviews";
        private const int PREVIEW_SEED = 137;

        [MenuItem("Tools/Map/Generate Next Missing Preview")]
        public static void GenerateNext()
        {
            MapGenerationSettings settings = AssetDatabase.LoadAssetAtPath<MapGenerationSettings>(SETTINGS_PATH);
            foreach (PlanetMapProfileEntry entry in settings.Profiles.Entries)
            {
                if (entry.Preview != null) continue;
                Generate(entry.Planet);
                return;
            }
            Debug.Log("All planet map previews are assigned.");
        }

        public static void Generate(PlanetType planet)
        {
            MapGenerationSettings settings = AssetDatabase.LoadAssetAtPath<MapGenerationSettings>(SETTINGS_PATH);
            CaptureSiteData siteData = AssetDatabase.LoadAssetAtPath<CaptureSiteData>(
                "Assets/Settings/Data/Models/CaptureSites/CaptureSiteData.asset");
            CaptureSiteView site = AssetDatabase.LoadAssetAtPath<CaptureSiteView>(
                "Assets/Prefabs/View/CaptureSites/CaptureSite.prefab");
            MapFeatureRadii radii = new MapFeatureRadii(
                AssetDatabase.LoadAssetAtPath<ReinforcementZoneView>(
                    "Assets/Prefabs/View/ReinforcementZones/ReinforcementZone.prefab").Radius,
                siteData.GetSettings(CaptureSiteKind.Mining).Radius *
                    Mathf.Max(Mathf.Abs(site.transform.lossyScale.x), Mathf.Abs(site.transform.lossyScale.z)),
                AssetDatabase.LoadAssetAtPath<ReinforcementZoneView>(
                    "Assets/Prefabs/View/ReinforcementZones/MainReinforcementZone.prefab").Radius);
            PlayerSlot[] players =
            {
                new PlayerSlot(new PlayerId(0), new TeamId(0), FactionType.Republic,
                    PlayerController.Human, EnemyAiDifficulty.Medium, 0),
                new PlayerSlot(new PlayerId(1), new TeamId(1), FactionType.Separatist,
                    PlayerController.Ai, EnemyAiDifficulty.Medium, 1)
            };
            MapLayout layout = new MapLayoutGenerator(settings, radii).Generate(
                MapSize.Medium, players, new Random(PREVIEW_SEED), PREVIEW_SEED, planet);
            Texture2D texture = new MapPreviewRaster(layout).Render(players, settings, radii);
            string path = OUTPUT_PATH + "/" + planet + ".png";
            try
            {
                if (!AssetDatabase.IsValidFolder(OUTPUT_PATH))
                    AssetDatabase.CreateFolder("Assets/Art/Textures/Ui", "MapPreviews");
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally { Object.DestroyImmediate(texture); }
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = MapPreviewRaster.RESOLUTION;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();

            SerializedObject serialized = new SerializedObject(settings.Profiles);
            SerializedProperty entries = serialized.FindProperty("entries");
            for (int i = 0; i < entries.arraySize; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("<Planet>k__BackingField").intValue != (int)planet) continue;
                entry.FindPropertyRelative("<Preview>k__BackingField").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings.Profiles);
            AssetDatabase.SaveAssets();
            Debug.Log($"Map preview saved: {planet}, Medium, duel, seed {PREVIEW_SEED}: {path}");
        }
    }
}
