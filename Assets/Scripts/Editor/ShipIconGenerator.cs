using System;
using System.Collections.Generic;
using System.IO;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.ShipUi;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace EmpireAtWar.Editor
{
    public static class ShipIconGenerator
    {
        private const string PREFAB_FOLDER = "Assets/Prefabs/Models/Ships";
        private const string ICON_OUTPUT_FOLDER = "Assets/Art/Textures/Ui/Icons/ShipIcon";
        private const string SHIP_UI_MODEL_PATH = "Assets/Settings/Data/Models/ShipUi/ShipUiModel.asset";
        private const string FACTIONS_MODEL_PATH = "Assets/Settings/Data/Models/Factions/FactionsModel.asset";
        private const int ICON_RESOLUTION = 512;
        private const int ICON_RENDER_LAYER = 31; // Dedicated layer to isolate ship from scene environment

        private class ShipMappingInfo
        {
            public ShipType ShipType;
            public string PrefabName;
            public string IconFileName;
        }

        private static readonly List<ShipMappingInfo> MAPPINGS = new List<ShipMappingInfo>
        {
            new ShipMappingInfo { ShipType = ShipType.Venator, PrefabName = "VenatorShipView.prefab", IconFileName = "VenatorIcon.png" },
            new ShipMappingInfo { ShipType = ShipType.Acclamator, PrefabName = "AcclamatorShipView.prefab", IconFileName = "AcclamatorIcon.png" },
            new ShipMappingInfo { ShipType = ShipType.Arquitens, PrefabName = "ArquitensShipView.prefab", IconFileName = "ArquitensIcon.png" },
            new ShipMappingInfo { ShipType = ShipType.StarDestroyer1, PrefabName = "StarDestroyer1ShipView.prefab", IconFileName = "StarDestroyer1Icon.png" },
            new ShipMappingInfo { ShipType = ShipType.StarDestroyer2, PrefabName = "StarDestroyer2ShipView.prefab", IconFileName = "StarDestroyer2Icon.png" },
            new ShipMappingInfo { ShipType = ShipType.HeavyDreadnought, PrefabName = "HeavyDreadnoughtShipView.prefab", IconFileName = "HeavyDreadnoughtIcon.png" },
            new ShipMappingInfo { ShipType = ShipType.Providence, PrefabName = "ProvidenceShipView.prefab", IconFileName = "ProvidenceIcon.png" },
            new ShipMappingInfo { ShipType = ShipType.Recusant, PrefabName = "RecusantShipView.prefab", IconFileName = "RecusantIcon.png" },
            new ShipMappingInfo { ShipType = ShipType.Munificent, PrefabName = "MunificentShipView.prefab", IconFileName = "MunificentIcon.png" },
            new ShipMappingInfo { ShipType = ShipType.Lucrehulk, PrefabName = "LucrehulkShipView.prefab", IconFileName = "LucrehulkIcon.png" },
        };

        [MenuItem("Tools/Generate Ship Icons")]
        public static void GenerateAllShipIcons()
        {
            if (!Directory.Exists(ICON_OUTPUT_FOLDER))
            {
                Directory.CreateDirectory(ICON_OUTPUT_FOLDER);
            }

            // Inspect scene references ScreenShotBlueprintCamera & VenatorShipView
            GameObject refCamGo = null;
            GameObject refVenatorGo = null;

            foreach (GameObject go in UnityEngine.Object.FindObjectsOfType<GameObject>())
            {
                if (go.name.Contains("ScreenShotBlueprintCamera")) refCamGo = go;
                if (go.name.Contains("VenatorShipView")) refVenatorGo = go;
            }

            // Default fallback rotation & FOV matching user's blueprint camera setup
            Quaternion refCamRot = Quaternion.Euler(40.76f, 125.00f, 0.00f);
            float fov = 60f;

            if (refCamGo != null)
            {
                Camera blueprintCam = refCamGo.GetComponent<Camera>();
                if (blueprintCam != null)
                {
                    fov = blueprintCam.fieldOfView;
                }
                refCamRot = refCamGo.transform.rotation;
            }

            // Disable scene blueprint references before rendering real screenshots
            List<GameObject> disabledRefs = new List<GameObject>();
            if (refCamGo != null && refCamGo.activeSelf) { refCamGo.SetActive(false); disabledRefs.Add(refCamGo); }
            if (refVenatorGo != null && refVenatorGo.activeSelf) { refVenatorGo.SetActive(false); disabledRefs.Add(refVenatorGo); }

            Vector3 camForward = refCamRot * Vector3.forward;

            // Save ambient light settings to restore after rendering
            AmbientMode originalAmbientMode = RenderSettings.ambientMode;
            Color originalAmbientLight = RenderSettings.ambientLight;
            Color originalSkyColor = RenderSettings.ambientSkyColor;

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.48f, 0.50f, 0.55f, 1.0f);

            // Create temporary camera and lights setup at isolated offscreen location
            Vector3 offscreenPos = new Vector3(5000f, 5000f, 5000f);
            GameObject setupRoot = new GameObject("IconGen_SetupRoot");
            setupRoot.transform.position = offscreenPos;

            // Key light (main directional light matching blueprint camera angle)
            GameObject keyLightGo = new GameObject("KeyLight");
            keyLightGo.transform.SetParent(setupRoot.transform);
            keyLightGo.transform.position = offscreenPos;
            Light keyLight = keyLightGo.AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.color = new Color(1.0f, 0.98f, 0.94f);
            keyLight.intensity = 1.4f;
            keyLight.cullingMask = 1 << ICON_RENDER_LAYER;
            keyLightGo.transform.rotation = Quaternion.Euler(45f, 35f, 0f);

            // Fill light (softer directional light from opposite side)
            GameObject fillLightGo = new GameObject("FillLight");
            fillLightGo.transform.SetParent(setupRoot.transform);
            fillLightGo.transform.position = offscreenPos;
            Light fillLight = fillLightGo.AddComponent<Light>();
            fillLight.type = LightType.Directional;
            fillLight.color = new Color(0.75f, 0.85f, 1.0f);
            fillLight.intensity = 0.7f;
            fillLight.cullingMask = 1 << ICON_RENDER_LAYER;
            fillLightGo.transform.rotation = Quaternion.Euler(-15f, -135f, 0f);

            // Camera setup - Perspective mode matching blueprint camera
            GameObject camGo = new GameObject("IconCam");
            camGo.transform.SetParent(setupRoot.transform);
            camGo.transform.position = offscreenPos;
            Camera cam = camGo.AddComponent<Camera>();
            cam.cullingMask = 1 << ICON_RENDER_LAYER;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0f, 0f, 0f, 0f); // Transparent background
            cam.orthographic = false; // Perspective mode matching blueprint camera
            cam.fieldOfView = fov;
            cam.aspect = 1.0f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 2000f;

            SetLayerRecursively(setupRoot, ICON_RENDER_LAYER);

            RenderTexture renderTexture = RenderTexture.GetTemporary(ICON_RESOLUTION, ICON_RESOLUTION, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = renderTexture;

            Dictionary<ShipMappingInfo, byte[]> renderedBytesMap = new Dictionary<ShipMappingInfo, byte[]>();

            try
            {
                foreach (ShipMappingInfo mapping in MAPPINGS)
                {
                    string prefabPath = Path.Combine(PREFAB_FOLDER, mapping.PrefabName).Replace('\\', '/');
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    if (prefab == null)
                    {
                        Debug.LogError($"[ShipIconGenerator] Could not load prefab at: {prefabPath}");
                        continue;
                    }

                    GameObject shipInstance = UnityEngine.Object.Instantiate(prefab, offscreenPos, Quaternion.identity, setupRoot.transform);
                    shipInstance.transform.localPosition = Vector3.zero;
                    shipInstance.transform.localRotation = Quaternion.identity;

                    SetLayerRecursively(shipInstance, ICON_RENDER_LAYER);

                    Renderer[] allRenderers = shipInstance.GetComponentsInChildren<Renderer>(true);
                    List<Renderer> validShipRenderers = new List<Renderer>();

                    // Strictly filter renderers: keep solid 3D ship meshes (MeshRenderer / SkinnedMeshRenderer).
                    // Ignore empty zero-extent mesh outliers (such as empty point nodes in HeavyDreadnought).
                    // Disable LineRenderers (selection rings), ParticleSystems (VFX/engine flares), and Shield bubble meshes.
                    foreach (Renderer r in allRenderers)
                    {
                        bool isMesh = (r is MeshRenderer || r is SkinnedMeshRenderer);
                        string objName = r.gameObject.name.ToLower();
                        string matName = r.sharedMaterial != null ? r.sharedMaterial.name.ToLower() : "";

                        bool isShield = objName.Contains("shield") || (objName == "sphere" && matName.Contains("shield"));
                        bool isNonShipRenderer = !isMesh ||
                                                 objName.Contains("vfx") ||
                                                 objName.Contains("line") ||
                                                 objName.Contains("selected") ||
                                                 objName.Contains("indicator") ||
                                                 matName.Contains("linerender") ||
                                                 matName.Contains("vfx");

                        bool isEmptyOutlier = r.bounds.extents.sqrMagnitude < 0.01f;

                        if (isMesh && !isShield && !isNonShipRenderer && !isEmptyOutlier)
                        {
                            r.enabled = true;
                            validShipRenderers.Add(r);
                        }
                        else
                        {
                            r.enabled = false;
                        }
                    }

                    if (validShipRenderers.Count == 0)
                    {
                        Debug.LogWarning($"[ShipIconGenerator] No valid ship mesh renderers found in prefab {mapping.PrefabName}");
                        UnityEngine.Object.DestroyImmediate(shipInstance);
                        continue;
                    }

                    // Calculate combined bounds of valid ship mesh renderers only
                    Bounds bounds = validShipRenderers[0].bounds;
                    for (int i = 1; i < validShipRenderers.Count; i++)
                    {
                        bounds.Encapsulate(validShipRenderers[i].bounds);
                    }

                    // Solve camera distance aimed directly at bounds.center so the ship is DEAD-CENTERED and fills 85% of icon frame with 7.5% margin
                    float solvedCamDistance = SolveDeadCenteredFittingDistance(cam, bounds, camForward, refCamRot, 0.075f);

                    camGo.transform.position = bounds.center - camForward * solvedCamDistance;
                    camGo.transform.rotation = refCamRot;

                    // Render camera
                    cam.Render();

                    // Read pixels into Texture2D
                    RenderTexture.active = renderTexture;
                    Texture2D tex = new Texture2D(ICON_RESOLUTION, ICON_RESOLUTION, TextureFormat.RGBA32, false);
                    tex.ReadPixels(new Rect(0, 0, ICON_RESOLUTION, ICON_RESOLUTION), 0, 0);
                    tex.Apply();
                    RenderTexture.active = null;

                    renderedBytesMap[mapping] = tex.EncodeToPNG();
                    UnityEngine.Object.DestroyImmediate(tex);
                    UnityEngine.Object.DestroyImmediate(shipInstance);
                }

                // Phase 1: Write all PNG files to disk using FileShare.ReadWrite stream to prevent Win32 IO 1224 locks
                AssetDatabase.StartAssetEditing();
                try
                {
                    foreach (var kvp in renderedBytesMap)
                    {
                        string iconPath = Path.Combine(ICON_OUTPUT_FOLDER, kvp.Key.IconFileName).Replace('\\', '/');
                        SafeWriteAllBytes(iconPath, kvp.Value);
                    }
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                }

                // Phase 2: Refresh asset database and configure texture importers
                AssetDatabase.Refresh();

                Dictionary<ShipType, Sprite> generatedSprites = new Dictionary<ShipType, Sprite>();
                foreach (var kvp in renderedBytesMap)
                {
                    string iconPath = Path.Combine(ICON_OUTPUT_FOLDER, kvp.Key.IconFileName).Replace('\\', '/');
                    TextureImporter importer = AssetImporter.GetAtPath(iconPath) as TextureImporter;
                    if (importer != null)
                    {
                        importer.textureType = TextureImporterType.Sprite;
                        importer.spriteImportMode = SpriteImportMode.Single;
                        importer.alphaIsTransparency = true;
                        importer.mipmapEnabled = false;
                        importer.filterMode = FilterMode.Bilinear;
                        importer.SaveAndReimport();
                    }

                    Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
                    if (sprite != null)
                    {
                        generatedSprites[kvp.Key.ShipType] = sprite;
                    }
                }

                UpdateShipUiModel(generatedSprites);
                UpdateFactionsModel(generatedSprites);
                Debug.Log("[ShipIconGenerator] Successfully generated dead-centered 85%-fill blueprint ship icons and updated ShipUiModel & FactionsModel!");
            }
            finally
            {
                RenderTexture.active = null;
                cam.targetTexture = null;
                RenderTexture.ReleaseTemporary(renderTexture);

                RenderSettings.ambientMode = originalAmbientMode;
                RenderSettings.ambientLight = originalAmbientLight;
                RenderSettings.ambientSkyColor = originalSkyColor;

                UnityEngine.Object.DestroyImmediate(setupRoot);

                // Restore scene reference objects
                foreach (GameObject go in disabledRefs)
                {
                    if (go != null) go.SetActive(true);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private static void SafeWriteAllBytes(string relativePath, byte[] bytes)
        {
            string fullPath = Path.GetFullPath(relativePath);
            using (FileStream fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            {
                fs.Write(bytes, 0, bytes.Length);
                fs.Flush(true);
            }
        }

        private static float SolveDeadCenteredFittingDistance(Camera cam, Bounds bounds, Vector3 camForward, Quaternion camRot, float targetMargin)
        {
            Vector3 center = bounds.center;
            Vector3 extents = bounds.extents;
            Vector3[] corners = new Vector3[8]
            {
                center + new Vector3(-extents.x, -extents.y, -extents.z),
                center + new Vector3(-extents.x, -extents.y,  extents.z),
                center + new Vector3(-extents.x,  extents.y, -extents.z),
                center + new Vector3(-extents.x,  extents.y,  extents.z),
                center + new Vector3( extents.x, -extents.y, -extents.z),
                center + new Vector3( extents.x, -extents.y,  extents.z),
                center + new Vector3( extents.x,  extents.y, -extents.z),
                center + new Vector3( extents.x,  extents.y,  extents.z),
            };

            float minAllowedVp = targetMargin;
            float maxAllowedVp = 1.0f - targetMargin;

            float low = 0.1f;
            float high = 1000f;
            float bestD = 50f;

            cam.transform.rotation = camRot;

            for (int iter = 0; iter < 30; iter++)
            {
                float midD = (low + high) * 0.5f;
                cam.transform.position = center - camForward * midD;

                bool fits = true;
                foreach (Vector3 c in corners)
                {
                    Vector3 vp = cam.WorldToViewportPoint(c);
                    if (vp.z <= 0.1f || vp.x < minAllowedVp || vp.x > maxAllowedVp || vp.y < minAllowedVp || vp.y > maxAllowedVp)
                    {
                        fits = false;
                        break;
                    }
                }

                if (fits)
                {
                    bestD = midD;
                    high = midD; // Try closer for tighter centered fit
                }
                else
                {
                    low = midD; // Move further away to fit all corners inside margin
                }
            }

            return bestD;
        }

        private static void SetLayerRecursively(GameObject obj, int layer)
        {
            if (obj == null) return;
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                if (child != null)
                {
                    SetLayerRecursively(child.gameObject, layer);
                }
            }
        }

        private static void UpdateShipUiModel(Dictionary<ShipType, Sprite> generatedSprites)
        {
            ShipUiModel modelAsset = AssetDatabase.LoadAssetAtPath<ShipUiModel>(SHIP_UI_MODEL_PATH);
            if (modelAsset == null)
            {
                Debug.LogError($"[ShipIconGenerator] Could not load ShipUiModel at {SHIP_UI_MODEL_PATH}");
                return;
            }

            SerializedObject serializedModel = new SerializedObject(modelAsset);
            SerializedProperty wrapperProp = serializedModel.FindProperty("shipIconWrapper");
            if (wrapperProp == null)
            {
                Debug.LogError("[ShipIconGenerator] Could not find shipIconWrapper property in ShipUiModel");
                return;
            }

            SerializedProperty keyValueArray = wrapperProp.FindPropertyRelative("keyValue");
            if (keyValueArray == null)
            {
                Debug.LogError("[ShipIconGenerator] Could not find keyValue array property in shipIconWrapper");
                return;
            }

            foreach (var kvp in generatedSprites)
            {
                int shipTypeKey = (int)kvp.Key;
                Sprite sprite = kvp.Value;

                bool found = false;
                for (int i = 0; i < keyValueArray.arraySize; i++)
                {
                    SerializedProperty element = keyValueArray.GetArrayElementAtIndex(i);
                    SerializedProperty keyProp = element.FindPropertyRelative("key");
                    if (keyProp.intValue == shipTypeKey)
                    {
                        SerializedProperty valProp = element.FindPropertyRelative("value");
                        valProp.objectReferenceValue = sprite;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    int newIndex = keyValueArray.arraySize;
                    keyValueArray.InsertArrayElementAtIndex(newIndex);
                    SerializedProperty element = keyValueArray.GetArrayElementAtIndex(newIndex);
                    element.FindPropertyRelative("key").intValue = shipTypeKey;
                    element.FindPropertyRelative("value").objectReferenceValue = sprite;
                }
            }

            serializedModel.ApplyModifiedProperties();
            EditorUtility.SetDirty(modelAsset);
        }

        private static void UpdateFactionsModel(Dictionary<ShipType, Sprite> generatedSprites)
        {
            UnityEngine.Object modelAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(FACTIONS_MODEL_PATH);
            if (modelAsset == null)
            {
                Debug.LogError($"[ShipIconGenerator] Could not load FactionsModel at {FACTIONS_MODEL_PATH}");
                return;
            }

            SerializedObject serializedModel = new SerializedObject(modelAsset);
            SerializedProperty wrapperProp = serializedModel.FindProperty("factionDataWrapper");
            if (wrapperProp == null)
            {
                Debug.LogError("[ShipIconGenerator] Could not find factionDataWrapper property in FactionsModel");
                return;
            }

            SerializedProperty keyValueArray = wrapperProp.FindPropertyRelative("keyValue");
            if (keyValueArray == null)
            {
                Debug.LogError("[ShipIconGenerator] Could not find keyValue array property in factionDataWrapper");
                return;
            }

            for (int i = 0; i < keyValueArray.arraySize; i++)
            {
                SerializedProperty element = keyValueArray.GetArrayElementAtIndex(i);
                SerializedProperty factionDictProp = element.FindPropertyRelative("value");
                if (factionDictProp == null) continue;

                SerializedProperty shipDataArray = factionDictProp.FindPropertyRelative("keyValue");
                if (shipDataArray == null) continue;

                for (int j = 0; j < shipDataArray.arraySize; j++)
                {
                    SerializedProperty shipElement = shipDataArray.GetArrayElementAtIndex(j);
                    SerializedProperty keyProp = shipElement.FindPropertyRelative("key");
                    int shipTypeKey = keyProp.intValue;

                    if (generatedSprites.TryGetValue((ShipType)shipTypeKey, out Sprite sprite))
                    {
                        SerializedProperty valueProp = shipElement.FindPropertyRelative("value");
                        if (valueProp != null)
                        {
                            SerializedProperty iconProp = valueProp.FindPropertyRelative("<Icon>k__BackingField");
                            if (iconProp != null)
                            {
                                iconProp.objectReferenceValue = sprite;
                            }
                        }
                    }
                }
            }

            serializedModel.ApplyModifiedProperties();
            EditorUtility.SetDirty(modelAsset);
        }
    }
}
