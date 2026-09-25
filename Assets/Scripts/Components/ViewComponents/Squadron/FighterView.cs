using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.ViewComponents.Health;
using UnityEngine;

namespace EmpireAtWar.ViewComponents.Squadrons
{
    /// <summary>
    /// One strikecraft of a squadron. The root follows the flight path, <see cref="body"/> carries the roll.
    /// It is also the hardpoint enemy weapons aim at.
    /// </summary>
    public sealed class FighterView : MonoBehaviour, IHardPoint
    {
        [SerializeField] private int id;
        [SerializeField] private Transform body;
        [SerializeField] private Collider hitCollider;
        [SerializeField] private WeaponHardPoint gun;
        [SerializeField] private ParticleSystem deathExplosion;
        [SerializeField] private TrailRenderer[] engineTrails;

        private Quaternion _bodyRestRotation;
        private float _healthPercentage = 1f;

        public int Id => id;
        public HardPointType HardPointType => HardPointType.Any;
        public Vector3 Position => transform.position;
        public Transform Transform => transform;
        public bool IsDestroyed => _healthPercentage <= 0f;
        public WeaponHardPoint Gun => gun;

        private void Awake()
        {
            _bodyRestRotation = body.localRotation;
        }

        public void ApplyPose(Vector3 position, Vector3 forward, float bank)
        {
            transform.SetPositionAndRotation(position, Quaternion.LookRotation(forward, Vector3.up));
            body.localRotation = Quaternion.AngleAxis(bank, Vector3.forward) * _bodyRestRotation;
        }

        public void ClearTrails()
        {
            foreach (TrailRenderer trail in engineTrails)
            {
                trail.Clear();
            }
        }

        public void UpdateData(float healthPercentage)
        {
            bool wasAlive = !IsDestroyed;
            _healthPercentage = healthPercentage;
            if (wasAlive && IsDestroyed)
            {
                Explode();
            }
        }

        private void Explode()
        {
            gun.UpdateData(0f);
            body.gameObject.SetActive(false);
            hitCollider.enabled = false;
            foreach (TrailRenderer trail in engineTrails)
            {
                trail.emitting = false;
            }

            deathExplosion.Play(true);
        }

#if UNITY_EDITOR
        public void SetEditorReferences(int fighterId, Transform bodyTransform, Collider collider,
            WeaponHardPoint weaponHardPoint, ParticleSystem explosion, TrailRenderer[] trails)
        {
            id = fighterId;
            body = bodyTransform;
            hitCollider = collider;
            gun = weaponHardPoint;
            deathExplosion = explosion;
            engineTrails = trails;
        }
#endif
    }
}
