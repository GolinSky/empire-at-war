using EmpireAtWar.Components.AttackComponent;
using UnityEngine;

namespace EmpireAtWar.Services.Audio
{
    public interface IShipSfxService
    {
        bool TryPlayOneShot(ShipSfxHandle ship, SfxProfile sfx, Vector3 position);

        bool TryPlayWeaponShot(ShipSfxHandle ship, WeaponProfile weapon, Transform muzzle);

        bool TryHoldLoop(ShipSfxHandle ship, SfxProfile loop, Vector3 position, float volumeScale, float pitch);

        bool TryPlayVoice(AudioClip clip);

        void ReleaseShip(ShipSfxHandle ship);
    }
}
