using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class Verify
{
    public static object Run()
    {
        const string VISUAL_PATH = "Assets/Prefabs/Models/Other/SpaceSensorNode.prefab";
        var visual = AssetDatabase.LoadAssetAtPath<GameObject>(VISUAL_PATH);
        var rows = new[] { "ReinforcementZone", "MainReinforcementZone" }.Select(name =>
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/View/ReinforcementZones/" + name + ".prefab");
            var relay = root.transform.Find("Relay/RelayModel");
            var renderers = relay.GetComponentsInChildren<MeshRenderer>(true);
            if (renderers.Length != 2 || renderers.Any(r => r.sharedMaterial == null))
                throw new InvalidOperationException("Invalid saved relay geometry/materials: " + name);
            var bounds = renderers[0].bounds;
            bounds.Encapsulate(renderers[1].bounds);
            return new { name, meshes = renderers.Select(r => r.GetComponent<MeshFilter>().sharedMesh.name).ToArray(),
                triangles = renderers.Sum(r => r.GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3),
                size = bounds.size.ToString("F4"), center = bounds.center.ToString("F4"),
                shader = renderers[0].sharedMaterial.shader.name };
        }).ToArray();
        var preview = new PreviewRenderUtility();
        try
        {
            preview.AddSingleGO(UnityEngine.Object.Instantiate(visual));
            preview.camera.clearFlags = CameraClearFlags.SolidColor;
            preview.camera.backgroundColor = new Color(0.035f, 0.05f, 0.075f, 1f);
            preview.camera.orthographic = true;
            preview.camera.orthographicSize = 32f;
            preview.camera.nearClipPlane = 0.1f;
            preview.camera.farClipPlane = 1000f;
            preview.camera.transform.position = new Vector3(75f, 48f, -90f);
            preview.camera.transform.LookAt(Vector3.zero);
            preview.lights[0].intensity = 1.2f;
            preview.lights[0].transform.rotation = Quaternion.Euler(40f, 35f, 0f);
            preview.lights[1].intensity = 0.8f;
            preview.lights[1].transform.rotation = Quaternion.Euler(320f, 210f, 0f);
            preview.ambientColor = new Color(0.25f, 0.25f, 0.25f, 1f);
            preview.BeginStaticPreview(new Rect(0, 0, 900, 900));
            preview.Render(true, true);
            var image = preview.EndStaticPreview();
            File.WriteAllBytes("Temp/ZoneRelayImport/UnityPreview.png", image.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(image);
        }
        finally
        {
            preview.Cleanup();
        }
        var report = Newtonsoft.Json.JsonConvert.SerializeObject(rows, Newtonsoft.Json.Formatting.Indented);
        File.WriteAllText("Temp/ZoneRelayImport/UnityVerification.json", report);
        return rows;
    }
}
