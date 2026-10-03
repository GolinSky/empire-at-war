using System;

namespace EmpireAtWar.Entities.SuperWeapons.Ui
{
    public interface ISuperWeaponsView
    {
        event Action<SuperWeaponType> Pressed;

        event Action ToggleRequested;

        event Action CloseRequested;

        void Initialize();

        void Dispose();

        void SetState(SuperWeaponType type, SuperWeaponState state);

        void SetPending(SuperWeaponType? type);

        void SetOpen(bool open);

        void SetBattleAvailable(bool available);

        void SetRemaining(SuperWeaponType type, float seconds);
    }
}
