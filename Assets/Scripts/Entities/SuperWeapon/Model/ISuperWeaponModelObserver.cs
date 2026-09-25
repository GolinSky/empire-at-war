using System;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Entities.SuperWeapons
{
    public interface ISuperWeaponModelObserver : IModelObserver
    {
        event Action<SuperWeaponType, SuperWeaponState> OnStateChanged;

        SuperWeaponState GetState(SuperWeaponType type);
        bool CanPurchase(SuperWeaponType type);
    }
}
