using System.Linq;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Map.Generation;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Utils;
using EmpireAtWar.Views.ReinforcementZones;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("GameFlow")]
    public sealed class SurvivalCoopTests
    {
        private const string SETTINGS_PATH = "Assets/Settings/Data/Models/Map/MapGenerationSettings.asset";
        private const string ZONE_PREFAB_PATH = "Assets/Prefabs/View/ReinforcementZones/ReinforcementZone.prefab";
        private const string MAIN_ZONE_PREFAB_PATH = "Assets/Prefabs/View/ReinforcementZones/MainReinforcementZone.prefab";
        private const string MINING_PREFAB_PATH = "Assets/Prefabs/View/CaptureSites/CaptureSite.prefab";
        private const float TOLERANCE = 0.01f;

        private MapGenerationSettings _settings;
        private MapFeatureRadii _radii;

        [SetUp]
        public void SetUp()
        {
            _settings = AssetDatabase.LoadAssetAtPath<MapGenerationSettings>(SETTINGS_PATH);
            CaptureSiteData siteData = AssetDatabase.LoadAssetAtPath<CaptureSiteData>(
                "Assets/Settings/Data/Models/CaptureSites/CaptureSiteData.asset");
            CaptureSiteView miningSite = AssetDatabase.LoadAssetAtPath<CaptureSiteView>(MINING_PREFAB_PATH);
            _radii = new MapFeatureRadii(
                AssetDatabase.LoadAssetAtPath<ReinforcementZoneView>(ZONE_PREFAB_PATH).Radius,
                siteData.GetSettings(CaptureSiteKind.Mining).Radius *
                Mathf.Max(Mathf.Abs(miningSite.transform.lossyScale.x), Mathf.Abs(miningSite.transform.lossyScale.z)),
                AssetDatabase.LoadAssetAtPath<ReinforcementZoneView>(MAIN_ZONE_PREFAB_PATH).Radius);
        }

        private static BattleDefinition Coop(MapSize size, FactionType? ally, params FactionType[] coopHumans) =>
            SurvivalPreset.Create(new SurvivalSettings(FactionType.Republic, ally, FactionType.Separatist, size,
                EnemyAiDifficulty.Medium, true, coopHumans)).Build();

        [Test]
        public void Preset_CoopHumansShareTheDefenderTeam_AndTheAllyIsHostedByPlayerZero()
        {
            BattleDefinition battle = Coop(MapSize.Medium, FactionType.Republic, FactionType.Separatist);

            Assert.That(battle.Players.Select(player => player.Role), Is.EqualTo(new[]
                { PlayerRole.Human, PlayerRole.Human, PlayerRole.AiAlly, PlayerRole.WaveAttacker }));
            Assert.That(battle.Humans.Select(human => human.Slot.Team).Distinct().Single(), Is.EqualTo(new TeamId(0)));
            Assert.That(battle.GetProductionHost(battle.Players[2]).Slot.Id, Is.EqualTo(new PlayerId(0)));
        }

        [Test]
        public void Preset_WithoutCoopHumans_KeepsTheOfflineLineup()
        {
            BattleDefinition battle = SurvivalPreset.Create(new SurvivalSettings(FactionType.Republic, FactionType.Republic,
                FactionType.Separatist, MapSize.Small, EnemyAiDifficulty.Medium, true)).Build();

            Assert.That(battle.Players.Select(player => player.Role), Is.EqualTo(new[]
                { PlayerRole.Human, PlayerRole.AiAlly, PlayerRole.WaveAttacker }));
            Assert.That(battle.Humans, Has.Count.EqualTo(1));
        }

        [Test]
        public void Layout_SingleHuman_StationStaysAtTheCenter()
        {
            BattleDefinition battle = Coop(MapSize.Small, FactionType.Republic);
            MapLayout layout = new SurvivalLayoutGenerator(_settings, _radii).Generate(battle);
            MapSizeSettings size = _settings.GetSize(MapSize.Small);
            Vector2 middle = (size.Bounds.Min + size.Bounds.Max) * 0.5f;

            Vector3 station = layout.GetStationPosition(new PlayerId(0));
            Assert.That(new Vector2(station.x, station.z), Is.EqualTo(middle));
            Assert.That(layout.GetStationPosition(new PlayerId(1)), Is.EqualTo(station), "The hosted ally shares the station.");
        }

        [Test]
        public void Preset_CoopOnSmallMap_IsRejected()
        {
            Assert.Throws<System.ArgumentException>(() => Coop(MapSize.Small, null, FactionType.Separatist));
        }

        [TestCase(MapSize.Medium, 1)]
        [TestCase(MapSize.Medium, 2)]
        [TestCase(MapSize.Large, 2)]
        public void Layout_CoopHumans_GetSeparateStationsClearOfSitesAndZones(MapSize mapSize, int extraHumans)
        {
            FactionType[] coop = Enumerable.Repeat(FactionType.Separatist, extraHumans).ToArray();
            BattleDefinition battle = Coop(mapSize, null, coop);
            MapLayout layout = new SurvivalLayoutGenerator(_settings, _radii).Generate(battle);
            float radius = battle.Humans.Max(human => _settings.GetStationRadius(human.Slot.Faction));

            for (int i = 0; i < battle.Humans.Count; i++)
            {
                Vector3 station = layout.GetStationPosition(battle.Humans[i].Slot.Id);
                for (int j = i + 1; j < battle.Humans.Count; j++)
                    Assert.That(PlanarGeometry.Distance(station, layout.GetStationPosition(battle.Humans[j].Slot.Id)),
                        Is.GreaterThanOrEqualTo(2f * radius - TOLERANCE), "Co-op stations must not overlap.");
                foreach (SiteSpot site in layout.Sites)
                    Assert.That(PlanarGeometry.Distance(station, site.Center), Is.GreaterThan(radius),
                        "Capture sites must stay outside every station.");
            }

            Assert.That(layout.Zones.Select(zone => zone.Owner),
                Is.EquivalentTo(battle.Humans.Select(human => human.Slot.Id)), "Each human gets its own deploy zone.");
            Assert.That(layout.Sites.Where(site => site.Kind == CaptureSiteKind.Satellite).Select(site => site.Owner).Distinct(),
                Is.EquivalentTo(battle.Humans.Select(human => human.Slot.Id)), "Satellite pads are shared out between humans.");
        }

        [Test]
        public void Outcome_DefendersLoseOnlyWhenEveryHumanStationHasFallen()
        {
            BattleDefinition battle = Coop(MapSize.Medium, null, FactionType.Separatist);
            SurvivalModel model = new SurvivalModel();
            SurvivalOutcomeRule rule = new SurvivalOutcomeRule(model, battle);

            Assert.That(rule.Evaluate(BattleVictoryCondition.DestroyOpponentBase, States(true, false), new TeamId(0)),
                Is.EqualTo(BattleOutcome.None));
            Assert.That(model.Phase, Is.Not.EqualTo(SurvivalPhase.Lost));

            Assert.That(rule.Evaluate(BattleVictoryCondition.DestroyOpponentBase, States(false, false), new TeamId(0)),
                Is.EqualTo(BattleOutcome.EnemyVictory));
        }

        private static PlayerBattleState[] States(bool firstStationAlive, bool secondStationAlive) => new[]
        {
            new PlayerBattleState(new PlayerId(0), new TeamId(0), 1, firstStationAlive, false),
            new PlayerBattleState(new PlayerId(1), new TeamId(0), 1, secondStationAlive, false),
            new PlayerBattleState(new PlayerId(2), new TeamId(1), 1, false, false)
        };
    }
}
