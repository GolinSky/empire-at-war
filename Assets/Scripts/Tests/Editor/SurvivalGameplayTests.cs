using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.Factions;
using System.Linq;
using UnityEditor;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class SurvivalGameplayTests
    {
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
