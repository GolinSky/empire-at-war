using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using UnityEngine.Rendering;
using EmpireAtWar.ViewComponents.Health;

public static class VerifyHeavyDreadnought
{
    const string TASK = "Temp/HeavyDreadnoughtImport/";
    const string VISUAL = "Assets/Prefabs/Models/Ships/Republic/HeavyDreadnought.prefab";
    const string VIEW = "Assets/Prefabs/Models/Ships/Republic/HeavyDreadnoughtShipView.prefab";
    const string MODEL = "Assets/Art/Models/RepublicShips/HeavyDreadnought/HeavyDreadnought.fbx";
    const string DATA = "Assets/Settings/Data/Ship/Republic/HeavyDreadnoughtShipData.asset";
    const string WRECK = "Assets/Prefabs/Models/Wrecks/HeavyDreadnoughtWreckView.prefab";
    const string WRECK_DATA = "Assets/Settings/Data/Models/Wrecks/Republic/HeavyDreadnoughtWreckData.asset";
    const string PREVIEW = "Assets/Prefabs/Ui/Reinforcement/HeavyDreadnoughtReinforcementView.prefab";
    const string WRECK_GUID = "757a79319c0388c469ac8ac2e2c80f92";
    const float TOLERANCE = .001f;
    static readonly List<string> _checks = new List<string>();

    public static string Main()
    {
        Directory.CreateDirectory(TASK); _checks.Clear();
        try
        {
            Verify();
            var report = new JObject { ["success"] = true, ["opaqueMeshes"] = 25, ["opaqueTriangles"] = 8110, ["hardpoints"] = 8, ["checks"] = new JArray(_checks) };
            File.WriteAllText(TASK + "Verification.json", report.ToString());
            return report.ToString();
        }
        catch (Exception error)
        {
            File.WriteAllText(TASK + "Verification.json", new JObject { ["success"] = false, ["error"] = error.ToString(), ["checks"] = new JArray(_checks) }.ToString());
            throw;
        }
    }

