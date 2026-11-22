using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Editor;
using EmpireAtWar.Editor.Rendering;
using EmpireAtWar.Editor.Ship;
using EmpireAtWar.ViewComponents.Health;
using EmpireAtWar.ViewComponents.Wreck;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Builds and registers the Viscount Prototype (331) and Viscount Dreadnought (332) from the staged FBX files.
/// Run: unity command script run --file Tools/Blender/Viscount/BuildViscount.cs --entry ViscountBuilder.BuildPrototype
/// (then BuildDreadnought, then Register). Mount tables: Tools/Blender/Viscount/*Mounts.json.
/// </summary>
public static class ViscountBuilder
{
    private const string TOOLS = "Tools/Blender/Viscount/";
    private const string SHIPS = "Assets/Prefabs/Models/Ships/Rebellion/";
    private const string DONOR_VIEW = SHIPS + "View/MC80IndependenceShipView.prefab";
    private const string DONOR_DATA = "Assets/Settings/Data/Ship/Rebellion/MC80IndependenceShipData.asset";
    private const string EXECUTOR_DATA = "Assets/Settings/Data/Ship/Empire/ExecutorShipData.asset";
    private const string DONOR_WRECK = "Assets/Settings/Data/Models/Wrecks/Rebellion/MC80IndependenceWreckData.asset";
    private const string DONOR_PREVIEW = "Assets/Prefabs/Ui/Reinforcement/MC80IndependenceReinforcementView.prefab";
    private const string DONOR_MATCHUPS = "Assets/Settings/Data/Tooltip/Matchups/MC80IndependenceMatchups.asset";
    private const string REGEN_TIERS = "Assets/Settings/Data/Ship/Rebellion/RebellionShipShieldRegenTiersData.asset";
    private const string SHIP_LIT = "EmpireAtWar/Ship Lit";
    // Side-circle masks come from TeamMask.cs; partial strength keeps the original paint visible.
    private const float TEAM_MASK_STRENGTH = 0.6f;
    private const int DONOR_SHIP_TYPE = 305; // MC80Independence
    private const int MON_CAL_CRUISER = 302;
    private const float RING_VISIBLE_FRACTION = 1038f / 1254f;
    private const float RING_MARGIN = 1.15f;

    private sealed class Variant
    {
        public string Name;
        public int ShipType;
        public float Length;
        public float Hull, Shields, Speed, RegenMultiplier;
        public int ShipClass, RegenTier;
        public bool ExecutorMovement;
        public int[] Abilities;
        public (int Type, int Reserve, int Active)[] Bays;
        public string DisplayName, FullName, Role, Description;
        public int MaxCount, Level, Price, BuildTime, Capacity;

        public string Fbx => $"Assets/Art/Models/RebellionShips/{Name}/{Name}.fbx";
        public string Textures => $"Assets/Art/Textures/Models/RebellionShips/{Name}";
        public string Materials => $"Assets/Art/Materials/Models/RebellionShips/{Name}";
        public string Visual => SHIPS + $"Models/{Name}.prefab";
        public string View => SHIPS + $"View/{Name}ShipView.prefab";
        public string Data => $"Assets/Settings/Data/Ship/Rebellion/{Name}ShipData.asset";
        public string WreckData => $"Assets/Settings/Data/Models/Wrecks/Rebellion/{Name}WreckData.asset";
        public string WreckView => $"Assets/Prefabs/Models/Wrecks/{Name}WreckView.prefab";
        public string Preview => $"Assets/Prefabs/Ui/Reinforcement/{Name}ReinforcementView.prefab";
        public string Icon => $"Assets/Art/Textures/Ui/Icons/ShipIcon/Rebellion/{Name}Icon.png";
        public string Matchups => $"Assets/Settings/Data/Tooltip/Matchups/{Name}Matchups.asset";
    }

    private static readonly Variant PROTOTYPE = new Variant
    {
        Name = "ViscountPrototype", ShipType = 331, Length = 315f,
        Hull = 55000f, Shields = 60000f, Speed = 26f, RegenMultiplier = 1f,
        ShipClass = 5, RegenTier = 4, ExecutorMovement = false, // BattleCruiser
        Abilities = new[] { 3 }, // BoostShieldPower
        Bays = new[] { (300, 8, 1), (301, 8, 1) },
        DisplayName = "Viscount Prototype", FullName = "Viscount-class Star Defender Prototype",
        Role = "Battlecruiser / Anti-capital line breaker",
        Description = "3 km Mon Calamari prototype battlecruiser: 55,000 hull and 60,000 shields with battlecruiser " +
            "shield regeneration. Ten heavy dual turbolasers, ten medium quad turbolasers, twelve heavy dual ion cannons, " +
            "fourteen medium dual laser cannons, four missile launchers and four heavy proton torpedo launchers. " +
            "Two shield generators, five engines and two hangars can be destroyed. Boost Shield Power raises shield " +
            "regeneration at the cost of speed and firepower. Deploys X-wing and A-wing squadrons.",
        MaxCount = 3, Level = 5, Price = 22000, BuildTime = 85, Capacity = 25,
    };

