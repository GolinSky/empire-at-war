using EmpireAtWar.Components.Ui.Tooltip;
using System;

namespace EmpireAtWar.Entities.SuperWeapons.Ui
{
    public interface ISuperWeaponsView
    {
        TooltipHoverView TooltipHover { get; }
        event Action<SuperWeaponType> Pressed;

        event Action ToggleRequested;

        event Action CloseRequested;

        void Initialize();

        void Dispose();

        void SetState(SuperWeaponType type, SuperWeaponState state, bool canFire);

        void SetPending(SuperWeaponType? type);

        void SetOpen(bool open);

        void SetAvailable(bool available);

        void SetBattleAvailable(bool available);

        void SetRemaining(SuperWeaponType type, float seconds);
    }
}
