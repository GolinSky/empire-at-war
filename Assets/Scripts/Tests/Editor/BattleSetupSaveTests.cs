using System;
using System.Linq;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.MainMenu.Skirmish;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Factions;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("GameFlow")]
    public sealed class BattleSetupSaveTests
    {
        private const int MAP_SEED = 918273;

        [TestCase(true)]
        [TestCase(false)]
        public void Skirmish_JsonRoundTrip_RebuildsEqualDefinition(bool superWeapons)
        {
            BattleDefinition battle = SkirmishPreset.Create(TestPlayers.CreateTeamGame().Players, PlanetType.Coruscant,
                MapSize.Large, BattleVictoryCondition.DestroyEnemyFleet, 2500f, superWeapons, MAP_SEED).Build();

            AssertSameDefinition(battle, RoundTrip(battle));
        }

        [Test]
        public void Skirmish_SetupScreenBattle_RoundTripsWithItsSeed()
        {
            BattleDefinition battle = new SkirmishModel().CreateBattle();

            BattleDefinition rebuilt = RoundTrip(battle);

            AssertSameDefinition(battle, rebuilt);
            Assert.That(rebuilt.Map.Seed, Is.EqualTo(battle.Map.Seed));
        }

        [Test]
        public void Skirmish_SetupScreen_PicksNewSeedPerBattle()
        {
            SkirmishModel model = new SkirmishModel();

            Assert.That(model.CreateBattle().Map.Seed, Is.Not.EqualTo(model.CreateBattle().Map.Seed));
        }

        [TestCase(true, true)]
        [TestCase(false, false)]
        public void Survival_JsonRoundTrip_RebuildsEqualDefinition(bool ally, bool superWeapons)
        {
            BattleDefinition battle = SurvivalPreset.Create(new SurvivalSettings(FactionType.Empire,
                ally ? FactionType.Republic : (FactionType?)null, FactionType.Rebellion, MapSize.Medium,
                EnemyAiDifficulty.Hard, superWeapons)).Build();

            AssertSameDefinition(battle, RoundTrip(battle));
        }

        [Test]
        public void Simulation_IsNotSaved()
        {
            SimulationFleetEntry[] fleet = { new SimulationFleetEntry(UnitTypeId.Ship(ShipType.Venator), 1) };
            BattleDefinition battle = SimulationPreset.Create(new SimulationSetup(FactionType.Empire,
                FactionType.Rebellion, fleet, fleet, 1000f, 60f, false)).Build();

            Assert.Throws<InvalidOperationException>(() => BattleSetupSave.From(battle));
        }

        private static BattleDefinition RoundTrip(BattleDefinition battle)
        {
            string json = JsonUtility.ToJson(BattleSetupSave.From(battle));
            return JsonUtility.FromJson<BattleSetupSave>(json).Rebuild();
        }

        private static void AssertSameDefinition(BattleDefinition expected, BattleDefinition actual)
        {
            Assert.That(actual.Mode, Is.EqualTo(expected.Mode));
            Assert.That(actual.Map.Type, Is.EqualTo(expected.Map.Type));
            Assert.That(actual.Map.Planet, Is.EqualTo(expected.Map.Planet));
            Assert.That(actual.Map.Size, Is.EqualTo(expected.Map.Size));
            Assert.That(actual.Map.Seed, Is.EqualTo(expected.Map.Seed));
            Assert.That(actual.Features, Is.EqualTo(expected.Features));
            Assert.That(actual.Outcome, Is.EqualTo(expected.Outcome));
            Assert.That(actual.VictoryCondition, Is.EqualTo(expected.VictoryCondition));
            Assert.That(actual.StartingMoney, Is.EqualTo(expected.StartingMoney));
            Assert.That(actual.InitialStationLevel, Is.EqualTo(expected.InitialStationLevel));
            Assert.That(actual.Players.Select(Describe), Is.EqualTo(expected.Players.Select(Describe)));
            Assert.That(Describe(actual.Survival), Is.EqualTo(Describe(expected.Survival)));
        }

        private static string Describe(BattlePlayer player) =>
            $"{player.Slot.Id.Index}/{player.Slot.Team.Value}/{player.Slot.Faction}/{player.Slot.Controller}/" +
            $"{player.Slot.Difficulty}/{player.Slot.ColorIndex}/{player.Role}/{player.Features}";

        private static string Describe(SurvivalSettings settings) => settings == null ? "none" :
            $"{settings.PlayerFaction}/{settings.AllyFaction}/{settings.EnemyFaction}/{settings.MapSize}/" +
            $"{settings.Difficulty}/{settings.SuperWeapons}";
    }
}
