using EmpireAtWar.Components.Ui.Tooltip;
using System;
using UnityEngine;

namespace EmpireAtWar.Entities.SuperWeapons.Ui
{
    public interface ISuperWeaponsView
    {
        TooltipHoverView TooltipHover { get; }
        event Action<SuperWeaponType> Pressed;
        event Action<SuperWeaponType> PurchaseRequested;

        event Action ToggleRequested;

        event Action CloseRequested;

        void Initialize();

        void Dispose();

        void SetReady(SuperWeaponType type, bool ready);

        void SetRow(SuperWeaponType type, SuperWeaponState state, bool canAct, string detail, float progress);

        void BlinkRejected(SuperWeaponType type);

        void SetPending(SuperWeaponType? type);

        void SetOpen(bool open);

        bool ContainsScreenPoint(Vector2 position);

        void SetAvailable(bool available);

        void SetBattleAvailable(bool available);
    }
}
