using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Editor;
using EmpireAtWar.Editor.Rendering;
using EmpireAtWar.Editor.Ship;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.ShipAbilities.Abilities;
using EmpireAtWar.ViewComponents.Health;
using EmpireAtWar.ViewComponents.Wreck;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

/// <summary>
/// Builds and registers the Empire Xyston-class Star Destroyer (ShipType 220) from the staged FBX files.
/// Run in order: unity command script run --file Tools/Blender/Xyston/BuildXyston.cs --entry XystonBuilder.Build
/// then --entry XystonBuilder.Register. Mounts: Tools/Blender/Xyston/XystonMounts.json.
/// ISD I superstructure and turret parts are cloned from the ISD I Remake visual (same source mod and rig).
/// </summary>
public static class XystonBuilder
{
    private const string NAME = "Xyston";
    private const int SHIP_TYPE = 220;
    private const string TOOLS = "Tools/Blender/Xyston/";
    private const string SHIPS = "Assets/Prefabs/Models/Ships/Empire/";
    private const string FBX = "Assets/Art/Models/EmpireShips/Xyston/Xyston.fbx";
    private const string CANNON_FBX = "Assets/Art/Models/EmpireShips/XystonCannon/XystonCannon.fbx";
    private const string VISUAL = SHIPS + "Models/Xyston.prefab";
    private const string VIEW = SHIPS + "View/XystonShipView.prefab";
    private const string DATA = "Assets/Settings/Data/Ship/Empire/XystonShipData.asset";
    private const string WRECK_DATA = "Assets/Settings/Data/Models/Wrecks/Empire/XystonWreckData.asset";
    private const string PREVIEW = "Assets/Prefabs/Ui/Reinforcement/XystonReinforcementView.prefab";
    private const string ICON = "Assets/Art/Textures/Ui/Icons/ShipIcon/Empire/XystonIcon.png";
    private const string MATCHUPS = "Assets/Settings/Data/Tooltip/Matchups/XystonMatchups.asset";

    // Allegiance view: Empire capital view without a hangar component (user: no hangar).
    private const string DONOR_VIEW = SHIPS + "View/AllegianceShipView.prefab";
    private const string DONOR_VISUAL_CHILD = "Allegiance";
    private const string ISDI_VISUAL = SHIPS + "Models/ISDI.prefab";
    private const string ISDI_VIEW = SHIPS + "View/ISDIShipView.prefab";
    private const string DONOR_DATA = "Assets/Settings/Data/Ship/Empire/ISDIShipData.asset";
    private const string DONOR_WRECK = "Assets/Settings/Data/Models/Wrecks/Empire/ISDIWreckData.asset";
    private const string DONOR_PREVIEW = "Assets/Prefabs/Ui/Reinforcement/ISDIReinforcementView.prefab";
    private const string DONOR_MATCHUPS = "Assets/Settings/Data/Tooltip/Matchups/ISDIShipMatchups.asset";
    private const int DONOR_SHIP_TYPE = 201; // ISD I: same hull family, engine/explosion and voice sets
    // Team color: albedo hue livery (Allegiance values) on the source Sith red armor band — grey texture × red tint, hue 0.
    private const string TEAM_MESH = "Hull_Armor_Red";
    private const float LIVERY_HUE = 0f, LIVERY_HUE_RANGE = 0.07f, LIVERY_MIN_SATURATION = 0.4f;
    // 2,406 m × 0.105 units/m (project anchor: Venator 1,137 m = 119.38 units).
    private const float LENGTH = 2406f * 0.105f;
    private const string ENGINE_BONE = "pe_imperial_star_destroyer_large";
    private const string SHIP_LIT = "EmpireAtWar/Ship Lit";
    private const float RING_VISIBLE_FRACTION = 1038f / 1254f;
    private const float RING_MARGIN = 1.15f;

