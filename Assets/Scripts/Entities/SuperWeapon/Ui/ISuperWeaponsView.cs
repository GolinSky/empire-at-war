using System;

namespace EmpireAtWar.Entities.SuperWeapons.Ui
{
    public interface ISuperWeaponsView
    {
        event Action<SuperWeaponType> Pressed;
        void Initialize();
        void Dispose();
        void SetState(SuperWeaponType type, SuperWeaponState state);
        void SetPending(SuperWeaponType? type);
    }
}
