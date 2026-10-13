using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Components.Ship.Audio
{
    public interface IAudioShipComponent : IComponent
    {
        void InitializeAudio(IReadOnlyList<ShipAbilitySlot> abilities);

        void UpdateAudio();

        void HandleAbilityChanged();

        void PlayWeaponShot(WeaponProfile profile, Transform muzzle);

        void PlayHyperSpace();

        void HandleEnemyDetected();
    }
}
