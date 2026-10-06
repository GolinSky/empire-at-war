using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Views.Cheats
{
    public interface ICheatView
    {
        event Action<string> AddMoneyRequested;

        event Action<FactionType> FactionSelected;

        event Action<ShipType> AddReinforcementRequested;

        event Action<ShipType> SpawnForceRequested;

        event Action<SuperWeaponType> GrantSuperWeaponRequested;

        event Action GrantAllSuperWeaponsRequested;

        event Action<bool> RangeDebugToggled;

        event Action DestroyOwnShipsRequested;

        void SetFactions(IReadOnlyList<FactionType> factions);

        void SetShips(IReadOnlyList<ShipType> ships);

        void SetStatus(string status);
    }
}
