using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EmpireAtWar.Entities.BattleNotifications;
using EmpireAtWar.Entities.TacticalMarkers;
using EmpireAtWar.Entities.Tooltip;
using EmpireAtWar.Models.MiniMap;
using EmpireAtWar.Repository.Data;
using EmpireAtWar.Services.Input;
using EmpireAtWar.Services.TacticalMarkers;
using EmpireAtWar.Services.Tutorial;
using EmpireAtWar.Services.VContainer;
using EmpireAtWar.Views.MiniMap;
using TMPro;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Editor
{
    public static class TacticalMarkerAssetBuilder
    {
        private const string ART = "Assets/Art/Textures/Ui/TacticalMarkers/";
        private const string PREFABS = "Assets/Prefabs/Ui/TacticalMarkers/";
        private const string SETTINGS = "Assets/Settings/Data/TacticalMarkers/";
        private const string SCENE_CONTEXT = "Assets/Prefabs/View/LifetimeScopes/SceneContext.prefab";
        private static readonly string[] TYPES = { "Look", "Attack", "Capture", "Defend", "Rally", "Retreat" };

        public static void RenderPreview()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
            var target = new RenderTexture(900, 600, 24, RenderTextureFormat.ARGB32);
            var previous = RenderTexture.active;
            MarkerRadialUi view = null;
            UnityEngine.Camera camera = null;
            Texture2D image = null;
            try
            {
                var cameraObject = new GameObject("MarkerPreviewCamera");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject, scene);
                camera = cameraObject.AddComponent<UnityEngine.Camera>();
                camera.scene = scene;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color32(8, 12, 20, 255);
                camera.orthographic = true; camera.orthographicSize = 300; camera.nearClipPlane = 0.1f;
                camera.farClipPlane = 100; camera.transform.position = new Vector3(0, 0, -10);
                camera.targetTexture = target; camera.cullingMask = 1 << LayerMask.NameToLayer("UI");
                var canvasObject = new GameObject("PreviewCanvas", typeof(RectTransform), typeof(Canvas));
                canvasObject.layer = LayerMask.NameToLayer("UI");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(canvasObject, scene);
                var canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(
                    AssetDatabase.LoadAssetAtPath<GameObject>(PREFABS + "MarkerRadialUi.prefab"), scene);
                instance.transform.SetParent(canvas.transform, false);
                view = instance.GetComponent<MarkerRadialUi>();
                var model = new MarkerRadialModel();
                view.Initialize(model, AssetDatabase.LoadAssetAtPath<TacticalMarkerSettings>(SETTINGS + "TacticalMarkerSettings.asset"));
                view.Show();
                model.Open(); model.Select(100, 50);
                Canvas.ForceUpdateCanvases();
                foreach (TMP_Text label in instance.GetComponentsInChildren<TMP_Text>(true)) label.ForceMeshUpdate();
                camera.Render(); RenderTexture.active = target;
                image = new Texture2D(900, 600, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 900, 600), 0, 0); image.Apply();
                Directory.CreateDirectory("Logs/TacticalMarkers");
                File.WriteAllBytes("Logs/TacticalMarkers/radial-preview.png", image.EncodeToPNG());
                Debug.Log("Saved Logs/TacticalMarkers/radial-preview.png without changing open scenes.");
            }
            finally
            {
                if (view != null) view.Dispose();
                if (camera != null) camera.targetTexture = null;
                UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
                RenderTexture.active = previous;
                if (image != null) Object.DestroyImmediate(image);
                target.Release(); Object.DestroyImmediate(target);
            }
        }

        public static string Validate()
        {
            TacticalMarkerSettings settings = AssetDatabase.LoadAssetAtPath<TacticalMarkerSettings>(SETTINGS + "TacticalMarkerSettings.asset");
            for (int i = 0; i < 6; i++)
                if (settings.GetIcon((TacticalMarkerType)i) == null || settings.GetSound((TacticalMarkerType)i) == null)
                    throw new InvalidOperationException("Unbound tactical style " + TYPES[i]);
            if (settings.OctagonFrame == null || settings.SquareFrame == null || settings.WorldMaterial == null)
                throw new InvalidOperationException("Missing tactical frames or material.");
            GameObject radial = AssetDatabase.LoadAssetAtPath<GameObject>(PREFABS + "MarkerRadialUi.prefab");
            var view = radial.GetComponent<MarkerRadialUi>();
            RequireReferences(view, "canvasGroup", "pointerBlocker", "radial", "hub", "worldMarkerPrefab", "audioSource");
            var so = new SerializedObject(view);
            foreach (string arrayName in new[] { "slices", "frames", "glyphs" })
            {
                var array = so.FindProperty(arrayName);
                if (array.arraySize != 6) throw new InvalidOperationException("Expected six " + arrayName);
                for (int i = 0; i < 6; i++)
                    if (array.GetArrayElementAtIndex(i).objectReferenceValue == null)
                        throw new InvalidOperationException("Missing " + arrayName + " " + i);
            }
            RequireReferences(view.WorldMarkerPrefab, "billboard", "frame", "glyph", "sender", "ring", "ringRenderer", "beam", "pulse");
            var scope = AssetDatabase.LoadAssetAtPath<GameObject>(SCENE_CONTEXT).GetComponentsInChildren<SkirmishScope>(true).Single();
            if (new SerializedObject(scope).FindProperty("tacticalMarkerSettings").objectReferenceValue != settings)
                throw new InvalidOperationException("Battle scope settings not assigned.");
            var entry = AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(PREFABS + "MarkerRadialUi.prefab"));
            if (entry.address != "MarkerRadialUi" || entry.parentGroup.Name != "Ui")
                throw new InvalidOperationException("Radial Addressable owner/address is incorrect.");
            var mapping = AssetDatabase.LoadAssetAtPath<AssetMappingData>("Assets/Settings/AssetMappingData.asset");
            var pairs = new SerializedObject(mapping).FindProperty("assetMappings").FindPropertyRelative("keyValue");
            bool mapped = false;
            for (int i = 0; i < pairs.arraySize; i++)
            {
                var pair = pairs.GetArrayElementAtIndex(i);
                if (pair.FindPropertyRelative("key").stringValue == "MarkerRadialUi")
                    mapped = pair.FindPropertyRelative("value").FindPropertyRelative("m_AssetGUID").stringValue == entry.guid;
            }
            if (!mapped) throw new InvalidOperationException("Radial asset mapping is incorrect.");
            var miniMap = AssetDatabase.LoadAssetAtPath<MiniMapData>(AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("t:MiniMapData").Single()));
            for (int i = 0; i < 6; i++) if (miniMap.GetIcon((MarkType)((int)MarkType.TacticalLook + i)) == null)
                throw new InvalidOperationException("Missing minimap marker icon.");
            RequireReferences(miniMap.MarkViewPrefab, "arrivalRing", "tacticalMarkerSettings");
            var actions = new GameInputActions();
            try
            {
                var action = actions.asset.FindAction("Battle/PlaceMarker", true);
                if (action.bindings.Count != 1 || action.bindings[0].path != "<Keyboard>/b")
                    throw new InvalidOperationException("Marker binding was not generated.");
            }
            finally { Object.DestroyImmediate(actions.asset); }
            return "Verified 6 marker glyph/sound styles, world material/frames, radial/world serialized references, " +
                   "battle settings, Ui Addressable + mapping, 6 minimap marks/arrival ring, generated B binding. " +
                   "Prefabs: " + PREFABS + "; settings: " + SETTINGS;
        }

        private static void RequireReferences(Object target, params string[] fields)
        {
            var so = new SerializedObject(target);
            foreach (string field in fields)
                if (so.FindProperty(field).objectReferenceValue == null)
                    throw new InvalidOperationException(target.name + " has no " + field);
        }

        public static void Build()
        {
            Directory.CreateDirectory(PREFABS); Directory.CreateDirectory(SETTINGS);
            Directory.CreateDirectory("Assets/Art/Models/Vfx/TacticalMarkers");
            AssetDatabase.Refresh();
            foreach (string filePath in Directory.GetFiles(ART, "*.png"))
            {
                string path = filePath.Replace('\\', '/');
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 256;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                if (path.EndsWith("Square.png")) importer.spriteBorder = new Vector4(48, 48, 48, 48);
                importer.SaveAndReimport();
            }
            var settings = LoadOrCreate<TacticalMarkerSettings>(SETTINGS + "TacticalMarkerSettings.asset");
            var serialized = new SerializedObject(settings);
            var styles = serialized.FindProperty("styles");
            for (int i = 0; i < TYPES.Length; i++)
            {
                var style = styles.GetArrayElementAtIndex(i);
                style.FindPropertyRelative("icon").objectReferenceValue = Sprite(TYPES[i]);
                string sound = i == 1 || i == 3 || i == 5 ? "Urgent" : "Info";
                style.FindPropertyRelative("sound").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(
                    "Assets/Audio/SFX/TacticalMarkers/" + sound + ".wav");
            }
            serialized.FindProperty("octagonFrame").objectReferenceValue = Sprite("Octagon");
            serialized.FindProperty("squareFrame").objectReferenceValue = Sprite("Square");
            string materialPath = "Assets/Art/Materials/Vfx/TacticalMarker.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("EmpireAtWar/UI/Tactical Marker"));
                AssetDatabase.CreateAsset(material, materialPath);
            }
            serialized.FindProperty("worldMaterial").objectReferenceValue = material;
            serialized.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(settings);
            WorldMarkerView world = BuildWorld(material);
            BuildRadial(world);
            BuildMiniMap();
            BuildIconCatalog();
            BuildHint();
            GameObject context = PrefabUtility.LoadPrefabContents(SCENE_CONTEXT);
            try
            {
                var scope = context.GetComponentsInChildren<SkirmishScope>(true).Single();
                Set(scope, "tacticalMarkerSettings", settings);
                PrefabUtility.SaveAsPrefabAsset(context, SCENE_CONTEXT);
            }
            finally { PrefabUtility.UnloadPrefabContents(context); }
            AssetDatabase.SaveAssets();
            Debug.Log("Tactical marker art, settings, world/radial prefabs, minimap and tutorial references saved.");
        }

        private static WorldMarkerView BuildWorld(Material material)
        {
            GameObject root = new GameObject("WorldMarkerView");
            try
            {
                var view = root.AddComponent<WorldMarkerView>();
                Transform billboard = Child(root.transform, "Billboard");
                var frame = Child(billboard, "Frame").gameObject.AddComponent<SpriteRenderer>();
                var glyph = Child(billboard, "Glyph").gameObject.AddComponent<SpriteRenderer>();
                glyph.transform.localScale = Vector3.one * 0.65f; glyph.sortingOrder = 1;
                var sender = Child(billboard, "Sender").gameObject.AddComponent<TextMeshPro>();
                sender.font = Font(); sender.fontSize = 1.8f; sender.alignment = TextAlignmentOptions.Center;
                sender.rectTransform.sizeDelta = new Vector2(4f, 0.5f);
                sender.transform.localPosition = new Vector3(0, -0.75f, 0);
                var ring = Child(root.transform, "DashedRing");
                var filter = ring.gameObject.AddComponent<MeshFilter>();
                filter.sharedMesh = BuildRingMesh();
                var renderer = ring.gameObject.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                var beam = Line(root.transform, "Beam", material, 3f);
                beam.positionCount = 2; beam.SetPositions(new[] { Vector3.up * 2f, Vector3.up * 90f });
                var pulse = Line(root.transform, "ArrivalPulse", material, 3f);
                pulse.loop = true; pulse.positionCount = 64;
                for (int i = 0; i < 64; i++)
                {
                    float angle = i * Mathf.PI * 2f / 64;
                    pulse.SetPosition(i, new Vector3(Mathf.Sin(angle) * 65f, 2f, Mathf.Cos(angle) * 65f));
                }
                Set(view, "billboard", billboard); Set(view, "frame", frame); Set(view, "glyph", glyph);
                Set(view, "sender", sender); Set(view, "ring", ring); Set(view, "ringRenderer", renderer);
                Set(view, "beam", beam); Set(view, "pulse", pulse);
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = LayerMask.NameToLayer("WorldUi");
                return PrefabUtility.SaveAsPrefabAsset(root, PREFABS + "WorldMarkerView.prefab").GetComponent<WorldMarkerView>();
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static Mesh BuildRingMesh()
        {
            const string PATH = "Assets/Art/Models/Vfx/TacticalMarkers/TacticalMarkerRing.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(PATH);
            if (mesh != null) return mesh;
            var vertices = new List<Vector3>(); var triangles = new List<int>(); var colors = new List<Color>();
            for (int i = 0; i < 24; i++)
            {
                float start = i * Mathf.PI * 2 / 24, end = start + Mathf.PI * 2 / 24 * 0.62f;
                int at = vertices.Count;
                foreach (var point in new[] { (start, 62f), (start, 66f), (end, 66f), (end, 62f) })
                { vertices.Add(new Vector3(Mathf.Sin(point.Item1) * point.Item2, 2f, Mathf.Cos(point.Item1) * point.Item2)); colors.Add(Color.white); }
                triangles.AddRange(new[] { at, at + 1, at + 2, at, at + 2, at + 3 });
            }
            mesh = new Mesh { name = "TacticalMarkerRing" }; mesh.SetVertices(vertices); mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0); mesh.RecalculateBounds(); AssetDatabase.CreateAsset(mesh, PATH);
            return mesh;
        }

        private static void BuildRadial(WorldMarkerView world)
        {
            var root = new GameObject("MarkerRadialUi", typeof(RectTransform), typeof(CanvasGroup));
            try
            {
                Stretch((RectTransform)root.transform);
                var view = root.AddComponent<MarkerRadialUi>();
                Set(view, "canvasGroup", root.GetComponent<CanvasGroup>());
                var blocker = Rect(root.transform, "PointerBlocker", Vector2.zero, Vector2.zero); Stretch(blocker);
                Image blockerImage = blocker.gameObject.AddComponent<Image>(); blockerImage.color = Color.clear;
                var radial = Rect(root.transform, "Radial", Vector2.zero, Vector2.one * 300);
                var hubImage = Image(radial, "Hub", Vector2.zero, Vector2.one * 74, Sprite("Octagon"));
                hubImage.color = new Color32(110, 140, 170, 255);
                TMP_Text hub = Text(radial, "HubLabel", "B\nPING", Vector2.zero, new Vector2(70, 60), 16);
                var slices = new Object[6]; var frames = new Object[6]; var glyphs = new Object[6];
                for (int i = 0; i < 6; i++)
                {
                    float angle = i * Mathf.PI / 3f;
                    var slice = Rect(radial, TYPES[i], new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * 112, Vector2.one * 80);
                    slices[i] = slice;
                    frames[i] = Image(slice, "Frame", Vector2.zero, Vector2.one * 76, Sprite("Octagon"));
                    glyphs[i] = Image(slice, "Glyph", Vector2.zero, Vector2.one * 43, Sprite(TYPES[i]));
                    Text(slice, "Label", TYPES[i].ToUpperInvariant(), Vector2.down * 48, new Vector2(100, 20), 12);
                }
                var audio = root.AddComponent<AudioSource>(); audio.playOnAwake = false; audio.spatialBlend = 0f;
                string mixerGuid = AssetDatabase.FindAssets("t:AudioMixer").First();
                AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(AssetDatabase.GUIDToAssetPath(mixerGuid));
                audio.outputAudioMixerGroup = mixer.FindMatchingGroups("Sfx").Single(group => group.name == "Sfx");
                Set(view, "radial", radial); Set(view, "pointerBlocker", blocker.gameObject); Set(view, "hub", hub);
                Set(view, "worldMarkerPrefab", world); Set(view, "audioSource", audio);
                SetArray(view, "slices", slices); SetArray(view, "frames", frames); SetArray(view, "glyphs", glyphs);
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = LayerMask.NameToLayer("UI");
                string path = PREFABS + "MarkerRadialUi.prefab";
                PrefabUtility.SaveAsPrefabAsset(root, path); RegisterUi(path, "MarkerRadialUi");
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static void BuildMiniMap()
        {
            string path = AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("t:MiniMapData").Single());
            var data = AssetDatabase.LoadAssetAtPath<MiniMapData>(path);
            var so = new SerializedObject(data);
            var pairs = so.FindProperty("<MarkWrapper>k__BackingField").FindPropertyRelative("keyValue");
            for (int i = 0; i < 6; i++)
            {
                int key = (int)MarkType.TacticalLook + i, index = -1;
                for (int j = 0; j < pairs.arraySize; j++)
                    if (pairs.GetArrayElementAtIndex(j).FindPropertyRelative("key").intValue == key) index = j;
                if (index < 0) { index = pairs.arraySize; pairs.arraySize++; }
                var pair = pairs.GetArrayElementAtIndex(index);
                pair.FindPropertyRelative("key").intValue = key;
                pair.FindPropertyRelative("value").objectReferenceValue = Sprite("Map" + TYPES[i]);
            }
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(data);
            string prefabPath = AssetDatabase.GetAssetPath(data.MarkViewPrefab);
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                MarkView view = root.GetComponent<MarkView>();
                var existing = root.transform.Find("TacticalArrival");
                Image ring = existing != null ? existing.GetComponent<Image>() :
                    Image(root.transform, "TacticalArrival", Vector2.zero, Vector2.one * 24, Sprite("Ring"));
                ring.gameObject.SetActive(false); Set(view, "arrivalRing", ring);
                Set(view, "tacticalMarkerSettings", AssetDatabase.LoadAssetAtPath<TacticalMarkerSettings>(SETTINGS + "TacticalMarkerSettings.asset"));
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void BuildIconCatalog()
        {
            var data = AssetDatabase.LoadAssetAtPath<TooltipIconData>("Assets/Settings/Data/Tooltip/TooltipIconData.asset");
            var so = new SerializedObject(data); var icons = so.FindProperty("icons");
            for (int i = 0; i < 6; i++)
            {
                string key = "Marker" + TYPES[i]; int index = -1;
                for (int j = 0; j < icons.arraySize; j++)
                    if (icons.GetArrayElementAtIndex(j).FindPropertyRelative("key").stringValue == key) index = j;
                if (index < 0) { index = icons.arraySize; icons.arraySize++; }
                var entry = icons.GetArrayElementAtIndex(index); entry.FindPropertyRelative("key").stringValue = key;
                entry.FindPropertyRelative("sprite").objectReferenceValue = Sprite("Map" + TYPES[i]);
            }
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(data);
            string notificationPath = AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("BattleNotificationUi t:Prefab").Single());
            GameObject root = PrefabUtility.LoadPrefabContents(notificationPath);
            try
            {
                foreach (BattleNotificationCardView card in root.GetComponentsInChildren<BattleNotificationCardView>(true))
                {
                    var serialized = new SerializedObject(card);
                    var body = (RectTransform)serialized.FindProperty("body").objectReferenceValue;
                    body.GetComponent<UnityEngine.UI.Graphic>().raycastTarget = true;
                }
                PrefabUtility.SaveAsPrefabAsset(root, notificationPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void BuildHint()
        {
            var hint = LoadOrCreate<HintDefinition>(SETTINGS + "Hint_TacticalMarker.asset");
            var so = new SerializedObject(hint);
            so.FindProperty("id").stringValue = "tactical_markers";
            so.FindProperty("title").stringValue = "TEAM MARKERS";
            so.FindProperty("body").stringValue = "Tap {0} to ping. Hold {0} to choose a team marker. Markers are requests, not unit orders.";
            var actions = so.FindProperty("inputActions"); actions.arraySize = 1; actions.GetArrayElementAtIndex(0).stringValue = "Battle/PlaceMarker";
            so.FindProperty("showDuringCombat").boolValue = true;
            so.FindProperty("autoCloseSeconds").floatValue = 12f;
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(hint);
            var lesson = LoadOrCreate<HintLesson>(SETTINGS + "Lesson_TacticalMarker.asset");
            so = new SerializedObject(lesson); so.FindProperty("id").stringValue = "tactical_markers";
            so.FindProperty("trigger").intValue = (int)HintConditionId.AlliedMarkerPlaced;
            so.ApplyModifiedPropertiesWithoutUndo(); SetArray(lesson, "steps", new Object[] { hint });
            string trackPath = "Assets/Settings/Data/Tutorial/HintTrack.asset";
            var track = AssetDatabase.LoadAssetAtPath<HintTrack>(trackPath);
            so = new SerializedObject(track); var events = so.FindProperty("events");
            bool exists = false;
            for (int i = 0; i < events.arraySize; i++) if (events.GetArrayElementAtIndex(i).objectReferenceValue == lesson) exists = true;
            if (!exists) { events.arraySize++; events.GetArrayElementAtIndex(events.arraySize - 1).objectReferenceValue = lesson; }
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(track);
        }

        private static void RegisterUi(string path, string key)
        {
            string guid = AssetDatabase.AssetPathToGUID(path);
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var group = settings.FindGroup("Ui"); settings.CreateOrMoveEntry(guid, group).address = key;
            EditorUtility.SetDirty(group); EditorUtility.SetDirty(settings);
            var mapping = AssetDatabase.LoadAssetAtPath<AssetMappingData>("Assets/Settings/AssetMappingData.asset");
            var so = new SerializedObject(mapping); var pairs = so.FindProperty("assetMappings").FindPropertyRelative("keyValue");
            int index = -1;
            for (int i = 0; i < pairs.arraySize; i++) if (pairs.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue == key) index = i;
            if (index < 0) { index = pairs.arraySize; pairs.arraySize++; }
            var pair = pairs.GetArrayElementAtIndex(index); pair.FindPropertyRelative("key").stringValue = key;
            pair.FindPropertyRelative("value").FindPropertyRelative("m_AssetGUID").stringValue = guid;
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(mapping);
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, path); return asset;
        }
        private static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(ART + name + ".png");
        private static TMP_FontAsset Font() => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Art/Fonts/CoreGameHud/Inter SDF.asset");
        private static Transform Child(Transform parent, string name)
        { var child = new GameObject(name).transform; child.SetParent(parent, false); return child; }
        private static RectTransform Rect(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false); rect.sizeDelta = size; rect.anchoredPosition = position; return rect;
        }
        private static void Stretch(RectTransform rect)
        { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero; }
        private static Image Image(Transform parent, string name, Vector2 position, Vector2 size, Sprite sprite)
        { var image = Rect(parent, name, position, size).gameObject.AddComponent<Image>(); image.sprite = sprite; image.raycastTarget = false; return image; }
        private static TMP_Text Text(Transform parent, string name, string text, Vector2 position, Vector2 size, float fontSize)
        {
            var label = Rect(parent, name, position, size).gameObject.AddComponent<TextMeshProUGUI>();
            label.font = Font(); label.text = text; label.fontSize = fontSize; label.alignment = TextAlignmentOptions.Center;
            label.color = new Color32(248, 250, 252, 255); label.raycastTarget = false; return label;
        }
        private static LineRenderer Line(Transform parent, string name, Material material, float width)
        {
            var line = Child(parent, name).gameObject.AddComponent<LineRenderer>();
            line.sharedMaterial = material; line.useWorldSpace = false; line.widthMultiplier = width;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; line.receiveShadows = false;
            return line;
        }
        private static void Set(Object owner, string name, Object value)
        { var so = new SerializedObject(owner); so.FindProperty(name).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(owner); }
        private static void SetArray(Object owner, string name, Object[] values)
        {
            var so = new SerializedObject(owner); var array = so.FindProperty(name); array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(owner);
        }
    }
}
