using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Entities.Game
{
    public static class SurvivalPreset
    {
        public static BattleDefinitionBuilder Create(SurvivalSettings settings)
        {
            BattleDefinitionBuilder builder = new BattleDefinitionBuilder
            {
                Mode = GameMode.Survival,
                Map = new MapSpec(MapType.Survival, PlanetType.Coruscant, settings.MapSize),
                Outcome = OutcomeRule.Survival,
                Survival = settings,
                Features = settings.SuperWeapons ? BattleFeatures.All : BattleFeatures.All & ~BattleFeatures.SuperWeapons
            };
            builder.Players.Add(new BattlePlayer(new PlayerSlot(new PlayerId(0), new TeamId(0),
                settings.PlayerFaction, PlayerController.Human, settings.Difficulty, 0), PlayerRole.Human, builder.Features));
            if (settings.AllyFaction.HasValue)
                builder.Players.Add(new BattlePlayer(new PlayerSlot(new PlayerId(1), new TeamId(0),
                    settings.AllyFaction.Value, PlayerController.Ai, settings.Difficulty, 1), PlayerRole.AiAlly, builder.Features));
            int enemyIndex = builder.Players.Count;
            builder.Players.Add(new BattlePlayer(new PlayerSlot(new PlayerId(enemyIndex), new TeamId(1),
                settings.EnemyFaction, PlayerController.Ai, settings.Difficulty, enemyIndex), PlayerRole.WaveAttacker, BattleFeatures.None));
            return builder;
        }
    }
}
