using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.Factions;
using System.Linq;
using UnityEditor;
using NUnit.Framework;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Services.IdGeneration;
using EmpireAtWar.Ship;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class SurvivalGameplayTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void ShipFactory_ISDIII_RejectsSpawnAndRestoreBeforeLoadingAssets(bool restore)
        {
            var factory = new ShipFactory(null, null, null, null, null, new UniqueIdGenerator());

            var exception = Assert.Throws<System.InvalidOperationException>(() =>
            {
                if (restore)
                    factory.Create(new PlayerId(0), ShipType.ISDIII, Vector3.zero, 1,
                        ShipSpawnPose.Restored(Quaternion.identity));
                else
                    factory.Create(new PlayerId(0), ShipType.ISDIII, Vector3.zero);
            });

            Assert.That(exception.Message, Does.Contain("ISDIII is disabled in every game mode"));
        }

        [Test]
        public void PlayableRostersAndAuthoredWaves_ExcludeISDIII()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<FactionCatalog>(AssetDatabase.GUIDToAssetPath(
                AssetDatabase.FindAssets("t:FactionCatalog").Single()));
            foreach (var faction in catalog.Factions)
                Assert.That(faction.Ships.ContainsKey(ShipType.ISDIII), Is.False,
                    $"{faction.FactionType}: player, AI and simulation rosters must exclude ISDIII.");

            var data = AssetDatabase.LoadAssetAtPath<SurvivalWaveData>("Assets/Settings/Data/Battles/SurvivalWaveData.asset");
            foreach (var faction in data.Factions)
                foreach (var wave in faction.Waves)
                    Assert.That(wave.Entries.Any(entry => entry.Unit.IsShip
                        && entry.Unit.ShipType == ShipType.ISDIII), Is.False,
                        $"{faction.Faction}: survival waves must exclude ISDIII.");
        }

        [Test]
        public void AuthoredWaves_CoverEveryFactionWithFiveValidWaves()
        {
            var data = AssetDatabase.LoadAssetAtPath<SurvivalWaveData>("Assets/Settings/Data/Battles/SurvivalWaveData.asset");
            var catalog = AssetDatabase.LoadAssetAtPath<FactionCatalog>(AssetDatabase.GUIDToAssetPath(
                AssetDatabase.FindAssets("t:FactionCatalog").Single()));
            Assert.That(data.Factions.Count, Is.EqualTo(catalog.Factions.Count));
            foreach (var faction in catalog.Factions)
            {
                var configured = data.Factions.Single(entry => entry.Faction == faction.FactionType);
                Assert.That(configured.Waves.Count, Is.EqualTo(5));
                foreach (var wave in configured.Waves)
                {
                    Assert.That(wave.SpawnGroupCount, Is.InRange(1, 4));
                    Assert.That(wave.Entries, Is.Not.Empty);
                    foreach (var entry in wave.Entries)
                    {
                        Assert.That(entry.Quantity, Is.GreaterThan(0));
                        Assert.That(entry.Unit.IsShip ? faction.Ships.ContainsKey(entry.Unit.ShipType) :
                            faction.Squadrons.ContainsKey(entry.Unit.SquadronType), Is.True, entry.Unit.ToString());
                    }
                }
            }
        }

        [Test]
        public void AuthoredWaves_IncludeEveryUnlockedShipWithEqualTierCounts()
        {
            var data = AssetDatabase.LoadAssetAtPath<SurvivalWaveData>("Assets/Settings/Data/Battles/SurvivalWaveData.asset");
            var catalog = AssetDatabase.LoadAssetAtPath<FactionCatalog>(AssetDatabase.GUIDToAssetPath(
                AssetDatabase.FindAssets("t:FactionCatalog").Single()));
            foreach (var faction in catalog.Factions)
            {
                var waves = data.Factions.Single(entry => entry.Faction == faction.FactionType).Waves;
                for (int level = 1; level <= waves.Count; level++)
                {
                    var ships = waves[level - 1].Entries.Where(entry => entry.Unit.IsShip)
                        .ToDictionary(entry => entry.Unit.ShipType, entry => entry.Quantity);
                    Assert.That(ships.Keys, Is.EquivalentTo(faction.Ships
                        .Where(entry => entry.Value.AvailableLevel <= level).Select(entry => entry.Key)),
                        $"{faction.FactionType}, wave {level}");
                    for (int tier = 1; tier <= level; tier++)
                    {
                        int expected = 2 * catalog.Factions.Max(candidate => candidate.Ships
                            .Count(entry => entry.Value.AvailableLevel == tier));
                        Assert.That(ships.Where(entry => faction.Ships[entry.Key].AvailableLevel == tier)
                            .Sum(entry => entry.Value), Is.EqualTo(expected),
                            $"{faction.FactionType}, wave {level}, tier {tier}");
                    }
                    foreach (var hero in faction.Ships.Where(entry => entry.Value.IsHero
                                 && entry.Value.AvailableLevel <= level))
                        Assert.That(ships[hero.Key], Is.EqualTo(1));
                    if (level > 1)
                        foreach (var previous in waves[level - 2].Entries.Where(entry => entry.Unit.IsShip))
                            Assert.That(ships[previous.Unit.ShipType], Is.EqualTo(previous.Quantity));
                }
            }
        }

        [Test]
        public void FiveWaves_UsePreparationThenWin()
        {
            SurvivalModel model = new SurvivalModel();
            model.Advance(119);
            Assert.That(model.Phase, Is.EqualTo(SurvivalPhase.Preparation));
            model.Advance(1);
            for (int wave = 1; wave <= 5; wave++)
            {
                Assert.That(model.WaveIndex, Is.EqualTo(wave));
                Assert.That(model.Phase, Is.EqualTo(SurvivalPhase.WaveActive));
                model.CompleteWave();
                if (wave == 5) break;
                Assert.That(model.PreparationTimeLeft, Is.EqualTo(60));
                model.StartWaveNow();
            }
            Assert.That(model.Phase, Is.EqualTo(SurvivalPhase.Won));
        }

        [Test]
        public void StationLossDuringPreparation_EndsSurvival()
        {
            SurvivalModel model = new SurvivalModel();
            SurvivalOutcomeRule rule = new SurvivalOutcomeRule(model, SurvivalPreset.Create(new SurvivalSettings(FactionType.Empire, null, FactionType.Rebellion, MapSize.Small, EnemyAiDifficulty.Medium, false)).Build());
            Assert.That(rule.Evaluate(default, new[] {
                new PlayerBattleState(new PlayerId(0), new TeamId(0), 10, false, false)
            }, new TeamId(0)), Is.EqualTo(BattleOutcome.EnemyVictory));
            model.StartWaveNow();
            model.CompleteWave();
            Assert.That(model.Phase, Is.EqualTo(SurvivalPhase.Lost));
        }
    }
}
