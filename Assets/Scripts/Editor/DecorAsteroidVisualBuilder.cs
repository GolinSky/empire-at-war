using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Environment;
using EmpireAtWar.Entities.Map;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Editor
{
    public static class DecorAsteroidVisualBuilder
    {
        private const string SCENE_CONTEXT_PATH = "Assets/Prefabs/View/LifetimeScopes/SceneContext.prefab";
        private const string LOD_FOLDER = "Assets/Art/Models/Asteroids/BattleRockLods/";

        public static void Build()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(SCENE_CONTEXT_PATH);
            try
            {
                MapLayoutView view = root.GetComponentInChildren<MapLayoutView>(true);
                SerializedObject map = new SerializedObject(view);
                List<DecorAsteroidMesh> meshes = new List<DecorAsteroidMesh>();
                Add(meshes, (GameObject)map.FindProperty("largeRockPrefab").objectReferenceValue, AsteroidSize.Large);
                Add(meshes, (GameObject)map.FindProperty("mediumRockPrefab").objectReferenceValue, AsteroidSize.Medium);
                SerializedProperty debris = map.FindProperty("debrisRockPrefabs");
                for (int i = 0; i < debris.arraySize; i++)
                    Add(meshes, (GameObject)debris.GetArrayElementAtIndex(i).objectReferenceValue, AsteroidSize.Debris);

                DecorAsteroidRenderer renderer = view.GetComponent<DecorAsteroidRenderer>();
                if (renderer == null) renderer = view.gameObject.AddComponent<DecorAsteroidRenderer>();
                SerializedObject serialized = new SerializedObject(renderer);
                SerializedProperty entries = serialized.FindProperty("meshes");
                entries.arraySize = meshes.Count;
                for (int i = 0; i < meshes.Count; i++)
                    entries.GetArrayElementAtIndex(i).boxedValue = meshes[i];
                serialized.ApplyModifiedPropertiesWithoutUndo();
                map.FindProperty("decorRenderer").objectReferenceValue = renderer;
                map.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, SCENE_CONTEXT_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            AssetDatabase.SaveAssets();
        }

        private static void Add(List<DecorAsteroidMesh> meshes, GameObject prefab, AsteroidSize size)
        {
            MeshFilter filter = prefab.GetComponent<MeshFilter>();
            if (filter == null)
                throw new InvalidOperationException($"Decor rock {prefab.name} must contain a root mesh.");
            MeshRenderer renderer = filter.GetComponent<MeshRenderer>();
            Matrix4x4 local = Matrix4x4.Scale(prefab.transform.localScale) *
                prefab.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
            Mesh lowDetail = AssetDatabase.LoadAssetAtPath<Mesh>(LOD_FOLDER + prefab.name + "_LOD1.asset");
            if (lowDetail == null)
                throw new InvalidOperationException($"Decor rock {prefab.name} is missing its LOD mesh.");
            meshes.Add(new DecorAsteroidMesh(size, filter.sharedMesh, renderer.sharedMaterials, local, lowDetail));
        }
    }
}