    static void Verify()
    {
        Check(!EditorApplication.isPlayingOrWillChangePlaymode, "Saved-asset verification requires Edit Mode");
        var visual = AssetDatabase.LoadAssetAtPath<GameObject>(VISUAL);
        var view = AssetDatabase.LoadAssetAtPath<GameObject>(VIEW);
        var preview = AssetDatabase.LoadAssetAtPath<GameObject>(PREVIEW);
        var wreck = AssetDatabase.LoadAssetAtPath<GameObject>(WRECK);
        Check(AssetDatabase.LoadAssetAtPath<GameObject>(MODEL) != null, "Converted FBX is imported");
        foreach (var prefab in new[] {visual, view, preview, wreck}) CheckReferences(prefab);
        foreach (var path in new[] {VISUAL, VIEW, PREVIEW, WRECK})
            Check(!AssetDatabase.GetDependencies(path, true).Any(p => p.EndsWith("/HeavyDreadnought.blend", StringComparison.OrdinalIgnoreCase)), "Old Blender model is absent: " + path);
        Check(new[] {visual, view, preview, wreck}.All(p => p.transform.localScale == Vector3.one), "All prefab roots have unit scale");
        var hull = Hull(view); var visualHull = Hull(visual);
        Check(hull.Length == 25 && visualHull.Length == 25 && Triangles(hull) == 8110 && Triangles(visualHull) == 8110, "Complete hull and twelve paired turret meshes: 25 meshes, 8110 triangles");
        Check(hull.All(r => visualHull.Any(v => Mesh(v) == Mesh(r))), "Gameplay uses dedicated visual meshes");
        var body = (Transform)Config(view, "ShipMoveComponent").FindProperty("bodyTransform").objectReferenceValue;
        Check(body != null && body.name == "Body" && body.localScale == Vector3.one && body.IsChildOf(view.transform), "Explicit unit-scale Body banking pivot");
        var points = view.GetComponentsInChildren<HardPoint>(true).OrderBy(p => p.Id).ToArray();
        var weapons = points.OfType<WeaponHardPoint>().ToArray();
        Check(points.Length == 8 && points.Select(p => p.Id).SequenceEqual(Enumerable.Range(0, 8)), "Eight unique hardpoint IDs 0 through 7");
        Check(weapons.Length == 6 && new[] {2, 3, 11}.All(t => weapons.Count(p => (int)p.WeaponType == t) == 2), "Existing six weapon profiles retained");
        Check(points.All(p => p.transform.IsChildOf(body)) && hull.All(r => r.transform.IsChildOf(body)), "Hull and every hardpoint bank together");
        foreach (var point in points) Check(new SerializedObject(point).FindProperty("destroyedVfxAnchor").objectReferenceValue != null, "Hardpoint VFX anchor " + point.Id);
        var health = Config(view, "HealthComponent");
        Same(References(health.FindProperty("<ShipUnits>k__BackingField")), points, "All eight hardpoints remain targetable");
        Same(References(Config(view, "WeaponComponent").FindProperty("hardPoints")), weapons, "Weapon component bindings");
        var ownership = One(view, "UnitRenderStateComponent");
        var injectables = References(Config(view, "EntityBindings").FindProperty("<Injectables>k__BackingField"));
        Check(points.All(injectables.Contains) && injectables.Count(o => o == ownership) == 1, "Explicit hardpoint and ownership injection");
        Same(References(new SerializedObject(ownership).FindProperty("meshRenderers")), view.GetComponentsInChildren<MeshRenderer>(true), "Ownership covers every mesh renderer");
        Same(References(Config(view, "Ship").FindProperty("explosionHullRenderers")), hull, "Explosion uses opaque hull only");
        Same(References(Config(view, "FogVisibilityComponent").FindProperty("renderers")), view.GetComponentsInChildren<Renderer>(true).Where(r => !(r is SpriteRenderer)), "Fog covers hull, effects, shield and movement line");
        var shield = (UnityEngine.Component)health.FindProperty("shieldView").objectReferenceValue;
        Check(shield != null && shield.GetComponents<MeshRenderer>().Length == 1 && Mesh(shield.GetComponents<MeshRenderer>().Single()) != null, "Baked shield and explicit health binding");
        var vertices = hull.SelectMany(r => Mesh(r).vertices.Select(v => view.transform.InverseTransformPoint(r.transform.TransformPoint(v)))).ToArray();
        var collider = view.GetComponents<BoxCollider>().Single();
        Check(vertices.All(p => new Bounds(collider.center, collider.size + Vector3.one * TOLERANCE).Contains(p)), "Collider contains replacement hull");
        var data = Load(DATA);
        foreach (var item in new[] {new {name = "Hull", value = 3600f}, new {name = "Shields", value = 2500f}, new {name = "Speed", value = 18f}, new {name = "RotationSpeed", value = 7.5f}, new {name = "TurnAcceleration", value = 7.5f}, new {name = "BodyRotationMaxAngle", value = 15f}})
            Check(Mathf.Abs(Value(data, item.name) - item.value) < TOLERANCE, "Unchanged " + item.name);
        Check(data.FindProperty("abilities").arraySize == 1 && data.FindProperty("abilities").GetArrayElementAtIndex(0).intValue == 6, "Existing ability 6 retained");
        var loadout = data.FindProperty("weaponLoadout");
        Check(loadout.arraySize == 3 && new[] {2, 3, 11}.All(t => Enumerable.Range(0, 3).Count(i => loadout.GetArrayElementAtIndex(i).FindPropertyRelative("weaponType").intValue == t && loadout.GetArrayElementAtIndex(i).FindPropertyRelative("count").intValue == 2) == 1), "Unchanged AI weapon loadout");
        var banked = new[] {-15f, 0, 15f}.SelectMany(a => vertices.Select(p => Quaternion.Euler(0, 0, a) * p)).ToArray();
        Check(banked.All(p => p.y >= Value(data, "HullBottom") - TOLERANCE && p.y <= Value(data, "HullTop") + TOLERANCE), "Height bounds contain banked hull");
        Check(vertices.All(p => new Vector2(p.x, p.z).magnitude <= Value(data, "NavigationRadius") + TOLERANCE), "Navigation radius contains hull");
        Check(banked.All(p => health.FindProperty("ionFieldBounds").boundsValue.Contains(p)), "Ion effect bounds contain banked hull");
        foreach (var material in hull.SelectMany(r => r.sharedMaterials).Distinct()) CheckLivery(material);
        var wreckRenderers = wreck.GetComponentsInChildren<MeshRenderer>(true);
        Check(wreckRenderers.Length == 25 && Triangles(wreckRenderers) == 8110, "Current generated wreck has all 25 hull meshes and 8110 triangles");
        foreach (var source in hull)
        {
            var copy = wreckRenderers.Single(r => r.name == source.name);
            Check(Mesh(copy) == Mesh(source), "Wreck source mesh " + source.name);
            MatrixEqual(view.transform.worldToLocalMatrix * source.transform.localToWorldMatrix, wreck.transform.worldToLocalMatrix * copy.transform.localToWorldMatrix, "Wreck transform " + source.name);
            Check(copy.sharedMaterials.Length == source.sharedMaterials.Length, "Wreck material slots " + source.name);
            for (int i = 0; i < source.sharedMaterials.Length; i++) CheckMaterial(source.sharedMaterials[i], copy.sharedMaterials[i]);
        }
        Same(References(Config(wreck, "UnitWreckView").FindProperty("meshRenderers")), wreckRenderers, "Wreck renderer array");
        Check(References(Config(wreck, "UnitWreckView").FindProperty("meshFilters")).SequenceEqual(References(Config(wreck, "UnitWreckView").FindProperty("meshRenderers")).Cast<MeshRenderer>().Select(r => (UnityEngine.Object)r.GetComponents<MeshFilter>().Single())), "Wreck filter/renderer pairs align");
        var wreckData = Load(WRECK_DATA);
        Check(data.FindProperty("<Wreck>k__BackingField").objectReferenceValue == wreckData.targetObject && wreckData.FindProperty("prefab.m_AssetGUID").stringValue == WRECK_GUID && AssetDatabase.AssetPathToGUID(WRECK) == WRECK_GUID, "Existing Addressable wreck identity retained");
        Check(Value(wreckData, "MinParts") == 2 && Value(wreckData, "MaxParts") == 3 && Value(wreckData, "Lifetime") == 30 && Value(wreckData, "DissolveDuration") == 8, "Existing procedural wreck timing and part count retained");
        Check(AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(WRECK_GUID).parentGroup.Name == "View", "Wreck remains in View Addressables");
        foreach (var path in new[] {DATA, WRECK_DATA}) Check(!AssetDatabase.GetDependencies(path, true).Contains(WRECK), "No hard wreck-prefab dependency: " + path);
        var spawn = Config(preview, "UnitSpawnView"); var previewRenderers = preview.GetComponentsInChildren<MeshRenderer>(true);
        Same(References(spawn.FindProperty("meshRenderers")), previewRenderers, "Placement renderer array");
        Check(previewRenderers.Length == 25 && Triangles(previewRenderers) == 8110, "Placement shows complete replacement geometry");
        var hologram = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Vfx/Hologram.mat");
        Check(spawn.FindProperty("hologramMaterial").objectReferenceValue == hologram && previewRenderers.All(r => r.sharedMaterials.All(m => m == hologram)), "Placement uses tintable hologram slots");
        var previewCollider = preview.GetComponents<BoxCollider>().Single();
        Check(previewCollider.isTrigger && Vector3.Distance(previewCollider.center, collider.center) < TOLERANCE && Vector3.Distance(previewCollider.size, collider.size) < TOLERANCE, "Placement trigger matches gameplay collider");
        foreach (var source in hull) Check(previewRenderers.Any(r => Mesh(r) == Mesh(source)), "Placement source mesh " + source.name);
    }

