using System;
using System.IO;
using System.Linq;
using EmpireAtWar.ViewComponents.Health;
using EmpireAtWar.ViewComponents.Station;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

public static class BuildRebelStationView
{
    private const string PREFABS = "Assets/Prefabs/Models/Stations/";
    private const string VIEW = PREFABS + "RebellionSpaceStationView.prefab";
    private const string DONOR = PREFABS + "RepublicSpaceStationView.prefab";

    public static string Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Station construction requires Edit Mode.");
        if (AssetDatabase.LoadAssetAtPath<GameObject>(VIEW) == null && !AssetDatabase.CopyAsset(DONOR, VIEW)) throw new IOException(VIEW);
        var root = PrefabUtility.LoadPrefabContents(VIEW);
        try
        {
            root.name = "RebellionSpaceStationView";
            var entity = root.GetComponent<EmpireAtWar.Entities.SpaceStation.SpaceStation>();
            var entitySerialized = new SerializedObject(entity);
            var oldRenderers = entitySerialized.FindProperty("explosionHullRenderers");
            if (root.GetComponent<StationLevelView>() == null)
            {
                var geometry = Enumerable.Range(0, oldRenderers.arraySize).Select(i => ((Renderer)oldRenderers.GetArrayElementAtIndex(i).objectReferenceValue).gameObject).Distinct().ToArray();
                foreach (var go in geometry) UnityEngine.Object.DestroyImmediate(go);
                Vector3 oldScale = root.transform.localScale;
                root.transform.localScale = Vector3.one;
                foreach (Transform child in root.transform)
                {
                    child.localPosition = Vector3.Scale(child.localPosition, oldScale);
                    child.localScale = Vector3.Scale(child.localScale, oldScale);
                }
            }
            else
            {
                foreach (var oldModel in root.GetComponentsInChildren<StationLevelModel>(true)) UnityEngine.Object.DestroyImmediate(oldModel.gameObject);
            }
            if (root.GetComponent<StationLevelView>() == null) root.AddComponent<StationLevelView>();
            var models = new StationLevelModel[5];
            for (int i = 0; i < models.Length; i++)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFABS + "RebelSpaceStationLevel" + (i + 1) + ".prefab");
                models[i] = ((GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform)).GetComponent<StationLevelModel>();
                models[i].gameObject.layer = root.layer;
            }
            var selectedCanvas = (RectTransform)root.transform.Find("SelectedCanvas");
            selectedCanvas.localScale = Vector3.one;
            var selection = new SerializedObject(root.GetComponents<MonoBehaviour>().Single(c => c.GetType().Name == "SelectionComponent"));
            var selectedImage = ((UnityEngine.UI.Image)selection.FindProperty("selectedImage").objectReferenceValue).rectTransform;
            selectedImage.localScale = Vector3.one;
            selectedImage.anchorMin = Vector2.zero;
            selectedImage.anchorMax = Vector2.one;
            selectedImage.anchoredPosition = Vector2.zero;
            selectedImage.sizeDelta = Vector2.zero;
            var shield = root.GetComponentInChildren<Shield>(true);
            shield.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            shield.transform.localScale = Vector3.one;
            var allRenderers = models.SelectMany(m => m.GetComponentsInChildren<MeshRenderer>(true)).Append(shield.GetComponent<MeshRenderer>()).ToArray();
            var team = new SerializedObject(root.GetComponents<MonoBehaviour>().Single(c => c.GetType().Name == "TeamColorView"));
            Assign(team.FindProperty("meshRenderers"), allRenderers); Save(team);
            var fog = new SerializedObject(root.GetComponents<MonoBehaviour>().Single(c => c.GetType().Name == "FogVisibilityComponent"));
            Assign(fog.FindProperty("renderers"), allRenderers);
            Save(fog);
            // Level mounts, shields and the remaining StationLevelView wiring: Tools/Blender/SpaceStations/BuildStationLevels.cs.
            PrefabUtility.SaveAsPrefabAsset(root, VIEW);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }

        var mapping = new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/Settings/AssetMappingData.asset"));
        var entries = mapping.FindProperty("assetMappings.keyValue");
        var rebellion = Enumerable.Range(0, entries.arraySize).Select(entries.GetArrayElementAtIndex).Single(p => p.FindPropertyRelative("key").stringValue == "RebellionSpaceStationView");
        string guid = AssetDatabase.AssetPathToGUID(VIEW);
        rebellion.FindPropertyRelative("value.m_AssetGUID").stringValue = guid; Save(mapping);
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        var entry = settings.CreateOrMoveEntry(guid, settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(DONOR)).parentGroup);
        entry.address = "RebellionSpaceStationView";
        EditorUtility.SetDirty(entry.parentGroup); EditorUtility.SetDirty(settings);
        // No damaged Rebel ALO was supplied. Do not display the Republic station wreck for Rebellion.
        var data = new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/Settings/Data/Models/SpaceStation/SpaceStationData.asset"));
        var wrecks = data.FindProperty("wrecks.keyValue");
        for (int i = wrecks.arraySize - 1; i >= 0; i--) if (wrecks.GetArrayElementAtIndex(i).FindPropertyRelative("key").intValue == 2) wrecks.DeleteArrayElementAtIndex(i);
        Save(data);
        AssetDatabase.SaveAssets();
        return "Saved Rebellion-only station view, five nested level models, fog/team bindings and existing View-group registration. Run BuildStationLevels next.";
    }

    private static void Assign(SerializedProperty property, UnityEngine.Object[] objects)
    {
        property.arraySize = objects.Length;
        for (int i = 0; i < objects.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = objects[i];
    }
    private static void Save(SerializedObject data) { data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(data.targetObject); }
}
