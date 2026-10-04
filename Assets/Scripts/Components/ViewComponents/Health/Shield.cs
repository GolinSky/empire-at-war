using UnityEngine;

using EmpireAtWar.Components.Ship.Health;

namespace EmpireAtWar.ViewComponents.Health
{
    public sealed class Shield : MonoBehaviour, IShieldView
    {
        private const int MAX_IMPACTS = 8;
        // Impact radius in world units grows with the square root of the hit's damage:
        // laser (6) ~2.4, turbolaser (20) ~4.5, heavy turbolaser (50) ~7.1, beam (150) ~12.2.
        private const float RADIUS_PER_SQRT_DAMAGE = 1f;
        private const float MIN_IMPACT_RADIUS = 2f;
        private const float MAX_IMPACT_RADIUS = 16f;
        // Vertex bulge as a fraction of the impact radius.
        private const float DISPLACEMENT_RATIO = 0.08f;

        [SerializeField] private MeshRenderer shieldRenderer;
        private readonly Vector4[] _impacts = new Vector4[MAX_IMPACTS];
        private readonly float[] _impactRadii = new float[MAX_IMPACTS];
        private MaterialPropertyBlock _properties;

        [SerializeField, ColorUsage(true, true)] private Color color = new Color(0.15f, 0.65f, 1f, 0.65f);

        [SerializeField, Min(0f)] private float brightness = 2f;
        [SerializeField, Min(0.01f)] private float fadeDuration = 1.2f;

        [Tooltip("Baked by ShieldHullBaker: local planes of the convex shell around the hull, inside where dot(xyz, p) <= w.")]
        [SerializeField] private Vector4[] hullPlanes;

        private Bounds _meshBounds;

        private int _impactCount;

        private bool _active;

        private void Awake()
        {
            _meshBounds = shieldRenderer.localBounds;
        }

        public void SetActive(bool active)
        {
            _active = active;
            if (!active)
            {
                _impactCount = 0;
                shieldRenderer.enabled = false;
            }
        }

        public Vector3 GetSurfacePosition(Vector3 origin, Vector3 target)
        {
            // Clips the shot segment against every shell plane; the segment enters the shell at the latest entry.
            Transform surface = shieldRenderer.transform;
            Vector3 localOrigin = surface.InverseTransformPoint(origin);
            Vector3 direction = surface.InverseTransformPoint(target) - localOrigin;
            float enter = float.NegativeInfinity;
            float exit = float.PositiveInfinity;
            for (int i = 0; i < hullPlanes.Length; i++)
            {
                Vector3 normal = hullPlanes[i];
                float approach = Vector3.Dot(normal, direction);
                float gap = hullPlanes[i].w - Vector3.Dot(normal, localOrigin);
                if (approach < 0f) enter = Mathf.Max(enter, gap / approach);
                else if (approach > 0f) exit = Mathf.Min(exit, gap / approach);
                else if (gap < 0f) return target;
            }

            // An origin inside the shield must not shoot through the hull to its far surface.
            if (enter > exit || enter < 0f || enter > 1f) return target;
            return surface.TransformPoint(localOrigin + direction * enter);
        }

        public void ShowImpact(Vector3 position, float damage)
        {
            if (!_active) return;
            Vector3 local = shieldRenderer.transform.InverseTransformPoint(position);
            if (_impactCount == MAX_IMPACTS)
            {
                System.Array.Copy(_impacts, 1, _impacts, 0, MAX_IMPACTS - 1);
                System.Array.Copy(_impactRadii, 1, _impactRadii, 0, MAX_IMPACTS - 1);
                _impactCount--;
            }

            _impacts[_impactCount] = new Vector4(local.x, local.y, local.z, Time.time);
            _impactRadii[_impactCount] = Mathf.Clamp(RADIUS_PER_SQRT_DAMAGE * Mathf.Sqrt(damage),
                MIN_IMPACT_RADIUS, MAX_IMPACT_RADIUS);
            _impactCount++;
            UpdateMaterial();
        }

        private void LateUpdate()
        {
            if (!_active || _impactCount == 0) return;
            int liveCount = 0;
            for (int i = 0; i < _impactCount; i++)
            {
                if (Time.time - _impacts[i].w >= fadeDuration) continue;
                _impacts[liveCount] = _impacts[i];
                _impactRadii[liveCount] = _impactRadii[i];
                liveCount++;
            }

            _impactCount = liveCount;
            shieldRenderer.enabled = _impactCount > 0;
            if (_impactCount > 0) UpdateMaterial();
        }

        private void UpdateMaterial()
        {
            if (_properties == null) _properties = new MaterialPropertyBlock();
            Vector3 axes = shieldRenderer.transform.lossyScale;
            axes = new Vector3(Mathf.Abs(axes.x), Mathf.Abs(axes.y), Mathf.Abs(axes.z));
            float maxRadius = 0f;
            for (int i = 0; i < _impactCount; i++)
                maxRadius = Mathf.Max(maxRadius, _impactRadii[i]);
            float maxDisplacement = maxRadius * DISPLACEMENT_RATIO;
            _properties.SetVectorArray("_Impacts", _impacts);
            _properties.SetFloatArray("_ImpactRadii", _impactRadii);
            _properties.SetInt("_ImpactCount", _impactCount);
            _properties.SetFloat("_ShieldTime", Time.time);
            _properties.SetVector("_ShieldAxes", axes);
            _properties.SetColor("_ShieldColor", color);
            _properties.SetFloat("_Brightness", brightness);
            _properties.SetFloat("_FadeDuration", fadeDuration);
            _properties.SetFloat("_DisplacementRatio", DISPLACEMENT_RATIO);
            _properties.SetFloat("_MaxDisplacement", maxDisplacement);
            shieldRenderer.SetPropertyBlock(_properties);
            // GPU displacement needs bounds beyond the undeformed shell.
            shieldRenderer.localBounds = new Bounds(_meshBounds.center, _meshBounds.size + new Vector3(
                2f * maxDisplacement / axes.x,
                2f * maxDisplacement / axes.y,
                2f * maxDisplacement / axes.z));
            shieldRenderer.enabled = true;
        }
    }
}
