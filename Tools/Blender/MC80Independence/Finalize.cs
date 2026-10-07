using System;
using System.Linq;
using EmpireAtWar.Editor.Rendering;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json.Linq;

public static class FinalizeMC80Independence
{
    const string VIEW = "Assets/Prefabs/Models/Ships/MC80IndependenceShipView.prefab";
    const string WRECK_DATA = "Assets/Settings/Data/Ship/Wreck/MC80IndependenceWreckData.asset";
    static readonly string[] PREFABS = {
        "Assets/Prefabs/Models/Ships/MC80Independence.prefab", VIEW,
        "Assets/Prefabs/Models/Wrecks/MC80IndependenceWreckView.prefab",
        "Assets/Prefabs/Ui/Reinforcement/MC80IndependenceReinforcementView.prefab"
    };

    public static string Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Finalization requires Edit Mode.");
        var before = UnitHelperMeshStripper.FindHelpers(PREFABS, Array.Empty<string>());
        string stripReport = UnitHelperMeshStripper.Strip(PREFABS, Array.Empty<string>());
        int collisionMeshesRemoved = 0;
        foreach (string path in PREFABS)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                // Verified hidden ALO collision meshes use Coll_01/02, outside the generic helper-name pattern.
                foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true)
                    .Where(r => (r.name == "Coll_01" || r.name == "Coll_02") && !PrefabUtility.IsPartOfPrefabInstance(r.gameObject)).ToArray())
                {
                    if (renderer.enabled || renderer.transform.childCount != 0 || renderer.GetComponents<Component>().Length != 3)
                        throw new InvalidOperationException("Collision helper structure differs from the source audit.");
                    UnityEngine.Object.DestroyImmediate(renderer.gameObject);
                    collisionMeshesRemoved++;
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        var wreck = ShipWreckBuilder.Build(VIEW);
        var data = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(WRECK_DATA);
        var serialized = new SerializedObject(data);
        serialized.FindProperty("<Prefab>k__BackingField").objectReferenceValue = wreck;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
        var after = UnitHelperMeshStripper.FindHelpers(PREFABS, Array.Empty<string>());
        if (after.Any(h => h.BlockedBy == null)) throw new InvalidOperationException("MC80 prefab helpers remain.");
        var sourceAudit = JObject.Parse(System.IO.File.ReadAllText("Temp/MC80IndependenceImport/ConversionReport.json"));
        var hiddenNames = ((JObject)sourceAudit["before"]["meshes"]).Properties().Where(p => (bool)p.Value["hidden"]).Select(p => p.Name).ToArray();
        foreach (string path in PREFABS)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentsInChildren<MeshRenderer>(true).Any(r => hiddenNames.Contains(r.name)))
                throw new InvalidOperationException("An authored helper mesh remains in " + path);
        }
        var view = AssetDatabase.LoadAssetAtPath<GameObject>(VIEW);
        var sources = ShipWreckBuilder.GetSourceRenderers(view);
        var copies = wreck.GetComponentsInChildren<MeshRenderer>(true);
        if (sources.Count != 8 || copies.Length != sources.Count) throw new InvalidOperationException("Wreck must contain all eight original opaque hull renderers.");
        for (int i = 0; i < sources.Count; i++)
        {
            if (sources[i].GetComponent<MeshFilter>().sharedMesh != copies[i].GetComponent<MeshFilter>().sharedMesh)
                throw new InvalidOperationException("Wreck mesh order differs from the view.");
            for (int element = 0; element < 16; element++)
                if (Mathf.Abs(sources[i].transform.localToWorldMatrix[element] - copies[i].transform.localToWorldMatrix[element]) > .001f)
                    throw new InvalidOperationException("Wreck renderer transform differs from the view.");
            for (int slot = 0; slot < sources[i].sharedMaterials.Length; slot++)
            {
                var source = sources[i].sharedMaterials[slot];
                var copy = copies[i].sharedMaterials[slot];
                for (int property = 0; property < source.shader.GetPropertyCount(); property++)
                {
                    string name = source.shader.GetPropertyName(property);
                    if (!copy.HasProperty(name)) continue;
                    bool equal = source.shader.GetPropertyType(property) switch {
                        UnityEngine.Rendering.ShaderPropertyType.Texture => source.GetTexture(name) == copy.GetTexture(name) && source.GetTextureScale(name) == copy.GetTextureScale(name) && source.GetTextureOffset(name) == copy.GetTextureOffset(name),
                        UnityEngine.Rendering.ShaderPropertyType.Color or UnityEngine.Rendering.ShaderPropertyType.Vector => (source.GetVector(name) - copy.GetVector(name)).sqrMagnitude < .0001f,
                        UnityEngine.Rendering.ShaderPropertyType.Int => source.GetInteger(name) == copy.GetInteger(name),
                        _ => Mathf.Abs(source.GetFloat(name) - copy.GetFloat(name)) < .0001f
                    };
                    if (!equal) throw new InvalidOperationException("Wreck material differs at " + name);
                }
            }
        }
        var report = new JObject {
            ["helpersBefore"] = before.Count,
            ["helperTrianglesBefore"] = before.Sum(h => h.Triangles),
            ["collisionMeshesRemoved"] = collisionMeshesRemoved,
            ["stripping"] = stripReport,
            ["strippableHelpersAfter"] = after.Count(h => h.BlockedBy == null),
            ["sourceOpaqueRenderers"] = sources.Count,
            ["wreckRenderers"] = copies.Length,
            ["authoredHelperMeshesAfter"] = 0,
            ["wreckGeometryAndMaterialsMatch"] = true,
            ["wreckDataReference"] = AssetDatabase.GetAssetPath(wreck)
        };
        System.IO.File.WriteAllText("Temp/MC80IndependenceImport/PrefabFinalization.json", report.ToString());
        return report.ToString();
    }
}
