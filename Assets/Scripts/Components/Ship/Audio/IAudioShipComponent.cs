using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Mvc;
using UnityEngine;
using EmpireAtWar.Models.Health;

namespace EmpireAtWar.Components.Ship.Audio
{
    public interface IAudioShipComponent : IComponent
    {
        void InitializeAudio(IReadOnlyList<ShipAbilitySlot> abilities);

        void UpdateAudio();

        void ResetShields(IHealthModelObserver health);

        void HandleHealthChanged(IHealthModelObserver health);

        void PlayShieldImpact(Vector3 position);

        void PlayHardPointLost();

        void HandleAbilityChanged();

        void PlayWeaponShot(WeaponProfile profile, Transform muzzle);

        void PlayHyperSpace();

        void HandleEnemyDetected();
    }
}
