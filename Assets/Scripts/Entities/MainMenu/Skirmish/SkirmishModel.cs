using System;
using System.Linq;
using System.Collections.Generic;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Entities.MainMenu.Skirmish
{
    public class SkirmishModel : Model, ISkirmishModelObserver
    {
        public const int NO_MEMBER = -1;
        private const float MIN_STARTING_MONEY = 500f;
        private const float MAX_STARTING_MONEY = 10000f;
        private const float DEFAULT_STARTING_MONEY = 2000f;
        private const float MONEY_STEP = 100f;

        private readonly SkirmishSlotSetup[] _slots =
        {
            new SkirmishSlotSetup(SkirmishSlotOccupant.Human, FactionType.Republic, 0, 0),
            new SkirmishSlotSetup(SkirmishSlotOccupant.AiMedium, FactionType.Separatist, 1, 1),
            new SkirmishSlotSetup(SkirmishSlotOccupant.Closed, FactionType.Republic, 0, 2),
            new SkirmishSlotSetup(SkirmishSlotOccupant.Closed, FactionType.Separatist, 1, 3)
        };

        public event Action Changed;

        public IReadOnlyList<SkirmishSlotSetup> Slots => _slots;
        public int TeamCount => MatchRules.MAX_PLAYERS;
        public int ActivePlayerCount => _slots.Count(slot => slot.IsOpen);
        public int ActiveTeamCount => CountOpenTeams();
        public PlanetType Planet { get; private set; }
        public MapSize MapSize { get; private set; }
        public BattleVictoryCondition VictoryCondition { get; private set; }
        public float MinStartingMoney => MIN_STARTING_MONEY;
        public float MaxStartingMoney => MAX_STARTING_MONEY;
        public float StartingMoney { get; private set; } = DEFAULT_STARTING_MONEY;
        public bool CanStart => ActiveTeamCount >= 2;
        public bool SuperWeapons { get; private set; } = true;

        public bool IsOnline { get; private set; }
        public bool IsHost { get; private set; }
        public int LocalMemberId { get; private set; } = NO_MEMBER;
        public bool CanEditSettings => !IsOnline || IsHost;
        public int HumanCount => _slots.Count(slot => slot.IsHuman);
        public int ReadyHumanCount => _slots.Count(slot => slot.IsHuman && slot.Ready);
        public bool AllHumansReady => ReadyHumanCount == HumanCount;
        public bool IsLocalReady => _slots.Any(slot => slot.IsHuman && IsLocal(slot) && slot.Ready);

        /// <summary>Offline: the human row; online: the row this PC's member sits in.</summary>
        public bool IsLocalSlot(int slotIndex) => IsLocal(_slots[slotIndex]);

        /// <summary>Offline: AI rows; online: the host changes every row but its own (a member's row: kick).</summary>
        public bool CanEditOccupant(int slotIndex) =>
            IsOnline ? IsHost && !IsLocalSlot(slotIndex) : !_slots[slotIndex].IsHuman;

        /// <summary>Faction, team and color: each human picks their own; the host also picks for AI rows.</summary>
        public bool CanEditDetails(int slotIndex)
        {
            SkirmishSlotSetup slot = _slots[slotIndex];
            if (!slot.IsOpen) return false;
            return !IsOnline || IsLocal(slot) || IsHost && !slot.IsHuman;
        }

        public void SelectSuperWeapons(bool enabled)
        {
            SuperWeapons = enabled;
            Changed?.Invoke();
        }

        public BattleDefinition CreateBattle()
        {
            return SkirmishPreset.Create(CreatePlayers(), Planet, MapSize,
                VictoryCondition, StartingMoney, SuperWeapons, SkirmishPreset.NewMapSeed()).Build();
        }

        /// <param name="occupant">
        /// Offline, row 0 always stays the human; other rows pick Closed or an AI level. Online, the host may also
        /// open a seat (<see cref="SkirmishSlotOccupant.Vacant"/>); a member's row loses its member.
        /// </param>
        public void SelectSlotOccupant(int slotIndex, SkirmishSlotOccupant occupant)
        {
            if (!CanEditOccupant(slotIndex) || occupant == SkirmishSlotOccupant.Human ||
                occupant == SkirmishSlotOccupant.Vacant && !IsOnline)
            {
                throw new ArgumentException($"Row {slotIndex} cannot become {occupant}.", nameof(slotIndex));
            }

            _slots[slotIndex].Occupant = occupant;
            _slots[slotIndex].ClearMember();
            Changed?.Invoke();
        }

        /// <summary>Host: the lobby opens with the host in row 0, an open seat in row 1 and the rest closed.</summary>
        public void BeginHostLobby(int hostMemberId, string hostName)
        {
            IsOnline = true;
            IsHost = true;
            LocalMemberId = hostMemberId;
            SkirmishLobbySeating.SeatHost(_slots, hostMemberId, hostName);
            Changed?.Invoke();
        }

        /// <summary>Client: rows come from the host's snapshots (<see cref="ApplyLobby"/>).</summary>
        public void BeginClientLobby()
        {
            IsOnline = true;
            IsHost = false;
            LocalMemberId = NO_MEMBER;
        }

        /// <summary>
        /// Host: a joining member takes the first open seat, else a closed row, else an AI row; a member who left
        /// leaves an open seat. Raises <see cref="Changed"/> only when a row changed.
        /// </summary>
        public void SeatMembers(IReadOnlyList<SkirmishLobbySeat> members)
        {
            if (SkirmishLobbySeating.SeatMembers(_slots, members)) Changed?.Invoke();
        }

        /// <summary>Host: a member's own row choices; false when no row holds the member or a value is invalid.</summary>
        public bool TryEditMemberSlot(int memberId, FactionType faction, int team, int colorIndex)
        {
            int slotIndex = Array.FindIndex(_slots, slot => slot.MemberId == memberId);
            if (slotIndex < 0 || !Enum.IsDefined(typeof(FactionType), faction) || team < 0 || team >= TeamCount ||
                colorIndex < 0 || colorIndex >= MatchRules.MAX_TEAM_COLORS) return false;
            _slots[slotIndex].Faction = faction;
            _slots[slotIndex].Team = team;
            SelectSlotColor(slotIndex, colorIndex);
            return true;
        }

        /// <summary>Host: member connection id to its slot index in <see cref="CreatePlayers"/>.</summary>
        public IReadOnlyDictionary<int, int> CreateMemberSlots() => SkirmishLobbySeating.CreateMemberSlots(_slots);

        public SkirmishLobbySnapshot CaptureLobby() => new SkirmishLobbySnapshot
        {
            slots = _slots.Select(SkirmishLobbySeating.Capture).ToArray(),
            planet = (int)Planet,
            mapSize = (int)MapSize,
            victoryCondition = (int)VictoryCondition,
            startingMoney = StartingMoney,
            superWeapons = SuperWeapons
        };

        /// <summary>Client: shows the host's setup; <paramref name="localMemberId"/> marks this PC's row.</summary>
        public void ApplyLobby(SkirmishLobbySnapshot snapshot, int localMemberId)
        {
            LocalMemberId = localMemberId;
            for (int i = 0; i < _slots.Length; i++) SkirmishLobbySeating.Apply(_slots[i], snapshot.slots[i]);

            Planet = (PlanetType)snapshot.planet;
            MapSize = (MapSize)snapshot.mapSize;
            VictoryCondition = (BattleVictoryCondition)snapshot.victoryCondition;
            StartingMoney = snapshot.startingMoney;
            SuperWeapons = snapshot.superWeapons;
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

        /// <summary>Colors stay unique: taking a color another row uses swaps the two rows' colors.</summary>
        public void SelectSlotColor(int slotIndex, int colorIndex)
        {
            if (colorIndex < 0 || colorIndex >= MatchRules.MAX_TEAM_COLORS)
            {
                throw new ArgumentOutOfRangeException(nameof(colorIndex), colorIndex, "No such team color.");
            }

            SkirmishSlotSetup slot = _slots[slotIndex];
            foreach (SkirmishSlotSetup other in _slots)
            {
                if (other != slot && other.ColorIndex == colorIndex)
                {
                    other.ColorIndex = slot.ColorIndex;
                }
            }

            slot.ColorIndex = colorIndex;
            Changed?.Invoke();
        }

        public void SelectPlanet(PlanetType planet)
        {
            Planet = planet;
            Changed?.Invoke();
        }

        public void SelectMapSize(MapSize mapSize)
        {
            MapSize = mapSize;
            Changed?.Invoke();
        }

        public void SelectVictoryCondition(BattleVictoryCondition condition)
        {
            VictoryCondition = condition;
            Changed?.Invoke();
        }

        public void SelectStartingMoney(float rawValue)
        {
            float snappedValue = (float)Math.Round(rawValue / MONEY_STEP) * MONEY_STEP;
            StartingMoney = Math.Max(MIN_STARTING_MONEY,
                Math.Min(MAX_STARTING_MONEY, snappedValue));
            Changed?.Invoke();
        }

        /// <summary>Open rows become players in row order, keeping the color each row picked.</summary>
        public IReadOnlyList<PlayerSlot> CreatePlayers()
        {
            List<PlayerSlot> players = new List<PlayerSlot>();
            foreach (SkirmishSlotSetup slot in _slots)
            {
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
                    slot.ColorIndex));
            }

            return players;
        }

        private bool IsLocal(SkirmishSlotSetup slot) => IsOnline ? slot.MemberId == LocalMemberId : slot.IsHuman;

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
