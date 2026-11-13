using System;
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

        [Test]
        public void CaptureAndApplyLobby_EightRowsKeepTheirChoices()
        {
            SkirmishModel host = new SkirmishModel();
            host.SelectMapSize(MapSize.Large);
            host.SelectSlotOccupant(7, SkirmishSlotOccupant.AiHard);
            host.SelectSlotTeam(7, 7);
            SkirmishModel client = new SkirmishModel();
            client.BeginClientLobby();
            client.ApplyLobby(host.CaptureLobby(), SkirmishModel.NO_MEMBER);

            Assert.That(client.Slots.Count, Is.EqualTo(8));
            Assert.That(client.Slots[7].Occupant, Is.EqualTo(SkirmishSlotOccupant.AiHard));
            Assert.That(client.Slots[7].Team, Is.EqualTo(7));
            Assert.That(client.Slots[7].ColorIndex, Is.EqualTo(7));
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
            BattleDefinition original = model.CreateBattle();
            BattleDefinition rebuilt = SkirmishSetupSave.From(original).Rebuild();

            Assert.That(rebuilt.Players.Count, Is.EqualTo(8));
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
