using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using EmpireAtWar.ViewComponents.Health;
using EmpireAtWar.ViewComponents.Station;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

public static class VerifyRepublicStationMapping
{
    private const string TASK = "Temp/RepublicStationImport/";
    private const string PREFABS = "Assets/Prefabs/Models/Stations/";
    private const string VIEW = PREFABS + "RepublicSpaceStationView.prefab";
    private static readonly string[] PRESERVED = {
        PREFABS + "EmpireSpaceStationView.prefab", PREFABS + "RebellionSpaceStationView.prefab",
        PREFABS + "SeparatistSpaceStationView.prefab",
        "Assets/Settings/Data/Factions/Shared/StationLevelData.asset",
        "Assets/Settings/Data/Models/SpaceStation/SpaceStationData.asset"
    };

    public static string Main(bool capture)
    {
        var mapping = new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/Settings/AssetMappingData.asset"));
        var rows = mapping.FindProperty("assetMappings.keyValue");
        var mappings = new JObject();
        for (int i = 0; i < rows.arraySize; i++)
        {
            var row = rows.GetArrayElementAtIndex(i);
            mappings[row.FindPropertyRelative("key").stringValue] = row.FindPropertyRelative("value.m_AssetGUID").stringValue;
        }
        var hashes = new JObject();
        using (var hash = SHA256.Create())
            foreach (string path in PRESERVED) hashes[path] = BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path)));
        if (capture)
        {
            var station = AssetDatabase.LoadAssetAtPath<GameObject>(VIEW);
            var renderers = new SerializedObject(station.GetComponent<EmpireAtWar.Entities.SpaceStation.SpaceStation>()).FindProperty("explosionHullRenderers");
            Bounds bounds = ((Renderer)renderers.GetArrayElementAtIndex(0).objectReferenceValue).bounds;
            for (int i = 1; i < renderers.arraySize; i++) bounds.Encapsulate(((Renderer)renderers.GetArrayElementAtIndex(i).objectReferenceValue).bounds);
            var levelView = station.GetComponent<StationLevelView>();
            if (levelView != null)
            {
                var levels = new SerializedObject(levelView).FindProperty("levelModels");
                bounds = ((StationLevelModel)levels.GetArrayElementAtIndex(levels.arraySize - 1).objectReferenceValue).HullBounds;
            }
            var profiles = new JArray(station.GetComponentsInChildren<HardPoint>(true).OrderBy(h => h.Id).Select(h => new JObject { ["id"] = h.Id, ["unlock"] = h.UnlockLevel }));
            File.WriteAllText(TASK + "Before.json", new JObject { ["mappings"] = mappings, ["hashes"] = hashes, ["targetWidth"] = Mathf.Max(bounds.size.x, bounds.size.z), ["hardpoints"] = profiles }.ToString());
            return "Captured faction routing, other station prefabs and shared level/combat configuration.";
        }
        var before = JObject.Parse(File.ReadAllText(TASK + "Before.json"));
        var hullMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Models/SpaceStations/RepublicSpaceStation/RepublicSpaceStation_RepublicStationSlot00.mat");
        if (hullMaterial.GetFloat("_TeamRimStrength") != 0 || hullMaterial.GetFloat("_TeamMaskStrength") != 1 || hullMaterial.GetFloat("_TeamLiveryStrength") != 0)
            throw new InvalidOperationException("Republic station must recolor its stripe mask without a broad team rim.");
        if (!JToken.DeepEquals(before["hashes"], hashes)) throw new InvalidOperationException("Shared station data or another faction prefab changed.");
        foreach (var row in ((JObject)before["mappings"]).Properties().Where(p => p.Name != "RepublicSpaceStationView"))
            if (!JToken.DeepEquals(row.Value, mappings[row.Name])) throw new InvalidOperationException("Other faction routing changed: " + row.Name);
        string guid = AssetDatabase.AssetPathToGUID(VIEW);
        if ((string)mappings["RepublicSpaceStationView"] != guid) throw new InvalidOperationException("Republic mapping does not resolve to its own prefab.");
        if (mappings.Properties().Any(p => p.Name != "RepublicSpaceStationView" && (string)p.Value == guid))
            throw new InvalidOperationException("Republic station used by another mapping.");
        var entry = AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(guid);
        if (entry.address != "RepublicSpaceStationView") throw new InvalidOperationException("Incorrect Addressables address.");
        var root = PrefabUtility.LoadPrefabContents(VIEW);
        var results = new JArray();
        try
        {
            if (root.transform.localScale != Vector3.one) throw new InvalidOperationException("Gameplay root scale changed.");
            var view = root.GetComponent<StationLevelView>();
            var serialized = new SerializedObject(view);
            var levels = serialized.FindProperty("levelModels");
            var hardPointList = serialized.FindProperty("hardPoints");
            var collider = (BoxCollider)serialized.FindProperty("hullCollider").objectReferenceValue;
            if (levels.arraySize != 5 || hardPointList.arraySize != 17) throw new InvalidOperationException("Incorrect model/hardpoint mapping size.");
            var hardpoints = root.GetComponentsInChildren<HardPoint>(true).OrderBy(h => h.Id).ToArray();
            var profiles = (JArray)before["hardpoints"];
            if (hardpoints.Length != profiles.Count) throw new InvalidOperationException("Gameplay hardpoint count changed.");
            for (int i = 0; i < hardpoints.Length; i++)
            {
                if (hardpoints[i].Id != (int)profiles[i]["id"] || hardpoints[i].UnlockLevel != (int)profiles[i]["unlock"])
                    throw new InvalidOperationException("Gameplay hardpoint profile changed.");
                if (hardPointList.GetArrayElementAtIndex(i).objectReferenceValue != hardpoints[i])
                    throw new InvalidOperationException("Hardpoint order changed.");
            }
            var art = JObject.Parse(File.ReadAllText(TASK + "Attachments.json"));
            var audit = JObject.Parse(File.ReadAllText(TASK + "SourceAudit.json"));
            foreach (int level in new[] { 1, 5, 2, 3, 4, 5, 1 })
            {
                view.ApplyLevel(level);
                var model = (StationLevelModel)levels.GetArrayElementAtIndex(level - 1).objectReferenceValue;
                string path = PREFABS + "RepublicSpaceStationLevel" + level + ".prefab";
                if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(model.gameObject) != path)
                    throw new InvalidOperationException("Level does not use its own prefab: " + level);
                if (view.CurrentModel != model || root.GetComponentsInChildren<StationLevelModel>(true).Count(m => m.gameObject.activeSelf) != 1)
                    throw new InvalidOperationException("More than one station level active.");
                if (collider.center != model.HullBounds.center || collider.size != model.HullBounds.size)
                    throw new InvalidOperationException("Collider does not follow level.");
                if (model.ShieldMesh == null || model.ShieldPlanes.Length != 1024 || model.HullRenderers.Any(r => r == null || !r.enabled))
                    throw new InvalidOperationException("Missing hull or shield surface.");
                foreach (var mount in model.Mounts)
                    if (Vector3.Distance(hardpoints.Single(h => h.Id == mount.HardPointId).transform.position, mount.Point.position) > .001f)
                        throw new InvalidOperationException("Attachment does not follow level.");
                foreach (var piece in art[level.ToString()])
                {
                    var attachment = model.GetComponentsInChildren<Transform>(true).Single(t => t.name == (string)piece["model"]);
                    if (!attachment.parent.name.Equals((string)piece["bone"], StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Attachment parent changed.");
                }
                var source = (string)audit["models"]["RepublicSpaceStationLevel" + level]["file"];
                if (!source.EndsWith("ReB_Shipyard_Level_0" + level + ".ALO")) throw new InvalidOperationException("Incorrect original ALO.");
                var sourceRoot = model.transform.Find(model.name);
                var sourceBones = (JArray)audit["models"][model.name]["bones"];
                var matrices = new Matrix4x4[sourceBones.Count];
                var report = JArray.Parse(File.ReadAllText(TASK + "ConversionReport.json")).Single(r => (int)r["level"] == level);
                var transportNames = ((JObject)report["source_bone_names"]).Properties().Select(p => p.Name).ToArray();
                var transforms = sourceRoot.GetComponentsInChildren<Transform>(true);
                var basis = new Matrix4x4(new Vector4(-1, 0, 0, 0), new Vector4(0, 0, -1, 0), new Vector4(0, 1, 0, 0), new Vector4(0, 0, 0, 1));
                for (int index = 0; index < sourceBones.Count; index++)
                {
                    var matrix = Matrix4x4.identity;
                    for (int row = 0; row < 3; row++)
                        for (int col = 0; col < 4; col++) matrix[row, col] = (float)sourceBones[index]["matrix"][row * 4 + col];
                    long parent = (long)sourceBones[index]["parent_index"];
                    matrices[index] = parent == uint.MaxValue ? matrix : matrices[parent] * matrix;
                    string transport = transportNames[index];
                    var bone = transforms.Single(t => t.GetComponent<Renderer>() == null && PrefabUtility.GetCorrespondingObjectFromOriginalSource(t) != null && PrefabUtility.GetCorrespondingObjectFromOriginalSource(t).name == transport);
                    if (Quaternion.Angle(bone.rotation, sourceRoot.rotation * (basis * matrices[index] * basis.inverse).rotation) > .1f)
                        throw new InvalidOperationException("Authored bone rotation lost: " + bone.name);
                }
                results.Add(new JObject { ["level"] = level, ["prefab"] = path, ["source"] = source,
                    ["configuration"] = VIEW + ":StationLevelView.levelModels[" + (level - 1) + "]",
                    ["attachedPieces"] = ((JArray)art[level.ToString()]).Count });
            }
            var renderers = root.GetComponentsInChildren<MeshRenderer>(true);
            foreach (string componentName in new[] { "TeamColorView", "FogVisibilityComponent" })
            {
                var component = root.GetComponents<MonoBehaviour>().Single(c => c.GetType().Name == componentName);
                var list = new SerializedObject(component).FindProperty(componentName == "TeamColorView" ? "meshRenderers" : "renderers");
                var bound = Enumerable.Range(0, list.arraySize).Select(i => list.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
                if (bound.Length != renderers.Length || renderers.Any(r => !bound.Contains(r))) throw new InvalidOperationException("Incomplete renderer bindings.");
            }
            File.WriteAllText(TASK + "VerifiedMapping.json", results.ToString());
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        return "Republic-only routing, distinct source models, level changes, collider/attachments/shields, fog/team bindings, and preserved shared configuration verified.";
    }
}
