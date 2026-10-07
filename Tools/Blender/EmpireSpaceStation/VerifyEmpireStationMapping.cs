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

public static class VerifyEmpireStationMapping
{
    private const string TASK = "Temp/EmpireStationImport/";
    private const string PREFABS = "Assets/Prefabs/Models/Stations/";
    private const string VIEW = PREFABS + "EmpireSpaceStationView.prefab";
    private static readonly string[] PRESERVED = {
        PREFABS + "RepublicSpaceStationView.prefab", PREFABS + "RebellionSpaceStationView.prefab",
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
            File.WriteAllText(TASK + "Before.json", new JObject { ["mappings"] = mappings, ["hashes"] = hashes }.ToString());
            return "Captured faction routing, other station prefabs and shared level/combat configuration.";
        }
        var before = JObject.Parse(File.ReadAllText(TASK + "Before.json"));
        var hullMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Models/SpaceStations/EmpireSpaceStation/EmpireSpaceStation_EmpireStationSlot01.mat");
        if (hullMaterial.GetFloat("_TeamRimStrength") != 0 || hullMaterial.GetFloat("_TeamMaskStrength") != 1 || hullMaterial.GetFloat("_TeamLiveryStrength") != 0)
            throw new InvalidOperationException("Imperial station must recolor its stripe mask without a broad team rim.");
        if (!JToken.DeepEquals(before["hashes"], hashes)) throw new InvalidOperationException("Shared station data or another faction prefab changed.");
        foreach (var row in ((JObject)before["mappings"]).Properties().Where(p => p.Name != "EmpireSpaceStationView"))
            if (!JToken.DeepEquals(row.Value, mappings[row.Name])) throw new InvalidOperationException("Other faction routing changed: " + row.Name);
        string guid = AssetDatabase.AssetPathToGUID(VIEW);
        if ((string)mappings["EmpireSpaceStationView"] != guid) throw new InvalidOperationException("Empire mapping does not resolve to its own prefab.");
        if (mappings.Properties().Any(p => p.Name != "EmpireSpaceStationView" && (string)p.Value == guid))
            throw new InvalidOperationException("Imperial station used by another mapping.");
        var entry = AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(guid);
        if (entry.address != "EmpireSpaceStationView") throw new InvalidOperationException("Incorrect Addressables address.");
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
            var donor = AssetDatabase.LoadAssetAtPath<GameObject>(PREFABS + "RepublicSpaceStationView.prefab").GetComponentsInChildren<HardPoint>(true).OrderBy(h => h.Id).ToArray();
            if (hardpoints.Length != donor.Length) throw new InvalidOperationException("Gameplay hardpoint count changed.");
            for (int i = 0; i < hardpoints.Length; i++)
            {
                if (hardpoints[i].Id != donor[i].Id || hardpoints[i].UnlockLevel != donor[i].UnlockLevel)
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
                string path = PREFABS + "EmpireSpaceStationLevel" + level + ".prefab";
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
                var source = (string)audit["models"]["EmpireSpaceStationLevel" + level]["file"];
                if (!source.EndsWith("EB_STATION_0" + level + ".ALO")) throw new InvalidOperationException("Incorrect original ALO.");
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
        return "Empire-only routing, distinct source models, level changes, collider/attachments/shields, fog/team bindings, and preserved shared configuration verified.";
    }
}