    // User decisions 2026-11-23: glass cannon, 36k superlaser (raised to 46.8k, +30%), source-like guns, no hangar. Roster values provisional.
    private const float HULL = 28000f;
    private const float SHIELDS = 11000f;
    private static readonly int[] ABILITIES = { (int)ShipAbilityId.XystonSuperlaser, (int)ShipAbilityId.TractorBeam };
    // ShipRoleProposal gives Fragile; AntiCapital is the superlaser's role, which the gun-only proposal cannot see.
    private const int ROLES = (1 << 3) | (1 << 12); // Fragile | AntiCapital
    private const string DISPLAY_NAME = "Xyston";
    private const string FULL_NAME = "Xyston-class Star Destroyer";
    private const string ROLE = "Anti-capital artillery / Glass cannon";
    private const string DESCRIPTION = "28,000 hull; 11,000 shields; speed 20. Built around a spinal axial superlaser: " +
        "46,800 base beam damage over 6 s against a frigate, capital ship or station, enough to destroy an ISD II outright; " +
        "recovers for 60 s and cannot target fighters or corvettes. Six heavy dual turbolaser batteries, six heavy dual " +
        "laser cannons, five medium dual turbolasers, two heavy dual and two heavy triple ion cannons. Shield generator, " +
        "two engines and tractor beam can be destroyed. Tractor Beam slows enemy ships of a lower class. No hangar.";
    private const int MAX_COUNT = 2, LEVEL = 5, PRICE = 14000, BUILD_TIME = 80, CAPACITY = 15;

    private const string CATALOG = "Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset";
    private const float SUPERLASER_DAMAGE = 46800f, SUPERLASER_DURATION = 6f, SUPERLASER_RECOVERY = 60f, SUPERLASER_RANGE = 1000f;

