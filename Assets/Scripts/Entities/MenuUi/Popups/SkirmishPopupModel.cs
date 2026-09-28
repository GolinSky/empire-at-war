using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Entities.MenuUi.Popups
{
    public class SkirmishPopupModel : PureModel, ISkirmishPopupModelObserver
    {
        private const float MIN_STARTING_MONEY = 500f;
        private const float MAX_STARTING_MONEY = 10000f;
        private const float DEFAULT_STARTING_MONEY = 2000f;
        private const float MONEY_STEP = 500f;
        private const int HUMAN_SLOT_INDEX = 0;

        private readonly SkirmishSlotSetup[] _slots =
        {
            new SkirmishSlotSetup(SkirmishSlotOccupant.Human, FactionType.Republic, 0),
            new SkirmishSlotSetup(SkirmishSlotOccupant.AiMedium, FactionType.Separatist, 1),
            new SkirmishSlotSetup(SkirmishSlotOccupant.Closed, FactionType.Republic, 0),
            new SkirmishSlotSetup(SkirmishSlotOccupant.Closed, FactionType.Separatist, 1)
        };

        public event Action Changed;

        public IReadOnlyList<SkirmishSlotSetup> Slots => _slots;
        public int TeamCount => MatchRules.MAX_PLAYERS;
        public PlanetType Planet { get; private set; }
        public MapSize MapSize { get; private set; }
        public BattleVictoryCondition VictoryCondition { get; private set; }
        public float MinStartingMoney => MIN_STARTING_MONEY;
        public float MaxStartingMoney => MAX_STARTING_MONEY;
        public float StartingMoney { get; private set; } = DEFAULT_STARTING_MONEY;
        public bool CanStart => CountOpenTeams() >= 2;

        /// <param name="occupant">Row 0 always stays the human; other rows pick Closed or an AI level.</param>
        public void SelectSlotOccupant(int slotIndex, SkirmishSlotOccupant occupant)
        {
            if (slotIndex == HUMAN_SLOT_INDEX || occupant == SkirmishSlotOccupant.Human)
            {
                throw new ArgumentException("Only the first row holds the human player.", nameof(slotIndex));
            }

            _slots[slotIndex].Occupant = occupant;
            Changed?.Invoke();
        }

        public void SelectSlotFaction(int slotIndex, FactionType faction)
        {
            _slots[slotIndex].Faction = faction;
            Changed?.Invoke();
        }

        public void SelectSlotTeam(int slotIndex, int team)
        {
            _slots[slotIndex].Team = team;
            Changed?.Invoke();
        }

        public void SelectPlanet(PlanetType planet)
        {
            Planet = planet;
        }

        public void SelectMapSize(MapSize mapSize)
        {
            MapSize = mapSize;
        }

        public void SelectVictoryCondition(BattleVictoryCondition condition)
        {
            VictoryCondition = condition;
        }

        public void SelectStartingMoney(float rawValue)
        {
            float snappedValue = (float)Math.Round(rawValue / MONEY_STEP) * MONEY_STEP;
            StartingMoney = Math.Max(MIN_STARTING_MONEY,
                Math.Min(MAX_STARTING_MONEY, snappedValue));
            Changed?.Invoke();
        }

        /// <summary>Open rows become players in row order; the row index doubles as the team color.</summary>
        public IReadOnlyList<PlayerSlot> CreatePlayers()
        {
            List<PlayerSlot> players = new List<PlayerSlot>();
            for (int row = 0; row < _slots.Length; row++)
            {
                SkirmishSlotSetup slot = _slots[row];
                if (!slot.IsOpen)
                {
                    continue;
                }

                players.Add(new PlayerSlot(
                    new PlayerId(players.Count),
                    new TeamId(slot.Team),
                    slot.Faction,
                    slot.IsHuman ? PlayerController.Human : PlayerController.Ai,
                    ToDifficulty(slot.Occupant),
                    row));
            }

            return players;
        }

        private int CountOpenTeams()
        {
            HashSet<int> teams = new HashSet<int>();
            foreach (SkirmishSlotSetup slot in _slots)
            {
                if (slot.IsOpen)
                {
                    teams.Add(slot.Team);
                }
            }

            return teams.Count;
        }

        private static EnemyAiDifficulty ToDifficulty(SkirmishSlotOccupant occupant)
        {
            return occupant switch
            {
                SkirmishSlotOccupant.AiEasy => EnemyAiDifficulty.Easy,
                SkirmishSlotOccupant.AiHard => EnemyAiDifficulty.Hard,
                SkirmishSlotOccupant.AiUltraHard => EnemyAiDifficulty.UltraHard,
                // The human has no AI level; Medium keeps shared map tuning neutral.
                _ => EnemyAiDifficulty.Medium
            };
        }
    }
}
