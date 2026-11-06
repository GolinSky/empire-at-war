using EmpireAtWar.Services.Audio;
using UnityEngine;

namespace EmpireAtWar.ViewComponents.Weapon
{
    public sealed class HarmonicBombView : MonoBehaviour
    {
        private const float MARKER_RADIUS = 2f;
        private const float BLAST_DURATION = 0.35f;

        [SerializeField] private LineRenderer ring;
        [SerializeField] private SfxProfile detonation;

        private float _elapsed;
        private float _blastRadius;

        public SfxProfile Detonation => detonation;

        public void ShowMarker(Vector3 position)
        {
            _elapsed = -1f;
            transform.position = position;
            transform.localScale = Vector3.one * MARKER_RADIUS;
            ring.startColor = ring.endColor = Color.white;
            gameObject.SetActive(true);
        }

        public void BeginBlast(float radius)
        {
            _elapsed = 0f;
            _blastRadius = radius;
        }

        public void SetVisible(bool visible) => ring.forceRenderingOff = !visible;

        public bool Advance(float deltaTime)
        {
            if (_elapsed < 0f) return false;
            _elapsed += deltaTime;
            float progress = Mathf.Clamp01(_elapsed / BLAST_DURATION);
            transform.localScale = Vector3.one * Mathf.Lerp(MARKER_RADIUS, _blastRadius, progress);
            ring.startColor = ring.endColor = new Color(1f, 1f, 1f, 1f - progress);
            return progress >= 1f;
        }
    }
}