    private static readonly Variant DREADNOUGHT = new Variant
    {
        Name = "ViscountDreadnought", ShipType = 332, Length = 1785f,
        Hull = 180000f, Shields = 225000f, Speed = 13f, RegenMultiplier = 1.2f,
        ShipClass = 11, RegenTier = 5, ExecutorMovement = true, // Dreadnought
        Abilities = new[] { 3 },
        Bays = new[] { (300, 8, 2), (301, 8, 2), (1, 8, 2) },
        DisplayName = "Viscount", FullName = "Viscount-class Star Defender",
        Role = "Dreadnought / Fleet anchor",
        Description = "17 km Mon Calamari Star Defender, the Rebellion's answer to the Executor: 180,000 hull and " +
            "225,000 shields with 1,800 shield regeneration per second. Sixty turbolaser batteries, thirty heavy and " +
            "medium ion cannons and twelve proton torpedo launchers, strongest against shields. Four shield generators, " +
            "four engines and five hangars can be destroyed. Boost Shield Power raises shield regeneration at the cost of " +
            "speed and firepower. Deploys X-wing, A-wing and bomber squadrons.",
        MaxCount = 1, Level = 5, Price = 80500, BuildTime = 150, Capacity = 50,
    };

    public static string BuildPrototype() => Build(PROTOTYPE);
    public static string BuildDreadnought() => Build(DREADNOUGHT);

    public static string Register()
    {
        RegisterVariant(PROTOTYPE);
        RegisterVariant(DREADNOUGHT);
        AssetDatabase.SaveAssets();
        string synced = ShipReinforcementPreviewSync.Sync((EmpireAtWar.Models.Factions.ShipType)PROTOTYPE.ShipType,
                            AssetDatabase.LoadAssetAtPath<GameObject>(PROTOTYPE.View)) + " | " +
                        ShipReinforcementPreviewSync.Sync((EmpireAtWar.Models.Factions.ShipType)DREADNOUGHT.ShipType,
                            AssetDatabase.LoadAssetAtPath<GameObject>(DREADNOUGHT.View));
        AssetDatabase.SaveAssets();
        return "Registered 331 and 332. Preview sync: " + synced;
    }

    private static string Build(Variant v)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Build the Viscount in Edit Mode.");
        AssetDatabase.Refresh();
        EnsureFolder(v.Materials);
        EnsureFolder(SHIPS + "Models");
        EnsureFolder("Assets/Art/Textures/Ui/Icons/ShipIcon/Rebellion");

