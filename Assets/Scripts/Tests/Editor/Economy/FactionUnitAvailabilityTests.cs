using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.CoreGameUi;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.Faction.Ui;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Selection;
using EmpireAtWar.Services.Stations;
using EmpireAtWar.Ui.Base;
using MPUIKIT;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Economy")]
    [Category("BattleInput")]
    public sealed class FactionUnitAvailabilityTests
    {
        [TestCase(SelectionType.Base, false)]
        [TestCase(SelectionType.Base, true)]
        [TestCase(SelectionType.MiningFacility, false)]
        [TestCase(SelectionType.None, false)]
        [TestCase(SelectionType.Ship, false)]
        [TestCase(SelectionType.DefendPlatform, false)]
        public void ProductionCards_AreRoutedToTheCorrectMenus(SelectionType selectionType, bool secondStation)
        {
            const string FACTION_ASSETS = "Assets/Settings/Data/Factions/";
            const string SHARED_ASSETS = FACTION_ASSETS + "Shared/";
            var roster = new FactionRoster(
                AssetDatabase.LoadAssetAtPath<FactionDefinition>(FACTION_ASSETS + "Republic/RepublicFaction.asset"),
                AssetDatabase.LoadAssetAtPath<MiningFacilityCatalog>(SHARED_ASSETS + "MiningFacilityCatalog.asset"),
                AssetDatabase.LoadAssetAtPath<DefendPlatformCatalog>(SHARED_ASSETS + "DefendPlatformCatalog.asset"),
                AssetDatabase.LoadAssetAtPath<SuperWeaponCatalog>(SHARED_ASSETS + "SuperWeaponCatalog.asset"));
            StationLevelData levels = AssetDatabase.LoadAssetAtPath<StationLevelData>(
                SHARED_ASSETS + "StationLevelData.asset");
            GameObject root = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Ui/UiService.prefab");
            FactionUi view = Object.Instantiate(AssetDatabase.LoadAssetAtPath<FactionUi>(
                "Assets/Prefabs/Ui/Factions/FactionUi.prefab"), root.transform);
            CoreGameUi hud = Object.Instantiate(AssetDatabase.LoadAssetAtPath<CoreGameUi>(
                "Assets/Prefabs/Ui/SkirmishGame/CoreGameUi.prefab"), root.transform);
            try
            {
                view.SetContentLayout(root.GetComponent<UiPlacement>().FactionLayout);
                view.SetProductionMenus(hud.ProductionMenus);
                var model = new PlayerFactionModel(levels, roster)
                {
                    SelectionType = selectionType,
                    IsSecondStationSelected = secondStation
                };
                view.SetModel(model);
                view.SetResearch(new FactionResearchModel(roster));
                view.AssignPresenter(new UnlockedPresenter(CreatePresenter(model)));
                view.SetData(roster);
                view.SetUnitRequestFactory(new UnitRequestFactory());
                view.SetSecondStation(new SecondStationLevelModel { IsBuilt = true }, levels);
                view.Initialize();

                Assert.That(roster.SuperWeapons, Is.Not.Empty);
                FactionUnitUi[] cards = view.GetComponentsInChildren<FactionUnitUi>(true);
                Assert.That(cards.Any(card => card.Request is ShipUnitRequest), Is.True);
                Assert.That(cards.All(card => card.Request is ShipUnitRequest or SquadronUnitRequest
                    or LevelUnitRequest or SecondStationLevelUnitRequest), Is.True);
                Assert.That(cards.Any(card => card.Request is LevelUnitRequest), Is.True);
                Assert.That(cards.Any(card => card.Request is SecondStationLevelUnitRequest), Is.True);
                Transform stationGrid = cards.First().transform.parent;
                Assert.That(cards.All(card => card.transform.parent == stationGrid), Is.True);
                Assert.That(stationGrid.GetComponent<GridLayoutGroup>().constraintCount, Is.EqualTo(2));
                if (selectionType == SelectionType.Base)
                {
                    FactionUnitUi lastCard = cards.Last(card => card.gameObject.activeSelf);
                    Assert.That(lastCard.Request.GetType(), Is.EqualTo(secondStation
                        ? typeof(SecondStationLevelUnitRequest) : typeof(LevelUnitRequest)));
                }
                var researchCards = hud.ProductionMenus.ResearchCards.GetComponentsInChildren<FactionUnitUi>(true);
                Assert.That(researchCards.Any(card => card.Request is ResearchUnitRequest
                    { Key: ResearchType.IncreasedProduction }), Is.True);
                Assert.That(researchCards.Any(card => card.Request is ResearchUnitRequest
                    { Key: ResearchType.IncreasedStationIncome }), Is.True);
                Assert.That(researchCards.All(card => card.Request is ResearchUnitRequest), Is.True);
                var structures = hud.ProductionMenus.StructureCards.GetComponentsInChildren<FactionUnitUi>(true);
                Assert.That(structures.Any(card => card.Request is MiningFacilityUnitRequest), Is.True);
                Assert.That(structures.Any(card => card.Request is DefendPlatformUnitRequest), Is.True);
                Assert.That(structures.Any(card => card.Request is SecondStationUnitRequest), Is.True);
                Assert.That(structures.All(card => card.Request is MiningFacilityUnitRequest or
                    DefendPlatformUnitRequest or SecondStationUnitRequest), Is.True);

                foreach (FactionUnitUi card in cards.Concat(researchCards).Concat(structures))
                {
                    var background = (MPImage)card.GetComponent<Button>().targetGraphic;
                    var (width, color) = card.Request switch
                    {
                        SquadronUnitRequest => (2f, new Color32(222, 223, 218, 255)),
                        ShipUnitRequest => (2f, new Color32(214, 164, 121, 255)),
                        _ => (1f, new Color32(210, 169, 94, 255))
                    };
                    Assert.That(background.OutlineWidth, Is.EqualTo(width), card.RequestId);
                    Assert.That((Color32)background.OutlineColor, Is.EqualTo(color), card.RequestId);
                }

                view.RefreshAvailability(10000f, false);
                foreach (FactionUnitUi card in cards)
                {
                    bool expected = selectionType == SelectionType.Base && (card.Request switch
                    {
                        LevelUnitRequest => !secondStation,
                        SecondStationLevelUnitRequest => secondStation,
                        _ => true
                    });
                    Assert.That(card.gameObject.activeSelf, Is.EqualTo(expected), card.Request.Id);
                }
                foreach (FactionUnitUi card in researchCards)
                {
                    bool expected = card.Request is not ResearchUnitRequest { Key: ResearchType.IncreasedProduction }
                                    || selectionType == SelectionType.MiningFacility;
                    Assert.That(card.gameObject.activeSelf, Is.EqualTo(expected), card.Request.Id);
                }
                Assert.That(structures.All(card => card.gameObject.activeSelf), Is.True);
            }
            finally
            {
                view.Dispose();
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [TestCase(false, false, false)]
        [TestCase(true, true, false)]
        [TestCase(true, false, true)]
        public void VisibilityRefresh_PreservesPurchaseRestrictions(
            bool affordable, bool battleEnded, bool atLimit)
        {
            FactionUnitUi prefab = AssetDatabase.LoadAssetAtPath<FactionUnitUi>(
                "Assets/Prefabs/Ui/Factions/FactionUnit.prefab");
            Assert.That(prefab, Is.Not.Null);
            FactionUi view = Object.Instantiate(AssetDatabase.LoadAssetAtPath<FactionUi>(
                "Assets/Prefabs/Ui/Factions/FactionUi.prefab"));
            FactionUnitUi card = Object.Instantiate(prefab, view.transform);

            try
            {
                var data = new FactionData();
                card.SetData(data, view, new ShipUnitRequest(data, ShipType.HomeOne));
                FieldInfo cardsField = typeof(FactionUi).GetField("_factionUnitsUi",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                ((List<FactionUnitUi>)cardsField.GetValue(view)).Add(card);
                typeof(FactionUi).GetField("_presenter", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(view, new UnlockedPresenter(CreatePresenter(null)));
                typeof(FactionUi).GetField("_isRouteActive", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(view, true);
                MethodInfo refresh = typeof(FactionUi).GetMethod("RefreshUnitVisibility",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                SerializedObject serializedCard = new SerializedObject(card);
                Button button = (Button)serializedCard.FindProperty("purchaseButton").objectReferenceValue;

                card.RenderAvailability(true, affordable, false, battleEnded, atLimit);
                Assert.That(button.interactable, Is.EqualTo(!battleEnded && !atLimit),
                    "An unaffordable card must still deliver a click for rejected-purchase feedback.");

                // Selection, level and research updates all use this visibility refresh.
                refresh.Invoke(view, new object[] { SelectionType.None });
                refresh.Invoke(view, new object[] { SelectionType.Base });
                Assert.That(card.gameObject.activeSelf, Is.True);
                Assert.That(button.interactable, Is.EqualTo(!battleEnded && !atLimit), "Reselection must preserve purchase restrictions.");

                card.RenderAvailability(true, affordable, false, battleEnded, atLimit);
                refresh.Invoke(view, new object[] { SelectionType.Base });
                Assert.That(button.interactable, Is.EqualTo(!battleEnded && !atLimit), "Repeated refreshes must preserve cached availability.");

                card.RenderAvailability(true, true, false, false, false);
                Assert.That(button.interactable, Is.True, "Removing the restriction must enable purchases.");
            }
            finally
            {
                Object.DestroyImmediate(view.gameObject);
            }
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        public void LockedCards_HideOnSelectionAndRefreshUntilUnlocked(bool unlocked, bool heroReserved)
        {
            FactionUnitUi prefab = AssetDatabase.LoadAssetAtPath<FactionUnitUi>(
                "Assets/Prefabs/Ui/Factions/FactionUnit.prefab");
            Assert.That(prefab, Is.Not.Null);
            FactionUi view = Object.Instantiate(AssetDatabase.LoadAssetAtPath<FactionUi>(
                "Assets/Prefabs/Ui/Factions/FactionUi.prefab"));
            FactionUnitUi card = Object.Instantiate(prefab, view.transform);
            var model = new PlayerFactionModel(null, null) { SelectionType = SelectionType.Base };
            var presenter = new AvailabilityPresenter { Unlocked = unlocked, HeroReserved = heroReserved };
            var data = new FactionData();
            card.SetData(data, view, new ShipUnitRequest(data, ShipType.HomeOne));

            try
            {
                ((List<FactionUnitUi>)typeof(FactionUi).GetField("_factionUnitsUi",
                    BindingFlags.Instance | BindingFlags.NonPublic).GetValue(view)).Add(card);
                view.SetModel(model);
                view.AssignPresenter(presenter);
                MethodInfo refresh = typeof(FactionUi).GetMethod("RefreshUnitVisibility",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                refresh.Invoke(view, new object[] { SelectionType.Base });
                Assert.That(card.gameObject.activeSelf, Is.False, "Unavailable cards must be hidden on selection.");
                card.SetActive(true);
                view.RefreshAvailability(0f, false);
                Assert.That(card.gameObject.activeSelf, Is.False, "Live updates must hide locked units.");

                presenter.Unlocked = true;
                presenter.HeroReserved = false;
                view.RefreshAvailability(0f, false);
                Assert.That(card.gameObject.activeSelf, Is.True, "Unlocked units must reappear.");

                presenter.Unlocked = unlocked;
                presenter.HeroReserved = heroReserved;
                view.RefreshAvailability(0f, false);
                Assert.That(card.gameObject.activeSelf, Is.False, "Level changes must hide locked cards again.");
                refresh.Invoke(view, new object[] { SelectionType.None });
                refresh.Invoke(view, new object[] { SelectionType.Base });
                Assert.That(card.gameObject.activeSelf, Is.False, "Reselection must keep locked cards hidden.");
            }
            finally
            {
                Object.DestroyImmediate(view.gameObject);
            }
        }

        [TestCase("Research", 1, 1)]
        [TestCase("MiningResearch", 1, 1)]
        [TestCase("Level", 1, 1)]
        [TestCase("Ship", 3, 10)]
        [TestCase("Ship", 1, 1)]
        [TestCase("Squadron", 20, 10)]
        [TestCase("Structure", 2, 2)]
        [TestCase("Hero", 3, 1)]
        public void PurchaseLimit_DisablesCardUntilCapacityIsReleased(string kind, int maxCount, int queueLimit)
        {
            var data = new FactionData();
            typeof(FactionData).GetField("<MaxCount>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(data, maxCount);
            typeof(FactionData).GetField("<BuildTime>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(data, 10);
            if (kind == "Hero")
                typeof(FactionData).GetField("isHero", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(data, true);
            UnitRequest request = kind switch
            {
                "Research" => new ResearchUnitRequest(data, ResearchType.ImprovedWeapons, 1),
                "MiningResearch" => new ResearchUnitRequest(data, ResearchType.IncreasedProduction, 1),
                "Level" => new LevelUnitRequest(data, 1),
                "Structure" => new MiningFacilityUnitRequest(data, MiningFacilityType.CommonMiner),
                "Squadron" => new SquadronUnitRequest(data, SquadronType.TIEFighter),
                _ => new ShipUnitRequest(data, ShipType.HomeOne)
            };
            var model = new PlayerFactionModel(null, null)
            {
                SelectionType = kind == "MiningResearch" ? SelectionType.MiningFacility : SelectionType.Base
            };
            FactionUiController presenter = CreatePresenter(model);
            FactionUnitUi prefab = AssetDatabase.LoadAssetAtPath<FactionUnitUi>(
                "Assets/Prefabs/Ui/Factions/FactionUnit.prefab");
            Assert.That(prefab, Is.Not.Null);
            GameObject root = new GameObject("Faction purchase limit test");
            FactionUi view = root.AddComponent<FactionUi>();
            FactionUnitUi card = Object.Instantiate(prefab, root.transform);
            Button button = (Button)new SerializedObject(card).FindProperty("purchaseButton").objectReferenceValue;

            try
            {
                card.SetData(data, view, request);
                ((List<FactionUnitUi>)typeof(FactionUi).GetField("_factionUnitsUi",
                    BindingFlags.Instance | BindingFlags.NonPublic).GetValue(view)).Add(card);
                view.SetModel(model);
                view.AssignPresenter(presenter);
                for (int count = 0; count < queueLimit; count++)
                {
                    view.RefreshAvailability(0f, false);
                    Assert.That(button.interactable, Is.True, "Cards below their own limit remain purchasable.");
                    model.QueueUnit(request);
                }

                bool hidesAtLimit = kind is not ("Ship" or "Squadron");
                Assert.That(presenter.IsUnitLimitReached(request), Is.True);
                Assert.That(model.CanQueueUnit(request), Is.False);
                view.RefreshAvailability(0f, false);
                Assert.That(card.gameObject.activeSelf, Is.EqualTo(!hidesAtLimit),
                    "Heroes, structures, upgrades and research hide at their limit.");
                Assert.That(button.interactable, Is.False, "Reaching the purchase limit must disable the card.");

                Assert.That(model.TryCancelCurrentUnit(request.Id, out _), Is.True);
                view.RefreshAvailability(0f, false);
                Assert.That(card.gameObject.activeSelf, Is.True, "Releasing a purchase must restore the card.");
                Assert.That(button.interactable, Is.True, "Releasing a purchase must re-enable the card.");

                model.QueueUnit(request);
                model.Advance(10f * queueLimit);
                view.RefreshAvailability(0f, false);
                bool hasLifetimeLimit = kind == "Structure" || kind == "Hero";
                Assert.That(card.gameObject.activeSelf, Is.EqualTo(!hasLifetimeLimit));
                Assert.That(button.interactable, Is.EqualTo(!hasLifetimeLimit),
                    "Completed structures and heroes retain their reservation; ordinary queues release capacity.");

                if (kind == "Structure") model.ReleaseStructure(request);
                if (kind == "Hero") model.SetHeroReserved(UnitTypeId.Ship(ShipType.HomeOne), false);
                view.RefreshAvailability(0f, false);
                Assert.That(card.gameObject.activeSelf, Is.True, "Releasing a hero restores its card.");
                Assert.That(button.interactable, Is.True, "Releasing a lifetime reservation re-enables the card.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void FullProductionPipelines_DoNotReportAnUnqueuedUnitsOwnLimitReached()
        {
            var model = new PlayerFactionModel(null, null);
            var data = new FactionData();
            typeof(FactionData).GetField("<MaxCount>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(data, 1);
            for (int key = 1; key <= PlayerFactionModel.MAX_ACTIVE_PIPELINES; key++)
                model.QueueUnit(new LevelUnitRequest(data, key));
            var request = new ShipUnitRequest(data, ShipType.HomeOne);

            Assert.That(model.CanQueueUnit(request), Is.False);
            Assert.That(CreatePresenter(model).IsUnitLimitReached(request), Is.False,
                "Global pipeline capacity must not hide every remaining roster card.");
        }

        private static FactionUiController CreatePresenter(PlayerFactionModel model)
        {
            return new FactionUiController(
                uiService: null, placement: null, cancelRouter: null, commands: null, owner: null,
                model: model, research: null, unitRequestFactory: null, routeNavigation: null,
                tooltipService: null, economy: null, hud: null, battleState: null, roster: null,
                reinforcements: null, assets: null, shipsData: null, interfaceAudio: null, notifications: null,
                secondStation: new SecondStationLevelModel(), stationLevels: null, productionAccess: new OwnBaseAccess(),
                productionMenusProvider: null);
        }

        private sealed class AvailabilityPresenter : IFactionPresenter
        {
            public bool Unlocked { get; set; }
            public bool HeroReserved { get; set; }
            public void TryPurchaseUnit(UnitRequest unitRequest) { }
            public bool IsUnitAvailable(FactionData data) => Unlocked;
            public bool IsUnlockedAtCurrentLevel(FactionData data) => false;
            public bool IsCarrier(UnitRequest request) => false;
            public bool IsScout(UnitRequest request) => false;
            public bool IsUnitLimitReached(UnitRequest request) => HeroReserved;
            public ShipClass GetUnitClass(UnitRequest request) => throw new System.NotSupportedException();
            public int GetUnitSortOrder(UnitRequest request) => 0;
        }

        private sealed class OwnBaseAccess : IProductionAccess
        {
            public bool CanProduce => true;
            public bool HasHostBase => true;
            public PlayerId ProductionHost => TestPlayers.Human;

            public bool TryGetProductionBase(out IEntity productionBase) =>
                throw new System.NotSupportedException();
        }

        private sealed class UnlockedPresenter : IFactionPresenter
        {
            private readonly FactionUiController _stationPresenter;

            public UnlockedPresenter(FactionUiController stationPresenter) => _stationPresenter = stationPresenter;

            public void TryPurchaseUnit(UnitRequest unitRequest) { }
            public bool IsUnitAvailable(FactionData data) => true;
            public bool IsUnlockedAtCurrentLevel(FactionData data) => false;
            public bool IsCarrier(UnitRequest request) => false;
            public bool IsScout(UnitRequest request) => false;
            public bool IsUnitLimitReached(UnitRequest request) => false;
            public ShipClass GetUnitClass(UnitRequest request) =>
                request is SquadronUnitRequest ? ShipClass.Fighter : ShipClass.Capital;
            public int GetUnitSortOrder(UnitRequest request) =>
                request is LevelUnitRequest or SecondStationLevelUnitRequest
                    ? _stationPresenter.GetUnitSortOrder(request)
                    : 0;
        }
    }
}
