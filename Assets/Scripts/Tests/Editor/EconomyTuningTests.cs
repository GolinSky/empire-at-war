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
using EmpireAtWar.Services.Saves;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Economy")]
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
        public void IncomeDelay_SurvivesSaveSoLoadingDoesNotPayEarly()
        {
            EconomyData data = Load<EconomyData>("Assets/Settings/Data/Economy/EconomyData.asset");
            EconomyModel model = new EconomyModel(data, 100f);
            Assert.That(model.AdvanceIncomeTimer(0f, 10f), Is.True);
            Assert.That(model.AdvanceIncomeTimer(4f, 10f), Is.False);

            PlayerSave save = PlayerSave.Create(TestPlayers.Human);
            save.SetMoney(model.Money, model.IncomeTimeLeft);
            PlayerSave loadedSave = JsonUtility.FromJson<PlayerSave>(JsonUtility.ToJson(save));
            EconomyModel loaded = new EconomyModel(data, 100f);
            loaded.Restore(loadedSave.Money, loadedSave.IncomeTimeLeft);

            Assert.That(loaded.AdvanceIncomeTimer(0f, 10f), Is.False);
            Assert.That(loaded.AdvanceIncomeTimer(5f, 10f), Is.False);
            Assert.That(loaded.AdvanceIncomeTimer(1f, 10f), Is.True);
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

        [TestCase(MiningFacilityType.CommonMiner, "MiningFacility")]
        [TestCase(MiningFacilityType.BattleMiner, "MiningFacility")]
        [TestCase(MiningFacilityType.AsteroidMiner, "AsteroidMiningFacility")]
        [TestCase(MiningFacilityType.TibannaGasStation, "TibannaGasStation")]
        public void MiningFacilityAssets_ResolveCanonicalNames(MiningFacilityType type, string expected)
        {
            Assert.That(MiningFacilityAssets.GetName(type), Is.EqualTo(expected));
        }

        [Test]
        public void MiningFacilityAssets_RejectUnknownTypes()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                MiningFacilityAssets.GetName((MiningFacilityType)999));
        }

        [TestCase("Empire", MiningFacilityType.TibannaGasStation)]
        [TestCase("Rebellion", MiningFacilityType.TibannaGasStation)]
        [TestCase("Republic", MiningFacilityType.CommonMiner)]
        [TestCase("Separatist", MiningFacilityType.CommonMiner)]
        public void MiningRoster_HasOnlyItsFactionFacility(string faction, MiningFacilityType expected)
        {
            Assert.That(CreateRoster(faction).MiningFacilities.Keys, Is.EquivalentTo(new[] { expected }));
        }

        [Test]
        public void TibannaGasStation_PreservesMiningEconomyAndHealth()
        {
            MiningFacilityCatalog catalog = Load<MiningFacilityCatalog>(SHARED_PATH + "MiningFacilityCatalog.asset");
            FactionData original = catalog.Get(MiningFacilityType.CommonMiner);
            FactionData tibanna = catalog.Get(MiningFacilityType.TibannaGasStation);
            Assert.That(tibanna.Price, Is.EqualTo(original.Price));
            Assert.That(tibanna.BuildTime, Is.EqualTo(original.BuildTime));
            Assert.That(tibanna.MaxCount, Is.EqualTo(original.MaxCount));
            Assert.That(tibanna.AvailableLevel, Is.EqualTo(original.AvailableLevel));
            Assert.That(tibanna.UnitCapacity, Is.EqualTo(original.UnitCapacity));
            MiningFacilityData originalData = Load<MiningFacilityData>(
                "Assets/Settings/Data/Models/MiningFacilities/MiningFacilityData.asset");
            MiningFacilityData tibannaData = Load<MiningFacilityData>(
                "Assets/Settings/Data/Models/MiningFacilities/TibannaGasStationData.asset");
            Assert.That(tibannaData.Income, Is.EqualTo(originalData.Income));
            Assert.That(tibannaData.ComponentData.Hull, Is.EqualTo(originalData.ComponentData.Hull));
            Assert.That(tibannaData.ComponentData.Shields, Is.EqualTo(originalData.ComponentData.Shields));
            Assert.That(tibannaData.ComponentData.ShieldRegenerateValue,
                Is.EqualTo(originalData.ComponentData.ShieldRegenerateValue));
            Assert.That(tibannaData.ComponentData.ShieldRegenerateDelay,
                Is.EqualTo(originalData.ComponentData.ShieldRegenerateDelay));
            Assert.That(tibannaData.ComponentData.SpawnBlockRadius, Is.EqualTo(originalData.ComponentData.SpawnBlockRadius));
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
