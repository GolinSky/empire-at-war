using System.Linq;
using System.Reflection;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Services.Saves;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class PlayerSaveTests
    {
        private const string SHARED_DATA = "Assets/Settings/Data/Factions/Shared/";

        [Test]
        public void UnitRequest_EveryKind_RebuildsTheSameRequestFromFactionData()
        {
            FactionRoster roster = CreateRoster(FactionType.Empire);
            StationLevelData levels = AssetDatabase.FindAssets("t:StationLevelData")
                .Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<StationLevelData>).First();
            UnitRequestFactory factory = new UnitRequestFactory();
            ResearchType research = roster.ResearchLines.Keys.First();
            UnitRequest[] requests =
            {
                factory.ConstructUnitRequest(roster.Ships.First().Value, roster.Ships.First().Key),
                factory.ConstructUnitRequest(roster.Squadrons.First().Value, roster.Squadrons.First().Key),
                factory.ConstructUnitRequest(levels.GetLevelFactionData(1), 1),
                factory.ConstructUnitRequest(roster.MiningFacilities.First().Value, roster.MiningFacilities.First().Key),
                factory.ConstructUnitRequest(roster.DefendPlatforms.First().Value, roster.DefendPlatforms.First().Key),
                factory.ConstructUnitRequest(roster.ResearchLines[research].Tiers.Last().FactionData, research,
                    roster.ResearchLines[research].Tiers.Length),
                factory.ConstructUnitRequest(roster.SuperWeapons.First().Value, roster.SuperWeapons.First().Key)
            };

            foreach (UnitRequest request in requests)
            {
                UnitRequestSave save = JsonUtility.FromJson<UnitRequestSave>(
                    JsonUtility.ToJson(UnitRequestSave.From(request, 3.5f)));
                UnitRequest rebuilt = save.ToRequest(factory, roster, levels);

                Assert.That(rebuilt.GetType(), Is.EqualTo(request.GetType()));
                Assert.That(rebuilt.Id, Is.EqualTo(request.Id));
                Assert.That(rebuilt.FactionData, Is.SameAs(request.FactionData), request.Id);
                Assert.That(save.TimeLeft, Is.EqualTo(3.5f));
            }
        }

        [Test]
        public void ProductionQueue_Restore_KeepsTimeLeftAndTakesStructureAndResoluteReservations()
        {
            PlayerFactionModel faction = new PlayerFactionModel(null, null);
            FactionData single = CreateFactionData(maxCount: 1, buildTime: 30);
            MiningFacilityUnitRequest mine = new MiningFacilityUnitRequest(single, MiningFacilityType.CommonMiner);

            faction.RestoreQueue(new[]
            {
                new ProductionQueueItem(new ShipUnitRequest(single, ShipType.Resolute), 12f),
                new ProductionQueueItem(mine, 5f)
            });

            Assert.That(faction.QueuedItems.Select(item => item.RemainingBuildTime), Is.EqualTo(new[] { 12f, 5f }));
            Assert.That(faction.IsResoluteReserved, Is.True);
            Assert.That(faction.CanQueueUnit(mine), Is.False, "The queued facility holds the only structure slot.");
        }

        [Test]
        public void Reserve_ListsWaitingRequestsUntilPlaced()
        {
            ReinforcementModel reinforcements = new ReinforcementModel(null);
            SquadronUnitRequest squadron = new SquadronUnitRequest(CreateFactionData(1, 1), SquadronType.XWing);

            reinforcements.AddReinforcement(squadron);
            reinforcements.AddReinforcement(squadron);

            Assert.That(reinforcements.Reserve.Single().Key, Is.SameAs(squadron));
            Assert.That(reinforcements.Reserve.Single().Value, Is.EqualTo(2));
            reinforcements.ConsumeReinforcement(squadron);
            reinforcements.ConsumeReinforcement(squadron);
            Assert.That(reinforcements.Reserve, Is.Empty);
        }

        [Test]
        public void PlacedSquadrons_ListsOnlyLivingPlacedSquadrons()
        {
            ReinforcementModel reinforcements = new ReinforcementModel(null);
            reinforcements.UpdateSquadronData(new SquadronUnitRequest(CreateFactionData(1, 1), SquadronType.XWing));

            reinforcements.AddUnitCapacity(SquadronType.XWing, 7);
            reinforcements.AddUnitCapacity(SquadronType.XWing, 8);
            reinforcements.RemoveUnitCapacity(SquadronType.XWing, 7);

            Assert.That(reinforcements.PlacedSquadrons, Is.EqualTo(new long[] { 8 }));
        }

        [Test]
        public void SuperWeapon_RestoredReadyWeapon_FiresOnlyAfterItsCooldown()
        {
            SuperWeaponModel superWeapons = new SuperWeaponModel();

            superWeapons.Restore(SuperWeaponType.IonCannon, SuperWeaponState.Ready, 30f);

            Assert.That(superWeapons.CanFire(SuperWeaponType.IonCannon), Is.False);
            superWeapons.Tick(30f);
            Assert.That(superWeapons.CanFire(SuperWeaponType.IonCannon), Is.True);
            Assert.That(superWeapons.GetCooldownRemaining(SuperWeaponType.IonCannon), Is.Zero);
        }

        [Test]
        public void BuildQueue_CompletesEachBuildOnItsOwnTimeInOrder()
        {
            EnemyBuildQueue builds = new EnemyBuildQueue();
            FactionData data = CreateFactionData(1, 10);
            ShipUnitRequest first = new ShipUnitRequest(data, ShipType.Venator);
            DefendPlatformUnitRequest second = new DefendPlatformUnitRequest(data, DefendPlatformType.Xq6);
            builds.Add(first, 2f);
            builds.Add(second, 2f);
            builds.Add(new ShipUnitRequest(data, ShipType.Acclamator), 5f);
            System.Collections.Generic.List<UnitRequest> finished = new System.Collections.Generic.List<UnitRequest>();

            builds.Advance(2f, finished.Add);

            Assert.That(finished, Is.EqualTo(new UnitRequest[] { first, second }));
            Assert.That(builds.Builds.Single().TimeLeft, Is.EqualTo(3f));
        }

        private static FactionRoster CreateRoster(FactionType faction) => new FactionRoster(
            Load<FactionCatalog>().Get(faction), Load<MiningFacilityCatalog>(), Load<DefendPlatformCatalog>(),
            Load<SuperWeaponCatalog>());

        private static T Load<T>() where T : ScriptableObject
        {
            T data = AssetDatabase.LoadAssetAtPath<T>(SHARED_DATA + typeof(T).Name + ".asset");
            Assert.That(data, Is.Not.Null, typeof(T).Name);
            return data;
        }

        private static FactionData CreateFactionData(int maxCount, int buildTime)
        {
            FactionData data = new FactionData();
            SetBackingField(data, nameof(FactionData.MaxCount), maxCount);
            SetBackingField(data, nameof(FactionData.BuildTime), buildTime);
            return data;
        }

        private static void SetBackingField<T>(object target, string propertyName, T value) =>
            target.GetType().GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(target, value);
    }
}
