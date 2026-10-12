using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Entities.DefendPlatform;

namespace EmpireAtWar.Views.Cheats
{
    public interface ICheatView
    {
        event Action LevelUpStationRequested;
        event Action<FactionType> SpawnAllShipsRequested;
        event Action<DefendPlatformType> AddPlatformReinforcementRequested;
        event Action<DefendPlatformType> SpawnPlatformRequested;
        event Action<PlayerId, string> AddEnemyMoneyRequested;
        event Action<string> AddAllEnemiesMoneyRequested;
        event Action<PlayerId> LevelUpEnemyStationRequested;
        event Action LevelUpAllEnemyStationsRequested;

        void SetEnemies(IReadOnlyList<PlayerSlot> enemies);
        void SetPlatforms(IReadOnlyList<DefendPlatformType> platforms);

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
