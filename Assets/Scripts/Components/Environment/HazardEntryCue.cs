using EmpireAtWar.Entities.Map.Hazards;
using UnityEngine;

namespace EmpireAtWar.Components.Environment
{
    public sealed class HazardEntryCue : MonoBehaviour
    {
        private const float DURATION = 0.6f;
        [SerializeField] private LineRenderer line;
        private float _elapsed;
        private bool _isIon;
        private float _radius;
        private Color _color;

        public void Configure(SpaceHazardData data, float radius)
        {
            _isIon = data.HazardType == HazardType.IonStorm;
            _radius = radius;
            _color = data.MinimapColor;
            line.loop = !_isIon;
            line.positionCount = _isIon ? 12 : 48;
            Draw(0f);
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            if (_elapsed >= DURATION)
            {
                Destroy(gameObject);
                return;
            }
            Draw(_elapsed / DURATION);
        }

        private void Draw(float progress)
        {
            Color tint = _color;
            tint.a *= 1f - progress;
            line.startColor = tint;
            line.endColor = tint;
            for (int i = 0; i < line.positionCount; i++)
            {
                float t = (float)i / line.positionCount;
                Vector3 point = _isIon
                    ? new Vector3((t * 2f - 1f) * _radius, 0f, Mathf.Sin(i * 13.7f) * _radius * 0.25f)
                    : new Vector3(Mathf.Cos(t * Mathf.PI * 2f), 0f, Mathf.Sin(t * Mathf.PI * 2f)) * (_radius * (0.7f + progress));
                line.SetPosition(i, point);
            }
        }
    }
}
