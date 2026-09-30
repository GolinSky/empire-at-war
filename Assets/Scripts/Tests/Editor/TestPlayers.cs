using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Tests.Editor
{
    /// <summary>
    /// Shared line-ups for tests. <see cref="Human"/> is the local player; <see cref="Enemy"/> is an AI on
    /// the other team. The team game adds <see cref="Ally"/> (AI, human's team) and <see cref="SecondEnemy"/>.
    /// </summary>
    public static class TestPlayers
    {
        public static readonly PlayerId Human = new PlayerId(0);
        public static readonly PlayerId Enemy = new PlayerId(1);
        public static readonly PlayerId Ally = new PlayerId(2);
        public static readonly PlayerId SecondEnemy = new PlayerId(3);

        private static readonly TeamId HUMAN_TEAM = new TeamId(0);
        private static readonly TeamId ENEMY_TEAM = new TeamId(1);

        public static PlayerRoster CreateDuel(EnemyAiDifficulty enemyDifficulty = EnemyAiDifficulty.Medium)
        {
            return new PlayerRoster(new[]
            {
                CreateSlot(Human, HUMAN_TEAM, FactionType.Republic, PlayerController.Human),
                CreateSlot(Enemy, ENEMY_TEAM, FactionType.Separatist, PlayerController.Ai, enemyDifficulty)
            });
        }

        /// <summary>Human + allied AI against two enemy AIs.</summary>
        public static PlayerRoster CreateTeamGame()
        {
            return new PlayerRoster(new[]
            {
                CreateSlot(Human, HUMAN_TEAM, FactionType.Republic, PlayerController.Human),
                CreateSlot(Enemy, ENEMY_TEAM, FactionType.Separatist, PlayerController.Ai),
                CreateSlot(Ally, HUMAN_TEAM, FactionType.Republic, PlayerController.Ai),
                CreateSlot(SecondEnemy, ENEMY_TEAM, FactionType.Separatist, PlayerController.Ai)
            });
        }

        /// <summary>Tally of a circle holding the given strength per player.</summary>
        public static CaptureStrength Tally(IPlayerRoster roster, params (PlayerId Player, float Strength)[] units)
        {
            CaptureStrengthBuilder builder = new CaptureStrengthBuilder(roster);
            foreach ((PlayerId player, float strength) in units)
            {
                builder.Add(player, strength);
            }

            return builder.Build();
        }

        /// <summary>Duel tally with human strength first and enemy strength second.</summary>
        public static CaptureStrength DuelTally(IPlayerRoster roster, float humanStrength, float enemyStrength)
        {
            return Tally(roster, (Human, humanStrength), (Enemy, enemyStrength));
        }

        public static LocalPlayer CreateLocalPlayer(IPlayerRoster roster)
        {
            return new LocalPlayer(roster);
        }

        public static PlayerSlot Slot(IPlayerRoster roster, PlayerId id)
        {
            return roster.Get(id);
        }

        private static PlayerSlot CreateSlot(PlayerId id, TeamId team, FactionType faction,
            PlayerController controller, EnemyAiDifficulty difficulty = EnemyAiDifficulty.Medium)
        {
            return new PlayerSlot(id, team, faction, controller, difficulty, id.Index);
        }
    }
}
