using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Services.Squadrons;
using EmpireAtWar.ViewComponents.Health;
using UnityEngine;
using VContainer;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.ViewComponents.Squadrons
{
    /// <summary>
    /// One strikecraft of a squadron. The root follows the flight path, <see cref="body"/> carries the roll.
    /// It is also the hardpoint enemy weapons aim at.
    /// </summary>
    public sealed class FighterView : MonoBehaviour, IHardPoint
    {
        [SerializeField] private Transform body;
        [SerializeField] private Collider hitCollider;
        [SerializeField] private WeaponHardPoint gun;
        [SerializeField] private WeaponHardPoint[] additionalGuns = System.Array.Empty<WeaponHardPoint>();
        [SerializeField] private TrailRenderer[] engineTrails;

        private IFighterExplosionPool _explosions;
        private Quaternion _bodyRestRotation;

        private float _healthPercentage = 1f;
        // The flight model's pose, so targeting reads no transform.
        private Vector3 _position;

        [SerializeField] private int id;

        public int Id => id;
        public HardPointType HardPointType => HardPointType.Any;
        public Vector3 Position => _position;
        public Transform Transform => transform;
        public bool IsDestroyed => _healthPercentage <= 0f;
        public WeaponHardPoint Gun => gun;
        public Transform Body => body;
        public Quaternion BodyRestRotation => _bodyRestRotation;

        public bool TryGetWeaponType(out WeaponType weaponType) => gun.TryGetWeaponType(out weaponType);

        // Injection runs before the inactive entity initializes and poses its fighters, so Awake would be too late.
        [Inject]
        private void Construct(IFighterExplosionPool explosions)
        {
            _explosions = explosions;
            _bodyRestRotation = body.localRotation;
        }

        /// <summary>Records the pose's position for targeting; <see cref="FighterPoseBatch"/> moves the transform.</summary>
        public void RecordPosition(Vector3 position) => _position = position;

        /// <summary>Moves the fighter at once, e.g. when it spawns and its trails are cleared right after.</summary>
        public void ApplyPose(Vector3 position, Vector3 forward, float bank)
        {
            _position = position;
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
                Disable();
                _explosions.Play(transform.position);
            }
            else if (!wasAlive && !IsDestroyed)
            {
                Restore();
            }
        }

        public void RestoreData(float healthPercentage)
        {
            _healthPercentage = healthPercentage;
            if (IsDestroyed) Disable();
            else Restore();
        }

        private void Disable()
        {
            gun.UpdateData(0f);
            foreach (WeaponHardPoint additionalGun in additionalGuns)
            {
                additionalGun.UpdateData(0f);
            }
            body.gameObject.SetActive(false);
            hitCollider.enabled = false;
            foreach (TrailRenderer trail in engineTrails)
            {
                trail.emitting = false;
            }
        }

        // A reused squadron brings its destroyed fighters back.
        private void Restore()
        {
            gun.UpdateData(_healthPercentage);
            foreach (WeaponHardPoint additionalGun in additionalGuns)
            {
                additionalGun.UpdateData(_healthPercentage);
            }
            body.gameObject.SetActive(true);
            hitCollider.enabled = true;
            foreach (TrailRenderer trail in engineTrails)
            {
                trail.emitting = true;
            }
        }

#if UNITY_EDITOR
        public void SetEditorReferences(int fighterId, Transform bodyTransform, Collider collider,
            WeaponHardPoint weaponHardPoint, TrailRenderer[] trails)
        {
            id = fighterId;
            body = bodyTransform;
            hitCollider = collider;
            gun = weaponHardPoint;
            engineTrails = trails;
        }
#endif
    }
}
