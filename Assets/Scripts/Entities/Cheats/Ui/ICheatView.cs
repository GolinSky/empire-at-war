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

        /// <summary>Handled by the tutorial hints only when they run in this match.</summary>
        event Action RestartHintsRequested;

        void SetEnemies(IReadOnlyList<PlayerSlot> enemies);
        void SetPlatforms(IReadOnlyList<DefendPlatformType> platforms);

        event Action<string> AddMoneyRequested;

        event Action<FactionType> FactionSelected;

        event Action<ShipType> AddReinforcementRequested;

        event Action<ShipType> SpawnForceRequested;

        event Action<SuperWeaponType> GrantSuperWeaponRequested;

        event Action GrantAllSuperWeaponsRequested;

        event Action<bool> RangeDebugToggled;

        event Action<bool> NetworkStatusToggled;

        /// <summary>Online battles only: shows the network status overlay toggle in the Debug tab.</summary>
        void SetNetworkStatusAvailable(bool available);

        event Action DestroyOwnShipsRequested;

        /// <summary>Fraction of max hull to take from each selected unit.</summary>
        event Action<float> DamageSelectedRequested;

        void SetFactions(IReadOnlyList<FactionType> factions);

        void SetShips(IReadOnlyList<ShipType> ships);

        void SetStatus(string status);
    }
}
