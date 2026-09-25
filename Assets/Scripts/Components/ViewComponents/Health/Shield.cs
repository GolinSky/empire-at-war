using UnityEngine;

using EmpireAtWar.Components.Ship.Health;

namespace EmpireAtWar.ViewComponents.Health
{
    public sealed class Shield : MonoBehaviour, IShieldView
    {
        private const int MAX_IMPACTS = 8;
        [SerializeField] private MeshRenderer shieldRenderer;
        [Tooltip("Radius of the visible patch along the surface, in world units.")]
        [SerializeField, Min(0.01f)] private float visibilityRadius = 4f;
        [SerializeField, ColorUsage(true, true)] private Color color = new Color(0.15f, 0.65f, 1f, 0.65f);
        [SerializeField, Min(0f)] private float brightness = 2f;
        [SerializeField, Min(0.01f)] private float fadeDuration = 1.2f;
        [Tooltip("Surface travel speed in world units per second.")]
        [SerializeField, Min(0f)] private float waveSpeed = 6f;
        [SerializeField, Min(0.01f)] private float waveWidth = 0.8f;
        [SerializeField, Min(0f)] private float displacementStrength = 0.15f;

        private readonly Vector4[] _impacts = new Vector4[MAX_IMPACTS];
        private MaterialPropertyBlock _properties;
        private int _impactCount;
        private bool _active;

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
            // The shared mesh is a unit sphere; its transform supplies the ellipsoid axes.
            Transform surface = shieldRenderer.transform;
            Vector3 localOrigin = surface.InverseTransformPoint(origin);
            Vector3 localTarget = surface.InverseTransformPoint(target);
            Vector3 direction = localTarget - localOrigin;
            float lengthSquared = direction.sqrMagnitude;
            if (lengthSquared == 0f) return target;

            float projection = Vector3.Dot(localOrigin, direction);
            float discriminant = projection * projection - lengthSquared * (localOrigin.sqrMagnitude - 1f);
            if (discriminant < 0f) return target;

            float root = Mathf.Sqrt(discriminant);
            float distance = (-projection - root) / lengthSquared;
            if (distance < 0f) distance = (-projection + root) / lengthSquared;
            // An origin inside the shield must not shoot through the hull to its far surface.
            if (distance < 0f || distance > 1f) return target;
            return surface.TransformPoint(localOrigin + direction * distance);
        }

        public void ShowImpact(Vector3 position)
        {
            if (!_active) return;
            Vector3 local = shieldRenderer.transform.InverseTransformPoint(position).normalized;
            if (_impactCount == MAX_IMPACTS)
            {
                System.Array.Copy(_impacts, 1, _impacts, 0, MAX_IMPACTS - 1);
                _impactCount--;
            }

            _impacts[_impactCount++] = new Vector4(local.x, local.y, local.z, Time.time);
            UpdateMaterial();
        }

        private void LateUpdate()
        {
            if (!_active || _impactCount == 0) return;
            int liveCount = 0;
            for (int i = 0; i < _impactCount; i++)
            {
                if (Time.time - _impacts[i].w < fadeDuration)
                    _impacts[liveCount++] = _impacts[i];
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
            _properties.SetVectorArray("_Impacts", _impacts);
            _properties.SetInt("_ImpactCount", _impactCount);
            _properties.SetFloat("_ShieldTime", Time.time);
            _properties.SetVector("_ShieldAxes", axes);
            _properties.SetFloat("_VisibilityRadius", visibilityRadius);
            _properties.SetColor("_ShieldColor", color);
            _properties.SetFloat("_Brightness", brightness);
            _properties.SetFloat("_FadeDuration", fadeDuration);
            _properties.SetFloat("_WaveSpeed", waveSpeed);
            _properties.SetFloat("_WaveWidth", waveWidth);
            _properties.SetFloat("_DisplacementStrength", displacementStrength);
            shieldRenderer.SetPropertyBlock(_properties);
            // GPU displacement needs bounds beyond the undeformed unit sphere.
            shieldRenderer.localBounds = new Bounds(Vector3.zero, new Vector3(
                2f + 2f * displacementStrength / axes.x,
                2f + 2f * displacementStrength / axes.y,
                2f + 2f * displacementStrength / axes.z));
            shieldRenderer.enabled = true;
        }
    }
}