    static void CheckMaterial(Material source, Material wreck)
    {
        Check(wreck.shader.name == "EmpireAtWar/Ship Wreck", "Current wreck shader"); CheckLivery(wreck);
        Check(wreck.GetFloat("_TeamRimStrength") == 0, "Neutral wreck panels");
        for (int i = 0; i < source.shader.GetPropertyCount(); i++)
        {
            string name = source.shader.GetPropertyName(i); if (!wreck.HasProperty(name) || name == "_TeamRimStrength") continue;
            bool equal = source.shader.GetPropertyType(i) switch {
                ShaderPropertyType.Texture => source.GetTexture(name) == wreck.GetTexture(name) && source.GetTextureScale(name) == wreck.GetTextureScale(name) && source.GetTextureOffset(name) == wreck.GetTextureOffset(name),
                ShaderPropertyType.Color or ShaderPropertyType.Vector => (source.GetVector(name) - wreck.GetVector(name)).sqrMagnitude < TOLERANCE,
                ShaderPropertyType.Int => source.GetInteger(name) == wreck.GetInteger(name),
                _ => Mathf.Abs(source.GetFloat(name) - wreck.GetFloat(name)) < TOLERANCE};
            Check(equal, "Wreck material parity " + source.name + ":" + name);
        }
    }
    static void CheckLivery(Material material) => Check(material.GetFloat("_TeamLiveryStrength") == 1 && material.GetFloat("_TeamLiveryHue") == 0 && Mathf.Abs(material.GetFloat("_TeamLiveryHueRange") - .07f) < TOLERANCE && Mathf.Abs(material.GetFloat("_TeamLiveryMinSaturation") - .4f) < TOLERANCE && material.GetFloat("_TeamMaskStrength") == 0, "Source-red team livery " + material.name);
    static void CheckReferences(GameObject root)
    {
        Check(root != null, "Required prefab exists");
        foreach (var transform in root.GetComponentsInChildren<Transform>(true)) Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) == 0, "No missing script: " + transform.name);
        foreach (var component in root.GetComponentsInChildren<UnityEngine.Component>(true))
        {
            var iterator = new SerializedObject(component).GetIterator();
            while (iterator.Next(true)) if (iterator.propertyType == SerializedPropertyType.ObjectReference) Check(iterator.objectReferenceValue != null || iterator.objectReferenceEntityIdValue.Equals(default(EntityId)), "No broken reference: " + root.name + ":" + iterator.propertyPath);
        }
    }
    static void MatrixEqual(Matrix4x4 source, Matrix4x4 copy, string detail) { for (int i = 0; i < 16; i++) Check(Mathf.Abs(source[i] - copy[i]) < TOLERANCE, detail); }
    static void Same(IEnumerable<UnityEngine.Object> actual, IEnumerable<UnityEngine.Object> expected, string detail)
    {
        var values = actual.ToArray(); var targets = expected.ToArray();
        Check(values.Length == targets.Length && !values.Except(targets).Any() && values.Distinct().Count() == values.Length, detail);
    }
    static int Triangles(IEnumerable<MeshRenderer> renderers) => renderers.Sum(r => Mesh(r).triangles.Length / 3);
    static Mesh Mesh(MeshRenderer renderer) => renderer.GetComponents<MeshFilter>().Single().sharedMesh;
    static MeshRenderer[] Hull(GameObject root) => root.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled && r.sharedMaterials.All(m => m.shader.name == "EmpireAtWar/Ship Lit")).ToArray();
    static UnityEngine.Object[] References(SerializedProperty array) => Enumerable.Range(0, array.arraySize).Select(i => array.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
    static MonoBehaviour One(GameObject root, string name) => root.GetComponentsInChildren<MonoBehaviour>(true).Single(m => m.GetType().Name == name);
    static SerializedObject Config(GameObject root, string name) => new SerializedObject(One(root, name));
    static SerializedObject Load(string path) => new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>(path));
    static float Value(SerializedObject data, string name) { var field = data.FindProperty("<" + name + ">k__BackingField"); return field.propertyType == SerializedPropertyType.Integer ? field.intValue : field.floatValue; }
    static void Check(bool value, string detail) { if (!value) throw new InvalidOperationException(detail); _checks.Add(detail); }
}
