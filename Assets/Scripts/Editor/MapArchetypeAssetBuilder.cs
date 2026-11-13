using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Planet;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Editor
{
    public static class MapArchetypeAssetBuilder
    {
        private const string MAP_PATH = "Assets/Settings/Data/Models/Map";
        private const string ARCHETYPE_PATH = MAP_PATH + "/Archetypes";
        private const string SETTINGS_PATH = MAP_PATH + "/MapGenerationSettings.asset";
        private const string PROFILE_PATH = MAP_PATH + "/PlanetMapProfiles.asset";

        [MenuItem("Tools/Map/Build Archetype Assets")]
        public static void Build()
        {
            if (!AssetDatabase.IsValidFolder(ARCHETYPE_PATH))
                AssetDatabase.CreateFolder(MAP_PATH, "Archetypes");
            Dictionary<MapArchetypeKind, MapArchetype> archetypes = new Dictionary<MapArchetypeKind, MapArchetype>();
            foreach (MapArchetypeKind kind in Enum.GetValues(typeof(MapArchetypeKind)))
            {
                string path = ARCHETYPE_PATH + "/" + kind + ".asset";
                MapArchetype asset = AssetDatabase.LoadAssetAtPath<MapArchetype>(path);
                if (asset == null)
                {
                    asset = ScriptableObject.CreateInstance<MapArchetype>();
                    AssetDatabase.CreateAsset(asset, path);
                    SerializedObject serialized = new SerializedObject(asset);
                    serialized.FindProperty("<Kind>k__BackingField").intValue = (int)kind;
                    serialized.FindProperty("<DisplayName>k__BackingField").stringValue = GetName(kind);
                    serialized.FindProperty("<MaximumPlayers>k__BackingField").intValue = 8;
                    serialized.FindProperty("<BoundsMultiplier>k__BackingField").floatValue = 1.15f;
                    serialized.FindProperty("<FieldThreshold>k__BackingField").floatValue =
                        kind == MapArchetypeKind.DenseField ? 0.48f : 0.68f;
                    serialized.FindProperty("<RidgeThreshold>k__BackingField").floatValue =
                        kind == MapArchetypeKind.DenseField ? 0.86f : 0.98f;
                    serialized.FindProperty("<FeatureSizeMultiplier>k__BackingField").floatValue =
                        kind == MapArchetypeKind.DenseField ? 0.7f : 1f;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(asset);
                }
                archetypes.Add(kind, asset);
            }
            PlanetMapProfile profiles = AssetDatabase.LoadAssetAtPath<PlanetMapProfile>(PROFILE_PATH);
            if (profiles == null)
            {
                profiles = ScriptableObject.CreateInstance<PlanetMapProfile>();
                AssetDatabase.CreateAsset(profiles, PROFILE_PATH);
                SerializedObject serialized = new SerializedObject(profiles);
                SerializedProperty entries = serialized.FindProperty("entries");
                Array planets = Enum.GetValues(typeof(PlanetType));
                entries.arraySize = planets.Length;
                for (int i = 0; i < planets.Length; i++)
                {
                    PlanetType planet = (PlanetType)planets.GetValue(i);
                    SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                    entry.FindPropertyRelative("<Planet>k__BackingField").intValue = (int)planet;
                    entry.FindPropertyRelative("<Archetype>k__BackingField").objectReferenceValue = archetypes[GetKind(planet)];
                    entry.FindPropertyRelative("<OverrideSeed>k__BackingField").boolValue = false;
                    entry.FindPropertyRelative("<PlanetSeed>k__BackingField").intValue = 0;
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(profiles);
            }
            MapGenerationSettings settings = AssetDatabase.LoadAssetAtPath<MapGenerationSettings>(SETTINGS_PATH);
            SerializedObject settingsObject = new SerializedObject(settings);
            settingsObject.FindProperty("<Profiles>k__BackingField").objectReferenceValue = profiles;
            settingsObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            foreach (PlanetType planet in Enum.GetValues(typeof(PlanetType))) profiles.Get(planet);
        }

        private static string GetName(MapArchetypeKind kind)
        {
            switch (kind)
            {
                case MapArchetypeKind.OpenField: return "Open Field";
                case MapArchetypeKind.AsteroidBelt: return "Asteroid Belt";
                case MapArchetypeKind.TwinPass: return "Twin Pass";
                case MapArchetypeKind.OrbitalRing: return "Orbital Ring";
                case MapArchetypeKind.DenseField: return "Dense Field";
                case MapArchetypeKind.NebulaCorridors: return "Nebula Corridors";
                case MapArchetypeKind.Islands: return "Islands";
                default: throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }

        private static MapArchetypeKind GetKind(PlanetType planet)
        {
            switch (planet)
            {
                case PlanetType.Coruscant:
                case PlanetType.Kuat:
                case PlanetType.Anaxes:
                case PlanetType.Corulag:
                case PlanetType.Fondor:
                case PlanetType.Balmorra:
                case PlanetType.Ralltiir:
                case PlanetType.Eriadu: return MapArchetypeKind.OpenField;
                case PlanetType.Hoth:
                case PlanetType.Geonosis:
                case PlanetType.AetenII:
                case PlanetType.AlzocIII:
                case PlanetType.Hypori:
                case PlanetType.Polus:
                case PlanetType.Sullust:
                case PlanetType.Shola: return MapArchetypeKind.AsteroidBelt;
                case PlanetType.Mandalore:
                case PlanetType.Corellia:
                case PlanetType.Carida:
                case PlanetType.AbregadoRae:
                case PlanetType.Ryloth:
                case PlanetType.Utapau:
                case PlanetType.Tatooine:
                case PlanetType.Korriban: return MapArchetypeKind.TwinPass;
                case PlanetType.Endor:
                case PlanetType.Bespin:
                case PlanetType.Byss:
                case PlanetType.Alderaan:
                case PlanetType.Yavin:
                case PlanetType.Hapes:
                case PlanetType.Naboo:
                case PlanetType.Dantooine: return MapArchetypeKind.OrbitalRing;
                case PlanetType.Kessel:
                case PlanetType.Ilum:
                case PlanetType.Kashyyyk:
                case PlanetType.Myrkr:
                case PlanetType.Wayland:
                case PlanetType.Honoghr:
                case PlanetType.Bonadan:
                case PlanetType.Muunilinst: return MapArchetypeKind.DenseField;
                case PlanetType.Dagobah:
                case PlanetType.Mustafar:
                case PlanetType.NalHutta:
                case PlanetType.Felucia:
                case PlanetType.Dathomir:
                case PlanetType.Atzerri:
                case PlanetType.Jabiim:
                case PlanetType.Lwhekk: return MapArchetypeKind.NebulaCorridors;
                case PlanetType.Kamino:
                case PlanetType.Manaan:
                case PlanetType.MonCalamari:
                case PlanetType.Bestine:
                case PlanetType.Bothawui:
                case PlanetType.Fresia:
                case PlanetType.Saleucami:
                case PlanetType.Taris:
                case PlanetType.Thyferra: return MapArchetypeKind.Islands;
                default: throw new ArgumentOutOfRangeException(nameof(planet), planet, "Planet requires an explicit archetype assignment.");
            }
        }
    }
}
