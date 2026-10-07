using System;
using System.IO;
using System.Linq;
using EmpireAtWar.Editor;
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
    // Preserve existing combat profiles/unlock levels; bind them to original ALO attachment names.
    private static readonly string[] ANCHORS = { "HP01_SHG_Bone", "FP01_TBL_00", "FP02_TBL_00", "FP04_TBL_00", "FP05_TBL_00",
        "FP01_LC_00", "FP01_CCM_00", "FP02_LC_00", "FP02_PRT_00", "FP03_CCM_00", "FP03_PRT_00", "FP03_IC_00",
        "FP04_LC_00", "FP04_IC_00", "FP05_CCM_00", "FP05_PRT_00", "FP05_IC_00" };

    public static string Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Station construction requires Edit Mode.");
        var donor = AssetDatabase.LoadAssetAtPath<GameObject>(DONOR);
        var donorShield = donor.GetComponentInChildren<Shield>(true);
        var donorHardpoints = donor.GetComponentsInChildren<HardPoint>(true).OrderBy(h => h.Id).ToArray();
        for (int level = 1; level <= 5; level++)
        {
            string path = PREFABS + "RebelSpaceStationLevel" + level + ".prefab";
            var modelRoot = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var model = modelRoot.GetComponent<StationLevelModel>();
                var transforms = modelRoot.GetComponentsInChildren<Transform>(true);
                var anchors = new Transform[ANCHORS.Length + 1];
                for (int i = 0; i < ANCHORS.Length; i++)
                {
                    string name = ANCHORS[i];
                    if (i == 0 && level >= 4) name = "HP04_SHG_Bone";
                    if (i == 2 && level == 5) name = "FP05_TBL2_00";
                    // Locked gameplay mounts remain at the station origin until their source geometry exists.
                    anchors[i] = donorHardpoints[i].UnlockLevel > level ? modelRoot.transform : transforms.Single(t => t.name == name);
                }
                var launch = modelRoot.transform.Cast<Transform>().FirstOrDefault(t => t.name == "GameplayLaunchExit");
                if (launch == null) { launch = new GameObject("GameplayLaunchExit").transform; launch.SetParent(modelRoot.transform, false); }
                // Spawn_00 is preserved; launch outside the station's collider so fighters do not start inside the hull.
                var spawn = transforms.Single(t => t.name == "Spawn_00");
                launch.position = spawn.position;
                launch.localPosition = new Vector3(launch.localPosition.x, model.HullBounds.min.y - 8f, launch.localPosition.z);
                anchors[anchors.Length - 1] = launch;
                var surface = UnityEngine.Object.Instantiate(donorShield.gameObject, modelRoot.transform);
                surface.name = "StationShieldBake";
                var shield = surface.GetComponent<Shield>();
                ShieldHullBaker.Bake(modelRoot.transform, shield);
                var shieldSerialized = new SerializedObject(shield);
                var planes = shieldSerialized.FindProperty("hullPlanes");
                var serialized = new SerializedObject(model);
                Assign(serialized.FindProperty("<AttachmentPoints>k__BackingField"), anchors);
                serialized.FindProperty("<ShieldMesh>k__BackingField").objectReferenceValue = surface.GetComponent<MeshFilter>().sharedMesh;
                var targetPlanes = serialized.FindProperty("<ShieldPlanes>k__BackingField");
                targetPlanes.arraySize = planes.arraySize;
                for (int i = 0; i < planes.arraySize; i++) targetPlanes.GetArrayElementAtIndex(i).vector4Value = planes.GetArrayElementAtIndex(i).vector4Value;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                if (surface.transform.localPosition.magnitude > .001f) throw new InvalidOperationException("Station hull must be centered before baking.");
                UnityEngine.Object.DestroyImmediate(surface);
                PrefabUtility.SaveAsPrefabAsset(modelRoot, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(modelRoot); }
        }

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
            var levelView = root.GetComponent<StationLevelView>();
            if (levelView == null) levelView = root.AddComponent<StationLevelView>();
            var models = new StationLevelModel[5];
            for (int i = 0; i < models.Length; i++)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFABS + "RebelSpaceStationLevel" + (i + 1) + ".prefab");
                models[i] = ((GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform)).GetComponent<StationLevelModel>();
                models[i].gameObject.layer = root.layer;
            }
            var targets = root.GetComponentsInChildren<HardPoint>(true).OrderBy(h => h.Id).Select(h => h.transform).Concat(new[] {root.transform.Find("HangarLaunchPoint")}).ToArray();
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
            var viewSerialized = new SerializedObject(levelView);
            Assign(viewSerialized.FindProperty("levelModels"), models);
            Assign(viewSerialized.FindProperty("attachmentTargets"), targets);
            viewSerialized.FindProperty("hullCollider").objectReferenceValue = root.GetComponent<BoxCollider>();
            viewSerialized.FindProperty("selectionMarker").objectReferenceValue = selectedCanvas;
            Save(viewSerialized);
            entitySerialized.Update();
            entitySerialized.FindProperty("levelView").objectReferenceValue = levelView;
            entitySerialized.FindProperty("levelShield").objectReferenceValue = shield;
            entitySerialized.FindProperty("levelShieldMesh").objectReferenceValue = shield.GetComponent<MeshFilter>();
            Assign(entitySerialized.FindProperty("explosionHullRenderers"), models[0].HullRenderers);
            Save(entitySerialized);
            var allRenderers = models.SelectMany(m => m.GetComponentsInChildren<MeshRenderer>(true)).Append(shield.GetComponent<MeshRenderer>()).ToArray();
            var team = new SerializedObject(root.GetComponents<MonoBehaviour>().Single(c => c.GetType().Name == "TeamColorView"));
            Assign(team.FindProperty("meshRenderers"), allRenderers); Save(team);
            var fog = new SerializedObject(root.GetComponents<MonoBehaviour>().Single(c => c.GetType().Name == "FogVisibilityComponent"));
            Assign(fog.FindProperty("renderers"), allRenderers);
            fog.FindProperty("revealRadius").floatValue = models.Max(m => new Vector2(m.HullBounds.extents.x, m.HullBounds.extents.z).magnitude);
            Save(fog);
            var health = new SerializedObject(root.GetComponents<MonoBehaviour>().Single(c => c.GetType().Name == "HealthComponent"));
            health.FindProperty("ionFieldBounds").boundsValue = models[4].HullBounds; Save(health);
            levelView.ApplyLevel(1);
            shield.SetHull(shield.GetComponent<MeshFilter>(), models[0].ShieldMesh, models[0].ShieldPlanes);
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
        return "Saved Rebellion-only station view, five level mappings, fitted shield shells, source attachment positions, fog/team bindings and existing View-group registration.";
    }

    private static void Assign(SerializedProperty property, UnityEngine.Object[] objects)
    {
        property.arraySize = objects.Length;
        for (int i = 0; i < objects.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = objects[i];
    }
    private static void Save(SerializedObject data) { data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(data.targetObject); }
}
