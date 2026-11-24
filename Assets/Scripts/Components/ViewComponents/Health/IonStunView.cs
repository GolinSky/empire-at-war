using UnityEngine;
using Utilities.ScriptUtils.Time;

namespace EmpireAtWar.ViewComponents.Health
{
    /// <summary>Spherical arc field of the ion pulse wave. Stunned hulls use the ShipIonStun shader pass instead.</summary>
    public sealed class IonStunView : MonoBehaviour
    {
        private const int ARC_POINTS = 20;

        private const float ARC_REFRESH_INTERVAL = 0.065f;

        private readonly ITimer _arcRefreshTimer = TimerFactory.ConstructTimer(ARC_REFRESH_INTERVAL);

        [SerializeField] private MeshRenderer shimmer;
        [SerializeField] private LineRenderer[] arcs;
        private readonly Vector3[] _points = new Vector3[ARC_POINTS];
        private MaterialPropertyBlock _properties;

        [SerializeField, ColorUsage(true, true)] private Color arcColor = new Color(0.25f, 0.8f, 1f, 1f);
        private Vector3 _extents;

        [SerializeField, Min(0.01f)] private float fadeDuration = 0.7f;
        private float _intensity;

        private bool _active;

        public void Configure(Bounds bounds)
        {
            transform.localPosition = bounds.center;
            transform.localRotation = Quaternion.identity;
            _extents = bounds.extents * 1.44f;
            shimmer.transform.localScale = _extents;
            _properties = new MaterialPropertyBlock();
            foreach (LineRenderer arc in arcs)
            {
                arc.positionCount = ARC_POINTS;
                arc.widthMultiplier = Mathf.Clamp(bounds.size.magnitude * 0.012f, 0.32f, 1.8f);
            }
            SetVisible(false);
            enabled = false;
        }

        public void SetActive(bool active)
        {
            _active = active;
            enabled = true;
        }

        private void Update()
        {
            _intensity = Mathf.MoveTowards(_intensity, _active ? 1f : 0f,
                Time.deltaTime / (_active ? 0.12f : fadeDuration));
            if (_intensity == 0f)
            {
                SetVisible(false);
                enabled = false;
                return;
            }

            SetVisible(true);
            _properties.SetFloat("_Opacity", _intensity);
            shimmer.SetPropertyBlock(_properties);
            bool refresh = _arcRefreshTimer.IsComplete;
            if (refresh) _arcRefreshTimer.StartTimer();
            for (int i = 0; i < arcs.Length; i++)
            {
                Color color = arcColor;
                color.a *= _intensity * (0.65f + 0.35f * Mathf.Sin(Time.time * 29f + i * 2.1f));
                arcs[i].startColor = color;
                arcs[i].endColor = color;
                if (refresh) UpdateArc(arcs[i], i);
            }
        }

        private void UpdateArc(LineRenderer arc, int index)
        {
            float phase = Time.time * 0.65f + index * 1.7f;
            Quaternion rotation = Quaternion.Euler(index * 57f, index * 83f, index * 31f);
            for (int point = 0; point < ARC_POINTS; point++)
            {
                float t = point / (float)(ARC_POINTS - 1);
                float angle = phase + t * 1.9f;
                float jitter = (Mathf.PerlinNoise(point * 2.3f + index * 17f,
                    Mathf.Floor(Time.time / ARC_REFRESH_INTERVAL)) - 0.5f) * 0.22f;
                Vector3 surface = rotation * new Vector3(Mathf.Cos(angle), jitter, Mathf.Sin(angle)).normalized;
                _points[point] = Vector3.Scale(surface, _extents);
            }
            arc.SetPositions(_points);
        }

        private void SetVisible(bool visible)
        {
            shimmer.enabled = visible;
            foreach (LineRenderer arc in arcs) arc.enabled = visible;
        }
    }
}
