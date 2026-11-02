using System.Collections.Generic;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Services.Analytics
{
    public sealed class MatchStartedData
    {
        public string MatchId { get; }
        public GameMode Mode { get; }
        public MapType MapType { get; }
        public PlanetType Planet { get; }
        public MapSize MapSize { get; }
        public BattleVictoryCondition VictoryCondition { get; }
        public FactionType PlayerFaction { get; }
        public IReadOnlyList<FactionType> OpponentFactions { get; }
        public int AiCount { get; }
        public float StartingMoney { get; }
        public int InitialStationLevel { get; }
        public bool IsLoadedSave { get; }

        public MatchStartedData(string matchId, GameMode mode, MapType mapType, PlanetType planet, MapSize mapSize,
            BattleVictoryCondition victoryCondition, FactionType playerFaction,
            IReadOnlyList<FactionType> opponentFactions, int aiCount, float startingMoney, int initialStationLevel,
            bool isLoadedSave)
        {
            MatchId = matchId;
            Mode = mode;
            MapType = mapType;
            Planet = planet;
            MapSize = mapSize;
            VictoryCondition = victoryCondition;
            PlayerFaction = playerFaction;
            OpponentFactions = opponentFactions;
            AiCount = aiCount;
            StartingMoney = startingMoney;
            InitialStationLevel = initialStationLevel;
            IsLoadedSave = isLoadedSave;
        }
    }
}