        ConfigureArt(v);
        BuildVisual(v);
        var fit = BuildView(v);
        BuildData(v, fit);
        BuildPreview(v, fit.Bounds);
        RenderIcon(v);
        AssetDatabase.SaveAssets();
        return $"{v.Name}: length={fit.Bounds.size.z:F2} size={fit.Bounds.size} radius={fit.Radius} " +
               $"bottom={fit.Bottom:F2} top={fit.Top:F2} weapons={fit.Weapons} systems={fit.Systems}";
    }

    // ---------- Art ----------

    private static void ConfigureArt(Variant v)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { v.Textures }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            string stem = Path.GetFileNameWithoutExtension(path);
            bool normal = stem.EndsWith("_B", StringComparison.Ordinal);
            bool mask = stem.EndsWith("_TeamMask", StringComparison.Ordinal);
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = !normal && !mask;
            importer.alphaIsTransparency = false;
            importer.flipGreenChannel = normal;
            importer.mipmapEnabled = true;
            importer.SaveAndReimport();
        }

        var model = (ModelImporter)AssetImporter.GetAtPath(v.Fbx);
        model.globalScale = 0.02f;
        model.preserveHierarchy = true;
        model.importAnimation = false;
        model.isReadable = true;
        model.weldVertices = false;
        model.meshCompression = ModelImporterMeshCompression.Off;
        model.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;

        foreach (JToken row in JArray.Parse(File.ReadAllText(TOOLS + v.Name + "Materials.json")))
        {
            string name = (string)row["name"], shader = (string)row["shader"];
            string baseStem = (string)row["base"], normalStem = (string)row["normal"];
            if (IsHelperShader(shader)) continue;
            string matPath = $"{v.Materials}/{name}.mat";
            bool lit = shader.Contains("Bump");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (material == null)
            {
                material = new Material(Shader.Find(lit ? SHIP_LIT : "Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(material, matPath);
            }
            if (lit)
            {
                material.shader = Shader.Find(SHIP_LIT);
                material.SetTexture("_BaseMap", Texture(v, baseStem));
                if (normalStem != null)
                {
                    material.SetTexture("_BumpMap", Texture(v, normalStem));
                    material.EnableKeyword("_NORMALMAP");
                }
                material.SetTexture("_TeamMaskMap", Texture(v, baseStem + "_TeamMask"));
                material.SetFloat("_TeamMaskStrength", TEAM_MASK_STRENGTH);
                material.SetFloat("_TeamLiveryStrength", 0f);
                material.SetFloat("_Smoothness", 0.25f);
                material.SetFloat("_Metallic", 0.1f);
            }
            else
            {
                material.SetTexture("_BaseMap", Texture(v, baseStem));
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", 2f);
                material.SetFloat("_SrcBlend", 5f);
                material.SetFloat("_DstBlend", 1f);
                material.SetFloat("_SrcBlendAlpha", 1f);
                material.SetFloat("_DstBlendAlpha", 1f);
                material.SetFloat("_ZWrite", 0f);
                material.SetFloat("_Cull", 2f);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = 3000;
            }
            EditorUtility.SetDirty(material);
            model.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), name), material);
        }
        AssetDatabase.SaveAssets();
        model.SaveAndReimport();
    }

    private static bool IsHelperShader(string shader) => shader == "MeshShadowVolume.fx" || shader == "MeshCollision.fx";

    private static Texture2D Texture(Variant v, string stem)
    {
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{v.Textures}/{stem}.png");
        if (texture == null) throw new InvalidOperationException($"Missing staged texture {stem} for {v.Name}.");
        return texture;
    }

    private static void BuildVisual(Variant v)
    {
        var helperNames = new HashSet<string>(JArray.Parse(File.ReadAllText(TOOLS + v.Name + "Materials.json"))
            .Where(row => IsHelperShader((string)row["shader"])).Select(row => (string)row["name"]));
        var root = new GameObject(v.Name);
        try
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(v.Fbx), root.transform);
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            // Shadow volumes, collision and the hidden source shield stay in the FBX but not in the unit prefab.
            foreach (MeshRenderer renderer in instance.GetComponentsInChildren<MeshRenderer>(true).ToArray())
            {
                bool helper = renderer.sharedMaterials.Any(m => m == null || helperNames.Contains(m.name)) ||
                              renderer.name == "Shield";
                if (helper) Object.DestroyImmediate(renderer.gameObject);
            }

            MeshRenderer[] hull = HullRenderers(root);
            Bounds bounds = BoundsOf(Points(root.transform, hull));
            // Bow must face +Z: source engines sit aft.
            float engineZ = instance.GetComponentsInChildren<Transform>(true)
                .Where(t => t.name.StartsWith("HP_E", StringComparison.OrdinalIgnoreCase))
                .Average(t => root.transform.InverseTransformPoint(t.position).z);
            if (engineZ > bounds.center.z)
            {
                instance.transform.localRotation = Quaternion.Euler(0f, 180f, 0f) * instance.transform.localRotation;
                bounds = BoundsOf(Points(root.transform, hull));
            }
            float scale = v.Length / bounds.size.z;
            instance.transform.localScale *= scale;
            bounds = BoundsOf(Points(root.transform, hull));
            instance.transform.localPosition -= bounds.center;
            PrefabUtility.SaveAsPrefabAsset(root, v.Visual);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    // ---------- Gameplay view ----------

    private struct Fit
    {
        public Bounds Bounds;
        public float Bottom, Top, Radius;
        public Vector2[] VisionHull;
        public int Weapons, Systems;
        public Dictionary<int, int> Loadout;
    }

    private static Fit BuildView(Variant v)
    {
        CopyAsset(DONOR_VIEW, v.View);
        GameObject root = PrefabUtility.LoadPrefabContents(v.View);
        Fit fit = default;
        Vector3[] nozzles;
        float[] widths;
        try
        {
            root.name = v.Name + "ShipView";
            var move = Config(root, "ShipMoveComponent");
            var body = (Transform)move.FindProperty("bodyTransform").objectReferenceValue;
            var donorPlumes = root.GetComponentsInChildren<TrailRenderer>(true);
            float donorPlumeWidth = donorPlumes.Average(t => t.widthMultiplier);
            float donorLength = BoundsOf(Points(root.transform, HullRenderers(body.Find("MC80Independence").gameObject))).size.z;
            Object.DestroyImmediate(body.Find("MC80Independence").gameObject);
            // The donor pivot is scaled for its own model; every child below is rebuilt in root space.
            body.localPosition = Vector3.zero;
            body.localRotation = Quaternion.identity;
            body.localScale = Vector3.one;
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(v.Visual), body);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;
            Transform[] sockets = visual.GetComponentsInChildren<Transform>(true);
            MeshRenderer[] hull = HullRenderers(visual);
            MeshRenderer[] visible = visual.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled).ToArray();
            Vector3[] points = Points(root.transform, hull);
            Bounds bounds = BoundsOf(points);

            Transform container = body.Find("Hardpoints");
            HardPoint[] donorPoints = container.GetComponentsInChildren<HardPoint>(true);
            WeaponHardPoint weaponTemplate = donorPoints.OfType<WeaponHardPoint>().First();
            HardPoint engineTemplate = donorPoints.First(p => !(p is WeaponHardPoint) && p.HardPointType == HardPointType.Engines);
            HardPoint shieldTemplate = donorPoints.First(p => !(p is WeaponHardPoint) && p.HardPointType == HardPointType.ShieldGenerator);
            HardPoint hangarTemplate = donorPoints.First(p => !(p is WeaponHardPoint) && p.HardPointType == HardPointType.Hangar);

            JObject mounts = JObject.Parse(File.ReadAllText(TOOLS + v.Name + "Mounts.json"));
            float centerBand = bounds.extents.x * 0.12f;
            var weapons = new List<WeaponHardPoint>();
            var loadout = new Dictionary<int, int>();
            foreach (JToken row in mounts["weapons"])
            {
                Transform socket = Socket(sockets, (string)row["socket"]);
                int type = (int)row["weaponType"];
                int id = (int)row["id"];
                WeaponHardPoint point = Object.Instantiate(weaponTemplate, container);
                point.name = $"{(EmpireAtWar.Components.AttackComponent.WeaponType)type}_{id:000}";
                Place(point.transform, socket.position, root.transform);
                ConfigurePoint(root.transform, point, points, id, HardPointType.Weapon);
                var config = new SerializedObject(point);
                config.FindProperty("<WeaponType>k__BackingField").intValue = type;
                float x = root.transform.InverseTransformPoint(socket.position).x;
                float min = x < -centerBand ? -180f : x > centerBand ? -15f : -180f;
                float max = x < -centerBand ? 15f : x > centerBand ? 180f : 180f;
                config.FindProperty("yAxisRange.<Min>k__BackingField").floatValue = min;
                config.FindProperty("yAxisRange.<Max>k__BackingField").floatValue = max;
                config.FindProperty("mainBattery").boolValue = false;
                Save(config);
                weapons.Add(point);
                loadout[type] = loadout.TryGetValue(type, out int count) ? count + 1 : 1;
            }

            var systems = new List<HardPoint>();
            var engineSockets = new List<Transform>();
            var hangarSockets = new List<Transform>();
            foreach (JToken row in mounts["systems"])
            {
                Transform socket = Socket(sockets, (string)row["socket"]);
                string system = (string)row["system"];
                HardPoint template = system == "Engines" ? engineTemplate : system == "Hangar" ? hangarTemplate : shieldTemplate;
                HardPoint point = Object.Instantiate(template, container);
                point.name = $"{system}_{systems.Count(p => p.HardPointType == template.HardPointType) + 1:00}";
                Place(point.transform, socket.position, root.transform);
                ConfigurePoint(root.transform, point, points, (int)row["id"], template.HardPointType);
                systems.Add(point);
                if (system == "Engines") engineSockets.Add(socket);
                if (system == "Hangar") hangarSockets.Add(socket);
            }
            foreach (HardPoint old in donorPoints) Object.DestroyImmediate(old.gameObject);

            // Hangar: first hangar launches; every hangar hardpoint gates the bays (bayHardPoints empty).
            var hangar = Config(root, "HangarComponent");
            HardPoint firstHangar = systems.First(p => p.HardPointType == HardPointType.Hangar);
            hangar.FindProperty("hangarHardPoint").objectReferenceValue = firstHangar;
            hangar.FindProperty("bayHardPoints").arraySize = 0;
            SerializedProperty launchPoints = hangar.FindProperty("bayLaunchPoints");
            var launches = Enumerable.Range(0, launchPoints.arraySize)
                .Select(i => (Transform)launchPoints.GetArrayElementAtIndex(i).objectReferenceValue).ToList();
            while (launches.Count > v.Bays.Length)
            {
                Object.DestroyImmediate(launches[launches.Count - 1].gameObject);
                launches.RemoveAt(launches.Count - 1);
            }
            while (launches.Count < v.Bays.Length)
                launches.Add(Object.Instantiate(launches[0], launches[0].parent));
            launchPoints.arraySize = launches.Count;
            for (int i = 0; i < launches.Count; i++)
            {
                Vector3 local = root.transform.InverseTransformPoint(hangarSockets[i % hangarSockets.Count].position);
                launches[i].name = $"{v.Name}LaunchPoint{i + 1}";
                launches[i].position = root.transform.TransformPoint(new Vector3(local.x, bounds.min.y - bounds.size.y * 0.05f, local.z));
                launches[i].rotation = root.transform.rotation;
                launches[i].localScale = Vector3.one;
                launchPoints.GetArrayElementAtIndex(i).objectReferenceValue = launches[i];
            }
            hangar.FindProperty("launchPoint").objectReferenceValue = launches[0];
            hangar.FindProperty("isDestroyable").boolValue = true;
            Save(hangar);

            HardPoint[] all = weapons.Cast<HardPoint>().Concat(systems).ToArray();
            MonoBehaviour health = Component(root, "HealthComponent");
            SetReferences(health, "<ShipUnits>k__BackingField", all);
            SetReferences(Component(root, "WeaponComponent"), "hardPoints", weapons.ToArray());
            MonoBehaviour renderState = Component(root, "UnitRenderStateComponent");
            SetReferences(Component(root, "EntityBindings"), "<Injectables>k__BackingField",
                all.Cast<Object>().Append(renderState).ToArray());
            SetReferences(Component(root, "Ship"), "explosionHullRenderers", hull);
            var shield = (Shield)new SerializedObject(health).FindProperty("shieldView").objectReferenceValue;
            ShieldHullBaker.Bake(root.transform, shield);
            SetReferences(renderState, "meshRenderers", visible.Append(shield.GetComponent<MeshRenderer>()).ToArray());
            SetReferences(Component(root, "FogVisibilityComponent"), "renderers",
                root.GetComponentsInChildren<Renderer>(true).Where(r => !(r is SpriteRenderer) && !(r is TrailRenderer)).ToArray());

            Bounds banked = BoundsOf(new[] { -15f, 0f, 15f }.SelectMany(angle =>
                points.Select(point => Quaternion.Euler(0, 0, angle) * point)).ToArray());
            float radius = Mathf.Ceil(points.Max(point => new Vector2(point.x, point.z).magnitude)) + 2f;
            var healthConfig = new SerializedObject(health);
            healthConfig.FindProperty("ionFieldBounds").boundsValue = new Bounds(banked.center, banked.size * 1.05f);
            Save(healthConfig);
            var collider = root.GetComponent<BoxCollider>();
            collider.center = bounds.center;
            collider.size = bounds.size;

            // Selection ring: visible part spans hull diameter x 1.15 after the 0.8 single-selection scale.
            var selection = Config(root, "SelectionComponent");
            float diameter = Mathf.Max(bounds.size.x, bounds.size.z);
            selection.FindProperty("ringSize").vector2Value = Vector2.one * (diameter * RING_MARGIN / RING_VISIBLE_FRACTION);
            var selectionRoot = (Transform)selection.FindProperty("selectionRoot").objectReferenceValue;
            selectionRoot.localPosition = new Vector3(0f, banked.min.y - 2f, 0f);
            var selected = (SpriteRenderer)selection.FindProperty("selectedRenderer").objectReferenceValue;
            Vector3 ringCenter = root.transform.TransformPoint(bounds.center);
            selected.transform.position = new Vector3(ringCenter.x, selected.transform.position.y, ringCenter.z);
            Save(selection);

            nozzles = engineSockets.Select(s => root.transform.InverseTransformPoint(s.position)).ToArray();
            float plumeWidth = donorPlumeWidth * v.Length / donorLength / Mathf.Max(1f, nozzles.Length / 3f);
            widths = nozzles.Select(_ => plumeWidth).ToArray();

            PrefabUtility.SaveAsPrefabAsset(root, v.View);
            fit = new Fit
            {
                Bounds = bounds, Bottom = banked.min.y, Top = banked.max.y, Radius = radius,
                VisionHull = ConvexHull2D(points.Select(p => new Vector2(p.x, p.z)).ToArray()),
                Weapons = weapons.Count, Systems = systems.Count, Loadout = loadout,
            };
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
        ShipEnginePlumeSetup.Apply(v.View, nozzles, widths);
        return fit;
    }

    private static void Place(Transform point, Vector3 worldPosition, Transform root)
    {
        point.position = worldPosition;
        point.rotation = root.rotation;
        point.localScale = Vector3.one;
    }

    private static void ConfigurePoint(Transform root, HardPoint point, Vector3[] hull, int id, HardPointType type)
    {
        var config = new SerializedObject(point);
        config.FindProperty("<Id>k__BackingField").intValue = id;
        config.FindProperty("<HardPointType>k__BackingField").intValue = (int)type;
        config.FindProperty("healthOverride").floatValue = 0f;
        config.FindProperty("healthWeight").floatValue = 1f;
        config.FindProperty("unlockLevel").intValue = 1;
        config.FindProperty("destroyedVfxScale").floatValue = 1f;
        var anchor = (Transform)config.FindProperty("destroyedVfxAnchor").objectReferenceValue;
        Vector3 origin = root.InverseTransformPoint(point.transform.position);
        Vector3 nearest = hull[0];
        float best = float.MaxValue;
        foreach (Vector3 vertex in hull)
        {
            float distance = (vertex - origin).sqrMagnitude;
            if (distance < best) { best = distance; nearest = vertex; }
        }
        anchor.position = root.TransformPoint(nearest);
        anchor.localScale = Vector3.one;
        Save(config);
    }

    // ---------- Data, wreck, preview, icon ----------

    private static void BuildData(Variant v, Fit fit)
    {
        UnitWreckView wreckView = ShipWreckBuilder.Build(v.View);
        CopyAsset(DONOR_WRECK, v.WreckData);
        var wreck = Load(v.WreckData);
        wreck.targetObject.name = v.Name + "WreckData";
        wreck.FindProperty("prefab.m_AssetGUID").stringValue = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(wreckView));
        Save(wreck);

        CopyAsset(DONOR_DATA, v.Data);
        var data = Load(v.Data);
        data.targetObject.name = v.Name + "ShipData";
        if (v.ExecutorMovement)
        {
            var executor = Load(EXECUTOR_DATA);
            foreach (string field in new[] { "<RotationSpeed>k__BackingField", "<TurnAcceleration>k__BackingField",
                         "<BodyRotationMaxAngle>k__BackingField", "<MinMoveCoefficient>k__BackingField",
                         "<HyperSpaceDuration>k__BackingField", "<Range>k__BackingField", "<HeightTier>k__BackingField",
                         "<IgnoresMapObstacles>k__BackingField", "roles" })
                data.CopyFromSerializedProperty(executor.FindProperty(field));
        }
        data.FindProperty("<Hull>k__BackingField").floatValue = v.Hull;
        data.FindProperty("<Shields>k__BackingField").floatValue = v.Shields;
        data.FindProperty("<Speed>k__BackingField").floatValue = v.Speed;
        data.FindProperty("<ShipClass>k__BackingField").intValue = v.ShipClass;
        data.FindProperty("shieldRegenTiers").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ScriptableObject>(REGEN_TIERS);
        data.FindProperty("<ShieldRegenTier>k__BackingField").intValue = v.RegenTier;
        data.FindProperty("<ShieldRegenMultiplier>k__BackingField").floatValue = v.RegenMultiplier;
        data.FindProperty("<HullBottom>k__BackingField").floatValue = fit.Bottom;
        data.FindProperty("<HullTop>k__BackingField").floatValue = fit.Top;
        data.FindProperty("<NavigationRadius>k__BackingField").floatValue = fit.Radius;
        data.FindProperty("<Wreck>k__BackingField").objectReferenceValue = wreck.targetObject;

        SerializedProperty vision = data.FindProperty("visionHull");
        vision.arraySize = fit.VisionHull.Length;
        for (int i = 0; i < fit.VisionHull.Length; i++) vision.GetArrayElementAtIndex(i).vector2Value = fit.VisionHull[i];

        SerializedProperty abilities = data.FindProperty("abilities");
        abilities.arraySize = v.Abilities.Length;
        for (int i = 0; i < v.Abilities.Length; i++) abilities.GetArrayElementAtIndex(i).intValue = v.Abilities[i];

        SerializedProperty bays = data.FindProperty("hangarBays");
        bays.arraySize = v.Bays.Length;
        for (int i = 0; i < v.Bays.Length; i++)
        {
            SerializedProperty bay = bays.GetArrayElementAtIndex(i);
            bay.FindPropertyRelative("squadronType").intValue = v.Bays[i].Type;
            bay.FindPropertyRelative("reserve").intValue = v.Bays[i].Reserve;
            bay.FindPropertyRelative("maxActive").intValue = v.Bays[i].Active;
        }

        SerializedProperty loadout = data.FindProperty("weaponLoadout");
        var rows = fit.Loadout.OrderBy(pair => pair.Key).ToArray();
        loadout.arraySize = rows.Length;
        for (int i = 0; i < rows.Length; i++)
        {
            loadout.GetArrayElementAtIndex(i).FindPropertyRelative("weaponType").intValue = rows[i].Key;
            loadout.GetArrayElementAtIndex(i).FindPropertyRelative("count").intValue = rows[i].Value;
        }
        Save(data);
    }

    private static void BuildPreview(Variant v, Bounds bounds)
    {
        CopyAsset(DONOR_PREVIEW, v.Preview);
        GameObject root = PrefabUtility.LoadPrefabContents(v.Preview);
        try
        {
            root.name = v.Name + "ReinforcementView";
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            root.transform.localScale = Vector3.one;
            foreach (Transform child in root.transform.Cast<Transform>().ToArray())
                Object.DestroyImmediate(child.gameObject);
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(v.Visual), root.transform);
            Material hologram = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Vfx/Hologram.mat");
            MeshRenderer[] renderers = visual.GetComponentsInChildren<MeshRenderer>(true);
            foreach (MeshRenderer renderer in renderers)
            {
                renderer.enabled = renderer.sharedMaterials.All(m => m != null && m.shader.name == SHIP_LIT);
                if (renderer.enabled) renderer.sharedMaterials = Enumerable.Repeat(hologram, renderer.sharedMaterials.Length).ToArray();
            }
            MonoBehaviour spawn = Component(root, "UnitSpawnView");
            var config = new SerializedObject(spawn);
            config.FindProperty("hologramMaterial").objectReferenceValue = hologram;
            Save(config);
            SetReferences(spawn, "meshRenderers", renderers.Where(r => r.enabled).ToArray());
            var collider = root.GetComponent<BoxCollider>();
            collider.center = bounds.center;
            collider.size = bounds.size;
            collider.isTrigger = true;
            PrefabUtility.SaveAsPrefabAsset(root, v.Preview);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void RenderIcon(Variant v)
    {
        var clone = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(v.Visual));
        GameObject camGo = null, lightGo = null;
        RenderTexture rt = null;
        Texture2D tex = null;
        try
        {
            const int LAYER = 31;
            foreach (Transform t in clone.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = LAYER;
            MeshRenderer[] renderers = clone.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled).ToArray();
            var bounds = new Bounds(renderers[0].bounds.center, Vector3.zero);
            foreach (MeshRenderer r in renderers) bounds.Encapsulate(r.bounds);

            camGo = new GameObject(v.Name + "IconCam");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0, 0, 0, 0);
            cam.cullingMask = 1 << LAYER;
            cam.fieldOfView = 35f;
            float maxExtent = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
            cam.nearClipPlane = Mathf.Max(0.1f, maxExtent * 0.01f);
            cam.farClipPlane = maxExtent * 20f;
            lightGo = new GameObject(v.Name + "IconLight");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.6f;
            light.cullingMask = 1 << LAYER;
            lightGo.transform.rotation = Quaternion.Euler(45f, 140f, 0f);
            camGo.transform.rotation = Quaternion.Euler(40.76f, 125.00f, 0.00f);
            float dist = maxExtent / Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.45f;
            camGo.transform.position = bounds.center - camGo.transform.forward * dist;

            rt = new RenderTexture(512, 512, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            tex = new Texture2D(512, 512, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, 512, 512), 0, 0);
            tex.Apply();
            File.WriteAllBytes(v.Icon, tex.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = null;
            if (camGo != null) camGo.GetComponent<Camera>().targetTexture = null;
            if (rt != null) Object.DestroyImmediate(rt);
            if (tex != null) Object.DestroyImmediate(tex);
            if (camGo != null) Object.DestroyImmediate(camGo);
            if (lightGo != null) Object.DestroyImmediate(lightGo);
            Object.DestroyImmediate(clone);
        }
        AssetDatabase.ImportAsset(v.Icon, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(v.Icon);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.sRGBTexture = true;
        importer.SaveAndReimport();
    }

    // ---------- Registration ----------

    private static void RegisterVariant(Variant v)
    {
        var audio = Load("Assets/Settings/Data/Models/Audio/ShipSfxData.asset");
        // Independence has no engine/explosion row (it uses the fallback); Mon Cal Cruiser (302) is the Rebel capital set.
        CloneRow(audio.FindProperty("ships.keyValue"), MON_CAL_CRUISER, v.ShipType);
        CloneRow(audio.FindProperty("shipVoices.keyValue"), DONOR_SHIP_TYPE, v.ShipType);
        Save(audio);

        var icon = AssetDatabase.LoadAssetAtPath<Sprite>(v.Icon);
        if (icon == null || !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(icon, out string iconGuid, out long localId))
            throw new InvalidOperationException("Missing icon sprite " + v.Icon);
        string iconKey = iconGuid + ":" + localId;

        var settings = AddressableAssetSettingsDefaultObject.Settings;
        var unitGroup = settings.FindGroup("RebellionUnitViews");
        var dataGroup = settings.FindGroup("Data");
        var iconGroup = settings.FindGroup("SharedIcons");
        var mapping = Load("Assets/Settings/AssetMappingData.asset");
        foreach (string path in new[] { v.View, v.Data })
        {
            string guid = AssetDatabase.AssetPathToGUID(path);
            string address = Path.GetFileNameWithoutExtension(path);
            Entry(mapping.FindProperty("assetMappings.keyValue"), address).FindPropertyRelative("m_AssetGUID").stringValue = guid;
            settings.CreateOrMoveEntry(guid, path == v.View ? unitGroup : dataGroup).address = address;
        }
        Save(mapping);
        settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(v.Preview), unitGroup).address = v.Name + "ReinforcementView";
        settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(v.WreckView), unitGroup).address = v.Name + "WreckView";
        settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(v.Icon), iconGroup).address = v.Icon;
        EditorUtility.SetDirty(unitGroup);
        EditorUtility.SetDirty(dataGroup);
        EditorUtility.SetDirty(iconGroup);
        EditorUtility.SetDirty(settings);

        var ships = Load("Assets/Settings/Data/Ship/Shared/ShipsData.asset");
        Entry(ships.FindProperty("shipsData.keyValue"), v.ShipType).FindPropertyRelative("m_AssetGUID").stringValue =
            AssetDatabase.AssetPathToGUID(v.Data);
        Save(ships);

        var ui = Load("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset");
        Entry(ui.FindProperty("shipIconWrapper.keyValue"), v.ShipType).objectReferenceValue = icon;
        Save(ui);

        var tooltip = Load("Assets/Settings/Data/Tooltip/TooltipIconData.asset");
        SerializedProperty icons = tooltip.FindProperty("icons");
        int index = Enumerable.Range(0, icons.arraySize)
            .Where(i => icons.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue == iconKey)
            .DefaultIfEmpty(-1).First();
        if (index < 0) { index = icons.arraySize; icons.arraySize++; }
        icons.GetArrayElementAtIndex(index).FindPropertyRelative("key").stringValue = iconKey;
        icons.GetArrayElementAtIndex(index).FindPropertyRelative("sprite").objectReferenceValue = icon;
        Save(tooltip);

        var reinforcement = Load("Assets/Settings/Data/Reinforcement/ReinforcementData.asset");
        Entry(reinforcement.FindProperty("spawnShipWrapper.keyValue"), v.ShipType).FindPropertyRelative("m_AssetGUID").stringValue =
            AssetDatabase.AssetPathToGUID(v.Preview);
        Save(reinforcement);

        if (!File.Exists(v.Matchups)) CopyAsset(DONOR_MATCHUPS, v.Matchups);
        var matchups = Load(v.Matchups);
        matchups.targetObject.name = v.Name + "Matchups";
        Save(matchups);

        var faction = Load("Assets/Settings/Data/Factions/Rebellion/RebellionFaction.asset");
        SerializedProperty unit = Entry(faction.FindProperty("ships.keyValue"), v.ShipType);
        unit.FindPropertyRelative("displayName").stringValue = v.DisplayName;
        unit.FindPropertyRelative("<Name>k__BackingField").stringValue = v.FullName;
        unit.FindPropertyRelative("role").stringValue = v.Role;
        unit.FindPropertyRelative("description").stringValue = v.Description;
        unit.FindPropertyRelative("iconKey").stringValue = iconKey;
        unit.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue = icon;
        unit.FindPropertyRelative("matchups").objectReferenceValue = matchups.targetObject;
        unit.FindPropertyRelative("isHero").boolValue = false;
        unit.FindPropertyRelative("<MaxCount>k__BackingField").intValue = v.MaxCount;
        unit.FindPropertyRelative("<AvailableLevel>k__BackingField").intValue = v.Level;
        unit.FindPropertyRelative("<Price>k__BackingField").intValue = v.Price;
        unit.FindPropertyRelative("<BuildTime>k__BackingField").intValue = v.BuildTime;
        unit.FindPropertyRelative("<UnitCapacity>k__BackingField").intValue = v.Capacity;
        Save(faction);
    }

    // ---------- Helpers ----------

    private static MeshRenderer[] HullRenderers(GameObject root) => root.GetComponentsInChildren<MeshRenderer>(true)
        .Where(r => r.enabled && r.sharedMaterials.All(m => m != null && m.shader.name == SHIP_LIT)).ToArray();

    private static Vector3[] Points(Transform space, MeshRenderer[] renderers) => renderers
        .SelectMany(r => r.GetComponent<MeshFilter>().sharedMesh.vertices
            .Select(vertex => space.InverseTransformPoint(r.transform.TransformPoint(vertex)))).ToArray();

    private static Bounds BoundsOf(Vector3[] points)
    {
        var bounds = new Bounds(points[0], Vector3.zero);
        foreach (Vector3 point in points) bounds.Encapsulate(point);
        return bounds;
    }

    private static Transform Socket(Transform[] sockets, string name) =>
        sockets.Single(socket => string.Equals(socket.name, name, StringComparison.OrdinalIgnoreCase));

    private static Vector2[] ConvexHull2D(Vector2[] pts)
    {
        Vector2[] sorted = pts.Distinct().OrderBy(p => p.x).ThenBy(p => p.y).ToArray();
        if (sorted.Length <= 3) return sorted;
        var lower = new List<Vector2>();
        foreach (Vector2 p in sorted)
        {
            while (lower.Count >= 2 && Cross(lower[lower.Count - 2], lower[lower.Count - 1], p) <= 0) lower.RemoveAt(lower.Count - 1);
            lower.Add(p);
        }
        var upper = new List<Vector2>();
        for (int i = sorted.Length - 1; i >= 0; i--)
        {
            Vector2 p = sorted[i];
            while (upper.Count >= 2 && Cross(upper[upper.Count - 2], upper[upper.Count - 1], p) <= 0) upper.RemoveAt(upper.Count - 1);
            upper.Add(p);
        }
        lower.RemoveAt(lower.Count - 1);
        upper.RemoveAt(upper.Count - 1);
        lower.AddRange(upper);
        return lower.ToArray();
    }

    private static float Cross(Vector2 o, Vector2 a, Vector2 b) => (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private static void CopyAsset(string source, string target)
    {
        if (File.Exists(target)) AssetDatabase.DeleteAsset(target);
        if (!AssetDatabase.CopyAsset(source, target)) throw new InvalidOperationException($"CopyAsset failed: {source} -> {target}");
    }

    private static MonoBehaviour Component(GameObject root, string typeName) =>
        root.GetComponentsInChildren<MonoBehaviour>(true).Single(c => c != null && c.GetType().Name == typeName);

    private static SerializedObject Config(GameObject root, string typeName) => new SerializedObject(Component(root, typeName));

    private static void SetReferences(Object target, string field, Object[] values)
    {
        var config = new SerializedObject(target);
        SerializedProperty refs = config.FindProperty(field);
        refs.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) refs.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        Save(config);
    }

    private static void CloneRow(SerializedProperty array, int source, int target)
    {
        for (int i = 0; i < array.arraySize; i++)
            if (array.GetArrayElementAtIndex(i).FindPropertyRelative("key").intValue == target) return;
        int donor = Enumerable.Range(0, array.arraySize)
            .Single(i => array.GetArrayElementAtIndex(i).FindPropertyRelative("key").intValue == source);
        array.InsertArrayElementAtIndex(donor);
        array.GetArrayElementAtIndex(donor + 1).FindPropertyRelative("key").intValue = target;
    }

    private static SerializedProperty Entry(SerializedProperty array, object key)
    {
        for (int i = 0; i < array.arraySize; i++)
        {
            SerializedProperty row = array.GetArrayElementAtIndex(i);
            SerializedProperty k = row.FindPropertyRelative("key");
            if (key is int n ? k.intValue == n : k.stringValue == (string)key) return row.FindPropertyRelative("value");
        }
        int index = array.arraySize;
        array.arraySize++;
        SerializedProperty entry = array.GetArrayElementAtIndex(index);
        if (key is int number) entry.FindPropertyRelative("key").intValue = number;
        else entry.FindPropertyRelative("key").stringValue = (string)key;
        return entry.FindPropertyRelative("value");
    }

    private static SerializedObject Load(string path) => new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>(path));

    private static void Save(SerializedObject data)
    {
        data.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(data.targetObject);
        // DictionaryWrapper caches its dictionary; clear it so the next read reflects the serialized rows.
        foreach (var field in data.targetObject.GetType()
                     .GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
                     .Where(f => f.FieldType.IsGenericType &&
                                 f.FieldType.GetGenericTypeDefinition() == typeof(Utilities.ScriptUtils.EditorSerialization.DictionaryWrapper<,>)))
        {
            var dictionary = field.FieldType.GetField("dictionary", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            object wrapper = field.GetValue(data.targetObject);
            if (dictionary != null && wrapper != null) dictionary.SetValue(wrapper, null);
        }
    }
}
