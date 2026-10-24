using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using UnityEngine.Rendering;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.ViewComponents.Health;

public static class VerifyPelta
{
    const int SHIP_ID = 14;
    const string VIEW = "Assets/Prefabs/Models/Ships/Republic/PeltaShipView.prefab";
    const string DATA = "Assets/Settings/Data/Ship/Republic/PeltaShipData.asset";
    const string WRECK = "Assets/Prefabs/Models/Wrecks/PeltaWreckView.prefab";
    const string WRECK_DATA = "Assets/Settings/Data/Models/Wrecks/Republic/PeltaWreckData.asset";
    const string PREVIEW = "Assets/Prefabs/Ui/Reinforcement/PeltaReinforcementView.prefab";
    const float TOLERANCE = .0001f;

    public static string Main()
    {
        Check(!EditorApplication.isPlayingOrWillChangePlaymode, "Pelta verification requires Edit Mode.");
        var view = AssetDatabase.LoadAssetAtPath<GameObject>(VIEW);
        var preview = AssetDatabase.LoadAssetAtPath<GameObject>(PREVIEW);
        var wreck = AssetDatabase.LoadAssetAtPath<GameObject>(WRECK);
        foreach (var prefab in new[] {view, preview, wreck}) CheckReferences(prefab);
        Check(view.transform.localScale == Vector3.one && wreck.transform.localScale == Vector3.one, "Identity ship and wreck root scales");
        var hardpoints = view.GetComponentsInChildren<HardPoint>(true).OrderBy(p => p.Id).ToArray();
        Check(hardpoints.Length == 12 && hardpoints.Select(p => p.Id).SequenceEqual(Enumerable.Range(0, 12)), "Twelve ordered unique hardpoint IDs");
        var weapons = hardpoints.OfType<WeaponHardPoint>().ToArray();
        Check(weapons.Length == 10 && weapons.Count(p => p.WeaponType == WeaponType.TurboLaser) == 4 && weapons.Count(p => p.WeaponType == WeaponType.Laser) == 4 && weapons.Count(p => p.WeaponType == WeaponType.IonCannon) == 2, "Four turbolasers, four lasers and two ion cannons");
        Check(hardpoints[10].HardPointType == HardPointType.Engines && hardpoints[11].HardPointType == HardPointType.ShieldGenerator, "Engine and shield generator hardpoints");
        var health = Config(view, "HealthComponent");
        Check(References(health.FindProperty("<ShipUnits>k__BackingField")).OrderBy(o => ((HardPoint)o).Id).SequenceEqual(hardpoints.Cast<UnityEngine.Object>()), "All hardpoints bound to health");
        Check(References(Config(view, "WeaponComponent").FindProperty("hardPoints")).OrderBy(o => ((HardPoint)o).Id).SequenceEqual(weapons.Cast<UnityEngine.Object>()), "All ten weapons bound to weapon component");
        var injectables = References(Config(view, "EntityBindings").FindProperty("<Injectables>k__BackingField"));
        Check(hardpoints.All(injectables.Contains), "All hardpoints bound for dependency injection");
        var body = (Transform)Config(view, "ShipMoveComponent").FindProperty("bodyTransform").objectReferenceValue;
        Check(body.localScale == Vector3.one && hardpoints.All(p => p.transform.IsChildOf(body)), "Unit-scale banking body and mounted hardpoints");
        var bones = view.GetComponentsInChildren<Transform>(true);
        string[][] sockets = {new[] {"HP01", "HP02"}, new[] {"HP06", "HP07"}, new[] {"HP03", "HP04"}, new[] {"HP08", "HP09"}, new[] {"HP11"}, new[] {"HP12"}, new[] {"HP13"}, new[] {"HP14"}, new[] {"HP05"}, new[] {"HP10"}};
        for (int i = 0; i < sockets.Length; i++)
        {
            var midpoint = sockets[i].Select(name => bones.Single(t => t.name == name).position).Aggregate(Vector3.zero, (sum, p) => sum + p) / sockets[i].Length;
            Check(Vector3.Distance(hardpoints[i].transform.position, midpoint) < TOLERANCE, "Pelta source muzzle/midpoint " + i);
            Check(new SerializedObject(hardpoints[i]).FindProperty("mainBattery").boolValue == (i < 4), "Main battery classification " + i);
        }
        foreach (var point in hardpoints)
        {
            var anchor = (Transform)new SerializedObject(point).FindProperty("destroyedVfxAnchor").objectReferenceValue;
            Check(anchor != null && Vector3.Distance(anchor.position, point.transform.position) < TOLERANCE, "Destruction VFX anchor " + point.Id);
        }
        var renderers = view.GetComponentsInChildren<MeshRenderer>(true);
        var state = Component(view, "UnitRenderStateComponent");
        Check(injectables.Contains(state) && view.GetComponentsInChildren<MonoBehaviour>(true).Count(m => m.GetType().Name == "UnitRenderStateComponent") == 1, "Sole render state bound for dependency injection");
        var owned = References(new SerializedObject(state).FindProperty("meshRenderers"));
        Check(owned.Length == renderers.Length && renderers.All(r => owned.Contains(r)), "Every mesh renderer bound to team ownership");
        VerifyOwnership();
        var hull = renderers.Where(r => r.enabled && r.sharedMaterials.All(m => m.shader.name == "EmpireAtWar/Ship Lit")).ToArray();
        Check(hull.Length == 3 && hull.Sum(r => Mesh(r).triangles.Length / 3) == 2668 && hull.Select(r => r.name).OrderBy(n => n).SequenceEqual(new[] {"Hull_Fixed", "Wing_L", "Wing_R"}), "Three authored opaque meshes and 2668 triangles");
        var ownVisual = bones.Single(t => t.name == "Pelta" && t.parent == body);
        Check(ownVisual.GetComponentsInChildren<MeshRenderer>(true).Count(r => r.enabled) == 6, "All six source hull/effect meshes rendered");
        var points = hull.SelectMany(r => Mesh(r).vertices.Select(v => view.transform.InverseTransformPoint(r.transform.TransformPoint(v)))).ToArray();
        var bounds = new Bounds(points[0], Vector3.zero); foreach (var point in points) bounds.Encapsulate(point);
        Check(Mathf.Abs(bounds.size.z - 45) < .001f && bounds.center.magnitude < TOLERANCE, "Centered 45-unit Pelta hull");
        var engineTarget = new Vector3(bounds.center.x, bounds.center.y, bounds.min.z);
        var generatorTarget = view.transform.InverseTransformPoint(bones.Single(t => t.name == "Hull_Fixed" && t.GetComponents<MeshFilter>().Length == 0).position); generatorTarget.x = bounds.center.x; generatorTarget.y = bounds.max.y;
        for (int i = 10; i < 12; i++) Check(Vector3.Distance(hardpoints[i].transform.position, view.transform.TransformPoint(points.OrderBy(p => (p - (i == 10 ? engineTarget : generatorTarget)).sqrMagnitude).First())) < TOLERANCE, "Own hull surface anchor " + i);
        var fog = References(Config(view, "FogVisibilityComponent").FindProperty("renderers"));
        Check(ownVisual.GetComponentsInChildren<MeshRenderer>(true).All(fog.Contains) && fog.Contains(((Component)health.FindProperty("shieldView").objectReferenceValue).GetComponents<MeshRenderer>().Single()), "Hull, effects and shield fog bindings");
        foreach (var material in hull.SelectMany(r => r.sharedMaterials).Distinct()) CheckLivery(material);
        var data = Load(DATA);
        Check(Value(data, "Hull") == 2200 && Value(data, "Shields") == 1000, "Requested hull and shields");
        Check(Mathf.Abs(Value(data, "ShieldRegenerateValue") - 1000f / 120f) < TOLERANCE, "Shield regeneration matches shield capacity");
        Check(data.FindProperty("abilities").arraySize == 0 && data.FindProperty("hangarBays").arraySize == 0, "No abilities or hangar");
        var loadout = data.FindProperty("weaponLoadout"); int[] profiles = {1, 8, 10}, counts = {4, 4, 2};
        Check(loadout.arraySize == 3, "Three AI weapon profiles");
        for (int i = 0; i < 3; i++) Check(loadout.GetArrayElementAtIndex(i).FindPropertyRelative("weaponType").intValue == profiles[i] && loadout.GetArrayElementAtIndex(i).FindPropertyRelative("count").intValue == counts[i], "AI weapon loadout " + i);
        Check(data.FindProperty("<Wreck>k__BackingField").objectReferenceValue == Load(WRECK_DATA).targetObject && Load(WRECK_DATA).FindProperty("prefab.m_AssetGUID").stringValue == AssetDatabase.AssetPathToGUID(WRECK), "Own wreck data AssetReference");
        var wreckRenderers = wreck.GetComponentsInChildren<MeshRenderer>(true);
        Check(wreckRenderers.Length == 3, "Three opaque wreck meshes");
        for (int i = 0; i < hull.Length; i++)
        {
            var copy = wreckRenderers.Single(r => r.name == hull[i].name);
            Check(Mesh(copy) == Mesh(hull[i]), "Wreck source mesh " + copy.name);
            var sourceMatrix = view.transform.worldToLocalMatrix * hull[i].transform.localToWorldMatrix;
            var copyMatrix = wreck.transform.worldToLocalMatrix * copy.transform.localToWorldMatrix;
            for (int j = 0; j < 16; j++) Check(Mathf.Abs(sourceMatrix[j] - copyMatrix[j]) < TOLERANCE, "Wreck transform " + copy.name);
            Check(copy.sharedMaterials.Length == hull[i].sharedMaterials.Length, "Wreck material slot count");
            for (int j = 0; j < copy.sharedMaterials.Length; j++) CheckMaterial(hull[i].sharedMaterials[j], copy.sharedMaterials[j]);
        }
        Check(References(Config(wreck, "UnitWreckView").FindProperty("meshRenderers")).SequenceEqual(wreckRenderers.Cast<UnityEngine.Object>()) && References(Config(wreck, "UnitWreckView").FindProperty("meshFilters")).SequenceEqual(wreckRenderers.Select(r => (UnityEngine.Object)r.GetComponents<MeshFilter>().Single())), "Wreck renderer/filter bindings");
        Check(References(Config(preview, "UnitSpawnView").FindProperty("meshRenderers")).Length == 3 && preview.GetComponents<BoxCollider>().Single().isTrigger, "Own placement hull and trigger collider");
        foreach (var source in hull) Check(preview.GetComponentsInChildren<MeshFilter>(true).Any(f => f.sharedMesh == Mesh(source)), "Placement source mesh " + source.name);
        var faction = Entry(Load("Assets/Settings/Data/Factions/Republic/RepublicFaction.asset").FindProperty("ships.keyValue"), SHIP_ID);
        Check(faction.FindPropertyRelative("<Name>k__BackingField").stringValue == "Pelta-class Frigate" && faction.FindPropertyRelative("<Price>k__BackingField").intValue == 1800 && faction.FindPropertyRelative("<UnitCapacity>k__BackingField").intValue == 2 && faction.FindPropertyRelative("<BuildTime>k__BackingField").intValue == 20, "Republic roster and requested economy");
        var icon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Ui/Icons/ShipIcon/PeltaIcon.png");
        Check(icon != null && faction.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue == icon && Entry(Load("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset").FindProperty("shipIconWrapper.keyValue"), SHIP_ID).objectReferenceValue == icon, "Own roster and HUD icon");
        Check(faction.FindPropertyRelative("matchups").objectReferenceValue == AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/Settings/Data/Tooltip/Matchups/PeltaMatchups.asset"), "Own matchups asset");
        var tooltip = Load("Assets/Settings/Data/Tooltip/TooltipIconData.asset").FindProperty("icons");
        Check(Enumerable.Range(0, tooltip.arraySize).Count(i => tooltip.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue == faction.FindPropertyRelative("iconKey").stringValue && tooltip.GetArrayElementAtIndex(i).FindPropertyRelative("sprite").objectReferenceValue == icon) == 1, "Tooltip icon mapping");
        Check(Entry(Load("Assets/Settings/Data/Ship/Shared/ShipsData.asset").FindProperty("shipsData.keyValue"), SHIP_ID).FindPropertyRelative("m_AssetGUID").stringValue == AssetDatabase.AssetPathToGUID(DATA), "Ship data registration");
        Check(Entry(Load("Assets/Settings/Data/Reinforcement/ReinforcementData.asset").FindProperty("spawnShipWrapper.keyValue"), SHIP_ID).objectReferenceValue == Component(preview, "UnitSpawnView"), "Placement registration");
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        foreach (var path in new[] {VIEW, DATA, WRECK}) Check(settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(path)).address == Path.GetFileNameWithoutExtension(path), "Addressables " + path);
        Check(settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(WRECK)).parentGroup.Name == "View", "Wreck View Addressables group");
        foreach (var path in new[] {VIEW, DATA}) Check(Entry(Load("Assets/Settings/AssetMappingData.asset").FindProperty("assetMappings.keyValue"), Path.GetFileNameWithoutExtension(path)).FindPropertyRelative("m_AssetGUID").stringValue == AssetDatabase.AssetPathToGUID(path), "Asset mapping " + path);
        foreach (var path in new[] {DATA, WRECK_DATA}) Check(!AssetDatabase.GetDependencies(path, true).Contains(WRECK), "No hard wreck dependency " + path);
        var report = new JObject {["sourceModel"] = "ReV_Pelta.ALO", ["opaqueTriangles"] = 2668, ["visibleMeshes"] = 6, ["shipId"] = SHIP_ID, ["targetableHardpoints"] = 12, ["turbolasers"] = 4, ["lasers"] = 4, ["ionCannons"] = 2, ["hull"] = 2200, ["shields"] = 1000, ["cost"] = 1800, ["population"] = 2, ["buildSeconds"] = 20, ["length"] = bounds.size.z, ["teamUserValues1Through8"] = true, ["liveryAndWreckBindings"] = true, ["savedReferences"] = true};
        File.WriteAllText("Temp/PeltaImport/UnityVerification.json", report.ToString());
        return report.ToString();
    }

    static void CheckLivery(Material material)
    {
        Check(material.GetFloat("_TeamLiveryStrength") == 1 && material.GetFloat("_TeamLiveryHue") == 0 && Mathf.Abs(material.GetFloat("_TeamLiveryHueRange") - .07f) < TOLERANCE && Mathf.Abs(material.GetFloat("_TeamLiveryMinSaturation") - .3f) < TOLERANCE && material.GetFloat("_TeamMaskStrength") == 0, "Source-red HSV team livery " + material.name);
        if (material.shader.name == "EmpireAtWar/Ship Wreck") Check(material.GetFloat("_TeamRimStrength") == 0, "Neutral wreck panels " + material.name);
    }
    static void CheckMaterial(Material source, Material wreck)
    {
        Check(wreck.shader.name == "EmpireAtWar/Ship Wreck", "Current wreck shader"); CheckLivery(wreck);
        for (int i = 0; i < source.shader.GetPropertyCount(); i++)
        {
            string name = source.shader.GetPropertyName(i); if (!wreck.HasProperty(name)) continue;
            bool equal = source.shader.GetPropertyType(i) switch {
                ShaderPropertyType.Texture => source.GetTexture(name) == wreck.GetTexture(name) && source.GetTextureScale(name) == wreck.GetTextureScale(name) && source.GetTextureOffset(name) == wreck.GetTextureOffset(name),
                ShaderPropertyType.Color or ShaderPropertyType.Vector => (source.GetVector(name) - wreck.GetVector(name)).sqrMagnitude < TOLERANCE,
                ShaderPropertyType.Int => source.GetInteger(name) == wreck.GetInteger(name),
                _ => Mathf.Abs(source.GetFloat(name) - wreck.GetFloat(name)) < TOLERANCE};
            Check(equal, "Wreck material parity " + source.name + ":" + name);
        }
    }
    static void VerifyOwnership()
    {
        var root = PrefabUtility.LoadPrefabContents(VIEW);
        try
        {
            var state = Component(root, "UnitRenderStateComponent");
            var apply = state.GetType().GetMethod("ApplyUserValue", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            for (uint value = 1; value <= 8; value++) { apply.Invoke(state, new object[] {value}); Check(root.GetComponentsInChildren<MeshRenderer>(true).All(r => r.GetShaderUserValue() == value), "Team user value " + value); }
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
    static void CheckReferences(GameObject root)
    {
        Check(root != null, "Missing prefab");
        foreach (var transform in root.GetComponentsInChildren<Transform>(true)) Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) == 0, "Missing script " + transform.name);
        foreach (var component in root.GetComponentsInChildren<Component>(true))
        {
            var iterator = new SerializedObject(component).GetIterator();
            while (iterator.Next(true)) if (iterator.propertyType == SerializedPropertyType.ObjectReference) Check(iterator.objectReferenceValue != null || iterator.objectReferenceEntityIdValue.Equals(default(EntityId)), "Broken reference " + root.name + ":" + iterator.propertyPath);
        }
    }
    static Mesh Mesh(MeshRenderer renderer) => renderer.GetComponents<MeshFilter>().Single().sharedMesh;
    static UnityEngine.Object[] References(SerializedProperty array) => Enumerable.Range(0, array.arraySize).Select(i => array.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
    static SerializedProperty Entry(SerializedProperty array, object key) => Enumerable.Range(0, array.arraySize).Select(i => array.GetArrayElementAtIndex(i)).Single(row => key is int n ? row.FindPropertyRelative("key").intValue == n : row.FindPropertyRelative("key").stringValue == (string)key).FindPropertyRelative("value");
    static MonoBehaviour Component(GameObject root, string name) => root.GetComponentsInChildren<MonoBehaviour>(true).Single(m => m.GetType().Name == name);
    static SerializedObject Config(GameObject root, string name) => new SerializedObject(Component(root, name));
    static SerializedObject Load(string path) => new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>(path));
    static float Value(SerializedObject data, string name) => data.FindProperty("<" + name + ">k__BackingField").floatValue;
    static void Check(bool value, string detail) { if (!value) throw new InvalidOperationException(detail); }
}
