using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Entities.Game
{
    public static class SurvivalPreset
    {
        public const int STATION_LEVEL = 5;

        public static BattleDefinitionBuilder Create(SurvivalSettings settings)
        {
            // Two or more stations plus the mining ring do not fit on a small Survival map.
            if (settings.CoopHumanFactions.Count > 0 && settings.MapSize == MapSize.Small)
                throw new System.ArgumentException("Survival co-op needs a Medium or Large map.", nameof(settings));
            BattleDefinitionBuilder builder = new BattleDefinitionBuilder
            {
                Mode = GameMode.Survival,
                Map = new MapSpec(MapType.Survival, PlanetType.Coruscant, settings.MapSize),
                Outcome = OutcomeRule.Survival,
                Survival = settings,
                InitialStationLevel = STATION_LEVEL,
                Features = settings.SuperWeapons ? BattleFeatures.All : BattleFeatures.All & ~BattleFeatures.SuperWeapons
            };
            builder.Players.Add(new BattlePlayer(new PlayerSlot(new PlayerId(0), new TeamId(0),
                settings.PlayerFaction, PlayerController.Human, settings.Difficulty, 0), PlayerRole.Human, builder.Features));
            // Co-op humans each own a station on the defenders' team.
            foreach (FactionType faction in settings.CoopHumanFactions)
            {
                int index = builder.Players.Count;
                builder.Players.Add(new BattlePlayer(new PlayerSlot(new PlayerId(index), new TeamId(0),
                    faction, PlayerController.Human, settings.Difficulty, index), PlayerRole.Human, builder.Features));
            }
            if (settings.AllyFaction.HasValue)
            {
                int index = builder.Players.Count;
                builder.Players.Add(new BattlePlayer(new PlayerSlot(new PlayerId(index), new TeamId(0),
                    settings.AllyFaction.Value, PlayerController.Ai, settings.Difficulty, index), PlayerRole.AiAlly, builder.Features,
                    new PlayerId(0)));
            }
            int enemyIndex = builder.Players.Count;
            builder.Players.Add(new BattlePlayer(new PlayerSlot(new PlayerId(enemyIndex), new TeamId(1),
                settings.EnemyFaction, PlayerController.Ai, settings.Difficulty, enemyIndex), PlayerRole.WaveAttacker, BattleFeatures.None));
            return builder;
        }
    }
}
