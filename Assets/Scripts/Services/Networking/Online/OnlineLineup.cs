using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Services.Networking.Online
{
    /// <summary>
    /// Host, Survival: gives every lobby member a co-op Human slot of the host's faction. (Skirmish members sit in
    /// setup rows already.)
    /// </summary>
    public static class OnlineLineup
    {
        // Survival: humans, optional AI ally and the wave attacker share the four slots.
        private const int MAX_SURVIVAL_PLAYERS = 4;

        public static bool TryFit(BattleDefinition battle, int humans, out BattleDefinition fitted, out OnlineError error) =>
            TryFitSurvival(battle.Survival, humans, out fitted, out error);

        // An AI ally gives up its slot when the humans need it.
        private static bool TryFitSurvival(SurvivalSettings settings, int humans, out BattleDefinition fitted,
            out OnlineError error)
        {
            fitted = null;
            List<FactionType> coop = settings.CoopHumanFactions.ToList();
            while (1 + coop.Count < humans) coop.Add(settings.PlayerFaction);
            FactionType? ally = settings.AllyFaction;
            if (1 + coop.Count + (ally.HasValue ? 1 : 0) + 1 > MAX_SURVIVAL_PLAYERS) ally = null;
            if (1 + coop.Count + 1 > MAX_SURVIVAL_PLAYERS)
            {
                error = OnlineError.SurvivalLobbyTooLarge;
                return false;
            }

            if (coop.Count > 0 && settings.MapSize == MapSize.Small)
            {
                error = OnlineError.SurvivalCoopNeedsLargerMap;
                return false;
            }

            error = OnlineError.None;
            fitted = SurvivalPreset.Create(new SurvivalSettings(settings.PlayerFaction, ally, settings.EnemyFaction,
                settings.MapSize, settings.Difficulty, settings.SuperWeapons, coop)).Build();
            return true;
        }
    }
}
