using EmpireAtWar.Controllers.Economy;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Models.Economy;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Economy;
using EmpireAtWar.Services.Factions;
using EmpireAtWar.Services.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class EconomyTuningTests
    {
        private const string SHARED_PATH = "Assets/Settings/Data/Factions/Shared/";

        [TestCase("Empire")]
        [TestCase("Republic")]
        [TestCase("Rebellion")]
        [TestCase("Separatist")]
        public void StationResearch_ReplacesTierAndDoesNotChangeMiningResearch(string faction)
        {
            FactionRoster roster = CreateRoster(faction);
            ResearchTierData[] tiers = roster.ResearchLines[ResearchType.IncreasedStationIncome].Tiers;
            Assert.That(tiers, Has.Length.EqualTo(2));
            Assert.That(tiers[0].FactionData.AvailableLevel, Is.EqualTo(4));
            Assert.That(tiers[1].FactionData.AvailableLevel, Is.EqualTo(5));

            FactionResearchModel research = new FactionResearchModel(roster);
            Assert.That(research.StationIncomeMultiplier, Is.EqualTo(1f));
            research.Complete(ResearchType.IncreasedProduction);
            Assert.That(research.IncomeMultiplier, Is.EqualTo(1.2f).Within(0.0001f));
            Assert.That(research.StationIncomeMultiplier, Is.EqualTo(1f));

            research.Complete(ResearchType.IncreasedStationIncome);
            Assert.That(research.StationIncomeMultiplier, Is.EqualTo(1.2f).Within(0.0001f));
            Assert.That(research.IncomeMultiplier, Is.EqualTo(1.2f).Within(0.0001f));
            research.Complete(ResearchType.IncreasedStationIncome);
            Assert.That(research.StationIncomeMultiplier, Is.EqualTo(1.4f).Within(0.0001f));
            Assert.That(research.IncomeMultiplier, Is.EqualTo(1.2f).Within(0.0001f));
            research.Complete(ResearchType.IncreasedProduction);
            Assert.That(research.IncomeMultiplier, Is.EqualTo(1.4f).Within(0.0001f));
            Assert.That(research.StationIncomeMultiplier, Is.EqualTo(1.4f).Within(0.0001f));
        }

        [TestCase(1, 18f)]
        [TestCase(2, 33f)]
        [TestCase(3, 48f)]
        [TestCase(4, 63f)]
        [TestCase(5, 78f)]
        public void StationIncome_IsTripledAndResearchRefreshesRegisteredProviders(int level, float baseIncome)
        {
            FactionRoster roster = CreateRoster("Empire");
            FactionResearchModel research = new FactionResearchModel(roster);
            PlayerFactionModel faction = new PlayerFactionModel(
                Load<StationLevelData>(SHARED_PATH + "StationLevelData.asset"), roster)
            { CurrentLevel = level };
            var owner = TestPlayers.CreateDuel().Get(TestPlayers.Human);
            EconomyData data = Load<EconomyData>("Assets/Settings/Data/Economy/EconomyData.asset");
            EconomyService economy = new EconomyService(new EconomyModel(data, data.StartMoneyAmount), data,
                new BattleStateNotifier(), new PlayerRegistry(), owner, research);
            FactionService station = new FactionService(null, null, null, economy, null,
                faction, research, null, owner, null);
            economy.Initialize();
            try
            {
                economy.AddProvider(station);
                Assert.That(economy.TotalIncome, Is.EqualTo(baseIncome));
                economy.AddProvider(new FixedIncomeProvider());
                research.Complete(ResearchType.IncreasedStationIncome);
                Assert.That(economy.TotalIncome, Is.EqualTo(baseIncome * 1.2f + 60f).Within(0.0001f));
                research.Complete(ResearchType.IncreasedStationIncome);
                Assert.That(economy.TotalIncome, Is.EqualTo(baseIncome * 1.4f + 60f).Within(0.0001f));
            }
            finally
            {
                economy.LateDispose();
            }
        }

        [Test]
        public void FacilityAndUpgradeAssets_UseTunedValues()
        {
            Assert.That(Load<MiningFacilityData>(
                "Assets/Settings/Data/Models/MiningFacilities/MiningFacilityData.asset").Income,
                Is.EqualTo(20f));
            Assert.That(Load<MiningFacilityData>(
                "Assets/Settings/Data/Models/MiningFacilities/AsteroidMiningFacilityData.asset").Income,
                Is.EqualTo(60f));
            Assert.That(Load<MiningFacilityCatalog>(SHARED_PATH + "MiningFacilityCatalog.asset")
                .Get(MiningFacilityType.CommonMiner).Price, Is.EqualTo(1000));
            Assert.That(Load<CaptureSiteData>(
                "Assets/Settings/Data/Models/CaptureSites/CaptureSiteData.asset")
                .GetCost(SiteFacilityType.BattleAsteroid).Price, Is.EqualTo(1600));
            StationLevelData levels = Load<StationLevelData>(SHARED_PATH + "StationLevelData.asset");
            Assert.That(levels.GetLevelFactionData(1).BuildTime, Is.EqualTo(160));
            Assert.That(levels.GetLevelFactionData(2).BuildTime, Is.EqualTo(200));
            Assert.That(levels.GetLevelFactionData(3).BuildTime, Is.EqualTo(240));
            Assert.That(levels.GetLevelFactionData(4).BuildTime, Is.EqualTo(280));
        }

        private static FactionRoster CreateRoster(string faction)
        {
            return new FactionRoster(
                Load<FactionDefinition>($"Assets/Settings/Data/Factions/{faction}/{faction}Faction.asset"),
                Load<MiningFacilityCatalog>(SHARED_PATH + "MiningFacilityCatalog.asset"),
                Load<DefendPlatformCatalog>(SHARED_PATH + "DefendPlatformCatalog.asset"),
                Load<SuperWeaponCatalog>(SHARED_PATH + "SuperWeaponCatalog.asset"));
        }

        private static T Load<T>(string path) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.That(asset, Is.Not.Null, path);
            return asset;
        }

        private sealed class FixedIncomeProvider : IIncomeProvider
        {
            public float Income => 60f;
        }

        private sealed class BattleStateNotifier : INotifier<BattleState>
        {
            public void AddObserver(IObserver<BattleState> observer) => observer.UpdateState(BattleState.Running);
            public void RemoveObserver(IObserver<BattleState> observer) { }
        }
    }
}
