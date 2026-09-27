using UnityEngine;

namespace EmpireAtWar.ViewComponents.Weapon
{
    public sealed class MuzzleFlashView : MonoBehaviour
    {
        [SerializeField] private LineRenderer flash;
        [SerializeField, Range(0.05f, 0.15f)] private float duration = 0.1f;

        private Transform _muzzle;
        private Vector3 _origin;
        private Vector3 _direction;
        private Color _color;
        private float _width;
        private float _length;
        private float _startedAt;

        public bool IsPlaying => flash.enabled;

        public void Play(Transform muzzle, Vector3 aimPoint, Color color, float size)
        {
            _muzzle = muzzle;
            _origin = muzzle.position;
            _direction = (aimPoint - _origin).normalized;
            _color = color;
            _width = size * 0.7f;
            _length = size * 1.5f;
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
            color.a *= (1f - progress) * (1f - progress);
            flash.startColor = color;
            flash.endColor = color;
            flash.widthMultiplier = _width * (1f - progress * 0.5f);
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
