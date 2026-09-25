using System;
using UnityEngine;

namespace EmpireAtWar.Services.SuperWeapons
{
    /// <summary>
    /// Bridges the registered scene origin to the superweapons, so registration order between
    /// contexts does not matter and the weapons never depend on the concrete origin.
    /// </summary>
    public sealed class SuperWeaponOriginRegistry : ISuperWeaponOriginRegistry, ISuperWeaponOrigin
    {
        private ISuperWeaponOrigin _origin;

        public void Register(ISuperWeaponOrigin origin)
        {
            if (_origin != null)
            {
                throw new InvalidOperationException("A superweapon origin is already registered.");
            }

            _origin = origin;
        }

        public void Unregister(ISuperWeaponOrigin origin)
        {
            if (_origin != origin)
            {
                throw new InvalidOperationException("The superweapon origin was not registered.");
            }

            _origin = null;
        }

        public Vector3 GetFirePosition(Vector3 targetPosition)
        {
            if (_origin == null)
            {
                throw new InvalidOperationException(
                    "No superweapon origin is registered. Add a PlanetSuperWeaponOrigin to the battle planet.");
            }

            return _origin.GetFirePosition(targetPosition);
        }
    }
}
