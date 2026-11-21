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
using EmpireAtWar.Services.Networking;
using EmpireAtWar.Services.Networking.States;
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
        public void ResearchRoster_ContainsEveryResearchLine(string faction)
        {
            var research = new FactionResearchModel(CreateRoster(faction));
            Assert.That(research.ResearchTypes, Is.EquivalentTo(System.Enum.GetValues(typeof(ResearchType))));
        }

        [TestCase("Empire")]
        [TestCase("Republic")]
        [TestCase("Rebellion")]
        [TestCase("Separatist")]
        public void StationResearch_ReplacesTierAndDoesNotChangeMiningResearch(string faction)
        {
            FactionRoster roster = CreateRoster(faction);
            ResearchTierData[] tiers = roster.ResearchLines[ResearchType.IncreasedStationIncome].Tiers;
            Assert.That(tiers, Has.Length.EqualTo(2));

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

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        public void StationIncome_GrowsWithLevelAndResearchRefreshesRegisteredProviders(int level)
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
                faction, research, null, owner, null, new SecondStationLevelModel(),
                TestPlayers.CreateLocalPlayer(TestPlayers.CreateDuel()));
            economy.Initialize();
            try
            {
                economy.AddProvider(station);
                float stationIncome = station.Income;
                faction.CurrentLevel = 1;
                Assert.That(stationIncome, Is.EqualTo(level * station.Income).Within(0.0001f),
                    "Station income grows in equal steps per level.");
                faction.CurrentLevel = level;
                float baseIncome = stationIncome + economy.Income;
                Assert.That(economy.TotalIncome, Is.EqualTo(baseIncome).Within(0.0001f));
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

        [TestCase(2, 8)]
        [TestCase(2, 12)]
        [TestCase(4, 10)]
        [TestCase(6, 16)]
        [TestCase(8, 22)]
        public void MiningPool_PaysTheSamePerPlayerForTheSameShareOfTheMap(int players, int spots)
        {
            // One of two equal teams holds half the spots and splits their income between its members.
            float perMember = MiningPool.GetSpotIncome(players, spots) * (spots / 2f) / (players / 2f);

            Assert.That(perMember, Is.EqualTo(MiningPool.PER_PLAYER).Within(0.0001f));
        }

        [Test]
        public void SecondStation_RaisesIncomeWhileItStands()
        {
            FactionRoster roster = CreateRoster("Empire");
            FactionResearchModel research = new FactionResearchModel(roster);
            PlayerFactionModel faction = new PlayerFactionModel(
                Load<StationLevelData>(SHARED_PATH + "StationLevelData.asset"), roster) { CurrentLevel = 3 };
            var secondStation = new SecondStationLevelModel();
            FactionService station = new FactionService(null, null, null, null, null,
                faction, research, null, TestPlayers.CreateDuel().Get(TestPlayers.Human), null, secondStation,
                TestPlayers.CreateLocalPlayer(TestPlayers.CreateDuel()));

            float single = station.Income;
            secondStation.IsBuilt = true;
            float withSecond = station.Income;
            Assert.That(withSecond, Is.GreaterThan(single));
            Assert.That(withSecond, Is.LessThan(2f * single), "The second station adds less than a full station.");
            secondStation.CompleteLevelUpgrade();
            Assert.That(station.Income, Is.EqualTo(withSecond));
            secondStation.HandleLost();
            Assert.That(station.Income, Is.EqualTo(single));
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

        [Test]
        [Category("Networking")]
        public void ClientIncome_UsesHostSnapshotsDespiteLocalProviderAndResearchChanges()
        {
            FactionRoster roster = CreateRoster("Empire");
            StationLevelData levels = Load<StationLevelData>(SHARED_PATH + "StationLevelData.asset");
            EconomyData data = Load<EconomyData>("Assets/Settings/Data/Economy/EconomyData.asset");
            var owner = TestPlayers.CreateDuel().Get(TestPlayers.Human);
            var hostResearch = new FactionResearchModel(roster);
            var clientResearch = new FactionResearchModel(roster);
            var hostMoney = new EconomyModel(data, 500f);
            var clientMoney = new EconomyModel(data, 100f);
            var host = new EconomyService(hostMoney, data, new BattleStateNotifier(),
                new PlayerRegistry(), owner, hostResearch);
            var client = new EconomyService(clientMoney, data, new BattleStateNotifier(),
                new PlayerRegistry(), owner, clientResearch);
            var hostState = new PlayerEconomyReplicatedState(new ReplicatedStateRegistry(), null, owner,
                hostMoney, host, new PlayerFactionModel(levels, roster), null, hostResearch, null, roster, levels);
            var clientState = new PlayerEconomyReplicatedState(new ReplicatedStateRegistry(), null, owner,
                clientMoney, client, new PlayerFactionModel(levels, roster), null, clientResearch, null, roster, levels);
            host.Initialize();
            client.Initialize();
            client.FollowHost();
            try
            {
                // The client may lack an allied mine's research or a newly replicated station's income.
                var hostMine = new FixedIncomeProvider();
                host.AddProvider(hostMine);
                hostResearch.Complete(ResearchType.IncreasedStationIncome);
                float incomeAtMoneyChanged = -1f;
                clientMoney.OnMoneyChanged += _ => incomeAtMoneyChanged = client.TotalIncome;
                clientState.Apply(hostState.Capture());
                Assert.That(client.TotalIncome, Is.EqualTo(63.6f).Within(0.0001f));
                Assert.That(incomeAtMoneyChanged, Is.EqualTo(host.TotalIncome));
                Assert.That(clientMoney.Money, Is.EqualTo(500f));

                var clientMine = new FixedIncomeProvider();
                client.AddProvider(clientMine);
                clientResearch.Complete(ResearchType.IncreasedStationIncome);
                client.RemoveProvider(clientMine);
                Assert.That(client.TotalIncome, Is.EqualTo(host.TotalIncome),
                    "Local provider and research changes must not overwrite the host's income.");
                client.Tick();
                Assert.That(clientMoney.Money, Is.EqualTo(500f), "A client must not pay income locally.");
                Assert.That(clientMoney.IncomeTimeLeft, Is.Zero);

                host.RemoveProvider(hostMine);
                clientState.Apply(hostState.Capture());
                Assert.That(host.TotalIncome, Is.EqualTo(3.6f).Within(0.0001f));
                Assert.That(client.TotalIncome, Is.EqualTo(host.TotalIncome),
                    "The next snapshot must reflect income lost when a host-side mine is destroyed.");
                Assert.That(incomeAtMoneyChanged, Is.EqualTo(host.TotalIncome));
            }
            finally
            {
                client.LateDispose();
                host.LateDispose();
            }
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
