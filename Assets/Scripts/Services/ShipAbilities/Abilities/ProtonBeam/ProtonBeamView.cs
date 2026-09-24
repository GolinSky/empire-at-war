using UnityEngine;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    public sealed class ProtonBeamView : MonoBehaviour
    {
        [SerializeField] private LineRenderer beam;
        [SerializeField] private GameObject hitEffectPrefab;
        [SerializeField] private float growthDuration = 0.3f;

        private Transform _target;
        private Vector3 _origin;
        private Vector3 _lastTarget;
        private float _startTime;
        private float _duration;
        private GameObject _hitEffect;

        public void Play(Vector3 from, Transform to, float duration)
        {
            _origin = from;
            _target = to;
            _lastTarget = to.position;
            _duration = duration;
            _startTime = Time.time;
            beam.positionCount = 2;
            beam.enabled = true;
            beam.SetPosition(0, _origin);
            beam.SetPosition(1, _origin);
        }

        private void Update()
        {
            if (!beam.enabled) return;
            if (_target != null) _lastTarget = _target.position;
            float elapsed = Time.time - _startTime;
            float growth = Mathf.Clamp01(elapsed / growthDuration);
            beam.SetPosition(0, _origin);
            beam.SetPosition(1, Vector3.Lerp(_origin, _lastTarget, growth));
            if (_hitEffect == null && growth >= 1f)
                _hitEffect = Instantiate(hitEffectPrefab, _lastTarget, Quaternion.identity, transform);
            if (_hitEffect != null) _hitEffect.transform.position = _lastTarget;
            if (elapsed >= _duration) beam.enabled = false;
        }
    }
}
