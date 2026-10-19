using System;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Planet;
using UnityEngine;

namespace EmpireAtWar.Entities.Game
{
    /// <summary>The <see cref="SkirmishPreset.Create"/> inputs of a saved battle.</summary>
    [Serializable]
    public sealed class SkirmishSetupSave
    {
        [SerializeField] private List<PlayerSlotSave> players = new List<PlayerSlotSave>();
        [SerializeField] private PlanetType planet;
        [SerializeField] private MapSize mapSize;
        [SerializeField] private BattleVictoryCondition victoryCondition;
        [SerializeField] private float startingMoney;
        [SerializeField] private bool superWeapons;
        [SerializeField] private int mapSeed;

        public static SkirmishSetupSave From(BattleDefinition battle) => new SkirmishSetupSave
        {
            players = battle.Slots.Select(PlayerSlotSave.From).ToList(),
            planet = battle.Map.Planet,
            mapSize = battle.Map.Size,
            victoryCondition = battle.VictoryCondition,
            startingMoney = battle.StartingMoney,
            superWeapons = battle.HasFeature(BattleFeatures.SuperWeapons),
            mapSeed = battle.Map.Seed
        };

        public BattleDefinition Rebuild() => SkirmishPreset.Create(players.Select(player => player.ToSlot()).ToArray(),
            planet, mapSize, victoryCondition, startingMoney, superWeapons, mapSeed).Build();
    }
}
