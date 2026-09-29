using System.Collections.Generic;
using EmpireAtWar.Services.UnitWreck;
using UnityEngine;

namespace EmpireAtWar.ViewComponents.Wreck
{
    /// <summary>
    /// Pooled wreck of a destroyed unit. Renders the EmpireAtWar/Ship Wreck shader, which cuts the ship's
    /// own meshes into parts and animates them on the GPU from the values written here once per spawn.
    /// </summary>
    public sealed class UnitWreckView : MonoBehaviour
    {
        // Past the end of any mesh: puts the unused second cut of a two-part wreck out of reach.
        private const float NO_CUT = 1000000f;

        private static readonly int AXIS_ID = Shader.PropertyToID("_WreckAxis");
        private static readonly int CENTER_ID = Shader.PropertyToID("_WreckCenter");
        private static readonly int CUTS_ID = Shader.PropertyToID("_WreckCuts");
        private static readonly int AXIS_RANGE_ID = Shader.PropertyToID("_WreckAxisRange");
        private static readonly int SEED_ID = Shader.PropertyToID("_WreckSeed");
        private static readonly int START_TIME_ID = Shader.PropertyToID("_WreckStartTime");
        private static readonly int LIFETIME_ID = Shader.PropertyToID("_WreckLifetime");
        private static readonly int DISSOLVE_DURATION_ID = Shader.PropertyToID("_WreckDissolveDuration");
        private static readonly int SEPARATION_SPEED_ID = Shader.PropertyToID("_WreckSeparationSpeed");
        private static readonly int TILT_SPEED_ID = Shader.PropertyToID("_WreckTiltSpeed");
        private static readonly int SINK_SPEED_ID = Shader.PropertyToID("_WreckSinkSpeed");
        private static readonly int GLOW_DURATION_ID = Shader.PropertyToID("_WreckGlowDuration");
        private static readonly int TORN_EDGE_WIDTH_ID = Shader.PropertyToID("_WreckTornEdgeWidth");
        private static readonly int HEAT_COLOR_ID = Shader.PropertyToID("_WreckHeatColor");
        private static readonly int DISSOLVE_EDGE_COLOR_ID = Shader.PropertyToID("_WreckDissolveEdgeColor");
        private static readonly int DISSOLVE_EDGE_WIDTH_ID = Shader.PropertyToID("_WreckDissolveEdgeWidth");
        private static readonly int DISSOLVE_NOISE_SCALE_ID = Shader.PropertyToID("_WreckDissolveNoiseScale");

        [SerializeField] private MeshRenderer[] meshRenderers;
        [SerializeField] private MeshFilter[] meshFilters;

        private readonly Dictionary<Material, Material> _instances = new Dictionary<Material, Material>();
        private Bounds _shipBounds;

        private void Awake()
        {
            // Own material instances: each pooled wreck keeps its own cuts and start time. One instance per
            // source material, shared by this wreck's renderers (some ships have hundreds of renderers).
            // Same shader variant, so the SRP Batcher still batches them.
            for (int i = 0; i < meshRenderers.Length; i++)
            {
                Material[] materials = meshRenderers[i].sharedMaterials;
                for (int slot = 0; slot < materials.Length; slot++)
                {
                    if (!_instances.TryGetValue(materials[slot], out Material instance))
                    {
                        instance = new Material(materials[slot]);
                        _instances.Add(materials[slot], instance);
                    }

                    materials[slot] = instance;
                }

                meshRenderers[i].sharedMaterials = materials;
            }

            // One cut frame for the whole ship, so hull, bridge and other meshes break along the same lines.
            _shipBounds = GetBoundsInRoot(0);
            for (int i = 1; i < meshRenderers.Length; i++)
            {
                _shipBounds.Encapsulate(GetBoundsInRoot(i));
            }
        }

        private void OnDestroy()
        {
            foreach (Material instance in _instances.Values)
            {
                Destroy(instance);
            }
        }

