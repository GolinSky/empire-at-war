using System;
using SurvivalModel = EmpireAtWar.Entities.MainMenu.Survival.SurvivalModel;
using System.Linq;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.MainMenu.Skirmish;
using EmpireAtWar.Entities.MainMenu.Survival;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.VContainer;
using NUnit.Framework;
using VContainer;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class BattleDefinitionTests
    {
        [Test]
        public void Skirmish_DefaultPreservesAllFeaturesAndStationOwners()
        {
            BattleDefinition battle = SkirmishPreset.Default();
            Assert.That(battle.Features, Is.EqualTo(BattleFeatures.All));
            Assert.That(battle.Map.Type, Is.EqualTo(MapType.Full));
            Assert.That(battle.Outcome, Is.EqualTo(OutcomeRule.Skirmish));
            Assert.That(battle.Players.Select(player => player.Role),
                Is.EqualTo(new[] { PlayerRole.Human, PlayerRole.AiOpponent }));
            Assert.That(battle.Players.All(player => player.OwnsStation), Is.True);
        }

        [Test]
        public void Skirmish_ToggleOverridesPresetWithoutChangingOtherFeatures()
        {
            SkirmishModel model = new SkirmishModel();
            model.SelectSuperWeapons(false);
            Assert.That(model.CreateBattle().Features, Is.EqualTo(BattleFeatures.All & ~BattleFeatures.SuperWeapons));
            model.SelectSuperWeapons(true);
            Assert.That(model.CreateBattle().Features, Is.EqualTo(BattleFeatures.All));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Survival_SetupHasRestrictedWaveAttackerAndOptionalHostedAlly(bool ally)
        {
            SurvivalModel model = new SurvivalModel();
            if (ally) model.SelectAlly(FactionType.Empire);
            model.SelectSuperWeapons(false);
            BattleDefinition battle = model.CreateBattle();
            Assert.That(battle.Players.Count, Is.EqualTo(ally ? 3 : 2));
            Assert.That(battle.Survival.SuperWeapons, Is.False);
            Assert.That(battle.Players.Count(player => player.OwnsStation), Is.EqualTo(1));
            BattlePlayer attacker = battle.Players.Last();
            Assert.That(attacker.Role, Is.EqualTo(PlayerRole.WaveAttacker));
            Assert.That(attacker.Features, Is.EqualTo(BattleFeatures.None));
            Assert.That(attacker.CanProduce || attacker.CanResearch, Is.False);
        }

        [TestCase(PlayerRole.WaveAttacker)]
        [TestCase(PlayerRole.SimulationSide)]
        public void StationlessCombatRoles_CannotEnableProductionEconomyOrSuperWeapons(PlayerRole role)
        {
            BattlePlayer player = new BattlePlayer(SkirmishPreset.Default().Slots[1], role, BattleFeatures.All);
            Assert.That(player.OwnsStation || player.CanProduce || player.CanResearch, Is.False);
            Assert.That(player.Features & (BattleFeatures.SuperWeapons | BattleFeatures.Economy), Is.EqualTo(BattleFeatures.None));
        }

        [Test]
        public void Builder_CopiesPlayerList()
        {
            var builder = SkirmishPreset.Create(SkirmishPreset.Default().Slots,
                EmpireAtWar.Entities.Planet.PlanetType.Coruscant, MapSize.Small,
                BattleVictoryCondition.DestroyEnemyFleet, 1000, true, 0);
            BattleDefinition definition = builder.Build();
            builder.Players.Clear();
            Assert.That(definition.Players.Count, Is.EqualTo(2));
        }

        [Test]
        public void DisabledSuperWeapons_FactoryRejectsRequests()
        {
            UnitRequestFactory factory = new UnitRequestFactory(false);
            Assert.Throws<InvalidOperationException>(() => factory.ConstructUnitRequest(null, SuperWeaponType.IonCannon));
        }

        [TestCase(BattleFeatures.None, 0)]
        [TestCase(BattleFeatures.SuperWeapons, 1)]
        [TestCase(BattleFeatures.Economy | BattleFeatures.Fog, 2)]
        [TestCase(BattleFeatures.All, 4)]
        public void Modules_SelectOnlyEnabledFeatures(BattleFeatures features, int count)
        {
            var preset = SkirmishPreset.Default();
            var definition = new BattleDefinition(preset.Mode, preset.Map, preset.Players, features,
                preset.Outcome, preset.VictoryCondition, preset.StartingMoney);
            var modules = new BattleModules(definition, new SuperWeaponsModule(null), new EconomyModule(null),
                new CaptureSitesModule(null, null), new FogModule(null));
            Assert.That(modules.Enabled.Count, Is.EqualTo(count));
        }

        [Test]
        public void Definition_LimitsPlayerFeaturesToBattleFeatures()
        {
            var preset = SkirmishPreset.Default();
            var definition = new BattleDefinition(preset.Mode, preset.Map, preset.Players, BattleFeatures.Fog,
                preset.Outcome, preset.VictoryCondition, preset.StartingMoney);
            Assert.That(definition.Players.Select(player => player.Features),
                Is.All.EqualTo(BattleFeatures.Fog));
            Assert.That(definition.Players[0].Has(BattleFeatures.SuperWeapons), Is.False);
        }

        [Test]
        public void RoleAndControllerMismatch_IsRejected()
        {
            var preset = SkirmishPreset.Default();
            var players = new[] { new BattlePlayer(preset.Slots[0], PlayerRole.AiOpponent, BattleFeatures.All), preset.Players[1] };
            var definition = new BattleDefinition(preset.Mode, preset.Map, players, preset.Features,
                preset.Outcome, preset.VictoryCondition, preset.StartingMoney);
            Assert.Throws<ArgumentException>(() => MatchRules.Validate(definition));
        }
    }
}
