using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Editor;
using EmpireAtWar.Editor.Rendering;
using EmpireAtWar.ViewComponents.Health;
using Object = UnityEngine.Object;

public static class BuildRecusantDreadnoughtShip
{
    private const string NAME = "RecusantDreadnought";
    private const string SHIPS = "Assets/Prefabs/Models/Ships/Separatist/";
    private const string DONOR = SHIPS + "View/InvincibleShipView.prefab";
    private const string VISUAL = SHIPS + NAME + ".prefab";
    private const string VIEW = SHIPS + "View/" + NAME + "ShipView.prefab";
    private const string PLACEMENTS = "Assets/Prefabs/Ui/Reinforcement/";
    private const string TOOLS = "Tools/Blender/" + NAME + "/";
    private const string TEMP = "Temp/" + NAME + "Import/";

    public static string Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Build the Recusant Dreadnought in Edit Mode.");

        GameObject root = PrefabUtility.LoadPrefabContents(DONOR);
        Bounds bounds;
        try
        {
            root.name = NAME + "ShipView";
            var move = Config(root, "ShipMoveComponent");
            var body = (Transform)move.FindProperty("bodyTransform").objectReferenceValue;
            Object.DestroyImmediate(body.Find("Invincible").gameObject);
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(VISUAL), body);
            Transform[] sockets = visual.GetComponentsInChildren<Transform>(true);
            MeshRenderer[] renderers = visual.GetComponentsInChildren<MeshRenderer>(true);
            MeshRenderer[] hull = renderers.Where(renderer =>
                renderer.sharedMaterials.All(material => material.shader.name == "EmpireAtWar/Ship Lit")).ToArray();
            Vector3[] points = hull.SelectMany(renderer => renderer.GetComponent<MeshFilter>().sharedMesh.vertices
                .Select(vertex => root.transform.InverseTransformPoint(renderer.transform.TransformPoint(vertex)))).ToArray();
            bounds = BoundsOf(points);
            if (Mathf.Abs(bounds.size.z - 266.90634f) > 0.02f)
                throw new InvalidOperationException("Unexpected hull length " + bounds.size.z);

            MonoBehaviour weapon = Component(root, "WeaponComponent");
            WeaponHardPoint[] donorWeapons = weapon.GetComponentsInChildren<WeaponHardPoint>(true);
            var mountRows = JArray.Parse(File.ReadAllText(TOOLS + "WeaponMounts.json"));
            var originalBySocket = mountRows.Where(row => !(bool)row["duplicate"])
                .ToDictionary(row => (string)row["socket"]);
            var weapons = mountRows.Select(row => BuildWeapon(root.transform, weapon.transform, donorWeapons[0], sockets,
                points, row, originalBySocket[(string)row["socket"]])).ToArray();
            foreach (Transform donorChild in donorWeapons.Select(point => point.transform).ToArray())
                Object.DestroyImmediate(donorChild.gameObject);

            HardPoint[] oldSystems = root.GetComponentsInChildren<HardPoint>(true).Where(point => point is not WeaponHardPoint).ToArray();
            HardPoint engineTemplate = oldSystems.Single(point => point.HardPointType == HardPointType.Engines);
            var hangar = Config(root, "HangarComponent");
            var hangarPoint = (HardPoint)hangar.FindProperty("hangarHardPoint").objectReferenceValue;
            int nextId = weapons.Length;
            var systems = JArray.Parse(File.ReadAllText(TOOLS + "SystemMounts.json")).Select(row =>
            {
                string name = (string)row["sourceId"];
                HardPoint point = name == "Hangar" ? hangarPoint : Object.Instantiate(engineTemplate, body);
                point.name = name;
                Transform socket = Socket(sockets, (string)row["socket"]);
                point.transform.position = socket.position;
                point.transform.localRotation = Quaternion.identity;
                point.transform.localScale = Vector3.one;
                ConfigurePoint(root.transform, point, points, nextId++);
                return point;
            }).ToArray();
            foreach (HardPoint old in oldSystems.Where(point => point != hangarPoint))
                Object.DestroyImmediate(old.gameObject);

