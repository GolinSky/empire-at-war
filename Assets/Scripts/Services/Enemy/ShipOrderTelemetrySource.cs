using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Extensions;
using UnityEngine;
using VContainer;

namespace EmpireAtWar.Services.Enemy
{
    public sealed class ShipOrderTelemetrySource
    {
        // Hits further apart than this end an under-fire spell.
        private const float DAMAGE_GAP_SECONDS = 3f;

        private readonly Transform _viewTransform;
        private readonly IRadarModelObserver _radar;
        private float _underFireSince = float.NegativeInfinity;
        private float _lastDamageTime = float.NegativeInfinity;
        private float _lastShotTime = float.NegativeInfinity;

        public long EntityId { get; }
        public PlayerId Owner { get; }
        public ShipType ShipType { get; }
        public Vector3 Forward => _viewTransform.forward;
        public float RadarRange => _radar.Range;

        /// <summary>Seconds this ship has been taking hits without firing a shot; 0 when not under fire.</summary>
        public float IdleUnderFireSeconds
        {
            get
            {
                float now = Time.time;
                if (now - _lastDamageTime > DAMAGE_GAP_SECONDS) return 0f;
                return now - Mathf.Max(_underFireSince, _lastShotTime);
            }
        }

        /// <summary>The current idle-under-fire spell was already written once.</summary>
        public bool IsIdleUnderFireReported { get; set; }

        public ShipOrderTelemetrySource(long entityId, PlayerId owner, ShipType shipType,
            [Key(EntityBindType.ViewTransform)] Transform viewTransform, IRadarModelObserver radar,
            IHealthDamageFacade damage, IWeaponFireEvents fireEvents)
        {
            EntityId = entityId;
            Owner = owner;
            ShipType = shipType;
            _viewTransform = viewTransform;
            _radar = radar;
            // Same entity scope as both sources, so the handlers never outlive them.
            damage.Damaged += RecordDamage;
            fireEvents.ShotEmitted += RecordShot;
        }

        private void RecordDamage()
        {
            float now = Time.time;
            if (now - _lastDamageTime > DAMAGE_GAP_SECONDS)
            {
                _underFireSince = now;
                IsIdleUnderFireReported = false;
            }
            _lastDamageTime = now;
        }

        private void RecordShot(WeaponProfile profile, Transform muzzle, float interval)
        {
            _lastShotTime = Time.time;
            IsIdleUnderFireReported = false;
        }
    }
}