        public void Show(Vector3 position, Quaternion rotation, uint teamUserValue, float startTime,
            WreckCutPlan cutPlan, float seed, UnitWreckData data)
        {
            transform.SetPositionAndRotation(position, rotation);
            // A mesh turns around its part's center, which can be up to the whole ship's size away.
            float shipRadius = transform.TransformVector(_shipBounds.extents).magnitude;
            for (int i = 0; i < meshRenderers.Length; i++)
            {
                MeshRenderer meshRenderer = meshRenderers[i];
                meshRenderer.SetShaderUserValue(teamUserValue);
                meshRenderer.localBounds = GetTravelBounds(meshFilters[i].sharedMesh.bounds,
                    (data.MaxTravel + shipRadius) / meshRenderer.transform.lossyScale.x);
            }

            foreach (Material material in _instances.Values)
            {
                ApplyCut(material, cutPlan, seed);
                Apply(material, startTime, data);
            }

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private static Bounds GetTravelBounds(Bounds meshBounds, float travel)
        {
            meshBounds.Expand(2f * (travel + meshBounds.extents.magnitude));
            return meshBounds;
        }

        private Matrix4x4 GetMeshToRoot(int index)
        {
            return transform.worldToLocalMatrix * meshRenderers[index].transform.localToWorldMatrix;
        }

        private Bounds GetBoundsInRoot(int index)
        {
            Bounds meshBounds = meshFilters[index].sharedMesh.bounds;
            Matrix4x4 meshToRoot = GetMeshToRoot(index);
            Bounds bounds = new Bounds(meshToRoot.MultiplyPoint3x4(meshBounds.center), Vector3.zero);
            Vector3 extents = meshBounds.extents;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 sign = new Vector3((corner & 1) == 0 ? -1f : 1f, (corner & 2) == 0 ? -1f : 1f,
                    (corner & 4) == 0 ? -1f : 1f);
                bounds.Encapsulate(meshToRoot.MultiplyPoint3x4(meshBounds.center + Vector3.Scale(extents, sign)));
            }

            return bounds;
        }

        // The ship is cut across the longest axis of the whole ship (all meshes, root space), written in
        // world space so every mesh of this wreck shares the same values. Positions are relative to the ship center.
        private void ApplyCut(Material material, WreckCutPlan cutPlan, float seed)
        {
            Vector3 extents = _shipBounds.extents;
            Vector3 rootAxis = extents.x >= extents.y && extents.x >= extents.z ? Vector3.right
                : extents.y >= extents.z ? Vector3.up : Vector3.forward;
            Vector3 worldAxis = transform.TransformVector(rootAxis);
            // World units per root unit along the axis: converts the ship's length into world units.
            float worldPerRoot = worldAxis.magnitude;
            float halfLength = Vector3.Dot(extents, rootAxis) * worldPerRoot;
            float firstCut = Mathf.Lerp(-halfLength, halfLength, cutPlan.FirstCut);
            float secondCut = cutPlan.PartCount == 3 ? Mathf.Lerp(-halfLength, halfLength, cutPlan.SecondCut) : NO_CUT;

            material.SetVector(AXIS_ID, worldAxis / worldPerRoot);
            material.SetVector(CENTER_ID, transform.TransformPoint(_shipBounds.center));
            material.SetVector(CUTS_ID, new Vector4(firstCut, secondCut, cutPlan.PartCount, 0f));
            material.SetVector(AXIS_RANGE_ID, new Vector4(-halfLength, halfLength, 0f, 0f));
            material.SetFloat(SEED_ID, seed);
        }

        private static void Apply(Material material, float startTime, UnitWreckData data)
        {
            material.SetFloat(START_TIME_ID, startTime);
            material.SetFloat(LIFETIME_ID, data.Lifetime);
            material.SetFloat(DISSOLVE_DURATION_ID, data.DissolveDuration);
            material.SetFloat(SEPARATION_SPEED_ID, data.SeparationSpeed);
            material.SetFloat(TILT_SPEED_ID, data.TiltSpeed);
            material.SetFloat(SINK_SPEED_ID, data.SinkSpeed);
            material.SetFloat(GLOW_DURATION_ID, data.GlowDuration);
            material.SetFloat(TORN_EDGE_WIDTH_ID, data.TornEdgeWidth);
            material.SetColor(HEAT_COLOR_ID, data.HeatColor);
            material.SetColor(DISSOLVE_EDGE_COLOR_ID, data.DissolveEdgeColor);
            material.SetFloat(DISSOLVE_EDGE_WIDTH_ID, data.DissolveEdgeWidth);
            material.SetFloat(DISSOLVE_NOISE_SCALE_ID, data.DissolveNoiseScale);
        }
    }
}
