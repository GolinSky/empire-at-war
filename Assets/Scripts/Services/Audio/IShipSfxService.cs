using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Entities.BaseEntity;
using UnityEngine;

namespace EmpireAtWar.Services.Audio
{
    public interface IShipSfxService
    {
        bool TryPlayOneShot(IEntity ship, SfxProfile sfx, Vector3 position);

        bool TryPlayWeaponShot(IEntity ship, WeaponProfile weapon, Transform muzzle);

        bool TryHoldLoop(IEntity ship, SfxProfile loop, Vector3 position, float volumeScale, float pitch);

        bool TryPlayVoice(AudioClip clip);

        void ReleaseShip(IEntity ship);
    }
}
