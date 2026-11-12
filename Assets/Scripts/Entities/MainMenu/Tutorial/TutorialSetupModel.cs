using System;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.MainMenu.Skirmish;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Entities.MainMenu.Tutorial
{
    /// <summary>
    /// The guided tutorial battle shown on the Skirmish setup screen. The player picks only their faction and colour;
    /// the opponent takes the rival faction and every other setting is fixed by <see cref="TutorialPreset"/>.
    /// </summary>
    public sealed class TutorialSetupModel : Model, ISkirmishModelObserver
    {
        public const int PLAYER_SLOT = 0;
        private const int OPPONENT_SLOT = 1;
        private const FactionType DEFAULT_FACTION = FactionType.Republic;

        private readonly SkirmishSlotSetup[] _slots =
        {
            new SkirmishSlotSetup(SkirmishSlotOccupant.Human, DEFAULT_FACTION, 0, 0),
            new SkirmishSlotSetup(ToOccupant(TutorialPreset.OPPONENT_DIFFICULTY),
                TutorialPreset.Opponent(DEFAULT_FACTION), 1, 1),
            new SkirmishSlotSetup(SkirmishSlotOccupant.Closed, DEFAULT_FACTION, 0, 2),
            new SkirmishSlotSetup(SkirmishSlotOccupant.Closed, TutorialPreset.Opponent(DEFAULT_FACTION), 1, 3)
        };

        public event Action Changed;

        public IReadOnlyList<SkirmishSlotSetup> Slots => _slots;
        public int TeamCount => MatchRules.MAX_PLAYERS;
        public int ActivePlayerCount => _slots.Count(slot => slot.IsOpen);
        public int ActiveTeamCount => _slots.Where(slot => slot.IsOpen).Select(slot => slot.Team).Distinct().Count();
        public PlanetType Planet => TutorialPreset.PLANET;
        public MapSize MapSize => TutorialPreset.MAP_SIZE;
        public BattleVictoryCondition VictoryCondition => TutorialPreset.VICTORY_CONDITION;
        public float MinStartingMoney => TutorialPreset.STARTING_MONEY;
        public float MaxStartingMoney => TutorialPreset.STARTING_MONEY;
        public float StartingMoney => TutorialPreset.STARTING_MONEY;
        public bool CanStart => true;
        // The tutorial player has every battle feature.
        public bool SuperWeapons => true;

        public bool IsOnline => false;
        public bool IsHost => false;
        public int LocalMemberId => SkirmishModel.NO_MEMBER;
        public bool CanEditSettings => false;
        public int HumanCount => _slots.Count(slot => slot.IsHuman);
        public int ReadyHumanCount => HumanCount;
        public bool AllHumansReady => true;
        public bool IsLocalReady => false;

        public bool IsLocalSlot(int slotIndex) => slotIndex == PLAYER_SLOT;
        public bool CanEditOccupant(int slotIndex) => false;
        public bool CanEditDetails(int slotIndex) => slotIndex == PLAYER_SLOT;
        public bool CanEditTeam(int slotIndex) => false;

        public FactionType PlayerFaction => _slots[PLAYER_SLOT].Faction;

        public void SelectPlayerFaction(FactionType faction)
        {
            _slots[PLAYER_SLOT].Faction = faction;
            _slots[OPPONENT_SLOT].Faction = TutorialPreset.Opponent(faction);
            Changed?.Invoke();
        }

        public void SelectPlayerColor(int colorIndex)
        {
            SkirmishSlotColors.Select(_slots, PLAYER_SLOT, colorIndex);
            Changed?.Invoke();
        }

        public BattleDefinition CreateBattle() => TutorialPreset.Create(PlayerFaction,
            _slots[PLAYER_SLOT].ColorIndex, _slots[OPPONENT_SLOT].ColorIndex);

        private static SkirmishSlotOccupant ToOccupant(EnemyAiDifficulty difficulty) => difficulty switch
        {
            EnemyAiDifficulty.Easy => SkirmishSlotOccupant.AiEasy,
            EnemyAiDifficulty.Medium => SkirmishSlotOccupant.AiMedium,
            EnemyAiDifficulty.Hard => SkirmishSlotOccupant.AiHard,
            EnemyAiDifficulty.UltraHard => SkirmishSlotOccupant.AiUltraHard,
            _ => throw new ArgumentOutOfRangeException(nameof(difficulty), difficulty, null)
        };
    }
}
