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
using TMPro;
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
                var research = new FactionResearchModel(roster);
                view.SetResearch(research);
                view.AssignPresenter(new UnlockedPresenter(CreatePresenter(model))
                {
                    LevelGatedData = roster.ResearchLines[ResearchType.IncreasedProduction].Tiers[1].FactionData
                });
                view.SetData(roster);
                view.SetUnitRequestFactory(new UnitRequestFactory());
                var secondStationModel = new SecondStationLevelModel { IsBuilt = true };
                view.SetSecondStation(secondStationModel, levels);
                view.Initialize();

                Assert.That(roster.SuperWeapons, Is.Not.Empty);
                FactionUnitUi[] cards = view.GetComponentsInChildren<FactionUnitUi>(true);
                Assert.That(cards.Any(card => card.Request is ShipUnitRequest), Is.True);
                Assert.That(cards.Any(card => card.FactionData.IsHero && card.Request is ShipUnitRequest), Is.True);
                Assert.That(cards.Any(card => card.FactionData.IsHero && card.Request is SquadronUnitRequest), Is.True);
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
                Assert.That(researchCards.All(card => card.Request is ResearchUnitRequest or
                    LevelUnitRequest or SecondStationLevelUnitRequest), Is.True);
                Assert.That(researchCards.Single(card => card.Request is LevelUnitRequest).Request,
                    Is.SameAs(cards.Single(card => card.Request is LevelUnitRequest).Request));
                Assert.That(researchCards.Single(card => card.Request is SecondStationLevelUnitRequest).Request,
                    Is.SameAs(cards.Single(card => card.Request is SecondStationLevelUnitRequest).Request));
                foreach (FactionUnitUi card in researchCards)
                {
                    var tierText = (TextMeshProUGUI)new SerializedObject(card).FindProperty("tierText").objectReferenceValue;
                    Assert.That(tierText.enableAutoSizing, Is.True, card.RequestId);
                    Assert.That(tierText.fontSizeMin, Is.EqualTo(6f), card.RequestId);
                    Assert.That(tierText.fontSizeMax, Is.EqualTo(14f), card.RequestId);
                    Assert.That(tierText.textWrappingMode, Is.EqualTo(TextWrappingModes.NoWrap), card.RequestId);
                }
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
                    var fill = (card.FactionData.IsHero, card.Request) switch
                    {
                        (true, SquadronUnitRequest) => new Color32(36, 77, 78, 255),
                        (true, ShipUnitRequest) => new Color32(99, 57, 46, 255),
                        _ => new Color32(32, 46, 64, 255)
                    };
                    Assert.That((Color32)background.color, Is.EqualTo(fill), card.RequestId);

                    var serializedCard = new SerializedObject(card);
                    var dot = (MPImage)serializedCard.FindProperty("queuedHighlight").objectReferenceValue;
                    Assert.That(dot.gameObject.activeSelf, Is.False, card.RequestId);
                    Assert.That(dot.rectTransform.parent, Is.EqualTo(card.transform), card.RequestId);
                    Assert.That(dot.rectTransform.anchorMin, Is.EqualTo(Vector2.up), card.RequestId);
                    Assert.That(dot.rectTransform.anchorMax, Is.EqualTo(Vector2.up), card.RequestId);
                    Assert.That(dot.rectTransform.anchoredPosition, Is.EqualTo(new Vector2(6f, -6f)), card.RequestId);
                    Assert.That(dot.DrawShape, Is.EqualTo(DrawShape.Circle), card.RequestId);
                    Assert.That(dot.raycastTarget, Is.False, card.RequestId);
                    Assert.That((Color32)dot.color, Is.EqualTo(new Color32(239, 78, 67, 255)), card.RequestId);
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
                    Assert.That(card.gameObject.activeSelf, Is.True, card.Request.Id);
                }
                Assert.That(structures.All(card => card.gameObject.activeSelf), Is.True);

                if (selectionType == SelectionType.Base && !secondStation)
                    VerifyRosterFilters(view, cards);

                var menus = hud.ProductionMenus;
                var serializedMenus = new SerializedObject(menus);
                var popup = (RectTransform)serializedMenus.FindProperty("popup").objectReferenceValue;
                var buffRow = (RectTransform)serializedMenus.FindProperty("researchBuffs").objectReferenceValue;
                ResearchBuffIcon[] buffs = buffRow.GetComponentsInChildren<ResearchBuffIcon>(true)
                    .Where(buff => buff.gameObject.activeSelf).ToArray();
                Assert.That(buffs.Length, Is.EqualTo(research.ResearchTypes.Count));
                var productionBuff = new SerializedObject(buffs.Single(buff =>
                    ((Image)new SerializedObject(buff).FindProperty("icon").objectReferenceValue).sprite ==
                    roster.ResearchLines[ResearchType.IncreasedProduction].Tiers[0].FactionData.Icon));
                var productionTier = (TMP_Text)productionBuff.FindProperty("tier").objectReferenceValue;
                Assert.That(productionTier.text, Is.EqualTo("–"));
                float initialPopupHeight = popup.rect.height;
                var researchButton = (Button)serializedMenus.FindProperty("researchButton").objectReferenceValue;
                var structuresButton = (Button)serializedMenus.FindProperty("structuresButton").objectReferenceValue;
                GameObject researchMarker = researchButton.transform.Find("ReadyIndicator").gameObject;
                GameObject structuresMarker = structuresButton.transform.Find("ReadyIndicator").gameObject;
                Assert.That(researchMarker.activeSelf && structuresMarker.activeSelf, Is.True);

                menus.Initialize();
                menus.SetAvailable(true);
                researchButton.onClick.Invoke();
                Assert.That(buffRow.gameObject.activeSelf, Is.True);
                Assert.That(menus.ResearchCards.anchoredPosition.y,
                    Is.LessThanOrEqualTo(buffRow.anchoredPosition.y - buffRow.rect.height));
                var researchGrid = menus.ResearchCards.GetComponent<GridLayoutGroup>();
                int rows = Mathf.CeilToInt((float)researchCards.Length / researchGrid.constraintCount);
                float requiredHeight = researchGrid.padding.vertical +
                    rows * researchGrid.cellSize.y + (rows - 1) * researchGrid.spacing.y;
                Assert.That(menus.ResearchCards.rect.height, Is.GreaterThanOrEqualTo(requiredHeight - 0.01f));
                Assert.That(popup.rect.height, Is.GreaterThan(initialPopupHeight));
                Assert.That(researchMarker.activeSelf, Is.False);
                Assert.That(structuresMarker.activeSelf, Is.True);
                structuresButton.onClick.Invoke();
                Assert.That(buffRow.gameObject.activeSelf, Is.False);
                Assert.That(popup.rect.height, Is.EqualTo(initialPopupHeight).Within(0.01f));
                Assert.That(structuresMarker.activeSelf, Is.False);
                menus.Close();
                view.RefreshAvailability(10000f, false);
                Assert.That(researchMarker.activeSelf || structuresMarker.activeSelf, Is.False);

                research.Complete(ResearchType.IncreasedProduction);
                Assert.That(productionTier.text, Is.EqualTo("I"));
                Assert.That(researchMarker.activeSelf, Is.False, "The next tier is still locked.");
                model.CompleteLevelUpgrade();
                var unlockedTier = menus.ResearchCards.GetComponentsInChildren<FactionUnitUi>(true)
                    .Single(card => card.Request is ResearchUnitRequest { Key: ResearchType.IncreasedProduction });
                Assert.That(unlockedTier.gameObject.activeSelf, Is.True);
                Assert.That(researchMarker.activeSelf, Is.True);
                Assert.That(structuresMarker.activeSelf, Is.False);

                secondStationModel.CompleteLevelUpgrade();
                FactionUnitUi stationUpgrade = view.GetComponentsInChildren<FactionUnitUi>(true)
                    .Single(card => card.Request is SecondStationLevelUnitRequest);
                FactionUnitUi researchUpgrade = menus.ResearchCards.GetComponentsInChildren<FactionUnitUi>(true)
                    .Single(card => card.Request is SecondStationLevelUnitRequest);
                Assert.That(((SecondStationLevelUnitRequest)researchUpgrade.Request).Level,
                    Is.EqualTo(secondStationModel.CurrentLevel));
                Assert.That(researchUpgrade.Request, Is.SameAs(stationUpgrade.Request));

                model.QueueUnit(researchUpgrade.Request);
                view.RefreshAvailability(10000f, false);
                Assert.That(stationUpgrade.gameObject.activeSelf || researchUpgrade.gameObject.activeSelf, Is.False);

                secondStationModel.HandleLost();
                Assert.That(view.GetComponentsInChildren<FactionUnitUi>(true)
                    .Any(card => card.Request is SecondStationLevelUnitRequest), Is.False);
                Assert.That(menus.ResearchCards.GetComponentsInChildren<FactionUnitUi>(true)
                    .Any(card => card.Request is SecondStationLevelUnitRequest), Is.False);
                menus.Dispose();
            }
            finally
            {
                view.Dispose();
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void VerifyRosterFilters(FactionUi view, FactionUnitUi[] cards)
        {
            var serializedView = new SerializedObject(view);
            var all = (Button)serializedView.FindProperty("allUnitsFilter").objectReferenceValue;
            var squadrons = (Button)serializedView.FindProperty("squadronsFilter").objectReferenceValue;
            var ships = (Button)serializedView.FindProperty("shipsFilter").objectReferenceValue;
            var types = (TMP_Dropdown)serializedView.FindProperty("unitTypeFilter").objectReferenceValue;
            var grid = (RectTransform)cards.First().transform.parent;
            var viewport = (RectTransform)grid.parent;
            var header = (RectTransform)all.transform.parent;
            Assert.That(viewport.rect.height, Is.EqualTo(210f).Within(0.01f));
            Assert.That(header.rect.height + viewport.rect.height,
                Is.EqualTo(((RectTransform)view.transform).rect.height).Within(0.01f));
            Assert.That(types.gameObject.activeSelf, Is.False);

            squadrons.onClick.Invoke();
            Assert.That(types.gameObject.activeSelf, Is.True);
            Assert.That(types.options.Select(option => option.text), Does.Contain("Hero Strikecraft"));
            Assert.That(cards.Any(card => card.gameObject.activeSelf && card.Request is SquadronUnitRequest), Is.True);
            Assert.That(cards.Any(card => card.gameObject.activeSelf && card.Request is ShipUnitRequest), Is.False);
            types.value = types.options.FindIndex(option => option.text == "Hero Strikecraft");
            view.RefreshAvailability(10000f, false);
            var shownSquadrons = cards.Where(card => card.gameObject.activeSelf && card.Request is SquadronUnitRequest);
            Assert.That(shownSquadrons, Is.Not.Empty);
            Assert.That(shownSquadrons.All(card => card.UnitClass == ShipClass.HeroStrikecraft), Is.True);
            Assert.That(cards.Any(card => card.gameObject.activeSelf && card.Request is LevelUnitRequest), Is.True);

            ships.onClick.Invoke();
            Assert.That(types.options[0].text, Is.EqualTo("All ship types"));
            Assert.That(types.value, Is.Zero);
            Assert.That(cards.Any(card => card.gameObject.activeSelf && card.Request is ShipUnitRequest), Is.True);
            Assert.That(cards.Any(card => card.gameObject.activeSelf && card.Request is SquadronUnitRequest), Is.False);

            all.onClick.Invoke();
            Assert.That(types.gameObject.activeSelf, Is.False);
            Assert.That(cards.Any(card => card.gameObject.activeSelf && card.Request is ShipUnitRequest), Is.True);
            Assert.That(cards.Any(card => card.gameObject.activeSelf && card.Request is SquadronUnitRequest), Is.True);
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
        [TestCase("Level", 1, 1)]
        [TestCase("Ship", 3, 10)]
        [TestCase("Ship", 1, 1)]
        [TestCase("Squadron", 20, 10)]
        [TestCase("Structure", 2, 2)]
        [TestCase("Defense", 1, 1)]
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
                "Level" => new LevelUnitRequest(data, 1),
                "Structure" => new MiningFacilityUnitRequest(data, MiningFacilityType.CommonMiner),
                "Defense" => new DefendPlatformUnitRequest(data, DefendPlatformType.Xq6),
                "Squadron" => new SquadronUnitRequest(data, SquadronType.TIEFighter),
                _ => new ShipUnitRequest(data, ShipType.HomeOne)
            };
            var model = new PlayerFactionModel(null, null) { SelectionType = SelectionType.Base };
            FactionUiController presenter = CreatePresenter(model);
            FactionUnitUi prefab = AssetDatabase.LoadAssetAtPath<FactionUnitUi>(
                "Assets/Prefabs/Ui/Factions/FactionUnit.prefab");
            Assert.That(prefab, Is.Not.Null);
            GameObject root = new GameObject("Faction purchase limit test");
            FactionUi view = root.AddComponent<FactionUi>();
            FactionUnitUi card = Object.Instantiate(prefab, root.transform);
            var serializedCard = new SerializedObject(card);
            var button = (Button)serializedCard.FindProperty("purchaseButton").objectReferenceValue;
            var marker = ((Graphic)serializedCard.FindProperty("queuedHighlight").objectReferenceValue).gameObject;

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

                Assert.That(presenter.IsUnitLimitReached(request), Is.True);
                Assert.That(model.CanQueueUnit(request), Is.False);
                view.RefreshAvailability(0f, false);
                bool hidesAtLimit = kind is not ("Ship" or "Squadron");
                Assert.That(card.gameObject.activeSelf, Is.EqualTo(!hidesAtLimit),
                    "Research, upgrades, heroes and structures hide at their limit.");
                Assert.That(marker.activeInHierarchy, Is.EqualTo(!hidesAtLimit),
                    "The queue marker appears on cards that remain visible.");
                Assert.That(button.interactable, Is.False, "Reaching the purchase limit must disable the card.");

                Assert.That(model.TryCancelCurrentUnit(request.Id, out _), Is.True);
                view.RefreshAvailability(0f, false);
                Assert.That(card.gameObject.activeSelf, Is.True, "Releasing a purchase must restore the card.");
                Assert.That(button.interactable, Is.True, "Releasing a purchase must re-enable the card.");
                Assert.That(marker.activeSelf, Is.EqualTo(queueLimit > 1),
                    "The marker stays visible while any copies remain queued.");

                model.QueueUnit(request);
                model.Advance(10f * queueLimit);
                view.RefreshAvailability(0f, false);
                bool hasLifetimeLimit = kind is "Structure" or "Defense" or "Hero";
                Assert.That(card.gameObject.activeSelf, Is.EqualTo(!hasLifetimeLimit));
                Assert.That(button.interactable, Is.EqualTo(!hasLifetimeLimit),
                    "Completed structures and heroes retain their reservation; ordinary queues release capacity.");
                Assert.That(marker.activeSelf, Is.False, "Completion clears the marker when the queue is empty.");

                if (kind is "Structure" or "Defense") model.ReleaseStructure(request);
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
            public bool TryPurchaseUnit(UnitRequest unitRequest) => true;
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

            public FactionData LevelGatedData { get; set; }

            public bool TryPurchaseUnit(UnitRequest unitRequest) => true;
            public bool IsUnitAvailable(FactionData data) =>
                data != LevelGatedData || _stationPresenter.IsUnitAvailable(data);
            public bool IsUnlockedAtCurrentLevel(FactionData data) => false;
            public bool IsCarrier(UnitRequest request) => false;
            public bool IsScout(UnitRequest request) => false;
            public bool IsUnitLimitReached(UnitRequest request) =>
                request is LevelUnitRequest or SecondStationLevelUnitRequest &&
                _stationPresenter.IsUnitLimitReached(request);
            public ShipClass GetUnitClass(UnitRequest request) => request switch
            {
                SquadronUnitRequest when request.FactionData.IsHero => ShipClass.HeroStrikecraft,
                SquadronUnitRequest => ShipClass.Fighter,
                _ => ShipClass.Capital
            };
            public int GetUnitSortOrder(UnitRequest request) =>
                request is LevelUnitRequest or SecondStationLevelUnitRequest
                    ? _stationPresenter.GetUnitSortOrder(request)
                    : 0;
        }
    }
}
