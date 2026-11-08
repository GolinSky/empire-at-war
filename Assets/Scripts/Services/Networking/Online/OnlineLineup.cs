using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Services.Networking.Online
{
    /// <summary>
    /// Host: gives every lobby member a Human slot so the host does not set them by hand.
    /// Skirmish turns AI slots into Human slots in slot order; Survival adds co-op humans of the host's faction.
    /// </summary>
    public static class OnlineLineup
    {
        // Survival: humans, optional AI ally and the wave attacker share the four slots.
        private const int MAX_SURVIVAL_PLAYERS = 4;

        public static bool TryFit(BattleDefinition battle, int humans, out BattleDefinition fitted, out OnlineError error) =>
            battle.Mode == GameMode.Survival
                ? TryFitSurvival(battle.Survival, humans, out fitted, out error)
                : TryFitSkirmish(battle, humans, out fitted, out error);

        private static bool TryFitSkirmish(BattleDefinition battle, int humans, out BattleDefinition fitted,
            out OnlineError error)
        {
            List<PlayerSlot> slots = battle.Slots.ToList();
            int missing = humans - slots.Count(slot => slot.Controller == PlayerController.Human);
            for (int i = 0; i < slots.Count && missing > 0; i++)
            {
                PlayerSlot slot = slots[i];
                if (slot.Controller != PlayerController.Ai) continue;
                slots[i] = new PlayerSlot(slot.Id, slot.Team, slot.Faction, PlayerController.Human, slot.Difficulty,
                    slot.ColorIndex);
                missing--;
            }

            fitted = null;
            if (missing > 0)
            {
                error = OnlineError.NotEnoughSlots;
                return false;
            }

            error = OnlineError.None;
            fitted = SkirmishPreset.Create(slots, battle.Map.Planet, battle.Map.Size, battle.VictoryCondition,
                battle.StartingMoney, battle.HasFeature(BattleFeatures.SuperWeapons), battle.Map.Seed).Build();
            return true;
        }

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