    public static string Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Build the Xyston in Edit Mode.");
        AssetDatabase.Refresh();
        ConfigureArt("Xyston");
        ConfigureArt("XystonCannon");
        string visual = BuildVisual();
        Fit fit = BuildView();
        BuildData(fit);
        BuildPreview(fit.Bounds);
        RenderIcon();
        AssetDatabase.SaveAssets();
        return $"{visual} | view: size={fit.Bounds.size} radius={fit.Radius} bottom={fit.Bottom:F2} top={fit.Top:F2} " +
               $"weapons={fit.Weapons} systems={fit.Systems} loadout={string.Join(",", fit.Loadout.Select(p => p.Key + "x" + p.Value))}";
    }

    public static string Register()
    {
        RegisterAbility();
        RegisterShip();
        AssetDatabase.SaveAssets();
        string synced = ShipReinforcementPreviewSync.Sync((EmpireAtWar.Models.Factions.ShipType)SHIP_TYPE,
            AssetDatabase.LoadAssetAtPath<GameObject>(VIEW));
        AssetDatabase.SaveAssets();
        return "Registered Xyston 220 and ability 58. Preview sync: " + synced;
    }

    // ---------- Art ----------

    private static void ConfigureArt(string variant)
    {
        string textures = $"Assets/Art/Textures/Models/EmpireShips/{variant}";
        string materials = $"Assets/Art/Materials/Models/EmpireShips/{variant}";
        EnsureFolder(materials);
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { textures }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            bool normal = Path.GetFileNameWithoutExtension(path).EndsWith("_B", StringComparison.Ordinal);
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = !normal;
            importer.alphaIsTransparency = path.EndsWith("_Additive.png", StringComparison.Ordinal);
            importer.flipGreenChannel = normal;
            importer.mipmapEnabled = true;
            importer.SaveAndReimport();
        }

        string fbx = variant == "Xyston" ? FBX : CANNON_FBX;
        var model = (ModelImporter)AssetImporter.GetAtPath(fbx);
        model.globalScale = 0.02f;
        model.preserveHierarchy = true;
        model.importAnimation = false;
        model.isReadable = true;
        model.weldVertices = false;
        model.meshCompression = ModelImporterMeshCompression.Off;
        model.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;

        foreach (JToken row in Rows(variant))
        {
            if ((bool)row["hidden"] || IsHelperShader((string)row["shader"])) continue;
            string name = (string)row["name"], shader = (string)row["shader"], baseStem = (string)row["base"];
            string normalStem = (string)row["normal"];
            bool effect = shader.Contains("Additive") || shader.Contains("Shield");
            string matPath = $"{materials}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (material == null)
            {
                material = new Material(Shader.Find(effect ? "Universal Render Pipeline/Particles/Unlit" : SHIP_LIT));
                AssetDatabase.CreateAsset(material, matPath);
            }
            if (effect)
            {
                material.shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                material.SetTexture("_BaseMap", Texture(textures, baseStem + "_Additive"));
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", 2f);
                material.SetFloat("_SrcBlend", (float)BlendMode.One);
                material.SetFloat("_DstBlend", (float)BlendMode.One);
                material.SetFloat("_SrcBlendAlpha", 1f);
                material.SetFloat("_DstBlendAlpha", 10f);
                material.SetFloat("_ZWrite", 0f);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = 3000;
            }
            else
            {
                material.shader = Shader.Find(SHIP_LIT);
                material.SetTexture("_BaseMap", Texture(textures, baseStem));
                if (normalStem != null)
                {
                    material.SetTexture("_BumpMap", Texture(textures, normalStem));
                    material.EnableKeyword("_NORMALMAP");
                }
                var diffuse = row["diffuse"] as JArray;
                material.SetColor("_BaseColor", diffuse == null ? Color.white
                    : new Color((float)diffuse[0], (float)diffuse[1], (float)diffuse[2], 1f));
                bool team = (string)row["mesh"] == TEAM_MESH;
                material.SetTexture("_TeamMaskMap", null);
                material.SetFloat("_TeamMaskStrength", 0f);
                material.SetFloat("_TeamLiveryHue", LIVERY_HUE);
                material.SetFloat("_TeamLiveryHueRange", LIVERY_HUE_RANGE);
                material.SetFloat("_TeamLiveryMinSaturation", LIVERY_MIN_SATURATION);
                material.SetFloat("_TeamLiveryStrength", team ? 1f : 0f);
                if (material.HasProperty("_TeamRimStrength")) material.SetFloat("_TeamRimStrength", 0f);
                material.SetFloat("_Smoothness", 0.25f);
                material.SetFloat("_Metallic", 0.1f);
            }
            EditorUtility.SetDirty(material);
            model.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), name), material);
        }
        AssetDatabase.SaveAssets();
        model.SaveAndReimport();
    }

    private static JArray Rows(string variant) => JArray.Parse(File.ReadAllText(TOOLS + variant + "Materials.json"));

    private static bool IsHelperShader(string shader) => shader == "MeshShadowVolume.fx" || shader == "MeshCollision.fx";

    private static Texture2D Texture(string folder, string stem)
    {
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{folder}/{stem}.png");
        if (texture == null) throw new InvalidOperationException($"Missing staged texture {folder}/{stem}.png");
        return texture;
    }

    private static string BuildVisual()
    {
        JObject mounts = Mounts();
        GameObject isdi = AssetDatabase.LoadAssetAtPath<GameObject>(ISDI_VISUAL);
        Transform isdiGeometry = isdi.transform.Find("Geometry");
        var root = new GameObject(NAME);
        try
        {
            var geometry = new GameObject("Geometry");
            geometry.transform.SetParent(root.transform, false);
            GameObject hull = AddFbx(FBX, geometry.transform, "Xyston");
            Transform[] bones = hull.GetComponentsInChildren<Transform>(true);

            // ISD I superstructure and turrets: same source models and attachment convention as the ISD I Remake.
            var partBones = mounts["weapons"].Where(w => w["part"] != null)
                .Select(w => ((string)w["part"], (string)w["bone"]))
                .Concat(mounts["parts"].Select(p => ((string)p["part"], (string)p["bone"])));
            int parts = 0;
            foreach ((string part, string bone) in partBones)
            {
                Transform source = isdiGeometry.GetComponentsInChildren<Transform>(true).Single(t => t.name == part);
                GameObject clone = Object.Instantiate(source.gameObject, Bone(bones, bone));
                clone.name = part;
                clone.transform.localPosition = source.localPosition;
                clone.transform.localRotation = source.localRotation;
                clone.transform.localScale = source.localScale;
                parts++;
            }

            // Superlaser cannon: separate source ALO on HP_Cannon_Attach, attached like the ISD I parts.
            // The attach bone is pitched 15°; Empire_Xyston_SD_Show.ALO places the barrel at the same point but
            // level along the keel, so the bone's own rotation is cancelled.
            Transform attach = Bone(bones, "HP_Cannon_Attach");
            GameObject cannon = AddFbx(CANNON_FBX, attach, "XystonCannon");
            Transform sample = isdiGeometry.GetComponentsInChildren<Transform>(true).Single(t => t.name == "ISDIPart03");
            cannon.transform.localPosition = sample.localPosition;
            cannon.transform.localRotation = Quaternion.Inverse(attach.localRotation) * sample.localRotation;
            cannon.transform.localScale = sample.localScale;

            if (root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Any(r => r.enabled))
                throw new InvalidOperationException("Unexpected skinned renderer in the Xyston visual.");

            // Bow must face +Z: source engines sit aft.
            Bounds bounds = BoundsOf(Points(root.transform, HullRenderers(root)));
            float engineZ = bones.Where(t => t.name.StartsWith(ENGINE_BONE, StringComparison.Ordinal))
                .Average(t => root.transform.InverseTransformPoint(t.position).z);
            if (engineZ > bounds.center.z)
                throw new InvalidOperationException("Xyston engines sit forward of the hull centre; expected bow +Z.");
            float scale = LENGTH / bounds.size.z;
            geometry.transform.localScale = Vector3.one * scale;
            bounds = BoundsOf(Points(root.transform, HullRenderers(root)));
            geometry.transform.localPosition = -bounds.center;
            bounds = BoundsOf(Points(root.transform, HullRenderers(root)));
            PrefabUtility.SaveAsPrefabAsset(root, VISUAL);
            return $"visual: parts={parts} scale={scale:F5} size={bounds.size}";
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static GameObject AddFbx(string path, Transform parent, string variant)
    {
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), parent);
        PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        instance.name = variant;
        // Collision, shadow volumes and the hidden source shield stay in the FBX but not in the unit prefab.
        var helpers = new HashSet<string>(Rows(variant)
            .Where(row => (bool)row["hidden"] || IsHelperShader((string)row["shader"]))
            .Select(row => (string)row["name"]));
        foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true).ToArray())
            if (renderer.sharedMaterials.Any(m => m == null || helpers.Contains(m.name)))
                Object.DestroyImmediate(renderer.gameObject);
        foreach (LODGroup lod in instance.GetComponentsInChildren<LODGroup>(true)) Object.DestroyImmediate(lod);
        return instance;
    }

    // ---------- Gameplay view ----------

    private struct Fit
    {
        public Bounds Bounds;
        public float Bottom, Top, Radius;
        public Vector2[] VisionHull;
        public int Weapons, Systems;
        public SortedDictionary<int, int> Loadout;
    }

    private static Fit BuildView()
    {
        CopyAsset(DONOR_VIEW, VIEW);
        GameObject isdiView = AssetDatabase.LoadAssetAtPath<GameObject>(ISDI_VIEW);
        // ISD I plumes sit on the same three nozzles; widen them with the hull length.
        float plumeWidth = isdiView.GetComponentsInChildren<TrailRenderer>(true).Average(t => t.widthMultiplier) *
                           LENGTH / isdiView.GetComponent<BoxCollider>().size.z;
        var isdiArcs = isdiView.GetComponentsInChildren<WeaponHardPoint>(true).ToDictionary(p => p.name);
        GameObject root = PrefabUtility.LoadPrefabContents(VIEW);
        Fit fit;
        Vector3[] nozzles;
        try
        {
            root.name = NAME + "ShipView";
            Object.DestroyImmediate(root.GetComponent<MeshCollider>()); // Allegiance-only pick hull, unreferenced
            var move = Config(root, "ShipMoveComponent");
            var body = (Transform)move.FindProperty("bodyTransform").objectReferenceValue;
            Object.DestroyImmediate(body.Find(DONOR_VISUAL_CHILD).gameObject);
            body.localPosition = Vector3.zero;
            body.localRotation = Quaternion.identity;
            body.localScale = Vector3.one;
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(VISUAL), body);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;
            Transform[] sockets = visual.GetComponentsInChildren<Transform>(true);
            MeshRenderer[] hull = HullRenderers(visual);
            MeshRenderer[] visible = visual.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled).ToArray();
            Vector3[] points = Points(root.transform, hull);
            Bounds bounds = BoundsOf(points);

            HardPoint[] donorPoints = body.GetComponentsInChildren<HardPoint>(true);
            WeaponHardPoint weaponTemplate = donorPoints.OfType<WeaponHardPoint>().First();
            HardPoint System(HardPointType type) => donorPoints.First(p => !(p is WeaponHardPoint) && p.HardPointType == type);
            var templates = new Dictionary<string, HardPoint>
            {
                ["Engines"] = System(HardPointType.Engines),
                ["ShieldGenerator"] = System(HardPointType.ShieldGenerator),
                ["TractorBeam"] = System(HardPointType.TractorBeam),
            };
            var container = new GameObject("Hardpoints").transform;
            container.SetParent(body, false);

            JObject mounts = Mounts();
            var weapons = new List<WeaponHardPoint>();
            var loadout = new SortedDictionary<int, int>();
            foreach (JToken row in mounts["weapons"])
            {
                string source = (string)row["source"];
                Transform socket = row["part"] != null
                    ? Bone(Bone(sockets, (string)row["part"]).GetComponentsInChildren<Transform>(true), (string)row["socket"])
                    : Bone(sockets, (string)row["socket"]);
                int type = (int)row["weaponType"];
                WeaponHardPoint point = Object.Instantiate(weaponTemplate, container);
                point.name = source;
                Place(point.transform, socket.position, root.transform);
                ConfigurePoint(root.transform, point, points, (int)row["id"], HardPointType.Weapon);
                var config = new SerializedObject(point);
                var arcs = new SerializedObject(isdiArcs[source]);
                config.FindProperty("<WeaponType>k__BackingField").intValue = type;
                foreach (string field in new[] { "yAxisRange.<Min>k__BackingField", "yAxisRange.<Max>k__BackingField" })
                    config.FindProperty(field).floatValue = arcs.FindProperty(field).floatValue;
                config.FindProperty("mainBattery").boolValue = arcs.FindProperty("mainBattery").boolValue;
                Save(config);
                weapons.Add(point);
                loadout[type] = loadout.TryGetValue(type, out int count) ? count + 1 : 1;
            }

            var systems = new List<HardPoint>();
            var engines = sockets.Where(t => t.name.StartsWith(ENGINE_BONE, StringComparison.Ordinal))
                .OrderBy(t => root.transform.InverseTransformPoint(t.position).x).ToArray();
            foreach (JToken row in mounts["systems"])
            {
                string system = (string)row["system"];
                Transform socket = system == "Engines"
                    ? ((string)row["pick"] == "minX" ? engines.First() : engines.Last())
                    : Bone(sockets, (string)row["socket"]);
                HardPoint point = Object.Instantiate(templates[system], container);
                point.name = $"{system}{systems.Count(p => p.HardPointType == templates[system].HardPointType) + 1}";
                Place(point.transform, socket.position, root.transform);
                ConfigurePoint(root.transform, point, points, (int)row["id"], templates[system].HardPointType);
                systems.Add(point);
            }
            foreach (HardPoint old in donorPoints) Object.DestroyImmediate(old.gameObject);

            HardPoint[] all = weapons.Cast<HardPoint>().Concat(systems).ToArray();
            MonoBehaviour health = Component(root, "HealthComponent");
            SetReferences(health, "<ShipUnits>k__BackingField", all);
            SetReferences(Component(root, "WeaponComponent"), "hardPoints", weapons.ToArray());
            MonoBehaviour renderState = Component(root, "UnitRenderStateComponent");
            SetReferences(Component(root, "EntityBindings"), "<Injectables>k__BackingField",
                all.Cast<Object>().Append(renderState).ToArray());
            MonoBehaviour ship = Component(root, "Ship");
            SetReferences(ship, "explosionHullRenderers", hull);
            Transform cannon = Bone(sockets, "XystonCannon");
            var shipConfig = new SerializedObject(ship);
            shipConfig.FindProperty("compositeBeamMuzzle").objectReferenceValue =
                Bone(cannon.GetComponentsInChildren<Transform>(true), "FP_01");
            Save(shipConfig);
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
            // Capital ships are picked by their hull box (ShipPickHullBaker.NeedsPickHull); the Allegiance donor's
            // box is disabled because battlecruisers use a convex pick hull instead.
            var collider = root.GetComponent<BoxCollider>();
            collider.center = bounds.center;
            collider.size = bounds.size;
            collider.enabled = true;

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

            nozzles = engines.Select(t => root.transform.InverseTransformPoint(t.position)).ToArray();
            PrefabUtility.SaveAsPrefabAsset(root, VIEW);
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
        ShipEnginePlumeSetup.Apply(VIEW, nozzles, nozzles.Select(_ => plumeWidth).ToArray());
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
        Vector3 nearest = hull.OrderBy(vertex => (vertex - origin).sqrMagnitude).First();
        anchor.position = root.TransformPoint(nearest);
        anchor.localScale = Vector3.one;
        Save(config);
    }

    // ---------- Data, wreck, preview, icon ----------

    private static void BuildData(Fit fit)
    {
        UnitWreckView wreckView = ShipWreckBuilder.Build(VIEW);
        CopyAsset(DONOR_WRECK, WRECK_DATA);
        var wreck = Load(WRECK_DATA);
        wreck.targetObject.name = NAME + "WreckData";
        wreck.FindProperty("prefab.m_AssetGUID").stringValue = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(wreckView));
        Save(wreck);

        CopyAsset(DONOR_DATA, DATA);
        var data = Load(DATA);
        data.targetObject.name = NAME + "ShipData";
        data.FindProperty("<Hull>k__BackingField").floatValue = HULL;
        data.FindProperty("<Shields>k__BackingField").floatValue = SHIELDS;
        data.FindProperty("<ShipClass>k__BackingField").intValue = (int)ShipClass.Capital;
        data.FindProperty("<HullBottom>k__BackingField").floatValue = fit.Bottom;
        data.FindProperty("<HullTop>k__BackingField").floatValue = fit.Top;
        data.FindProperty("<NavigationRadius>k__BackingField").floatValue = fit.Radius;
        data.FindProperty("<Wreck>k__BackingField").objectReferenceValue = wreck.targetObject;
        data.FindProperty("roles").intValue = ROLES;

        SerializedProperty vision = data.FindProperty("visionHull");
        vision.arraySize = fit.VisionHull.Length;
        for (int i = 0; i < fit.VisionHull.Length; i++) vision.GetArrayElementAtIndex(i).vector2Value = fit.VisionHull[i];

        SerializedProperty abilities = data.FindProperty("abilities");
        abilities.arraySize = ABILITIES.Length;
        for (int i = 0; i < ABILITIES.Length; i++) abilities.GetArrayElementAtIndex(i).intValue = ABILITIES[i];
        data.FindProperty("hangarBays").arraySize = 0;

        SerializedProperty loadout = data.FindProperty("weaponLoadout");
        var rows = fit.Loadout.ToArray();
        loadout.arraySize = rows.Length;
        for (int i = 0; i < rows.Length; i++)
        {
            loadout.GetArrayElementAtIndex(i).FindPropertyRelative("weaponType").intValue = rows[i].Key;
            loadout.GetArrayElementAtIndex(i).FindPropertyRelative("count").intValue = rows[i].Value;
        }
        Save(data);
    }

    private static void BuildPreview(Bounds bounds)
    {
        CopyAsset(DONOR_PREVIEW, PREVIEW);
        GameObject root = PrefabUtility.LoadPrefabContents(PREVIEW);
        try
        {
            root.name = NAME + "ReinforcementView";
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            root.transform.localScale = Vector3.one;
            foreach (Transform child in root.transform.Cast<Transform>().ToArray())
                Object.DestroyImmediate(child.gameObject);
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(VISUAL), root.transform);
            Material hologram = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Vfx/Hologram.mat");
            MeshRenderer[] renderers = visual.GetComponentsInChildren<MeshRenderer>(true);
            foreach (MeshRenderer renderer in renderers)
            {
                renderer.enabled = renderer.enabled && renderer.sharedMaterials.All(m => m != null && m.shader.name == SHIP_LIT);
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
            PrefabUtility.SaveAsPrefabAsset(root, PREVIEW);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void RenderIcon()
    {
        var clone = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(VISUAL));
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

            camGo = new GameObject(NAME + "IconCam");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0, 0, 0, 0);
            cam.cullingMask = 1 << LAYER;
            cam.fieldOfView = 35f;
            float maxExtent = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
            cam.nearClipPlane = Mathf.Max(0.1f, maxExtent * 0.01f);
            cam.farClipPlane = maxExtent * 20f;
            lightGo = new GameObject(NAME + "IconLight");
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
            File.WriteAllBytes(ICON, tex.EncodeToPNG());
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
        AssetDatabase.ImportAsset(ICON, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(ICON);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.sRGBTexture = true;
        importer.SaveAndReimport();
    }

    // ---------- Registration ----------

    private static void RegisterAbility()
    {
        var catalog = Load(CATALOG);
        SerializedProperty definitions = catalog.FindProperty("definitions.keyValue");
        Sprite beamIcon = (Sprite)Entry(definitions, (int)ShipAbilityId.ExecutorLaserBeam).FindPropertyRelative("icon").objectReferenceValue;
        SerializedProperty value = Entry(definitions, (int)ShipAbilityId.XystonSuperlaser);
        value.FindPropertyRelative("icon").objectReferenceValue = beamIcon;
        value.FindPropertyRelative("displayName").stringValue = "Superlaser";
        value.FindPropertyRelative("description").stringValue =
            "Fire the spinal superlaser at a frigate, capital ship or station. Deals 46,800 base beam damage over 6 s, " +
            "then recovers for 60 s. Cannot target fighters or corvettes.";
        value.FindPropertyRelative("duration").floatValue = SUPERLASER_DURATION;
        value.FindPropertyRelative("recoveryDelay").floatValue = SUPERLASER_RECOVERY;
        value.FindPropertyRelative("canCancel").boolValue = false;
        value.FindPropertyRelative("isToggle").boolValue = false;
        value.FindPropertyRelative("requiresEnemyTarget").boolValue = true;
        value.FindPropertyRelative("range").floatValue = SUPERLASER_RANGE;
        value.FindPropertyRelative("autoPower").intValue = (int)AutoPowerRole.Target;
        SerializedProperty settings = value.FindPropertyRelative("settings");
        if (!(settings.managedReferenceValue is SuperlaserSettings))
            settings.managedReferenceValue = new SuperlaserSettings();
        settings.FindPropertyRelative("damage").floatValue = SUPERLASER_DAMAGE;
        Save(catalog);
    }

    private static void RegisterShip()
    {
        var audio = Load("Assets/Settings/Data/Models/Audio/ShipSfxData.asset");
        CloneRow(audio.FindProperty("ships.keyValue"), DONOR_SHIP_TYPE, SHIP_TYPE);
        CloneRow(audio.FindProperty("shipVoices.keyValue"), DONOR_SHIP_TYPE, SHIP_TYPE);
        // Superlaser cues: same sustained-beam sound set as the Executor laser beam.
        SerializedProperty abilitySfx = audio.FindProperty("abilities");
        int Find(ShipAbilityId id) => Enumerable.Range(0, abilitySfx.arraySize)
            .Where(i => abilitySfx.GetArrayElementAtIndex(i).FindPropertyRelative("abilityId").intValue == (int)id)
            .DefaultIfEmpty(-1).First();
        if (Find(ShipAbilityId.XystonSuperlaser) < 0)
        {
            int donor = Find(ShipAbilityId.ExecutorLaserBeam);
            if (donor < 0) throw new InvalidOperationException("Missing ExecutorLaserBeam ability SFX to clone.");
            abilitySfx.InsertArrayElementAtIndex(donor);
            abilitySfx.GetArrayElementAtIndex(donor + 1).FindPropertyRelative("abilityId").intValue = (int)ShipAbilityId.XystonSuperlaser;
        }
        Save(audio);

        var icon = AssetDatabase.LoadAssetAtPath<Sprite>(ICON);
        if (icon == null || !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(icon, out string iconGuid, out long localId))
            throw new InvalidOperationException("Missing icon sprite " + ICON);
        string iconKey = iconGuid + ":" + localId;

        var settings = AddressableAssetSettingsDefaultObject.Settings;
        var unitGroup = settings.FindGroup("EmpireUnitViews");
        var dataGroup = settings.FindGroup("Data");
        var iconGroup = settings.FindGroup("SharedIcons");
        var mapping = Load("Assets/Settings/AssetMappingData.asset");
        foreach (string path in new[] { VIEW, DATA })
        {
            string guid = AssetDatabase.AssetPathToGUID(path);
            string address = Path.GetFileNameWithoutExtension(path);
            Entry(mapping.FindProperty("assetMappings.keyValue"), address).FindPropertyRelative("m_AssetGUID").stringValue = guid;
            settings.CreateOrMoveEntry(guid, path == VIEW ? unitGroup : dataGroup).address = address;
        }
        Save(mapping);
        settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(PREVIEW), unitGroup).address = NAME + "ReinforcementView";
        settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID("Assets/Prefabs/Models/Wrecks/" + NAME + "WreckView.prefab"), unitGroup)
            .address = NAME + "WreckView";
        settings.CreateOrMoveEntry(iconGuid, iconGroup).address = ICON;
        EditorUtility.SetDirty(unitGroup);
        EditorUtility.SetDirty(dataGroup);
        EditorUtility.SetDirty(iconGroup);
        EditorUtility.SetDirty(settings);

        var ships = Load("Assets/Settings/Data/Ship/Shared/ShipsData.asset");
        Entry(ships.FindProperty("shipsData.keyValue"), SHIP_TYPE).FindPropertyRelative("m_AssetGUID").stringValue =
            AssetDatabase.AssetPathToGUID(DATA);
        Save(ships);

        var ui = Load("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset");
        Entry(ui.FindProperty("shipIconWrapper.keyValue"), SHIP_TYPE).objectReferenceValue = icon;
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
        Entry(reinforcement.FindProperty("spawnShipWrapper.keyValue"), SHIP_TYPE).FindPropertyRelative("m_AssetGUID").stringValue =
            AssetDatabase.AssetPathToGUID(PREVIEW);
        Save(reinforcement);

        if (!File.Exists(MATCHUPS)) CopyAsset(DONOR_MATCHUPS, MATCHUPS);
        var matchups = Load(MATCHUPS);
        matchups.targetObject.name = NAME + "Matchups";
        Save(matchups);

        var faction = Load("Assets/Settings/Data/Factions/Empire/EmpireFaction.asset");
        SerializedProperty unit = Entry(faction.FindProperty("ships.keyValue"), SHIP_TYPE);
        unit.FindPropertyRelative("displayName").stringValue = DISPLAY_NAME;
        unit.FindPropertyRelative("<Name>k__BackingField").stringValue = FULL_NAME;
        unit.FindPropertyRelative("role").stringValue = ROLE;
        unit.FindPropertyRelative("description").stringValue = DESCRIPTION;
        unit.FindPropertyRelative("iconKey").stringValue = iconKey;
        unit.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue = icon;
        unit.FindPropertyRelative("matchups").objectReferenceValue = matchups.targetObject;
        unit.FindPropertyRelative("isHero").boolValue = false;
        unit.FindPropertyRelative("isUnique").boolValue = false;
        unit.FindPropertyRelative("<MaxCount>k__BackingField").intValue = MAX_COUNT;
        unit.FindPropertyRelative("<AvailableLevel>k__BackingField").intValue = LEVEL;
        unit.FindPropertyRelative("<Price>k__BackingField").intValue = PRICE;
        unit.FindPropertyRelative("<BuildTime>k__BackingField").intValue = BUILD_TIME;
        unit.FindPropertyRelative("<UnitCapacity>k__BackingField").intValue = CAPACITY;
        Save(faction);
    }

    // ---------- Helpers ----------

    private static JObject Mounts() => JObject.Parse(File.ReadAllText(TOOLS + "XystonMounts.json"));

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

    private static Transform Bone(Transform[] transforms, string name) =>
        transforms.Single(t => string.Equals(t.name, name, StringComparison.OrdinalIgnoreCase));

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