            var launch = (Transform)hangar.FindProperty("launchPoint").objectReferenceValue;
            launch.position = root.transform.TransformPoint(new Vector3(0, bounds.min.y - 4f, hangarPoint.transform.localPosition.z));
            launch.rotation = root.transform.rotation;
            launch.localScale = Vector3.one;
            hangar.FindProperty("isDestroyable").boolValue = true;
            Save(hangar);

            HardPoint[] allPoints = weapons.Cast<HardPoint>().Concat(systems).ToArray();
            MonoBehaviour health = Component(root, "HealthComponent");
            SetReferences(health, "<ShipUnits>k__BackingField", allPoints);
            SetReferences(weapon, "hardPoints", weapons);
            var renderState = Component(root, "UnitRenderStateComponent");
            SetReferences(Component(root, "EntityBindings"), "<Injectables>k__BackingField",
                allPoints.Cast<Object>().Append(renderState).ToArray());
            SetReferences(Component(root, "Ship"), "explosionHullRenderers", hull);
            var shield = (Shield)new SerializedObject(health).FindProperty("shieldView").objectReferenceValue;
            ShieldHullBaker.Bake(root.transform, shield);
            SetReferences(renderState, "meshRenderers", renderers.Append(shield.GetComponent<MeshRenderer>()).ToArray());
            SetReferences(Component(root, "FogVisibilityComponent"), "renderers",
                root.GetComponentsInChildren<Renderer>(true).Where(renderer => renderer is not SpriteRenderer).ToArray());

