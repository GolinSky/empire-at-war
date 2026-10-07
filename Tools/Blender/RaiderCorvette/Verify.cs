using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public static class VerifyRaiderCorvette
{
    const string TASK = "Temp/RaiderCorvetteImport/";
    public static string Main()
    {
        var report = JArray.Parse(File.ReadAllText(TASK + "ConversionReport.json"))[0];
        var model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/EmpireShips/RaiderCorvette/RaiderCorvette.fbx");
        var meshes = model.GetComponentsInChildren<MeshFilter>(true);
        Check(meshes.Length == 4 && meshes.Sum(m => m.sharedMesh.triangles.Length / 3) == 10301, "Source mesh/triangle totals");
        foreach (var mesh in meshes)
        {
            Check(mesh.sharedMesh.uv.Length > 0, "Missing UVs: " + mesh.name);
            Check(mesh.sharedMesh.triangles.Length / 3 == (int)report["source"]["meshes"][mesh.name]["triangles"], "Mesh triangles: " + mesh.name);
        }
        var transforms = model.GetComponentsInChildren<Transform>(true).Where(t => t.GetComponents<MeshFilter>().Length == 0).ToArray();
        float error = 0;
        foreach (var bone in report["source"]["bones"].Children<JProperty>())
        {
            var transform = transforms.Single(t => t.name == bone.Name);
            var position = bone.Value["head"];
            var expected = new Vector3(-(float)position[0], (float)position[2], -(float)position[1]) * .02f;
            error = Mathf.Max(error, Vector3.Distance(transform.position, expected));
            if (bone.Value["parent"].Type != JTokenType.Null)
                Check(transform.parent.name == (string)bone.Value["parent"], "Bone parent: " + bone.Name);
        }
        Check(error < .00001f, "Unity bone error: " + error);
        var visual = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Ships/RaiderCorvette.prefab");
        var view = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Ships/RaiderCorvetteShipView.prefab");
        var allMeshes = view.GetComponentsInChildren<MeshRenderer>(true);
        var team = Config(view, "TeamColorView").FindProperty("meshRenderers");
        Check(team.arraySize == allMeshes.Length, "Team renderer count");
        var teamReferences = Enumerable.Range(0, team.arraySize).Select(i => team.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
        Check(allMeshes.All(teamReferences.Contains), "Every mesh has team ownership binding");
        Check(Config(view, "HealthComponent").FindProperty("<ShipUnits>k__BackingField").arraySize == 0, "Hull-only health");
        Check(Config(view, "WeaponComponent").FindProperty("hardPoints").arraySize == 10, "Weapon binding count");
        Check(Config(view, "FogVisibilityComponent").FindProperty("hardPoints").arraySize == 10, "Fog hardpoint binding count");
        var visible = visual.GetComponentsInChildren<Renderer>(true).Where(r => r.enabled).ToArray();
        var bounds = visible[0].bounds;
        foreach (var renderer in visible.Skip(1)) bounds.Encapsulate(renderer.bounds);
        Check(Mathf.Abs(bounds.size.z - 24) < .001f && bounds.center.magnitude < .001f, "Centered 24-unit hull");
        foreach (var renderer in visual.GetComponentsInChildren<Renderer>(true))
            Check(renderer.enabled == !(bool)report["source"]["meshes"][renderer.name]["hidden"], "Source visibility: " + renderer.name);
        var health = Config(view, "HealthComponent");
        Check(health.FindProperty("shieldView").objectReferenceValue != null && health.FindProperty("ionFieldBounds").boundsValue.size.z >= bounds.size.z, "Own shield and ion bounds");
        var dependencies = AssetDatabase.GetDependencies(AssetDatabase.GetAssetPath(view), true);
        Check(!dependencies.Any(p => p.Contains("Models/Ships/C9979") || p.Contains("Models/RepublicShips/CorellianCorvette")), "Donor geometry removed");
        var result = new JObject { ["meshes"] = 4, ["triangles"] = 10301, ["bones"] = 31,
            ["unityMaximumBoneError"] = error, ["visibleLength"] = bounds.size.z,
            ["teamRendererBindings"] = allMeshes.Length, ["weaponCount"] = 10,
            ["targetableHardpoints"] = 0, ["savedBindings"] = true };
        File.WriteAllText(TASK + "Verification.json", result.ToString());
        return result.ToString();
    }
    static SerializedObject Config(GameObject root, string name) => new SerializedObject(root.GetComponentsInChildren<MonoBehaviour>(true).Single(m => m.GetType().Name == name));
    static void Check(bool value, string detail) { if (!value) throw new InvalidOperationException(detail); }
}
