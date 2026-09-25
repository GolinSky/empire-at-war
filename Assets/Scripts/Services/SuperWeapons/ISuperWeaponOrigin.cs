using UnityEngine;

namespace EmpireAtWar.Services.SuperWeapons
{
    /// <summary>Where superweapon shots come from, e.g. the surface of the battle's planet.</summary>
    public interface ISuperWeaponOrigin
    {
        Vector3 GetFirePosition(Vector3 targetPosition);
    }
}