            Bounds banked = BoundsOf(new[] {-10f, 0f, 10f}.SelectMany(angle =>
                points.Select(point => Quaternion.Euler(0, 0, angle) * point)).ToArray());
            float radius = Mathf.Ceil(points.Max(point => new Vector2(point.x, point.z).magnitude)) + 2f;
            var healthConfig = new SerializedObject(health);
            healthConfig.FindProperty("ionFieldBounds").boundsValue = new Bounds(banked.center, banked.size * 1.05f);
            Save(healthConfig);
            var collider = root.GetComponent<BoxCollider>();
            collider.center = bounds.center;
            collider.size = bounds.size;
            var selection = Config(root, "SelectionComponent");
            selection.FindProperty("ringSize").vector2Value = Vector2.one * (radius * 2f);
            var selectionRoot = (Transform)selection.FindProperty("selectionRoot").objectReferenceValue;
            selectionRoot.localPosition = new Vector3(0, banked.min.y - 2f, 0);
            selectionRoot.localScale = Vector3.one;
            var selected = (SpriteRenderer)selection.FindProperty("selectedRenderer").objectReferenceValue;
            Vector3 sprite = selected.sprite.bounds.size;
            selected.transform.localPosition = Vector3.zero;
            selected.transform.localScale = new Vector3(radius * 2f / sprite.x, radius * 2f / sprite.y, 1f);
            Save(selection);
            PrefabUtility.SaveAsPrefabAsset(root, VIEW);
            Directory.CreateDirectory(TEMP);
            File.WriteAllText(TEMP + "GameplayBounds.json", JsonConvert.SerializeObject(new
            {
                size = new[] {bounds.size.x, bounds.size.y, bounds.size.z},
                bottom = banked.min.y, top = banked.max.y, radius
            }));
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        BuildPlacement(bounds);
        ShipWreckBuilder.Build(VIEW);
        AssetDatabase.SaveAssets();
        return "Saved gameplay view, fitted shields, placement preview and wreck.";
    }

    private static WeaponHardPoint BuildWeapon(Transform root, Transform weaponRoot, WeaponHardPoint template,
        Transform[] sockets, Vector3[] hull, JToken row, JToken original)
    {
        float parentYaw = (float)row["parentYaw"];
        Transform parent = weaponRoot;
        if (!Mathf.Approximately(parentYaw, 0f))
        {
            parent = new GameObject((string)row["sourceId"] + "_FiringFrame").transform;
            parent.SetParent(weaponRoot, false);
            parent.localRotation = Quaternion.Euler(0, parentYaw, 0);
        }

        Transform socket = Socket(sockets, (string)row["socket"]);
        Vector3 position = ReadVector(row["position"]);
        Vector3 expected = ReadVector(original["position"]);
        Vector3 actual = root.InverseTransformPoint(socket.position);
        if (Vector3.Distance(expected, actual) > 0.2f)
            throw new InvalidOperationException($"Source socket {socket.name} expected {expected}, imported {actual}.");

        var point = Object.Instantiate(template, parent);
        var type = (WeaponType)(int)row["weaponType"];
        int id = (int)row["id"];
        point.name = $"{type}_{id:00}";
        point.transform.position = root.TransformPoint(actual + position - expected);
        point.transform.localRotation = Quaternion.identity;
        point.transform.localScale = Vector3.one;
        ConfigurePoint(root, point, hull, id);

        var config = new SerializedObject(point);
        config.FindProperty("<WeaponType>k__BackingField").intValue = (int)type;
        var range = config.FindProperty("yAxisRange");
        range.FindPropertyRelative("<Min>k__BackingField").floatValue = (float)row["minYaw"];
        range.FindPropertyRelative("<Max>k__BackingField").floatValue = (float)row["maxYaw"];
        config.FindProperty("mainBattery").boolValue = type == WeaponType.HeavyOctupleTurbolaser;
        Save(config);
        return point;
    }

    private static void ConfigurePoint(Transform root, HardPoint point, Vector3[] hull, int id)
    {
        var config = new SerializedObject(point);
        config.FindProperty("<Id>k__BackingField").intValue = id;
        config.FindProperty("destroyedVfxScale").floatValue = 1f;
        var anchor = (Transform)config.FindProperty("destroyedVfxAnchor").objectReferenceValue;
        Vector3 origin = root.InverseTransformPoint(point.transform.position);
        anchor.position = root.TransformPoint(hull.OrderBy(vertex => (vertex - origin).sqrMagnitude).First());
        anchor.localScale = Vector3.one;
        Save(config);
    }

    private static void BuildPlacement(Bounds bounds)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PLACEMENTS + "InvincibleReinforcementView.prefab");
        try
        {
            root.name = NAME + "ReinforcementView";
            foreach (Transform child in root.transform.Cast<Transform>().ToArray())
                Object.DestroyImmediate(child.gameObject);
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(VISUAL), root.transform);
            MeshRenderer[] renderers = visual.GetComponentsInChildren<MeshRenderer>(true);
            Material hologram = Load<Material>("Assets/Art/Materials/Vfx/Hologram.mat");
            foreach (MeshRenderer renderer in renderers)
            {
                renderer.enabled = renderer.sharedMaterials.All(material => material.shader.name == "EmpireAtWar/Ship Lit");
                if (renderer.enabled)
                    renderer.sharedMaterials = Enumerable.Repeat(hologram, renderer.sharedMaterials.Length).ToArray();
            }
            MonoBehaviour spawn = Component(root, "UnitSpawnView");
            var config = new SerializedObject(spawn);
            config.FindProperty("hologramMaterial").objectReferenceValue = hologram;
            Save(config);
            SetReferences(spawn, "meshRenderers", renderers.Where(renderer => renderer.enabled).ToArray());
            var collider = root.GetComponent<BoxCollider>();
            collider.center = bounds.center;
            collider.size = bounds.size;
            collider.isTrigger = true;
            PrefabUtility.SaveAsPrefabAsset(root, PLACEMENTS + root.name + ".prefab");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static Vector3 ReadVector(JToken value) => new Vector3((float)value[0], (float)value[1], (float)value[2]);
    private static Transform Socket(Transform[] sockets, string name) =>
        sockets.Single(socket => string.Equals(socket.name, name, StringComparison.OrdinalIgnoreCase));
    private static T Load<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);

    private static Bounds BoundsOf(Vector3[] points)
    {
        var bounds = new Bounds(points[0], Vector3.zero);
        foreach (Vector3 point in points) bounds.Encapsulate(point);
        return bounds;
    }

    private static MonoBehaviour Component(GameObject root, string name) =>
        root.GetComponentsInChildren<MonoBehaviour>(true).Single(component => component.GetType().Name == name);
    private static SerializedObject Config(GameObject root, string name) => new SerializedObject(Component(root, name));

    private static void SetReferences(Object target, string field, Object[] values)
    {
        var config = new SerializedObject(target);
        SerializedProperty refs = config.FindProperty(field);
        refs.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            refs.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        Save(config);
    }

    private static void Save(SerializedObject config)
    {
        config.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(config.targetObject);
    }
}
