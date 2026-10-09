using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using EmpireAtWar.ViewComponents.Health;

public static class InspectImperialVenator
{
    public static string Main()
    {
        var root = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Ships/VenatorShipView.prefab");
        var report = new
        {
            hierarchy = root.GetComponentsInChildren<Transform>(true).Select(t => new { t.name, parent = t.parent == null ? null : t.parent.name, position = t.position, localPosition = t.localPosition, scale = t.localScale }),
            components = root.GetComponentsInChildren<MonoBehaviour>(true).Where(c => c != null).Select(c => new { type = c.GetType().Name, c.name, data = EditorJsonUtility.ToJson(c) }),
            weapons = root.GetComponentsInChildren<WeaponHardPoint>(true).Select(w => new { w.name, w.Id, w.WeaponType, position = w.transform.position, data = EditorJsonUtility.ToJson(w) }),
            renderers = root.GetComponentsInChildren<Renderer>(true).Select(r => new { r.name, r.enabled, bounds = r.bounds, materials = r.sharedMaterials.Select(m => AssetDatabase.GetAssetPath(m)) })
        };
        File.WriteAllText("Temp/ImperialVenatorImport/RepublicDonor.json", Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented, new Newtonsoft.Json.JsonSerializerSettings { ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore }));
        return string.Join("; ", root.GetComponentsInChildren<WeaponHardPoint>(true).Select(w => w.name + " = " + w.WeaponType + " @ " + w.transform.position));
    }
}
