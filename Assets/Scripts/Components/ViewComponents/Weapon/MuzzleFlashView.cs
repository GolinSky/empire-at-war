using UnityEngine;

namespace EmpireAtWar.ViewComponents.Weapon
{
    public sealed class MuzzleFlashView : MonoBehaviour
    {
        private const float WIDTH_MULTIPLIER = 3f;
        private const float LENGTH_MULTIPLIER = 6f;
        private const float PEAK_DURATION_FRACTION = 0.4f;

        [SerializeField] private LineRenderer flash;
        private Transform _muzzle;

        private Vector3 _origin;
        private Vector3 _direction;
        private Color _color;

        [SerializeField, Range(0.05f, 0.15f)] private float duration = 0.1f;
        private float _width;
        private float _length;
        private float _startedAt;

        public bool IsPlaying => flash.enabled;

        public void Play(Transform muzzle, Vector3 aimPoint, Color color, float size)
        {
            _muzzle = muzzle;
            _origin = muzzle.position;
            _direction = (aimPoint - _origin).normalized;
            _color = Color.Lerp(color, Color.white, 0.35f);
            _width = size * WIDTH_MULTIPLIER;
            _length = size * LENGTH_MULTIPLIER;
            _startedAt = Time.time;
            flash.enabled = true;
            Tick();
        }

        public void Tick()
        {
            if (!flash.enabled) return;

            float progress = (Time.time - _startedAt) / duration;
            if (progress >= 1f)
            {
                flash.enabled = false;
                _muzzle = null;
                return;
            }

            // An emitted flash can finish after its source ship is destroyed.
            if (_muzzle != null) _origin = _muzzle.position;
            Color color = _color;
            // Preserve a readable peak for several frames before the short fade.
            float fade = Mathf.InverseLerp(PEAK_DURATION_FRACTION, 1f, progress);
            color.a *= 1f - fade;
            flash.startColor = color;
            flash.endColor = color;
            flash.widthMultiplier = _width * (1f - fade * 0.25f);
            flash.SetPosition(0, _origin);
            flash.SetPosition(1, _origin + _direction * _length);
        }

        private void OnDisable()
        {
            flash.enabled = false;
            _muzzle = null;
        }
    }
}
