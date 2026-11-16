using System;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.MainMenu.Skirmish;
using EmpireAtWar.Entities.MainMenu.Tutorial;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    [Category("GameFlow")]
    public sealed class SkirmishModelTests
    {
        [Test]
        public void SeatMembers_ClosedRowsOpenForSevenFriendsWithStableUniqueSeats()
        {
            var model = new SkirmishModel();
            model.BeginHostLobby(0, "Host");
            model.SelectSlotOccupant(1, SkirmishSlotOccupant.Closed);
            var members = new List<SkirmishLobbySeat> { new SkirmishLobbySeat(0, "Host", true, 0UL) };

            for (int i = 1; i < MatchRules.MAX_PLAYERS; i++)
            {
                members.Add(new SkirmishLobbySeat(i, $"Friend {i}", false, (ulong)i));
                model.SeatMembers(members);
                Assert.That(model.HumanCount, Is.EqualTo(i + 1));
                for (int row = 0; row <= i; row++)
                {
                    Assert.That(model.Slots[row].IsHuman, Is.True);
                    Assert.That(model.Slots[row].MemberId, Is.EqualTo(row));
                }
            }

            Assert.That(model.CreateMemberSlots().Count, Is.EqualTo(8));
            var client = new SkirmishModel();
            client.BeginClientLobby();
            client.ApplyLobby(model.CaptureLobby(), 7);
            Assert.That(client.HumanCount, Is.EqualTo(8));
            Assert.That(client.IsLocalSlot(7), Is.True);
            Assert.That(client.CanEditDetails(7), Is.True);
            Assert.That(client.CanEditDetails(0), Is.False);
        }

        [Test]
        public void SeatMembers_DepartureAndRejoin_PreserveOtherPlayersAndSeatChoices()
        {
            var model = new SkirmishModel();
            model.BeginHostLobby(0, "Host");
            var members = new List<SkirmishLobbySeat>
            {
                new SkirmishLobbySeat(0, "Host", true, 0UL),
                new SkirmishLobbySeat(1, "Leaving", true, 11UL),
                new SkirmishLobbySeat(2, "Staying", true, 22UL)
            };
            model.SeatMembers(members);
            model.TryEditMemberSlot(2, FactionType.Separatist, 3, 15);
            members.RemoveAt(1);
            model.SeatMembers(members);
            Assert.That(model.Slots[1].Occupant, Is.EqualTo(SkirmishSlotOccupant.Vacant));
            Assert.That(model.Slots[1].MemberId, Is.EqualTo(SkirmishModel.NO_MEMBER));
            model.SelectSlotOccupant(1, SkirmishSlotOccupant.Closed);

            members.Add(new SkirmishLobbySeat(9, "Rejoined", false, 11UL));
            model.SeatMembers(members);

            Assert.That(model.Slots[1].MemberId, Is.EqualTo(9));
            Assert.That(model.Slots[1].IsHuman, Is.True);
            Assert.That(model.Slots[1].Ready, Is.False);
            Assert.That(model.Slots[2].MemberId, Is.EqualTo(2));
            Assert.That(model.Slots[2].Faction, Is.EqualTo(FactionType.Separatist));
            Assert.That(model.Slots[2].Team, Is.EqualTo(3));
            Assert.That(model.Slots[2].ColorIndex, Is.EqualTo(15));
            Assert.That(model.Slots[2].Ready, Is.True);
            Assert.That(model.Slots.Where(row => row.IsHuman).Select(row => row.MemberId),
                Is.EquivalentTo(new[] { 0, 2, 9 }));
        }

        [Test]
        public void TutorialSetup_EightViewRowsStillBuildOnlyItsFixedDuel()
        {
            var model = new TutorialSetupModel();
            Assert.That(model.Slots.Count, Is.EqualTo(MatchRules.MAX_PLAYERS));
            for (int i = 2; i < model.Slots.Count; i++)
            {
                Assert.That(model.Slots[i].IsOpen, Is.False);
                Assert.That(model.CanEditOccupant(i), Is.False);
            }
            BattleDefinition battle = model.CreateBattle();
            Assert.That(battle.Players.Count, Is.EqualTo(2));
            Assert.That(battle.Map.Size, Is.EqualTo(MapSize.Small));
            Assert.That(battle.Map.Planet, Is.EqualTo(TutorialPreset.PLANET));
            Assert.That(battle.Mode, Is.EqualTo(GameMode.Tutorial));
        }

        [TestCase(MapSize.Small, 4)]
        [TestCase(MapSize.Medium, 6)]
        [TestCase(MapSize.Large, 8)]
        public void CreateBattle_MapCapacity_AcceptsMaximumAndRejectsExcess(MapSize size, int capacity)
        {
            SkirmishModel model = new SkirmishModel();
            model.SelectMapSize(size);
            for (int i = 2; i < capacity; i++)
                model.SelectSlotOccupant(i, SkirmishSlotOccupant.AiMedium);

            Assert.That(model.CanStart, Is.True);
            Assert.That(model.CreateBattle().Players.Count, Is.EqualTo(capacity));
            if (capacity == MatchRules.MAX_PLAYERS) return;

            model.SelectSlotOccupant(capacity, SkirmishSlotOccupant.AiMedium);
            Assert.That(model.CanStart, Is.False);
            Assert.Throws<ArgumentException>(() => model.CreateBattle());
        }

        [Test]
        public void SelectMapSize_ShrinkingKeepsRosterButBlocksStartUntilItFits()
        {
            SkirmishModel model = new SkirmishModel();
            model.SelectMapSize(MapSize.Large);
            for (int i = 2; i < MatchRules.MAX_PLAYERS; i++)
                model.SelectSlotOccupant(i, SkirmishSlotOccupant.AiMedium);

            model.SelectMapSize(MapSize.Small);
            Assert.That(model.ActivePlayerCount, Is.EqualTo(8));
            Assert.That(model.CanStart, Is.False);
            model.SelectMapSize(MapSize.Large);
            Assert.That(model.CanStart, Is.True);
        }

        [TestCase(7)]
        [TestCase(15)]
        public void CaptureAndApplyLobby_EightRowsKeepTheirChoices(int colorIndex)
        {
            SkirmishModel host = new SkirmishModel();
            host.SelectMapSize(MapSize.Large);
            host.SelectSlotOccupant(7, SkirmishSlotOccupant.AiHard);
            host.SelectSlotTeam(7, 7);
            host.SelectSlotColor(7, colorIndex);
            SkirmishModel client = new SkirmishModel();
            client.BeginClientLobby();
            client.ApplyLobby(host.CaptureLobby(), SkirmishModel.NO_MEMBER);

            Assert.That(client.Slots.Count, Is.EqualTo(8));
            Assert.That(client.Slots[7].Occupant, Is.EqualTo(SkirmishSlotOccupant.AiHard));
            Assert.That(client.Slots[7].Team, Is.EqualTo(7));
            Assert.That(client.Slots[7].ColorIndex, Is.EqualTo(colorIndex));
            Assert.That(client.MapSize, Is.EqualTo(MapSize.Large));
        }

        [Test]
        public void SaveSetup_EightPlayersRebuildWithStableIndexesTeamsAndColors()
        {
            SkirmishModel model = new SkirmishModel();
            model.SelectMapSize(MapSize.Large);
            for (int i = 2; i < MatchRules.MAX_PLAYERS; i++)
            {
                model.SelectSlotOccupant(i, SkirmishSlotOccupant.AiMedium);
                model.SelectSlotTeam(i, i);
            }
            model.SelectSlotColor(7, 15);
            BattleDefinition original = model.CreateBattle();
            BattleDefinition rebuilt = SkirmishSetupSave.From(original).Rebuild();

            Assert.That(rebuilt.Players.Count, Is.EqualTo(8));
            Assert.That(rebuilt.Slots[7].ColorIndex, Is.EqualTo(15));
            Assert.That(rebuilt.Map.Seed, Is.EqualTo(original.Map.Seed));
            for (int i = 0; i < rebuilt.Slots.Count; i++)
            {
                Assert.That(rebuilt.Slots[i].Id, Is.EqualTo(original.Slots[i].Id));
                Assert.That(rebuilt.Slots[i].Team, Is.EqualTo(original.Slots[i].Team));
                Assert.That(rebuilt.Slots[i].ColorIndex, Is.EqualTo(original.Slots[i].ColorIndex));
            }
        }

        [Test]
        public void SelectSlotColor_ColorUsedByAnotherRow_SwapsTheTwoColors()
        {
            SkirmishModel model = new SkirmishModel();

            model.SelectSlotColor(0, model.Slots[2].ColorIndex);

            Assert.That(model.Slots[0].ColorIndex, Is.EqualTo(2));
            Assert.That(model.Slots[2].ColorIndex, Is.EqualTo(0));
        }

        [Test]
        public void SelectSlotColor_FreeColor_OnlyChangesThatRow()
        {
            SkirmishModel model = new SkirmishModel();

            model.SelectSlotColor(1, 6);

            Assert.That(model.Slots[1].ColorIndex, Is.EqualTo(6));
            Assert.That(model.Slots[0].ColorIndex, Is.EqualTo(0));
        }

        [Test]
        public void CreatePlayers_KeepsEachRowsColor()
        {
            SkirmishModel model = new SkirmishModel();
            model.SelectSlotColor(0, 5);

            var players = model.CreatePlayers();

            Assert.That(players[0].ColorIndex, Is.EqualTo(5));
            Assert.That(players[1].ColorIndex, Is.EqualTo(1));
        }

        [Test]
        public void Validate_RepeatedColor_IsRejected()
        {
            PlayerSlot[] players =
            {
                new PlayerSlot(new PlayerId(0), new TeamId(0), FactionType.Republic,
                    PlayerController.Human, EnemyAiDifficulty.Medium, 3),
                new PlayerSlot(new PlayerId(1), new TeamId(1), FactionType.Separatist,
                    PlayerController.Ai, EnemyAiDifficulty.Medium, 3)
            };

            Assert.Throws<ArgumentException>(() => MatchRules.Validate(players));
        }
    }
}
