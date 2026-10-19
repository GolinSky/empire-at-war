using EmpireAtWar.Services.Saves;
using System.Collections.Generic;
using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Entities.Game
{
    public interface IGameModelObserver : IModelObserver
    {
        GameMode GameMode { get; }
        PlanetType PlanetType { get; }
        MapSize MapSize { get; }
        IReadOnlyList<PlayerSlot> Players { get; }
        BattleVictoryCondition VictoryCondition { get; }
        float StartingMoney { get; }
    }

    [CreateAssetMenu(fileName = nameof(GameData), menuName = "Data/GameData")]
    public class GameData : Data, IModel, IGameModelObserver
    {
        public BattleDefinition Battle { get; set; } = SkirmishPreset.Default();
        public BattleSave PendingBattleSave { get; set; }
        public GameMode GameMode => Battle.Mode;
        public PlanetType PlanetType => Battle.Map.Planet;
        public MapSize MapSize => Battle.Map.Size;
        public IReadOnlyList<PlayerSlot> Players => Battle.Slots;
        public BattleVictoryCondition VictoryCondition => Battle.VictoryCondition;
        public float StartingMoney => Battle.StartingMoney;
    }
}
