using System;
using System.Text;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Repository.Data;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Editor
{
    /// <summary>Fits each ship's reinforcement preview to the visible hull of its ship view, so the hologram
    /// matches the spawned ship's size and vertical offset. The ship view is the source of truth; runs
    /// automatically when a ship view is saved (see <see cref="ShipReinforcementPreviewPostprocessor"/>).</summary>
    [EditorToolInfo("Fit ship reinforcement previews to their ship view prefabs' size and offset.")]
    public static class ShipReinforcementPreviewSync
    {
        public const string SHIP_VIEW_SUFFIX = "ShipView";
        public const string IN_SYNC = "in sync";
        private const string REINFORCEMENT_DATA_PATH = "Assets/Settings/Data/Reinforcement/ReinforcementData.asset";
        private const string ASSET_MAPPING_PATH = "Assets/Settings/AssetMappingData.asset";
        private const string SHIELD_SURFACE_NAME = "ShieldSurface";
        // Thinnest side relative to the longest; glow cards are exactly flat, hull parts never are.
        private const float FLAT_MESH_RATIO = 0.001f;

        // Relative to the ship's hull diagonal; smaller differences are not visible in game.
        public const float TOLERANCE = 0.005f;

        [MenuItem("Tools/Empire At War/Units/Ships/Sync Reinforcement Previews")]
        public static void SyncAll()
        {
            StringBuilder report = new StringBuilder("Ship reinforcement previews synced:\n");
            foreach (ShipType shipType in Enum.GetValues(typeof(ShipType)))
            {
                GameObject shipView = LoadShipView(shipType);
                // Variant ship types without their own view reuse another ship's preview.
                if (shipView == null) continue;
                report.AppendLine($"{shipType}: {Sync(shipType, shipView)}");
            }

            Debug.Log(report.ToString());
        }

        public static string Sync(ShipType shipType, GameObject shipView)
        {
            string previewPath = GetPreviewPath(shipType);
            Bounds target = MeasureModel(shipView.transform);
            GameObject root = PrefabUtility.LoadPrefabContents(previewPath);
            try
            {
                Bounds current = MeasureModel(root.transform);
                if (IsMatch(current, target)) return IN_SYNC;

                float scale = Fit(root.transform, current, target);
                Bounds fitted = MeasureModel(root.transform);
                if (!IsMatch(fitted, target))
                {
                    // A uniform fit cannot fix a differently oriented or shaped model; saving it would only distort it.
                    Debug.LogWarning($"{previewPath}: preview hull {current} differs from {shipView.name} hull " +
                                     $"{target} in shape or orientation; not changed, fix it by hand.");
                    return "shape differs, not changed";
                }

                PrefabUtility.SaveAsPrefabAsset(root, previewPath);
                return $"scaled x{scale:F3}, center {current.center} -> {target.center}";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>Scales every visual about the preview's hull center, then moves it onto the target hull center.
        /// Returns the uniform scale applied. Bounds are in <see cref="MeasureModel"/> units (root scale included).</summary>
        public static float Fit(Transform root, Bounds current, Bounds target)
        {
            float scale = target.size.magnitude / current.size.magnitude;
            Vector3 toChildUnits = new Vector3(1f / root.localScale.x, 1f / root.localScale.y, 1f / root.localScale.z);
            Vector3 currentCenter = Vector3.Scale(current.center, toChildUnits);
            Vector3 targetCenter = Vector3.Scale(target.center, toChildUnits);
            foreach (Transform visual in root)
            {
                visual.localPosition = targetCenter + (visual.localPosition - currentCenter) * scale;
                visual.localScale *= scale;
            }

            return scale;
        }

        public static string GetPreviewPath(ShipType shipType)
        {
            ReinforcementData data = AssetDatabase.LoadAssetAtPath<ReinforcementData>(REINFORCEMENT_DATA_PATH);
            return AssetDatabase.GUIDToAssetPath(data.GetSpawnPrefabKey(shipType));
        }

        public static GameObject LoadShipView(ShipType shipType)
        {
            AssetMappingData mappings = AssetDatabase.LoadAssetAtPath<AssetMappingData>(ASSET_MAPPING_PATH);
            // An unmapped name comes back unchanged, resolves to no path and loads null.
            return AssetDatabase.LoadAssetAtPath<GameObject>(
                AssetDatabase.GUIDToAssetPath(mappings.GetAssetKey(shipType + SHIP_VIEW_SUFFIX)));
        }

        public static bool IsMatch(Bounds preview, Bounds ship)
        {
            float tolerance = ship.size.magnitude * TOLERANCE;
            return (preview.center - ship.center).magnitude <= tolerance &&
                   (preview.size - ship.size).magnitude <= tolerance;
        }

        /// <summary>Bounds of the hull meshes in the root's local space, multiplied by the root's own scale: spawning
        /// keeps the prefab root scale, while placement overwrites its position and rotation, so those are ignored.
        /// Previews keep parts the ship toggles at runtime, so active state is ignored.
        /// The ship's shield bubble and flat glow cards have no preview counterpart and are excluded.</summary>
        public static Bounds MeasureModel(Transform root)
        {
            bool hasBounds = false;
            Bounds bounds = default;
            foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                if (renderer.name == SHIELD_SURFACE_NAME || filter == null || filter.sharedMesh == null)
                    continue;

                Bounds mesh = filter.sharedMesh.bounds;
                Vector3 extents = mesh.extents;
                if (Mathf.Min(extents.x, extents.y, extents.z) <=
                    Mathf.Max(extents.x, extents.y, extents.z) * FLAT_MESH_RATIO)
                    continue;

                Matrix4x4 toRoot = Matrix4x4.Scale(root.localScale) * root.worldToLocalMatrix *
                                   renderer.transform.localToWorldMatrix;
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 point = toRoot.MultiplyPoint3x4(mesh.center + Vector3.Scale(mesh.extents, new Vector3(
                        (corner & 1) == 0 ? -1f : 1f, (corner & 2) == 0 ? -1f : 1f, (corner & 4) == 0 ? -1f : 1f)));
                    if (hasBounds)
                    {
                        bounds.Encapsulate(point);
                    }
                    else
                    {
                        bounds = new Bounds(point, Vector3.zero);
                        hasBounds = true;
                    }
                }
            }

            if (!hasBounds)
                throw new MissingComponentException($"{root.name} has no hull mesh renderer.");
            return bounds;
        }
    }
}
