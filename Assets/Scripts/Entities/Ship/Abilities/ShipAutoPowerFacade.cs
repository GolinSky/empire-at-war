using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Lifecycle;
using UnityEngine;

namespace EmpireAtWar.Entities.Ship.Abilities
{
    public sealed class ShipAutoPowerFacade : IShipAutoPowerFacade, IInitializable, ILateDisposable
    {
        private readonly HealthModel _health;
        private readonly IHealthTooltipObserver _regeneration;
        private IAutoPowerWeaponObserver _weapons;
        private bool _lastSuppressed;
        private float _lastMaximum;

        public bool Eligible { get; }
        public bool Enabled { get; private set; } = true;
        public float MaxShields => _health.MaxShields;
        public bool ShieldSuppressed => _health.IsShieldSuppressed;
        public int ShieldRevision { get; private set; }
        public bool CanRegenerateShields => _health.MaxShields > 0f && _regeneration.ShieldRegeneration > 0f;

        public ShipAutoPowerFacade(HealthModel health, IHealthTooltipObserver regeneration, IShipAbilityFacade abilities)
        {
            _health = health;
            _regeneration = regeneration;
            foreach (ShipAbilitySlot slot in abilities.Slots)
                if (AutoPowerEligibility.IsEligible(slot)) Eligible = true;
        }

        public void BindWeapons(IAutoPowerWeaponObserver weapons) => _weapons = weapons;
        public void SetEnabled(bool enabled) => Enabled = enabled;
        public bool CanEngage(ShipClass targetClass, Vector3 position) => _weapons.CanEngageMainWeapon(targetClass, position);

        public void Initialize()
        {
            _lastMaximum = MaxShields; _lastSuppressed = ShieldSuppressed;
            _health.OnValueChanged += ObserveShields;
            _health.OnRestored += HandleRestored;
        }

        private void ObserveShields()
        {
            if (_lastMaximum == MaxShields && _lastSuppressed == ShieldSuppressed) return;
            _lastMaximum = MaxShields; _lastSuppressed = ShieldSuppressed;
            ShieldRevision++;
        }

        private void HandleRestored() => ShieldRevision++;

        public void LateDispose()
        {
            _health.OnValueChanged -= ObserveShields;
            _health.OnRestored -= HandleRestored;
        }
    }
}
