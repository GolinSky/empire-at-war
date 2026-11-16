using System.Linq;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Entities.CoreGameUi
{
    internal static class BattleOpponentLabels
    {
        public static string Format(BattleDefinition battle, PlayerId localPlayerId, bool aiOnly)
        {
            if (localPlayerId.IsNone) return string.Empty;

            TeamId localTeam = battle.GetPlayer(localPlayerId).Slot.Team;
            var opponents = battle.Slots
                .Where(slot => slot.Team != localTeam && (!aiOnly || slot.IsAi))
                .Select(FormatSlot)
                .Distinct();
            return string.Join(aiOnly ? "\n" : " / ", opponents);
        }

        private static string FormatSlot(PlayerSlot slot)
        {
            string factionName = slot.Faction.GetDisplayName();
            if (!slot.IsAi) return factionName;

            string difficulty = slot.Difficulty == EnemyAiDifficulty.UltraHard
                ? "Ultra Hard"
                : slot.Difficulty.ToString();
            return $"{factionName} · {difficulty} AI";
        }
    }
}
