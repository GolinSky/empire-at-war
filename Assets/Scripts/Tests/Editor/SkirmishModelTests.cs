using System;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.MainMenu.Skirmish;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class SkirmishModelTests
    {
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
