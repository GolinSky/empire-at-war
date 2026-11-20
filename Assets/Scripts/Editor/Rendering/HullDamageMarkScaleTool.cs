using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.TeamColor;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Editor.Rendering
{
    /// <summary>
    /// One-time setup: gives every Ship Lit unit material a hull damage Mark Scale that follows the unit's hull
    /// size, so a mark covers the same share of a corvette and of a super star destroyer, and copies it into the
    /// existing wreck copies (same name + "_Wreck"). Only Mark Scale is written: Sync Wreck Materials would also
    /// overwrite deliberate wreck differences and create wreck materials no wreck prefab uses.
    /// Run again after adding units; fine-tune single ships by hand afterwards (a rerun overwrites them).
    /// </summary>
    public static class HullDamageMarkScaleTool
    {
        private const string MENU_PATH = "Tools/Empire At War/Rendering/Materials/Auto Scale Hull Damage Marks";
        private const string UNIT_FOLDER = "Assets/Prefabs/Models";
        private const string SHIP_LIT_SHADER = "EmpireAtWar/Ship Lit";
        private const string WRECK_SUFFIX = "_Wreck";
        private const string WRECK_MATERIAL_FOLDER = "Assets/Art/Materials/Wrecks";
        // Victory: the ship the shared look was tuned on (Mark Scale 1.3 by eye, 2026-11-20).
        private const string REFERENCE_VIEW_PATH = "Assets/Prefabs/Models/Ships/Empire/View/VictoryShipView.prefab";
        private const float REFERENCE_MARK_SCALE = 1.3f;
        private const float MIN_MARK_SCALE = 0.05f;
        private const float MAX_MARK_SCALE = 32f;
        // Materials shared by units of different size get the geometric mean; warn when they differ this much.
        private const float SHARED_SPREAD_WARNING = 1.25f;

        private static readonly int MARK_SCALE_ID = Shader.PropertyToID("_HullDamageMarkScale");

        [MenuItem(MENU_PATH)]
        public static void Run()
        {
            UnitRenderStateComponent reference = AssetDatabase.LoadAssetAtPath<GameObject>(REFERENCE_VIEW_PATH)
                .GetComponentInChildren<UnitRenderStateComponent>(true);
            float referenceRadius = GetHullRadius(reference);

            var scales = new Dictionary<Material, List<float>>();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { UNIT_FOLDER }))
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                foreach (UnitRenderStateComponent unit in prefab.GetComponentsInChildren<UnitRenderStateComponent>(true))
                {
                    float radius = GetHullRadius(unit);
                    if (radius <= 0f) continue;
                    float scale = Mathf.Clamp(REFERENCE_MARK_SCALE * radius / referenceRadius, MIN_MARK_SCALE,
                        MAX_MARK_SCALE);
                    foreach (MeshRenderer meshRenderer in GetRenderers(unit))
                    {
                        foreach (Material material in meshRenderer.sharedMaterials)
                        {
                            if (material == null || material.shader.name != SHIP_LIT_SHADER) continue;
                            if (!scales.TryGetValue(material, out List<float> list))
                                scales.Add(material, list = new List<float>());
                            list.Add(scale);
                        }
                    }
                }
            }

            Dictionary<string, List<Material>> wrecks = FindWreckMaterials();
            int wreckCount = 0;
            foreach (KeyValuePair<Material, List<float>> entry in scales)
            {
                float min = entry.Value.Min();
                float max = entry.Value.Max();
                if (max > min * SHARED_SPREAD_WARNING)
                    Debug.LogWarning($"[HullDamage] {entry.Key.name} is shared by units of different size " +
                        $"(Mark Scale {min:0.##}-{max:0.##}); using their geometric mean.", entry.Key);
                float scale = Mathf.Exp(entry.Value.Average(value => Mathf.Log(value)));
                SetMarkScale(entry.Key, scale);
                if (!wrecks.TryGetValue(entry.Key.name + WRECK_SUFFIX, out List<Material> copies)) continue;
                foreach (Material wreck in copies)
                {
                    SetMarkScale(wreck, scale);
                    wreckCount++;
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[HullDamage] Mark Scale set on {scales.Count} Ship Lit materials and {wreckCount} wreck copies " +
                $"(reference {reference.name}: radius {referenceRadius:0.#} → {REFERENCE_MARK_SCALE}).");
        }

        private static void SetMarkScale(Material material, float scale)
        {
            material.SetFloat(MARK_SCALE_ID, scale);
            EditorUtility.SetDirty(material);
        }

        private static Dictionary<string, List<Material>> FindWreckMaterials()
        {
            var wrecks = new Dictionary<string, List<Material>>();
            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { WRECK_MATERIAL_FOLDER }))
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (!wrecks.TryGetValue(material.name, out List<Material> list))
                    wrecks.Add(material.name, list = new List<Material>());
                list.Add(material);
            }

            return wrecks;
        }

        // Half the diagonal of the world-space box around the unit's hull renderers (prefab at its own scale).
        private static float GetHullRadius(UnitRenderStateComponent unit)
        {
            bool hasBounds = false;
            Bounds bounds = default;
            foreach (MeshRenderer meshRenderer in GetRenderers(unit))
            {
                MeshFilter meshFilter = meshRenderer.GetComponent<MeshFilter>();
                if (meshFilter == null || meshFilter.sharedMesh == null) continue;
                Bounds mesh = meshFilter.sharedMesh.bounds;
                Matrix4x4 toWorld = meshRenderer.transform.localToWorldMatrix;
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 sign = new Vector3((corner & 1) == 0 ? -1f : 1f, (corner & 2) == 0 ? -1f : 1f,
                        (corner & 4) == 0 ? -1f : 1f);
                    Vector3 point = toWorld.MultiplyPoint3x4(mesh.center + Vector3.Scale(mesh.extents, sign));
                    if (hasBounds) bounds.Encapsulate(point);
                    else bounds = new Bounds(point, Vector3.zero);
                    hasBounds = true;
                }
            }

            return hasBounds ? bounds.extents.magnitude : 0f;
        }

        private static IEnumerable<MeshRenderer> GetRenderers(UnitRenderStateComponent unit)
        {
            SerializedProperty renderers = new SerializedObject(unit).FindProperty("meshRenderers");
            for (int i = 0; i < renderers.arraySize; i++)
            {
                if (renderers.GetArrayElementAtIndex(i).objectReferenceValue is MeshRenderer meshRenderer)
                    yield return meshRenderer;
            }
        }
    }
}
