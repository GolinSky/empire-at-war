using EmpireAtWar.Components.Ship.Health;
using UnityEngine;

namespace EmpireAtWar.ViewComponents.Health
{
    public sealed class IonStunView : MonoBehaviour, IIonStunView
    {
        private const int ARC_POINTS = 20;
        private const float ARC_REFRESH_INTERVAL = 0.065f;
        [SerializeField] private MeshRenderer shimmer;
        [SerializeField] private LineRenderer[] arcs;
        [SerializeField, Min(0.01f)] private float fadeDuration = 0.7f;
        [SerializeField, ColorUsage(true, true)] private Color arcColor = new Color(0.25f, 0.8f, 1f, 1f);

        private readonly Vector3[] _points = new Vector3[ARC_POINTS];
        private MaterialPropertyBlock _properties;
        private Vector3 _extents;
        private float _intensity;
        private float _nextArcTime;
        private bool _active;

        public void Configure(Bounds bounds)
        {
            transform.localPosition = bounds.center;
            transform.localRotation = Quaternion.identity;
            _extents = bounds.extents * 1.08f;
            shimmer.transform.localScale = _extents;
            _properties = new MaterialPropertyBlock();
            foreach (LineRenderer arc in arcs)
            {
                arc.positionCount = ARC_POINTS;
                arc.widthMultiplier = Mathf.Clamp(bounds.size.magnitude * 0.003f, 0.08f, 0.45f);
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
            bool refresh = Time.time >= _nextArcTime;
            if (refresh) _nextArcTime = Time.time + ARC_REFRESH_INTERVAL;
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

        public void Release()
        {
            // The ship hierarchy may already have been destroyed during scene teardown.
            if (this != null) Destroy(gameObject);
        }
    }
}
